//#define CHATTY_READER // define if you want to receive debug output in the file CHATTY_OUTPUT_FILE
//#define NATIVE // define to call native functions instead of the provided .NET wrapper

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using RFID_UHF_Net;
using System.Diagnostics;

using INT8U = System.Byte;
using INT16U = System.UInt16;
using INT32U = System.UInt32;
using BOOL32 = System.Int32;
using HANDLE32 = System.UInt32;
using System.Reflection;
using System.Runtime.InteropServices;
using InterrogatorLib;

namespace RFIDApplied.InterrogatorLib
{
    public class M3ReaderGunLowLevel : IReaderLowLevel
    {
        RFIDUHF UHFNet;

        public const float m_fCutOffValue = 3.4f;

        public bool m_bSoundPlay = true;
        public bool m_bOpen = false;
        public bool m_bStartInventory = false;
        public bool m_bTriggerUp = true;

        private const int SERIAL_EPC = 0;
        private const int SERIAL_CNT = 1;

        const string CHATTY_OUTPUT_FILE = "m3reader.gun.txt";

        public M3ReaderGunLowLevel()
        {
            UHFNet = new RFIDUHF();
#if CHATTY_READER
            Trace.Listeners.Add(new TextWriterTraceListener(CHATTY_OUTPUT_FILE));
#endif
        }

        public void Dispose()
        {
            if (UHFNet != null)
            {
//                if (m_bStartInventory)
//                    Inventory(false);
                if (m_bOpen)
                    OpenRFID(false);

//                UHFNet.MessageClass.InventoryFunc -= new ReceivedInventory(OnReceivedInventory);
                UHFNet.MessageClass.PowerFunc -= new ReceivedPower(OnReceivedPower);
                UHFNet.MessageClass.AccessFunc -= new ReceivedMemoryData(ReadCallback);
                UHFNet = null;
            }
        }

        public void OnReceivedPower(bool a_bPowerOn)
        {
            if (a_bPowerOn == true)
            {
                if (m_bOpen == false)
                {
                    if (OpenRFID(true))
                    {
                        m_bOpen = true;
                    }
                }
            }
            else
            {
                if (m_bOpen == true)
                {
                    OpenRFID(false);
                    m_bOpen = false;
                }

            }
        }

