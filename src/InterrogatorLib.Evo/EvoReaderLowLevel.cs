#define CHATTY_READER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSrfeReaderInterface;
using CSrfeReaderInterface.protocol;
using System.Net;
using System.Threading;
using CSrfeReaderInterface.rfe.protocol;
using System.IO.Ports;
using System.Diagnostics;

using System.ComponentModel;
using System.Management; // for ChoosePort

namespace InterrogatorLib.Evo
{
    public class ConsoleTrace : CSrfeReaderInterface.trace.ITraceInterface
    {
        public override void write(string text)
        {
#if CHATTY_READER
            Trace.WriteLine(text);
#else
            System.Console.WriteLine(text);
#endif
        }
    }

    public class SerialDevice : CSrfeReaderInterface.device.IProtocolDeviceInterface
    {
        private SafeSerialPort m_port;
        private SerialDataReceivedEventHandler handler;

        public SerialDevice(string portName)
        {
            // Create a new SerialPort object with default settings.
            m_port = new SafeSerialPort(portName, 115200, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
            m_port.Handshake = Handshake.None;

            //            // Set the read/write timeouts
            //            m_port.ReadTimeout = 1; // TODO: test -> default -1
            //            m_port.WriteTimeout = 1; // TODO: test -> default -1
            this.handler = new SerialDataReceivedEventHandler(DataReceived);
            m_port.DataReceived += this.handler;
        }

        public override bool Open()
        {
            m_port.Open();
            return m_port.IsOpen;
        }

        public override bool Close()
        {
            m_port.DataReceived -= this.handler;
            m_port.Close();
            return !m_port.IsOpen;
        }

        public override bool Send(byte[] data)
        {
            try
            {
                m_port.Write(data, 0, data.Length);
            }
            catch (TimeoutException)
            {
                return false;
            }
            return true;
        }

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buffer = new byte[m_port.BytesToRead];
            m_port.Read(buffer, 0, buffer.Length);
            RaiseDataReadEvent(buffer);
        }
    }

	#region COM port chooser
	// from http://dariosantarelli.wordpress.com/2010/10/18/c-how-to-programmatically-find-a-com-port-by-friendly-name/
	internal class ProcessConnection
	{
		public static ConnectionOptions ProcessConnectionOptions()
		{
			ConnectionOptions options = new ConnectionOptions();

			options.Impersonation = ImpersonationLevel.Impersonate;
			options.Authentication = AuthenticationLevel.Default;
			options.EnablePrivileges = true;
			return options;
		}

		public static ManagementScope ConnectionScope(string machineName, ConnectionOptions options, string path)
		{
			ManagementScope connectScope = new ManagementScope();

			connectScope.Path = new ManagementPath(@"\\" + machineName + path);
			connectScope.Options = options;
			connectScope.Connect();

			return connectScope;
		}
	}

	class COMPortInfo
	{
		public string Name { get; set; }
		public string Description { get; set; }

		public COMPortInfo() { }

		public static List<COMPortInfo> GetCOMPortsInfo()
		{
			List<COMPortInfo> comPortInfoList = new List<COMPortInfo>();

			ConnectionOptions options = ProcessConnection.ProcessConnectionOptions();
			ManagementScope connectionScope = ProcessConnection.ConnectionScope(Environment.MachineName, options, @"\root\CIMV2");

			ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM Win32_PnPEntity WHERE ConfigManagerErrorCode = 0");
			ManagementObjectSearcher comPortSearcher = new ManagementObjectSearcher(connectionScope, objectQuery);
			using (comPortSearcher)
			{
				string caption = null;

				foreach (ManagementObject obj in comPortSearcher.Get())
				{
					if (obj != null)
					{
						object captionObj = obj["Caption"];

						if (captionObj != null)
						{
							caption = captionObj.ToString();

							if (caption.Contains("(COM"))
							{
								COMPortInfo comPortInfo = new COMPortInfo();

								comPortInfo.Name = caption.Substring(caption.LastIndexOf("(COM")).Replace("(", string.Empty).Replace(")",
																	 string.Empty);

								comPortInfo.Description = caption;
								comPortInfoList.Add(comPortInfo);
							}
						}
					}
				}
			}

			return comPortInfoList;
		}
	}

	public class PortChooser
	{
		public static string ChoosePort(string description)
		{
			var portinfo = COMPortInfo.GetCOMPortsInfo();
			foreach (COMPortInfo comPort in portinfo)
			{
				if (comPort.Description.Contains(description))
					return comPort.Name;
			}
			throw new Exception(String.Format("no serial port matches description {0}", description));
		}
	}
	#endregion

    public class EvoReaderLowLevel : IReaderLowLevel
    {
        CSrfeProtocolHandler hndl = null;
        SerialDevice dev = null;

        private bool tryPortNamed(string portname) {
            dev = new SerialDevice(portname);
            if (!dev.Open())
            {
                return false;
            }
            Global.m_tracer = new ConsoleTrace();
            Global.m_tracer.TraceLevel = 0;
            var ph = new CSrfeProtocolHandler(dev);
            if (!test_ReaderInfo(ph))
            {
                return false;
            }
            hndl = ph;
            return true;
        }

        public EvoReaderLowLevel()
        {
            _readerReady = false;
            _noPassword = new byte[] { 0, 0, 0, 0 };
        }

        byte[] _noPassword;
        bool _readerReady;

