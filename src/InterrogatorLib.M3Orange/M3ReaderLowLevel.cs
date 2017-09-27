//#define CHATTY_READER // define if you want to receive debug output in the file CHATTY_OUTPUT_FILE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CAEN = com.caen.RFIDLibrary;
using InterrogatorLib;

namespace RFIDApplied.InterrogatorLib
{

	#pragma warning disable CS0168 // exception not used
	public class M3ReaderLowLevel : IReaderLowLevel
    {
        private CAEN.CAENRFIDReader _reader = null;
        private CAEN.CAENRFIDLogicalSource _source;
        private bool _readerReady = false;

        const string CHATTY_OUTPUT_FILE = "m3reader.txt";

        public M3ReaderLowLevel() {
            _reader = new CAEN.CAENRFIDReader();
#if CHATTY_READER
            Trace.Listeners.Add(new TextWriterTraceListener(CHATTY_OUTPUT_FILE));
#endif
        }
        public void Initialize()
        {
            try
            {
                if (!_readerReady)
                {
                    _reader.Connect(CAEN.CAENRFIDPort.CAENRFID_RS232, "MOC1");
                    System.Threading.Thread.Sleep(500);
                    _source = _reader.GetSources()[0];
                    _readerReady = true;
                }
            }
            catch (CAEN.CAENRFIDException ex)
            {
                // where to output it?
                // NOTE: the actual source of the issue
                // is hidden from us
                _readerReady = false;
				// prevent CS0168
				ex = null;
            }
            //var chan = _reader.GetRFChannel(); // 2 by default
            //var reg = _reader.GetRFRegulation(); // KOREA by default!
            //_reader.SetRFRegulation(com.caen.RFIDLibrary.CAENRFIDRFRegulations.ETSI_300220);
        }
        public bool IsReady()
        {
            return _readerReady;
        }

        public int ChunkSize
        {
            get { return 4; } // 2?
        }

        public void Dispose()
        {
            if (_readerReady)
            {
                _reader.Disconnect();
                _source = null;
                _reader.Dispose();
                _reader = null;
            }
        }

        public object SingulateTag()
        {
            CAEN.CAENRFIDTag tag = null;
            try
            {
                CAEN.CAENRFIDTag[] tags;
                tags = _source.InventoryTag();

                if (tags != null)
                {
                    tag = tags[0];
                    return tag;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        byte[] _crcbytes = new byte[] { 0xC4, 0x1E, 0x34, 0x00 };

        // only pass objects given by [SingulateTag]
        public bool ReadBytes(object tag, int membank, int offset, int count, out byte[] arr)
        {
#if CHATTY_READER
            Trace.WriteLine(string.Format("ReadBytes: reading membank {0}, offset {1}, count {2}", membank, offset, count));
#endif
            try
            {
                arr = null;

                byte[] src = null;
                switch (membank)
                {
                    case 0x0:
                        return false; // not implemented
                    case 0x1: // EPC
                        // this gives us just the EPC minus the first 4 bytes,
                        // so... fake it!
                        // TODO: refactoring opportunity?
                        // - we don't actually need the full I/O intertwined with the parsing, it seems
                        // - yes, the parsing route should JUST be given a byte array, that is all
                        //   - and, the length of the byte array should imply that it's SGTIN-96, along with the magic header value
                        src = ((CAEN.CAENRFIDTag)tag).GetId();
                        offset = offset - 4;
                        if (offset < 0)
                        {
                            src = _crcbytes;
                            // FIXME: ugly: we know that the calling code will use multiples of 4 as offsets, so this kinda works
                            offset = 0;
                        }
                        break;
                    case 0x2: // TID
                        src = ((CAEN.CAENRFIDTag)tag).GetTID();
                        break;
                    case 0x3: // USER
                        arr = _source.ReadTagData_EPC_C1G2((CAEN.CAENRFIDTag)tag, (short)membank, (short)offset, (short)count);
#if CHATTY_READER
                        Trace.WriteLine(string.Format("read result OK, buffer: {0}", BitConverter.ToString(arr)));
#endif
                        return true;
                    default:
                        return false;
                }
                if (src == null)
                    return false;

                if (offset < 0)
                    return false;
                if (offset + count > src.Length + 1)
                    return false;
                arr = SubArray<byte>(src, offset, count);
                return true;
            }
            catch (Exception ex)
            {
#if CHATTY_READER
                Trace.WriteLine(string.Format("WriteBytes: failed! Message: {0}", ex.Message));
#endif
				ex = null; // prevent CS0168
                arr = null;
                return false;
            }
        }

        public bool WriteBytes(object tag, int membank, int offset, int count, byte[] arr)
        {
            try
            {
#if CHATTY_READER
                Trace.WriteLine(string.Format("WriteBytes: writing membank {0}, offset {1}, count {2}, arr {3}", membank, offset, count, BitConverter.ToString(arr)));
#endif
                _source.WriteTagData_EPC_C1G2((CAEN.CAENRFIDTag)tag, (short)membank, (short)offset, (short)count, arr);
                return true;
            }
            catch (Exception ex)
            {
                var iex = ex.InnerException;
                var msg = iex != null ? iex.Message : "";
#if CHATTY_READER
                Trace.WriteLine(string.Format("WriteBytes: failed! Message: {0}", ex.Message));
#endif
                return false;
            }
        }
    }
}