        bool IsEnoughBattery()
        {
            RFIDUHF.RFID_STATUS status = new RFIDUHF.RFID_STATUS();

            INT32U nADC = 0;

            float fVolt = 0;

            // get power first
            status = UHFNet.GetPower(ref nADC);
            if (status != RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
                return false;

            status = UHFNet.ReadBattery(ref nADC, ref fVolt);

            if (fVolt < m_fCutOffValue & status == RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        string strTagData = null;
        DateTime strTagDataRecvd = DateTime.MinValue;
        private void OnReceivedInventory()
        {
            if (m_bStartInventory == false)
            {
#if CHATTY_READER
                Trace.WriteLine("OnReceivedInventory: inventory not started! Ignoring");
#endif
                return;
            }

            int nTaglenth = 0;
            StringBuilder strData = new StringBuilder(260);

            nTaglenth = UHFNet.GetData(strData);
            strTagData = strData.ToString().Substring(0, nTaglenth);
            strTagDataRecvd = DateTime.Now;

#if CHATTY_READER
            Trace.WriteLine(String.Format("tag: {0} at {1}", strTagData, strTagDataRecvd));
#endif

            // non-continous inventory: stop after the first tag!
            bool nonContinous = true;
            if (nonContinous)
            {
                Inventory(false);
                m_bTriggerUp = true;
            }
        }

        void Inventory(bool bStart)
        {
            if (!m_bOpen)
            {
#if CHATTY_READER
                Trace.WriteLine("not open, cannot start/stop inventory!");
#endif
                return;
            }

            if (bStart)
            {
                if (!IsEnoughBattery())
                {
#if CHATTY_READER
                    Trace.WriteLine("Inventory start: reader needs more battery power.");
#endif

                    if (m_bOpen == true)
                    {
                        OpenRFID(false);
                        m_bOpen = false;
                    }
                }
#if CHATTY_READER
                Trace.WriteLine("Starting inventory");
#endif
                UHFNet.Inventory();
                m_bStartInventory = true;
            }
            else
            {
#if CHATTY_READER
                Trace.WriteLine("Stopping inventory");
#endif
                UHFNet.InventoryStop();
                m_bStartInventory = false;
            }
        }

        public bool OpenRFID(bool bOpen)
        {
            if (bOpen)
            {
                RFIDUHF.RFID_STATUS status = new RFIDUHF.RFID_STATUS();

                status = UHFNet.Init();

                if (status != RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
                {
                    // NOTE: NO_SUCH_RADIO might be because of the wrong
                    // COM port designated in the RFIDcomm.cfg file!
#if CHATTY_READER
                    strstatus = String.Format("RFID Init Error-{0:G}", status);
                    Trace.WriteLine(strstatus);
#endif
                    return false;
                }
            }
            else
            {
                RFIDUHF.RFID_STATUS status = new RFIDUHF.RFID_STATUS();

                status = UHFNet.Close();
                if (status != RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
                {
#if CHATTY_READER
                    strstatus = String.Format("{0:G}", status);
                    Trace.WriteLine(strstatus);
#endif
                    return false;
                }
            }
            return true;
        }

        public IntPtr ReceivedPower(IntPtr wParam, IntPtr lParam)
        {
            IntPtr bPowerOn = wParam;

#if CHATTY_READER
            Trace.WriteLine("Power: " + bPowerOn != null ? "received" : "lost");
#endif

            if (bPowerOn != null)
            {
                if (m_bOpen == false)
                {
                    if (OpenRFID(true))
                    {
                        m_bOpen = true;
                    }
                }
            }
            else
            {
#pragma warning disable
                if (m_bOpen == true)
                {
                    OpenRFID(false);
                    m_bOpen = false;
                }
#pragma warning restore
            }
            return IntPtr.Zero;
        }

        public int ChunkSize
        {
            // read/write up to 3 words
            get { return 12; }
        }

        public void Initialize() {
            if (m_bOpen)
                return;

            if (!OpenRFID(true))
            {
#if CHATTY_READER
                Trace.WriteLine("Rfid Init Error");
#endif
            }

            //UHFNet.MessageClass.InventoryFunc += new ReceivedInventory(OnReceivedInventory);
            UHFNet.MessageClass.PowerFunc += new ReceivedPower(OnReceivedPower);
            UHFNet.MessageClass.AccessFunc += new ReceivedMemoryData(ReadCallback);

            //m_bOpen = true;
        }
        public bool IsReady() { return true; }

        // try to singulate a tag
        public object SingulateTag()
        {
#if CHATTY_READER
            Trace.WriteLine("singulating tag...");
#endif
            if (!m_bOpen)
            {
#if CHATTY_READER
                Trace.WriteLine("NOT open! trying to open...");
#endif
                m_bOpen = OpenRFID(true);
                if (!m_bOpen)
                {
#if CHATTY_READER
                    Trace.WriteLine("failed to open!");
#endif
                    return null;
                }
            }
            //            if (!m_bStartInventory)
            //                Inventory(true);
#if CHATTY_READER
            Trace.WriteLine("open! inventory started!");
#endif

            // HACKHACKHACK: set power such that
            // we are unlikely to read more than 1 tag...
            // NOTE: this is specific to the use-case of
            // reading/over-writing EPC codes
            {
                // power level is in dBm!
                var powerlevel = (INT32U)100; // 300 is the default

                var status = UHFNet.SetPower(powerlevel);
                if (status != RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
                {
#if CHATTY_READER
                    Trace.WriteLine(string.Format("SetPower({0}) failure: {1}", powerlevel, status));
#endif
                }
            }

            /*

            // check to see if we singulated a tag recently
            if (String.IsNullOrEmpty(strTagData))
            {
#if CHATTY_READER
                Trace.WriteLine("tag data empty");
#endif
                return null;
            }

            var now = DateTime.Now;
            var diff = now - strTagDataRecvd;
#if CHATTY_READER
            Trace.WriteLine(String.Format("time difference: {0}", diff));
#endif
            if (diff.TotalSeconds <= 1.0)
                return strTagData;
            else
                return null;
             */
            return "";
        }

        bool GetBank(int membank, out RFID_UHF_Net.RFIDUHF.RFIDTagBank bank)
        {
            bank = RFIDUHF.RFIDTagBank.TAG_RESERVED;
            switch (membank)
            {
                case 0x00:
                    bank = RFIDUHF.RFIDTagBank.TAG_RESERVED;
                    return true;
                case 0x01:
                    bank = RFIDUHF.RFIDTagBank.TAG_EPC;
                    return true;
                case 0x02:
                    bank = RFIDUHF.RFIDTagBank.TAG_TID;
                    return true;
                case 0x03:
                    bank = RFIDUHF.RFIDTagBank.TAG_USER;
                    return true;
                default:
                    return false;
            }
        }

#if NATIVE
        // NOTE: the authors instead force me to use a StringBuilder, so lame!
        [DllImport("RFID_UHF.dll")]
        private static extern int UHF_GetData(byte[] data);
#endif

        // only pass objects given by [SingulateTag]
        public bool ReadBytes(object tag, int membank, int offset, int count, out byte[] arr)
        {
            var cmd = new RFIDUHF.RFIDReadCmd();
            RFID_UHF_Net.RFIDUHF.RFIDTagBank bank;

            // TODO: figure this out?
            if (count % 2 != 0)
            {
                arr = null;
                return false; // can't write uneven number of bytes!
            }

#if CHATTY_READER
            Trace.Write(String.Format("Reading bytes from membank {0}, offset {1}, count {2}", membank, offset, count));
#endif
            if (!GetBank(membank, out bank))
            {
                arr = null;
                return false;
            }
            cmd.bank = bank;
            cmd.offset = (byte)(offset / 2); // bytes to words
            cmd.wlength = (byte)(count / 2); // bytes to words

            // is it synchronous or not?
#if CHATTY_READER
            Trace.WriteLine("Invoking UHFNet.Read...");
#endif
            // NOTE: the callback might be called, or it might NOT be called,
            // if there is no tag to singulate!
            var status = UHFNet.Read(ref cmd);
            switch (status)
            {
                case RFID_UHF_Net.RFIDUHF.RFID_STATUS.RFID_STATUS_OK:
#if CHATTY_READER
                    Trace.WriteLine("read finished with status OK");
#endif
                    if (_readArray == null || _readArray.Length < count)
                    {
#if CHATTY_READER
                        Trace.WriteLine("failed to obtain results");
#endif
                        arr = null;
                        return false;
                    }
                    else
                    {
#if CHATTY_READER
                        Trace.WriteLine(string.Format("read error code: {0}", _readError));
#endif
                        if (_readError != RFIDUHF.RFIDErrorcode.MAC_NOERROR && _readError != RFIDUHF.RFIDErrorcode.TAG_SUCCESS)
                        {
#if CHATTY_READER
                            Trace.WriteLine(string.Format("failed due to read error {0}", _readError));
#endif
                            arr = null;
                            return false;
                        }
#if CHATTY_READER
                        Trace.WriteLine("success");
#endif
                        arr = _readArray;
                        _readArray = null; // it was used up
                        return true;
                    }
                default:
#if CHATTY_READER
                    Trace.WriteLine(String.Format("read result: {0}", status));
#endif
                    arr = null;
                    return false;
            }
        }
        RFID_UHF_Net.RFIDUHF.RFIDErrorcode _readError;
        byte[] _readArray;
        public void ReadCallback()
        {
            int nLength = 0;
#if NATIVE
#else
            StringBuilder strData = new StringBuilder(260);
#endif

#if CHATTY_READER
            Trace.WriteLine("UHFNet.Read callback invoked");
#endif

            byte[] array = new byte[260];
#if NATIVE
            nLength = UHF_GetData(array);
#else
            nLength = UHFNet.GetData(strData);
#endif

            if (nLength == 0)
            {
#if CHATTY_READER
                Trace.WriteLine("Read result: nLength = 0 (probably a write?)");
                Trace.WriteLine("Error code " + UHFNet.GetError().ToString());
#endif
                _readError = UHFNet.GetError();
                _readArray = null;
            }
            else
            {
                _readError = UHFNet.GetError();
#if NATIVE
#else
                var str = strData.ToString();
#endif

                // is it not a hex string? wow!
#if CHATTY_READER
#if NATIVE
                Trace.WriteLine(string.Format("Read result: {0}, nLength {1}", BitConverter.ToString(array), nLength));
#else
                Trace.WriteLine(string.Format("Read result: {0}, nLength {1}", str, nLength));
#endif
#endif
#if NATIVE
                _readArray = array;
#else
                _readArray = Utility.HexStringToByteArray(str);
#endif
            }
        }

        // copy [count] bytes of [arr] into membank at [offset]
        public bool WriteBytes(object tag, int membank, int offset, int count, byte[] arr)
        {
            RFIDUHF.RFIDWriteCmd WriteCmd = new RFIDUHF.RFIDWriteCmd();

#if CHATTY_READER
            Trace.WriteLine(String.Format("Writing: membank {0}, offset {1}, count {2}", membank, offset, count));
#endif

            if (count > arr.Length)
                return false; // overflow
            // TODO: figure out how to write unaligned bytes? e.g. offset 3, count 3 -- pad this or something?
            if (count % 2 != 0)
                return false; // cannot write this array

            RFID_UHF_Net.RFIDUHF.RFIDTagBank bank;
            if (!GetBank(membank, out bank))
            {
                return false;
            }

            // NOTE: because RFIDWriteCmd.wdata corresponds to
            // a flat array of 32 elements of type unsigned short
            // that is embedded in a struct in C, we have to allocate
            // an array of same size, otherwise the runtime will
            // throw a NotSupportedException
            ushort[] sdata = new ushort[32];
            // endianness troubles?
            //Buffer.BlockCopy(arr, 0, sdata, offset / 2, count);
            {
                // the good ol' C style...
                var bas = 0; // no matter what, it is 0!
                var n = count / 2;
                for (var i = 0; i < n; i++)
                {
                    var s = (ushort)((arr[i*2 + 0] << 8) | (arr[i*2 + 1] << 0));
                    sdata[bas + i] = s;
                }
            }

#if CHATTY_READER
            Trace.WriteLine("Input bytes:");
            foreach (var b in arr)
            {
                Trace.Write(string.Format("{0:X} ", b));
            }
            Trace.WriteLine("");
            Trace.WriteLine("Copy result:");
            foreach (var w in sdata)
            {
                Trace.Write(string.Format("{0:X} ", w));
            }
            Trace.WriteLine("");
#endif

            //WriteCmd.accpwd = Convert.ToUInt32(textBox_RWPwd.Text, 16);
            WriteCmd.wlength = (byte)(count / 2); // byte to word conversion
            WriteCmd.offset = (byte)(offset / 2); // byte to word conversion
            WriteCmd.bank = bank;
            WriteCmd.wdata = sdata;

            // NOTE: ReadCallback is invoked
            var status = UHFNet.Write(ref WriteCmd);
            if (status != RFIDUHF.RFID_STATUS.RFID_STATUS_OK)
                return false;
            // NOTE: we can only be sure that the write went through
            // if we get back _readError (via the ReadCallback)
            return (_readError == RFIDUHF.RFIDErrorcode.TAG_SUCCESS);
        }
    }
}
