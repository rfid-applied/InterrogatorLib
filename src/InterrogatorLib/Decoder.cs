using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace InterrogatorLib
{
	/// <summary>
	/// Decode error
	/// </summary>
    public enum DecodeError : int
    {
		/// no error, everything is OK
		None = 0,
		/// IO error (can retry)
		IO,
		/// EPC is invalid or something like that
		Invalid
	}

	/// <summary>
	/// RFID UHF Tag memory decoder.
	/// 
	/// Ref: EPCglobal Tag Data Structure v.1.9
	/// </summary>
    public static class Decoder
    {
        static bool IsBitSet(short word, int bitNumber)
        {
            return (word & (1 << bitNumber - 1)) != 0;
        }

        // 10 raised to specified power
        static ulong IntPow10(byte pow)
        {
            switch (pow)
            {
                case 0: return 1UL;
                case 1: return 10UL;
                case 2: return 100UL;
                case 3: return 1000UL;
                case 4: return 10000UL;
                case 5: return 100000UL;
                case 6: return 1000000UL;
                case 7: return 10000000UL;
                case 8: return 100000000UL;
                case 9: return 1000000000UL;
                case 10: return 10000000000UL;
                case 11: return 100000000000UL;
                case 12: return 1000000000000UL;
                // up to 32 can be added 
                default: // Vilx's solution is used for default
                    ulong ret = 1UL;
                    var x = 10;
                    while (pow > 0)
                    {
                        if ((pow & 1) == 1)
                            ret *= (ulong)x;
                        x *= x;
                        pow >>= 1;
                    }
                    return ret;
            }
        }

		/// <summary>
		/// Decode the TID memory bank of a chip.
		/// </summary>
		/// <param name="reader">Reference to the reader that is used to obtain the TID memory bank contents</param>
		/// <param name="tag">Reference to the singulated tag (reader-specific datum)</param>
		/// <param name="tid">The decoded contents</param>
		/// <returns>Error or success</returns>
        public static DecodeError ReadTID(this IReaderLowLevel reader, object tag, out TID tid)
        {
            tid = default(TID);

            if (tag == null)
                return DecodeError.IO;

            /*
             * TDS 1.5
             * - TID shall contain 0xE2 @ 0x00-0x07 (allocation class identifier)
             * - TID shall contain 12-bit Tag mask designer identifier MDID @ 0x08-0x13
             * - TID shall contain 12-bit Tag Model Number (TMN) @ 0x14-0x1F
             * - bit 0x08 is the XTID bit: if set to 0, the bank contains only
             *    allocation class identifier, MDID, and TMN
             *    A value of zero indicates a short TID in which
             *    the values beyond address 0x1F are not defined. A value of one indicates an Extended Tag
             *    Identification (XTID) in which the memory locations beyond 0x1F contain additional data
             *    as specified in Section 16.2.
             * 
             * what's in TMN?
             * - if MDID is Impinj:
             *   - 0001011xxxxx Monza 6 family tag
             *   - 0001001100xx Monza 5 family tag
             *   - 0001000xxxxx Monza 4 family tag
             * 
             * examples (bytes 0-8 of TID)
             * also: refer to http://www.kentraub.net/tools/tagxlate/TIDDecoder.html
             * E2-80-11-05-20-00-55-01 -- singular tag, Monza
             * E2-80-11-05-20-00-55-01-21-28-08-98
             *    (Monza 4QT, according to: Monza TID Memory Maps for Self-Serialization)
             *    MDID 801
             *      vendor: Impinj
             *    TMN 105 (binary 0001 0000 0101)
             *        000 100 000 101
             *        ^^^ ^^^ MCS identifier (3 bits), product (3 bits)
             *                ^^^ MCS product (unused)
             *                    ^^^ version
             *    manufacturer-assigned serial number: 550121280898 (hex)
             *    STID URI: urn:epc:stid:x801.x105.x550121280898
             *    already serialized!
             * E2-80-11-05-20-00-55-81 -- one of the twin tags, don't know which one exactly
             *    premature end of TID
             * E2-00-34-12-01-3B-F0-00 (screw-on tags)
             * E2-00-34-12-01-38-F0-00 (screw-on tags)
             *        ^ MCS identifier!
             *    MDID 003, TMN 412 (binary 0100 0001 0010)
             *      010 000 010 010
             *      ^^^ MCS identifier (vendor code?)
             *          ^^^ vendor code
             *              ^^^ product
             *                  ^^^ version
             * what about MCS?
             * - http://www.emmicroelectronic.com/sites/default/files/public/products/datasheets/an604004_0.pdf
             * - http://www.alientechnology.com/wp-content/uploads/white-paper-Alien-Technology-Higgs-4-Serialization.pdf
             * - http://www.nxp.com/documents/other/249820.pdf
             * - MCS serial number is
             *   MCS header (3 bits), MCS product (3 bits), IC serial (32 bits)
             *   where to get that data?
             *   break up TMN as (from MSB to LSB):
             *   - MCS identifier (6 bits)
             *     - highest bits: unused
             *     - lowest bits: vendor code
             *   - MCS product (3 bits)
             *   - version (3 bits)
             */
            {
                // read 12-byte TID (6-word, where word = 2 bytes)
                // 12 byte = 96
                // 64 bits of UTID (8 bytes)

                // read 4 bytes of TID first
                // if bit 0x08 is set, read XTID

                byte[] shortTID;
                var r0 = reader.ReadBytes(tag, 0x02, 0, 4, out shortTID);
                if (!r0)
                    return DecodeError.IO;
                if (shortTID[0] != 0xE2)
                    return DecodeError.Invalid; // we are not prepared to handle custom TID tags
                var MDID = shortTID[1] << 4 | ((shortTID[2] >> 4) & 0xF);
                var TMN = ((shortTID[2] & 0xF) << 8) | shortTID[3];

                var shortTID_hex = BitConverter.ToString(shortTID);

                // is XTID set?
                var XTID = (shortTID[1] & 0x80) > 0;
                // no: short tag
                // yes: long tag
                // - memory locations 0x20 to 0x2F contain 16-bit XTID header
                //   that specifies what info is present at 0x30 and above

                Debug.WriteLine(string.Format("MDID: {0:X} TMN: {1:X}", MDID, TMN));
                if (XTID)
                {
                    // read XTID header segment
                    byte[] XTID_header;
                    var r1 = reader.ReadBytes(tag, 0x02, 4, 2, out XTID_header);
                    if (!r1)
                        return DecodeError.IO;

                    //var XTID_header_hex = BitConverter.ToString(XTID_header);

                    var XTID_header_word = (short)((XTID_header[0] << 8) | (XTID_header[1] & 0xFF));
                    var extended_header_present = IsBitSet(XTID_header_word, 0);
                    var reserved = !IsBitSet(XTID_header_word, 1)
                        && !IsBitSet(XTID_header_word, 2)
                        && !IsBitSet(XTID_header_word, 3)
                        && !IsBitSet(XTID_header_word, 4)
                        && !IsBitSet(XTID_header_word, 5)
                        && !IsBitSet(XTID_header_word, 6)
                        && !IsBitSet(XTID_header_word, 7)
                        && !IsBitSet(XTID_header_word, 8)
                        && !IsBitSet(XTID_header_word, 9); // TDS 1.9: these should be 0
                    var has_user_mem_and_block_permalock_segment = IsBitSet(XTID_header_word, 10);
                    var has_blockwrite_and_blockerase_segment = IsBitSet(XTID_header_word, 11);
                    var has_optional_command_support_segment = IsBitSet(XTID_header_word, 12);

                    var serial_size = (XTID_header_word >> 13) & 0x7;
                    // if serialization is non-zero, specifies that XTID includes
                    // a unique serial number, whose length is in bits is 48 + 16(N-1),
                    // where N is the value of this field;
                    // otherwise, specifies that XTID does not include a unique serial number

                    Debug.WriteLine(string.Format("serial size: {0}", serial_size));
                    if (serial_size > 0)
                    {
                        var size = (48 + 16 * (serial_size - 1)) / 8;
                        byte[] XTID_serial_segment;

                        var r2 = reader.ReadBytes(tag, 0x02, 6, size, out XTID_serial_segment);
                        if (!r2)
                            return DecodeError.IO;

                        var XTID_serial_hex = BitConverter.ToString(XTID_serial_segment);
                        Debug.WriteLine(string.Format("serial segment: {0}", XTID_serial_hex));

                        tid.MDID = MDID;
                        tid.TMN = TMN;
                        tid.Serial = XTID_serial_segment;

                        var STID_uri = "urn:epc:stid:";
                        STID_uri += "x" + MDID.ToString("X3"); // as 3-character hex (upcase)
                        STID_uri += ".x" + TMN.ToString("X3"); // as 3-character hex (upcase)
                        STID_uri += ".x" + XTID_serial_hex.Replace("-", "");
                        tid.STID_URI = STID_uri;

                        // TODO: STID -> EPC SGTIN, and write it down

                        Debug.WriteLine(string.Format("STID URI: {0}", STID_uri));
                        //System.Windows.Forms.MessageBox.Show(STID_uri);
                        return DecodeError.None;
                    }
                }
                else
                {
                    // maybe it's Alien Higgs-3?
                    // refer to: https://www.rfid-alliance.com/RFIDshop/Alien-Technology-Higgs-3-IC-Datasheet.pdf
                    if (MDID == 0x3) // manufacturer: Alien
                    {
                        var model = (TMN >> 8) & 0xF;
                        var rev_major = (TMN >> 4) & 0xF;
                        var rev_minor = TMN & 0xF;

                        // e.g. model=4 (Higgs), Major=1, minor=2
                        Debug.WriteLine(string.Format("Alien: model {0:X}, major rev. {1}, minor rev. {2}", model, rev_major, rev_minor));
                        if (model == 4 && rev_major == 1 && rev_minor == 2)
                        {
                            byte[] UTID;
                            var r2 = reader.ReadBytes(tag, 0x02, 4, 8, out UTID);
                            if (!r2)
                                return DecodeError.IO;

                            tid.MDID = MDID;
                            tid.TMN = TMN;
                            tid.Serial = UTID;

                            var UTID_hex = BitConverter.ToString(UTID);

                            // indeed, we have UTID here
                            // but how do we obtain SGTIN from it?
                            //var UTID_hex = BitConverter.ToString(UTID);
                            //Console.WriteLine("Alien: {0}", UTID_hex);
                            return DecodeError.None;
                        }
                    }
                    else if (MDID == 0x6)
                    {
                        // NXP
                        // e.g. G2XM: http://www.nxp.com/documents/data_sheet/SL3ICS1002_1202.pdf
                        // used in e.g. HID IN TAG 500 UHF
                        var version = (TMN >> 4) & 0x3F;
                        // NOTE: whenever the 32 bit serial is exceeded the
                        // subversion is incremented by 1,
                        // so we include it into the serial!
                        var subversion = TMN & 0x0F;

                        byte[] UTID;
                        var r2 = reader.ReadBytes(tag, 0x02, 4, 4, out UTID);
                        if (!r2)
                            return DecodeError.IO;

                        byte[] serial = new byte[5];
                        serial[0] = (byte)subversion;
                        Buffer.BlockCopy(UTID, 0, serial, 1, 4);

                        tid.MDID = MDID;
                        tid.TMN = TMN;
                        tid.Serial = serial;

                        return DecodeError.None;
                    }
                    else
                    {
                        // unknown manufacturer's tag
                        // see the list here: http://www.gs1.org/epcglobal/standards/mdid
                        tid.MDID = MDID;
                        tid.TMN = TMN;
                        tid.Serial = null;
                        return DecodeError.None;
                    }
                }
            }

            return DecodeError.Invalid;
        }

		/// <summary>
		/// Read the first 4 bytes of a properly formatted EPC memory bank, and try to tease out
		/// the "protocol control" info
		/// </summary>
		/// <param name="reader">Reference to the reader that is used to obtain the TID memory bank contents</param>
		/// <param name="tag">Reference to the singulated tag (reader-specific datum)</param>
		/// <param name="pc">Decoded Protocol Control information</param>
		/// <returns>Error or success</returns>
		public static DecodeError ReadEPC_PC(this IReaderLowLevel reader, object tag, out EPC_PC pc)
        {
            pc = default(EPC_PC);

            int start = 0;
            int count = 4;
            byte[] epc;
            var rr = reader.ReadBytes(tag, 0x01, start, count, out epc);
            if (!rr)
                return DecodeError.IO;

            // extract CRC-16, protocol control
            pc.Crc16 = (short)((epc[0] << 8) | epc[1]);
            // represents the number of 16-bit words comprising the PC field and the EPC field
            // bits[0x10..0x14]
            pc.PC_EPC_Length = (short)(((epc[2] >> 3) & 0x1F) * 2); // full length of PC+EPC data, in bytes
            pc.UMI = (epc[2] & 0x4) != 0; // indicates if user memory bank is present
            pc.XI = (epc[2] & 0x2) != 0; // indicates if XPC is present
            // toggle (bit 0x17)
            // 0: bits 0x18-0x1F contain attribute bits and the remainder of EPC bank contains a binary encoded EPC
            // 1: bits 0x18-0x1F contain ISO AFI, and the remainder of EPC bank contains UII
            pc.Toggle = (epc[2] & 0x1) != 0;
            // bits 0x18-0x1F
            pc.Attribs = epc[3]; // bits that may guide the handling of physical object to which the tag is affixed (Gen2 v1.x tags only)

            return DecodeError.None;
        }

		/// <summary>
		/// Decode the EPC memory bank, which is assumed to be SGTIN-192, and return the Tag URI.
		/// </summary>
		/// <param name="reader">Reference to the reader that is used to obtain the TID memory bank contents</param>
		/// <param name="tag">Reference to the singulated tag (reader-specific datum)</param>
		/// <param name="uri">The resulting Tag URI</param>
		/// <returns>Success or failure</returns>
		public static DecodeError ReadEPC_SGTIN(this IReaderLowLevel reader, object tag, out TagEPCURI_SGTIN uri)
        {
            uri = default(TagEPCURI_SGTIN);

            // see also: http://www.rfidjournal.net/masterPresentations/rfid_live2012/np/traub_apr3_230_ITProf.pdf
            // what about the EPC?
            // bit 0x17 of EPC is the toggle
            // - we need to use SGTIN-96, and encode it
            //   - pure identity URI (e.g. urn:epc:id:sgtin:0614141.100743.401)
            //     - independent of RFID, so this is what the business apps should use
            //   - tag URI (e.g. urn:epc:tag:sgtin-96:3.0614141.100743.401) <-- contains control info of EPC memory bank as well
            //     - used when reading from a tag where the control info is of interest
            //     - used when writing to the EPC memory bank of an RFID tag, in order to fully specify the contents to be written
            //     - there exists 1:1 mapping between tag URI and binary representation
            // - what's the barcode equivalent?
            //   barcode:
            //      (01) 1 0614141 12345 2 (21) 401
            //      ^^^^ not included
            //   pure identity URI:
            // urn:epc:id:sgtin:0614141.112345.401
            // - also, need to learn how to encode SGTIN-96 to binary form and back!
            //
            // also, we might want to use the GS1 GIAI schema
            // - http://www.gs1.org/docs/idkeys/GS1_GIAI_Executive_Summary.pdf
            // - 1 2 ... n           n+1 n+2  ... <= 30
            //   GS1 company prefix| individual asset reference
            //   (numeric)         | (alphanumeric)
            //
            // what's in SGTIN? e.g. urn:epc:id:sgtin:0614141.112345.400
            // urn:epc:id:sgtin:CompanyPrefix.ItemRefAndIndicator.SerialNumber
            // - company prefix (assigned by GS1 to a managing entity or its delegates)
            // - item ref and indicator (assigned by the managing entity to a particular object class)
            // - serial number (assigned by the managing entity to an individual object)
            // what's in GIAI? e.g. urn:epc:id:giai:0614141.12345400
            // urn:epc:id:giai:CompanyPrefix.IndividulAssetReference
            // - company prefix (assigned by GS1 to a managing entity)
            // - individual asset reference (assigned uniquely by the managing entity to a specific asset)

            /*
             * how to assign?
             * 
             * what's in the EPC
             * - 2 bytes of CRC (we don't usually write these, right?)
             * - 2 bytes: length (5 bits), UMI (1 bit), XI (1 bit), T (1 bit), attribs (8 bits)
             *     - length: nr of words for PC and EPC, combined
             * - 2 bytes: EPC header (1 byte), filter (3 bits), partition (3 bits), GTIN (2 bits)
             * - 2 bytes: GTIN
             * - 2 bytes: GTIN
             * - 2 bytes: GTIN (10 bits), serial (6 bits)
             * - 2 bytes: serial
             * - 2 bytes: serial
             * 16 bytes in total
             */

            // see also: http://www.rfidjournal.net/masterPresentations/rfid_live2012/np/traub_apr3_230_ITProf.pdf
            // what about the EPC?
            // bit 0x17 of EPC is the toggle
            // - we need to use SGTIN-96, and encode it
            //   - pure identity URI (e.g. urn:epc:id:sgtin:0614141.100743.401)
            //     - independent of RFID, so this is what the business apps should use
            //   - tag URI (e.g. urn:epc:tag:sgtin-96:3.0614141.100743.401) <-- contains control info of EPC memory bank as well
            //     - used when reading from a tag where the control info is of interest
            //     - used when writing to the EPC memory bank of an RFID tag, in order to fully specify the contents to be written
            //     - there exists 1:1 mapping between tag URI and binary representation
            // - what's the barcode equivalent?
            //   barcode:
            //      (01) 1 0614141 12345 2 (21) 401
            //      ^^^^ not included
            //   pure identity URI:
            // urn:epc:id:sgtin:0614141.112345.401
            // - also, need to learn how to encode SGTIN-96 to binary form and back!
            //
            // also, we might want to use the GS1 GIAI schema
            // - http://www.gs1.org/docs/idkeys/GS1_GIAI_Executive_Summary.pdf
            // - 1 2 ... n           n+1 n+2  ... <= 30
            //   GS1 company prefix| individual asset reference
            //   (numeric)         | (alphanumeric)
            //
            // what's in SGTIN? e.g. urn:epc:id:sgtin:0614141.112345.400
            // urn:epc:id:sgtin:CompanyPrefix.ItemRefAndIndicator.SerialNumber
            // - company prefix (assigned by GS1 to a managing entity or its delegates)
            // - item ref and indicator (assigned by the managing entity to a particular object class)
            // - serial number (assigned by the managing entity to an individual object)
            // what's in GIAI? e.g. urn:epc:id:giai:0614141.12345400
            // urn:epc:id:giai:CompanyPrefix.IndividulAssetReference
            // - company prefix (assigned by GS1 to a managing entity)
            // - individual asset reference (assigned uniquely by the managing entity to a specific asset)

            /*
             * how to assign?
             * 
             * what's in the EPC
             * - 2 bytes of CRC (we don't usually write these, right?)
             * - 2 bytes: length (5 bits), UMI (1 bit), XI (1 bit), T (1 bit), attribs (8 bits)
             *     - length: nr of words for PC and EPC, combined
             * - 2 bytes: EPC header (1 byte), filter (3 bits), partition (3 bits), GTIN (2 bits)
             * - 2 bytes: GTIN
             * - 2 bytes: GTIN
             * - 2 bytes: GTIN (10 bits), serial (6 bits)
             * - 2 bytes: serial
             * - 2 bytes: serial
             * 16 bytes in total
             */

            EPC_PC pc;
            var rr = ReadEPC_PC(reader, tag, out pc);
            if (rr != DecodeError.None)
                return rr;

            var crc16 = pc.Crc16;
            var pc_epc_len = pc.PC_EPC_Length;
            var UMI = pc.UMI;
            var XI = pc.XI;
            var toggle = pc.Toggle;
            var attribs = pc.Attribs;

            // read full length of EPC (12 bytes or more)
            byte[] epc_payload = new byte[pc_epc_len];
            {
                const int chunkSize = 4;
                int sizeRead = 0;
                while (sizeRead < pc_epc_len)
                {
                    byte[] chunk;
                    if (!reader.ReadBytes(tag, 0x01, 4+sizeRead, chunkSize, out chunk))
                        return DecodeError.IO;
                    Buffer.BlockCopy(chunk, 0, epc_payload, sizeRead, chunkSize);
                    sizeRead += chunkSize;
                }
            }

            //var epc_payload_hex = BitConverter.ToString(epc_payload);
            //System.Windows.Forms.MessageBox.Show(BitConverter.ToString(epc_payload));

            // SGTIN-96
            var epc_header = epc_payload[0]; // must be 0x30 if that's SGTIN-96!

            if (epc_header != 0x30)
                return DecodeError.Invalid; // NOT SGTIN-96!
            if (pc_epc_len < 10/*96 bits*/)
                return DecodeError.Invalid; // wrong length of EPC! (see table 14.1 in EPC TDS 1.9)

            var filter = (epc_payload[1] >> 5) & 0x7;
            var partition = (epc_payload[1] >> 2) & 0x7;

            // 14.4.3
            var C = 0L; // company prefix
            var Cdigits = 0;
            var D = 0L; // indicator/item reference
            var Ddigits = 0;
            var S = 0L;
            // Consider these M bits to be an unsigned binary integer, C.
            // The value of C must be less than 10^L, where L is the value
            // specified in the “GS1 Company Prefix Digits (L)” column of
            // the matching partition table row.
            //
            // There are N bits remaining in the input bit string, where
            // N is the value specified in the “Other Field Bits” column
            // of the matching partition table row. Consider these N bits
            // to be an unsigned binary integer, D. The value of D must be
            // less than 10^K, where K is the value specified in the
            // “Other Field Digits (K)” column of the matching partition
            // table row. Note that if K = 0, then the value of D must be zero.
            switch (partition)
            {
                case 0:
                    // new SGTINPartInfo() { bitsM = 40, digitsL = 12, bitsN = 4, digits = 1 },
                    C |= ((long)(epc_payload[1] & 0x3) << 38);
                    C |= ((long)(epc_payload[2] & 0xFF) << 30);
                    C |= ((long)(epc_payload[3] & 0xFF) << 22);
                    C |= ((long)(epc_payload[4] & 0xFF) << 14);
                    C |= ((long)(epc_payload[5] & 0xFF) << 6);
                    C |= ((long)(((epc_payload[6] >> 2) & 0x3F)) << 0);
                    Cdigits = 12;
                    if ((ulong)C >= IntPow10((byte)Cdigits))
                        return DecodeError.Invalid; // something is wrong with it!

                    D = ((((epc_payload[6] & 0x3) << 2) | ((epc_payload[7] >> 6) & 0x3)) & 0xFF) << 0;
                    Ddigits = 1;
                    if ((ulong)D >= IntPow10((byte)1))
                        return DecodeError.Invalid;
                    break;
                /*
                                                case 1:
                                                    //{1, new SGTINPartInfo() { bitsM = 37, digitsL = 11, bitsN = 7, digits = 2 }},
                                                    C |= ((long)(epc_payload[1] & 0x03) << 35);
                                                    C |= ((long)(epc_payload[2] & 0xFF) << 27);
                                                    C |= ((long)(epc_payload[3] & 0xFF) << 19);
                                                    C |= ((long)(epc_payload[4] & 0xFF) << 11);
                                                    C |= ((long)(epc_payload[5] & 0xFF) << 3);
                                                    C |= ((long)(((epc_payload[6] >> 5) & 0x07)) << 0);
                                                    Cdigits = 11;
                                                    if ((ulong)C >= IntPow10((byte)Cdigits))
                                                        continue; // something is wrong with it!

                                                    D |= ((long)(epc_payload[6] & 0x07) << 3); // 3 bits
                                                    D |= ((((long)epc_payload[7] >> 4) & 0x0F) << 0); // 4 bits
                                                    Ddigits = 2;
                                                    if ((ulong)D >= IntPow10((byte)Ddigits))
                                                        continue;

                                                    break;*/
                case 2:
                    //{2, new SGTINPartInfo() { bitsM = 34, digitsL = 10, bitsN = 10, digits = 3 }},
                    C |= ((long)(epc_payload[1] & 0x03) << 32);
                    C |= ((long)(epc_payload[2] & 0xFF) << 24);
                    C |= ((long)(epc_payload[3] & 0xFF) << 16);
                    C |= ((long)(epc_payload[4] & 0xFF) << 8);
                    C |= ((long)(epc_payload[5] & 0xFF) << 0);
                    Cdigits = 10;
                    if ((ulong)C >= IntPow10((byte)Cdigits))
                        return DecodeError.Invalid; // something is wrong with it!

                    D |= ((long)epc_payload[6] << 2);
                    D |= ((long)((epc_payload[7] >> 6) & 0x03) << 0);
                    Ddigits = 3;
                    if ((ulong)D >= IntPow10((byte)Ddigits))
                        return DecodeError.Invalid;
                    break;
                /*
                case 3:
                    // {3, new SGTINPartInfo() { bitsM = 30, digitsL = 9, bitsN = 14, digits = 4 }},
                    break;
                case 4:
//            {4, new SGTINPartInfo() { bitsM = 30, digitsL = 9, bitsN = 14, digits = 4 }},
                 */
                case 5:
                    // {5, new SGTINPartInfo() { bitsM = 24, digitsL = 7, bitsN = 20, digits = 6 }},
                    C |= ((long)(epc_payload[1] & 0x03) << 22);
                    C |= ((long)(epc_payload[2] & 0xFF) << 14);
                    C |= ((long)(epc_payload[3] & 0xFF) << 6);
                    C |= ((((long)epc_payload[4] >> 2) & 0x3F) << 0);
                    Cdigits = 7;
                    if ((ulong)C >= IntPow10((byte)Cdigits))
                        return DecodeError.Invalid; // something is wrong with it!

                    D |= ((long)(epc_payload[4] & 0x03) << 18);
                    D |= ((long)(epc_payload[5] & 0xFF) << 10);
                    D |= ((long)(epc_payload[6] & 0xFF) << 2);
                    D |= ((long)((epc_payload[7] >> 6) & 0x03) << 0);
                    Ddigits = 6;
                    if ((ulong)D >= IntPow10((byte)Ddigits))
                        return DecodeError.Invalid;
                    break;

                /*
            case 6:
                //{6, new SGTINPartInfo() { bitsM = 20, digitsL = 6, bitsN = 24, digits = 7}},
                C |= ((long)(epc_payload[1] & 0x03) << 18);
                C |= ((long)(epc_payload[2] & 0xFF) << 10);
                C |= ((long)(epc_payload[3] & 0xFF) << 2);
                C |= ((((long)epc_payload[4] >> 2) & 0x3F) << 0);
                Cdigits = 6;
                if ((ulong)C >= IntPow10((byte)Cdigits))
                    continue; // something is wrong with it!

                D |= ((long)(epc_payload[4] & 0x03) << 22);
                D |= ((long)(epc_payload[5] & 0xFF) << 14);
                D |= ((long)(epc_payload[6] & 0xFF) << 6);
                D |= ((long)((epc_payload[7] >> 2) & 0x03) << 0);
                Ddigits = 7;
                if ((ulong)D >= IntPow10((byte)Ddigits))
                    continue; // something is wrong with it!
                break;*/

                default:
                    return DecodeError.Invalid; // unable to parse this partition
            }

            // serial: 38 bits
            // NOTE: must be less than 274,877,906,944
            // 6 bits, and then some more
            S |= ((long)(epc_payload[7] & 0x3F) << 32);
            S |= ((long)(epc_payload[8] & 0xFF) << 24);
            S |= ((long)(epc_payload[9] & 0xFF) << 16);
            S |= ((long)(epc_payload[10] & 0xFF) << 8);
            S |= ((long)(epc_payload[11] & 0xFF) << 0);

            uri.Filter = (byte)filter;
            uri.Identity = new IdentityEPCURI_SGTIN()
            {
                GS1CompanyPrefix = C,
                ItemRef = D,
                GS1CompanyPrefixLength = Cdigits,
                ItemRefLength = Ddigits,
                SerialNr = S
            };

            return DecodeError.None;
        }

		/// <summary>
		/// Decode the EPC memory bank, assuming it is in raw format. Ref: EPC TDS 1.9, 15.2.1.
		/// </summary>
		/// <param name="reader">low-level reader</param>
		/// <param name="tag">the singulated tag</param>
		/// <param name="uri">output Raw URI</param>
		/// <returns>Success or failure</returns>
		public static DecodeError ReadEPC_Raw(this IReaderLowLevel reader, object tag, out TagRaw uri)
        {
            uri = default(TagRaw);

            EPC_PC pc;
            var rr = ReadEPC_PC(reader, tag, out pc);
            if (rr != DecodeError.None)
                return rr;

            uri.PC = pc;
            var pc_epc_len = pc.PC_EPC_Length;

            // read full length of EPC (12 bytes)
            // NOTE: this is much more efficient to do than reading by tiny 4-byte chunks!
            byte[] epc_payload = new byte[pc_epc_len];
            {
                const int chunkSize = 4;
                int sizeRead = 0;
                while (sizeRead < pc_epc_len)
                {
                    byte[] chunk;
                    if (!reader.ReadBytes(tag, 0x01, 4 + sizeRead, chunkSize, out chunk))
                        return DecodeError.IO;
                    Buffer.BlockCopy(chunk, 0, epc_payload, sizeRead, chunkSize);
                    sizeRead += chunkSize;
                }
            }

            // bits[0x20..0x20+N]
            uri.V = epc_payload;

            return DecodeError.None;
        }

        /// <summary>
        /// Reads EPC URI from a tag. May return either an identity URI or a raw URI.
        /// </summary>
        /// <param name="reader">low-level reader</param>
        /// <param name="tag">the singulated tag</param>
        /// <param name="uri">output URI</param>
        /// <returns>true if able to obtain the URI</returns>
        public static bool ReadEPCURI(this IReaderLowLevel reader, object tag, out string uri)
        {
            DecodeError err;

            TagEPCURI_SGTIN sgtin;
            err = ReadEPC_SGTIN(reader, tag, out sgtin);
            if (err == DecodeError.None)
            {
                uri = sgtin.Identity.URI;
                return true;
            }
            else
            {
                TagRaw raw;
                err = ReadEPC_Raw(reader, tag, out raw);
                if (err == DecodeError.None)
                {
                    var builder = new StringBuilder();
                    raw.GetURI(builder);
                    uri = builder.ToString();
                    return true;
                }
            }

            uri = null;
            return false;
        }

		/// <summary>
		/// Given a barcode in GTIN-14 format as a string, and the Company Prefix Length (in digits, between 6 and 12),
		/// tries to parse the corresponding Identity EPC URI (SGTIN).
		/// </summary>
		/// <param name="GTIN14">GTIN-14 barcode</param>
		/// <param name="GCPLen">GS1 Company Prefix length</param>
		/// <param name="uri">The resulting SGTIN Identity URI</param>
		/// <returns>true if able to parse</returns>
		public static bool ParseFromGTIN14(string GTIN14, int GCPLen, out IdentityEPCURI_SGTIN uri)
        {
            uri = default(IdentityEPCURI_SGTIN);

            if (GTIN14.Length != 14)
                return false;
            if (GCPLen < 6 && GCPLen > 12)
                return false;

            {
                var index = 0;
                var sum = 0;
                foreach (var c in GTIN14)
                {
                    if (!Char.IsDigit(c))
                        return false;
                    // for validation, refer to: http://stackoverflow.com/questions/10143547/how-do-i-validate-a-upc-or-ean-code
                    var j = (int)(c - '0');
                    if (index < 13) // not the last digit, so add up the sum
                    {
                        sum = sum + j * (index % 2 == 0 ? 3 : 1);
                    }
                    else // last digit, so check if we have the right sum
                    {
                        // validate
                        var check = (10 - (sum % 10)) % 10;
                        var last = j;
                        if (check != last)
                            return false; // invalid check digit
                    }
                    index++;
                }
            }

            long C = 0;
            long D = 0;
            int Cdigits = GCPLen;
            int Ddigits = GTIN14.Length - Cdigits - 1; // excluding the check digit

            int i = 0;
            for (; i < GCPLen; i++)
            {
                var j = (int)(GTIN14[i] - '0');
                C = C * 10 + j;
            }
            // i == GCPLen
            for (; i < GTIN14.Length - 1/* excluding the check digit*/; i++)
            {
                var j = (int)(GTIN14[i] - '0');
                D = D * 10 + j;
            }
            uri = new IdentityEPCURI_SGTIN()
            {
                GS1CompanyPrefix = C,
                GS1CompanyPrefixLength = Cdigits,
                ItemRef = D,
                ItemRefLength = Ddigits
            };

            return true;
        }
    }
}
