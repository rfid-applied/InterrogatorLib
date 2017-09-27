using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
#if NET35_CF
using UHFAPI_NET;
#endif

namespace InterrogatorLib
{
#if NET35_CF
	public class DotelInterrogator : IInterrogator
    {
        private UHFAPI_NET.UHFAPI_NET _uhfapi = null;

        public event EPCEventHandler EPCEvent;

        public void Dispose()
        {
            _uhfapi.Cleanup();
        }

        public DotelInterrogator()
        {
            _uhfapi = new UHFAPI_NET.UHFAPI_NET();
            _uhfapi.evtInventoryEPC += new UHFAPI_NET.UHFAPI_NET.InventoryEPCDispacher(OnInventoryEPC);
        }

        public int Power
        {
            get
            {
                uint power;
                _uhfapi.UHFAPI_GET_PowerControl(out power);
                return (int)power;
            }
            set
            {
                _uhfapi.UHFAPI_SET_PowerControl((uint)value);
            }
        }

        void OnInventoryEPC(string epc)
        {
            var handler = EPCEvent;
            if (handler == null) return;
            handler(this, new EPCEventArgs() { EPC = new string[] { epc } });        
        }

        public bool StartInventory(byte[] AMask, int AMaskLength, int AOffset, int ATimeout)
        {
            UHFAPI_NET.UHFAPI_NET.structTAG_OP_PARAM LParam
                = UHFAPI_NET.UHFAPI_NET.UHFAPI_GetTagOpParam(
                        (byte)UHFAPI_NET.UHFAPI_NET.MEMBANK_CODE.BANK_EPC, (uint)AOffset*8, (uint)AMaskLength*8, AMask);

            if (AMaskLength > 0)
            {
                LParam.single_tag = (int)0;
                LParam.QuerySelected = (int)1;
            }
            _uhfapi.UHFAPI_SET_OpMode(LParam.single_tag > 0, AMaskLength > 0, LParam.QuerySelected > 0, (uint)ATimeout);
            var ret = _uhfapi.UHFAPI_Inventory(LParam, false);
            return ret == UHFAPI_NET.UHFAPI_NET.enumAccessResult.ACCESS_RESULT_OK ? true : false;
        }

        public void StopInventory()
        {
            _uhfapi.UHFAPI_Stop();
        }
    }
#endif
}
