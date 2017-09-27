using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
#if NET35_CF
using CAEN = com.caen.RFIDLibrary;
#endif

namespace InterrogatorLib
{
#if NET35_CF
	/// <summary>
	/// Manages M3 Orange device interrogator
	/// </summary>
	public class M3OrangeInterrogator : IInterrogator
    {
        public event EPCEventHandler EPCEvent;
        private bool _readerReady = false;

        /// <exception cref="Exception">Thrown when interrogator initialization has failed</exception>
        public M3OrangeInterrogator()
        {
            _reader = new CAEN.CAENRFIDReader();
            _reader.CAENRFIDEvent += new com.caen.RFIDLibrary.CAENRFIDEventHandler(HandleRFIDEvent);
            _reader.Connect(CAEN.CAENRFIDPort.CAENRFID_RS232, "MOC1");
            System.Threading.Thread.Sleep(500);
            _source = _reader.GetSources()[0];
            _readerReady = true;
        }

        private string CAENRfidTagSelector(CAEN.CAENRFIDNotify ANotify)
        {
            return Utility.ByteArrayToHexString(ANotify.TagID);
        }

        private void HandleRFIDEvent(object Sender, com.caen.RFIDLibrary.CAENRFIDEventArgs Event)
        {
            var handler = EPCEvent;
            if (handler == null) return;
            var tags = Event.Data.Select<CAEN.CAENRFIDNotify, string>(CAENRfidTagSelector).ToArray();
            handler(this, new EPCEventArgs() { EPC = tags });
        }

        public void Dispose()
        {
            if (_readerReady)
            {
                _source = null;
                _reader.Dispose();
                _reader = null;
            }
        }

        private CAEN.CAENRFIDReader _reader = null;
        private CAEN.CAENRFIDLogicalSource _source;

        public bool StartInventory(byte[] AMask, int AMaskLength, int AOffset, int ATimeout)
        {
            if (!_readerReady)
            {
                return false;
            }
            _source.SetReadCycle(ATimeout);
            return _source.EventInventoryTag(AMask, (short)AMaskLength, (short)AOffset, 0);
        }

        public int Power
        {
            get { return _reader.GetPower(); }
            set { _reader.SetPower(value); }
        }

        public void StopInventory()
        {
            if (_readerReady)
            {
                _reader.InventoryAbort();
            }
        }

        public bool GetSelectedTag(out CAEN.CAENRFIDTag tag, int tries)
        {
            if (!_readerReady)
            {
                tag = null;
                return false;
            }
            bool success = false;
            tag = null;
            try
            {
                CAEN.CAENRFIDTag[] tags;
                while (--tries > 0)
                {
                    tags = _source.InventoryTag();
                    if (tags != null)
                    {
                        tag = tags[0];
                        PlatformUtility.MessageBeep(0);
                        success = true;
                        break;
                    }
                }
            }
            catch
            {
                tag = null;
            }
            return success;
        }

        public bool ReadTag(CAEN.CAENRFIDTag tag, MemoryBankType MemType, int nStartAddr, int nLength, out byte[] data)
        {
            if (!_readerReady)
            {
                data = null;
                return false;
            }
            try
            {
                data = _source.ReadTagData_EPC_C1G2(tag, (short)MemType, (short)nStartAddr, (short)nLength);
            }
            catch
            {
                data = null;
                return false;
            }
            return true;
        }

        public bool WriteTag(CAEN.CAENRFIDTag tag, MemoryBankType MemType, int nStartAddr, int nLength, byte[] data)
        {
            if (!_readerReady)
            {
                return false;
            }
            try
            {
                _source.WriteTagData_EPC_C1G2(tag, (short)MemType, (short)nStartAddr, (short)nLength, data);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
#endif
}
