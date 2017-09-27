using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
#if NETSTANDARD2_0
using System.Diagnostics.Tracing;
#endif

namespace InterrogatorLib
{
	/// <summary>
	/// Multi-Chip Serialization algorithm (aka MCS).
	///
	/// Serialization is the process of assigning serialized identifiers to tags.
	/// 
	/// Serialization
	/// http://www.rfidjournal.net/masterPresentations/rfid_live2012/np/southall_cromhout_apr3_145_itemLevelApparelWorkshop.pdf
	///
	/// http://www.emmicroelectronic.com/sites/default/files/public/products/datasheets/an604004_0.pdf
	/// http://www.nxp.com/documents/other/249820.pdf
	/// http://www.alientechnology.com/wp-content/uploads/white-paper-Alien-Technology-Higgs-4-Serialization.pdf
	/// https://www.rfid-alliance.com/RFIDshop/Alien-Technology-Higgs-3-IC-Datasheet.pdf
	/// Monza Serialization: https://support.impinj.com/hc/en-us/articles/203444983
	/// </summary>
	public static class MCS
    {
		/// <summary>
		/// Generate an Tag EPC URI based on a given template and the TID.
		/// </summary>
		/// <param name="tid">TID of the tag that the EPC is to be generated for.</param>
		/// <param name="epc">Template EPC, containing all non-serial information (e.g. GS-1 company prefix, item reference, etc.)</param>
		/// <param name="res">The resulting EPC</param>
		/// <returns>true if succeeded</returns>
        public static bool GenerateEPC(ref TID tid, ref TagEPCURI_SGTIN epc, out TagEPCURI_SGTIN res)
        {
            res = default(TagEPCURI_SGTIN);

            var serial = tid.Serial;
            if (serial == null || serial.Length == 0)
                return false;

            var MCS_identifier = (tid.TMN >> 5) & 0x3F;
            var MCS_product = (tid.TMN >> 3) & 0x7;
            var MCS_version = tid.TMN & 0x7;
            // for Higgs-3: identifier 0x10, product 0x2, version 0x2
            // for Monza: identifier 0x4, product 0, version 5
            //                    System.Windows.Forms.MessageBox.Show(
            //                        string.Format("MCS: identifier {0:X}, product {1:X}, version {2:X}", MCS_identifier, MCS_product, MCS_version)
            //                    );

            // extract 35 least significant bits of desired serial number
            // from Serial
            var start = 0;
            if (serial.Length == 8) // 64 bits
            {
                switch (tid.TMN)
                {
                    case 0x412:
                        // Alien Higgs-3: 64-bits of uniqueness
                        // but we only want 35 bits, and since they do
                        // support MCS, it should be OK
                        if (tid.MDID == 0x3) // vendor: Alien, chip: Higgs-3
                            start = 3;
                        else
                            return false; // untested
                        break;
                    default:
                        return false; // untested
                }
            }
            else if (serial.Length == 6) // 48 bits
            {
                switch (tid.TMN)
                {
                    // TMN (hex) for Monza-4 chips (source: Monza Serialization, above)
                    case 0x100: // Monza 4D
                    case 0x10C: // Monza 4E
                    case 0x105: // Monza 4QT
                        // on Impinj Monza-4 chips, this gives the sought-for uniqueness
                        // or does it???
                        if (tid.MDID == 0x801) // vendor: Impinj
                            start = 0;
                        else
                            start = 1;
                        break;
                    case 0x412: // Alien Higgs-3? 64-bits of uniqueness
                        if (tid.MDID == 0x3) // vendor: Alien, chip: Higgs-3
                            // NOTE: still, I found two tags which have the same UTID, byte-by-byte
                            start = 1;
                        else
                            start = 1;
                        break;
                    default:
                        start = 1; // untested
                        break;
                }
            }
            else if (serial.Length == 5) // 40 bits (e.g. some NXP tags)
            {
                start = 0;
            }
            else
            {
                return false;
            }
            var newserial = 0L;
            newserial |= ((long)(serial[start+0] & 0x07) << 32);
            newserial |= ((long)(serial[start+1] & 0xFF) << 24);
            newserial |= ((long)(serial[start+2] & 0xFF) << 16);
            newserial |= ((long)(serial[start+3] & 0xFF) << 8);
            newserial |= ((long)(serial[start+4] & 0xFF) << 0);

            // put the 3 bits identifying the chip vendor
            int vendor = 0;
            var MDID = tid.MDID & ~0x800; // remove the XTID indicator
            switch (MDID)
            {
                case 0x003: // Alien
                    vendor = 0x06; // 110
                    break;
                case 0x001: // Impinj
                    vendor = 0x05; // 101
                    break;
                case 0x006: // NXP
                    vendor = 0x07; // 111
                    break;
                default:
                    return false; // unknown vendor, they may not support MCS!
            }
            newserial |= ((long)(vendor & 0x07) << 35);

			Debug.WriteLine(string.Format("serial: {0}, starting at {1}, newserial: {2}", BitConverter.ToString(serial), start, newserial));

            res = epc;
            // C# needs some Rust-style borrowing stuff for value types!
            // (or, some linear types would be even nicer, as they are simpler)
            var identity = epc.Identity;
            Debug.WriteLine(string.Format("identity EPC before serial assignment {0}", identity.URI));
            identity.SetSerial(newserial);
            res.Identity = identity;
            Debug.WriteLine(string.Format("identity EPC after serial assignment {0}", identity.URI));

            return true;
        }

    }
}