        public void Dispose()
        {
            if (_readerReady)
            {
                if (hndl != null)
                {
                    // TODO: stop cyclic inventory if enabled, prior to closing it down
                    hndl.Dispose();
                    hndl = null;
                }
                _readerReady = false;
            }
        }

        public bool IsReady()
        {
            return _readerReady;
        }

        public void Initialize()
        {
            if (_readerReady)
                return;
            try
            {
                var portname = PortChooser.ChoosePort("iDTRONIC - RFID Reader");
                if (tryPortNamed(portname))
                {
                    _readerReady = true;
                    // nop (don't save)
                }
            }
            catch
            {
                Console.WriteLine("Считывающее устройство не найдено, пожалуйста, подключите его.");
                _readerReady = false;
            }
        }

        public int ChunkSize
        {
            // NOTE: the reader uses EPC for addressing tags,
            // which also means that overwriting the EPC in chunks
            // will render the tag inaccessible, giving rise
            // to the phenomenon of writing tags in precisely
            // 3 attempts!
            get { return -1; } // 4?
        }

        public object SingulateTag()
        {
            if (!_readerReady)
            {
                Initialize();
                return null;
            }

            List<byte[]> epcs;
            if (!hndl.doSingleInventory(out epcs))
                return null;
            if (epcs.Count > 0)
                return epcs[0];
            else
                return null;
        }

        private static bool test_ReaderInfo(CSrfeProtocolHandler ph)
        {
            bool ok = false;

            // get reader id
            uint readerId = 0;
            ok = ph.getReaderID(out readerId);
            if (!ok)
                Console.WriteLine("ERROR: Could not get ReaderID");

            // get reader type
            uint readerType = 0;
            ok = ph.getReaderType(out readerType);
            if (!ok)
                Console.WriteLine("ERROR: Could not get ReaderType");

            // get hardware revision
            uint hwRev = 0;
            ok = ph.getHardwareRevision(out hwRev);
            if (!ok)
                Console.WriteLine("ERROR: Could not get HardwareRevision");

            // get software revision
            uint swRev = 0;
            ok = ph.getSoftwareRevision(out swRev);
            if (!ok)
                Console.WriteLine("ERROR: Could not get SoftwareRevision");

            // get bootloader revision
            uint blRev = 0;
            ok = ph.getBootloaderRevision(out blRev);
            if (!ok)
                Console.WriteLine("ERROR: Could not get BootloaderRevision");

            // get current system
            Constants.eRFE_CURRENT_SYSTEM system = 0;
            ok = ph.getCurrentSystem(out system);
            if (!ok)
                Console.WriteLine("ERROR: Could not get CurrentSystem");

            // get current state
            Constants.eRFE_CURRENT_READER_STATE state = 0;
            ok = ph.getCurrentState(out state);
            if (!ok)
                Console.WriteLine("ERROR: Could not get CurrentState");

            // get status register
            ulong statusReg = 0;
            ok = ph.getStatusRegister(out statusReg);
            if (!ok)
                Console.WriteLine("ERROR: Could not get StatusRegister");

            // print out results
            Console.WriteLine("Reader Information:");
            Console.WriteLine("\t -> ReaderID       = 0x{0:X08}", readerId);
            Console.WriteLine("\t -> ReaderType     = 0x{0:X08}", readerType);
            Console.WriteLine("\t -> HardwareRev    = 0x{0:X08}", hwRev);
            Console.WriteLine("\t -> SoftwareRev    = 0x{0:X08}", swRev);
            Console.WriteLine("\t -> BootloaderRev  = 0x{0:X08}", blRev);
            Console.WriteLine("\t -> Current System = {0}", system.ToString());
            Console.WriteLine("\t -> Current State  = {0}", state.ToString());
            Console.WriteLine("\t -> StatusRegister = 0x{0:X016}", statusReg);
            return ok;
        }

        byte[] _crcbytes = new byte[] { 0xC4, 0x1E, 0x34, 0x00 };

        // only pass objects given by [SingulateTag]
        public bool ReadBytes(object tag, int membank, int offset, int count, out byte[] arr)
        {
#if CHATTY_READER
            Trace.WriteLine(string.Format("ReadBytes: reading membank {0}, offset {1}, count {2}", membank, offset, count));
#endif
            var epc = (byte[])tag;

            // NOTE: address is in WORDS, but count is in BYTES!
            if (!hndl.readFromTag(epc, (byte)membank, (ushort)(offset/2), _noPassword, (byte)count, out arr)) {
#if CHATTY_READER
                Trace.WriteLine(string.Format("WriteBytes: failed! Message: {0}", hndl.LastReturnCode));
#endif
                arr = null;
                return false;
            }

#if CHATTY_READER
            Trace.WriteLine(string.Format("read result OK, buffer: {0}", BitConverter.ToString(arr)));
#endif

            return true;
        }

        public bool WriteBytes(object tag, int membank, int offset, int count, byte[] arr)
        {
            var epc = (byte[])tag;

            // NOTE: address is in words!
            if (!hndl.writeToTag(epc, (byte)membank, (ushort)(offset/2), _noPassword, arr))
            {
#if CHATTY_READER
                Trace.WriteLine(string.Format("WriteBytes: failed! Message: {0}", hndl.LastReturnCode));
#endif
                return false;
            }

#if CHATTY_READER
            Trace.WriteLine(string.Format("WriteBytes: writing membank {0}, offset {1}, count {2}, arr {3}", membank, offset, count, BitConverter.ToString(arr)));
#endif
            return true;
        }
    }
}
