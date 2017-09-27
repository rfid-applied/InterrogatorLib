using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace InterrogatorLib
{
	/// <summary>
	/// Encoding of EPC TDS data into raw bytes, suitable to be written into tag memory.
	/// 
	/// Ref: EPCglobal Tag Data Structure v.1.9
	/// </summary>
    public static class Encoder
    {
        static int NumDigits(long number)
        {
            return (int)Math.Floor(Math.Log10((double)number) + 1);
        }

		/// <summary>
		/// Encode the Tag EPC URI in SGTIN scheme into tag EPC memory bank.
		/// </summary>
		/// <param name="reader">Reference to the reader</param>
		/// <param name="tag">Reference to the singulated tag (reader-specific datum)</param>
		/// <param name="uri">The Tag URI</param>
		/// <returns>True if succeeded</returns>
		public static bool WriteEPC(this IReaderLowLevel reader, object tag, TagEPCURI_SGTIN uri)
        {
            // try to encode the EPC Tag URI

            // see 14.5.1, table 14-2
            // algorithm: 14.3.3
            byte[] res_control = new byte[4];
            byte[] res = new byte[12];

            res_control[0] = 0; // CRC-16
            res_control[1] = 0; // CRC-16
            /*
             * The number of bits, N, in the EPC binary encoding determined
             * in Step 2 above, divided by 16, and rounded up to the next
             * higher integer if N was not a multiple of 16
             */
            var nbits = 12 * 8; // full length of PC+EPC data, in bits
            int nbit_value = (nbits + 15) / 16;
            var UMI = true; // indicates if user memory bank is present
            var XI = false; // indicates if XPC is present
            var toggle = false; // indicates if bits 0x18-0x1F contain attribute bits and the remainder of EPC bank contains a binary encoded EPC
            res_control[2] =
//            var pc_epc_len = ((epc[2] >> 3) & 0x1F) * 2; // full length of PC+EPC data, in bytes
                (byte)((nbit_value << 3)
                    | (UMI? 0x4 : 0)
                    | (XI? 0x2 : 0)
                    | (toggle? 0x1 : 0));
            res_control[3] = 0;

            //Console.WriteLine("Control: {0}", BitConverter.ToString(res_control));

            // SGTIN-96

            // EPC header 1 byte = 00110000 (bin)
            // filter 3 bits (3 bit integer)
            // partition 3 bits (3 bit integer, see below)
            // GS1 Company Prefix 20-40 bits
            // Item Ref 24-4 bits
            // Serial 38 bits

            // write the header
            res[0] = 0x30;
            // write the filter
            res[1] = (byte)(res[1] | ((uri.Filter & 0x7) << 5));

            var uri0 = uri.Identity.URI;
            byte partition = 0;
            var C = uri.Identity.GS1CompanyPrefix;
            var D = uri.Identity.ItemRef;
            // value P (partition value, 3 bits)
            // value C (of M-bit width)
            var Cdigits = uri.Identity.GS1CompanyPrefixLength;
            // value D (of N-bit width)
            var Ddigits = uri.Identity.ItemRefLength;

            // NOTE: Cdigits + Ddigits must be 13
            var sum = Cdigits + Ddigits;

            // there's a complication
            // a GTIN might be:
            // - GTIN-8 (8 digits)
            // - GTIN-12 (12 digits)
            // - GTIN-13 (13 digits)
            // - GTIN-14 (14 digits)
            // first of all, how to convert GTIN-8 to GTIN-12?
            // what about anything else? should we use strings instead of long integers?

            /*
             * see: https://www.gs1us.org/resources/standards/company-prefix
             * - GS1 Company Prefix is a string of digits
             * - GS1 Company Prefix Length is between 7 and 11 digits
             * - GS1 Company Prefix Length provides capacity in emitting fresh GTINs
             *   - prefix length = 7, capacity = 100 000
             *   - prefix length = 8, capacity = 10 000
             * 
             * A GS1 Company Prefix and an Item Reference Number comprise a GTIN.
             * 
             * http://www.envioag.com/wp-content/uploads/2013/05/GTIN_070313.pdf
             */

            if (Cdigits == 12 && Ddigits == 1)
            {
                partition = 0;
                // new SGTINPartInfo() { bitsM = 40, digitsL = 12, bitsN = 4, digits = 1 },
                res[1] |= (byte)((C >> 38) & 0x3);
                res[2] |= (byte)((C >> 30) & 0xFF);
                res[3] |= (byte)((C >> 22) & 0xFF);
                res[4] |= (byte)((C >> 14) & 0xFF);
                res[5] |= (byte)((C >> 6) & 0xFF);
                res[6] |= (byte)(((C >> 0) & 0x3F) << 2);
                
                res[6] |= (byte)((D >> 2) & 0x3);
                res[7] |= (byte)(((D >> 0) & 0x3) << 6);
            }
            else if (Cdigits == 11 && Ddigits == 2)
            {
                // 1
                return false;
            }
            else if (Cdigits == 10 && Ddigits == 3)
            {
                partition = 2;
                res[1] |= ((byte)((C >> 32) & 0x03));
                res[2] |= ((byte)((C >> 24) & 0xFF));
                res[3] |= ((byte)((C >> 16) & 0xFF));
                res[4] |= ((byte)((C >> 8) & 0xFF));
                res[5] |= ((byte)((C >> 0) & 0xFF));

                res[6] |= (byte)(D >> 2);
                res[7] |= (byte)(((D >> 0) & 0x03) << 6);
            }
            else if (Cdigits == 9 && Ddigits == 4)
            {
                // 3
                return false;
            }
            else if (Cdigits == 8 && Ddigits == 5)
            {
                // 4
                return false;
            }
            else if (Cdigits == 7 && Ddigits == 6)
            {
                partition = 5;
                res[1] |= ((byte)((C >> 22) & 0x03));
                res[2] |= ((byte)((C >> 14) & 0xFF));
                res[3] |= ((byte)((C >> 6) & 0xFF));
                res[4] |= ((byte)(((C >> 0) & 0x3F) << 2));

                res[4] |= ((byte)((D >> 18) & 0x03));
                res[5] |= ((byte)((D >> 10) & 0xFF));
                res[6] |= ((byte)((D >> 2) & 0xFF));
                res[7] |= ((byte)(((D >> 0) & 0x03) << 6));
            }
            else if (Cdigits == 6 && Ddigits == 7)
            {
                // 6
                return false;
            }
            else
            {
                // unknown partition
                return false;
            }

            res[1] = (byte)(res[1] | ((partition & 0x7) << 2));

            var S = uri.Identity.SerialNr;
            res[7] = (byte)(res[7] | (byte)((S >> 32) & 0x3F));
            res[8] = (byte)(res[8] | (byte)((S >> 24) & 0xFF));
            res[9] = (byte)(res[9] | (byte)((S >> 16) & 0xFF));
            res[10] = (byte)(res[10] | (byte)((S >> 8) & 0xFF));
            res[11] = (byte)(res[11] | (byte)((S >> 0) & 0xFF));

            /*
            var res_hexstring = BitConverter.ToString(res);
            Console.WriteLine("result of WriteEPC: {0}", res_hexstring);
             */

            if (reader.ChunkSize > 0)
            {
                // try writing in blocks of ChunkSize
                var csize = reader.ChunkSize;
                var chunk = new byte[csize];
                var beg = 0;
                var ofs = 4;
                while (beg < res.Length)
                {
                    Buffer.BlockCopy(res, beg, chunk, 0, csize);
                    if (!reader.WriteBytes(tag, 0x01, ofs, csize, chunk))
                        return false;
                    ofs += csize;
                    beg += csize;
                }
                return true;
            }
            else
            {
                // skip the first 4 bytes
                return reader.WriteBytes(tag, 0x01, 4, res.Length, res);
            }
        }

    }
}
