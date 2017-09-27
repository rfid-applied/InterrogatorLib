using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace InterrogatorLib
{
	/// <summary>
	/// Tag identification
	/// </summary>
    public struct TID
    {
        // specific to reader
        public object Tag { get; set; }

		/// <summary>
		/// Model ID
		/// </summary>
        public int MDID { get; set; }
		/// <summary>
		/// Tag
		/// </summary>
        public int TMN { get; set; }
		/// <summary>
		/// Serial number, if available (check IsSerialized)
		/// </summary>
        public byte[] Serial { get; set; }

		/// <summary>STID URI</summary>
		public string STID_URI { get; set; }

		/// True if tag is serialized (i.e. contains a serial number)
        public bool IsSerialized
        {
            get
            {
                return Serial != null && Serial.Length > 0;
            }
        }
    }

	/// <summary>
	/// Identity EPC URI, Serialized GTIN (SGTIN) scheme
	/// </summary>
    public struct IdentityEPCURI_SGTIN
    {
		/// <summary>
		/// GS-1 Company Prefix
		/// </summary>
        public long GS1CompanyPrefix { get; set; }
		/// <summary>
		/// Item Reference Number
		/// </summary>
        public long ItemRef { get; set; }

		/// <summary>
		/// Length of GS-1 Company Prefix (in digits)
		/// </summary>
        public int GS1CompanyPrefixLength { get; set; }
		/// <summary>
		/// Length of Item Reference (in digits)
		/// </summary>
        public int ItemRefLength { get; set; }

		/// <summary>
		/// Serial number
		/// </summary>
        public long SerialNr { get; set; }

		/// <summary>
		/// SGTIN URI, a representation suitable for storing in a database or sending over a network.
		/// </summary>
        public string URI {
            get {
                // Pure Identity URI
                var Cstr = GS1CompanyPrefix.ToString().PadLeft(GS1CompanyPrefixLength, '0');
                var Dstr = ItemRef.ToString().PadLeft(ItemRefLength, '0');
                var identityURI =
                    String.Format( // NOTE: can't be changed
                        "urn:epc:id:sgtin:{0}.{1}.{2}",
                        Cstr,
                        Dstr,
                        SerialNr
                    );
                return identityURI;
            }
        }

		/// <summary>
		/// Set the new serial number.
		/// </summary>
		/// <param name="serial">The new serial number</param>
        public void SetSerial(long serial) {
            this.SerialNr = serial;
        }
    }

	/// <summary>
	/// Tag EPC URI, Serialized GTIN (SGTIN) scheme.
	/// </summary>
    public struct TagEPCURI_SGTIN
    {
        public byte Filter { get; set; }
        public byte Attribs { get; set; }
        public IdentityEPCURI_SGTIN Identity { get; set; }
    }

	/// <summary>
	/// EPC Protocol Control information
	/// </summary>
	public struct EPC_PC
    {
        /// CRC-16
        public short Crc16 { get; set; }

        // the rest: protocol control info

        /// represents the number of 16-bit words comprising the PC field and the EPC field
        /// (full length of PC+EPC data, in bytes)
        public short PC_EPC_Length { get; set; }
        /// toggle
        /// 0: bits 0x18-0x1F contain attribute bits and the remainder of EPC bank contains a binary encoded EPC
        /// 1: bits 0x18-0x1F contain ISO AFI, and the remainder of EPC bank contains UII
        public bool Toggle { get; set; }
        /// Attribute bits: bits that may guide the handling of physical object to which the tag is affixed (Gen2 v1.x tags only)
        public byte Attribs { get; set; }
        /// UMI (User Memory Indicator): indicates if user memory bank is present
        public bool UMI { get; set; }
        /// XI: indicates if XPC is present
        public bool XI { get; set; }
        /// XPC: Extended Protocol Control
        public uint XPC { get; set; }

        // EPC TDS 1.9 15.2.4
        public void GetURIpart(StringBuilder builder) {
            if (!Toggle && Attribs > 0) {
                builder.Append("[att=x");
                builder.AppendFormat("{0:X2}", Attribs);
                builder.Append("]");
            }
            if (UMI) {
                builder.Append("[umi=1]");
            }
            if (XI && XPC > 0) {
                builder.Append("[xpc=x");
                builder.AppendFormat("{0:X4}", XPC);
                builder.Append("]");
            }
        }
    }

	/// <summary>
	/// Raw EPC memory bank contents (almost raw...).
	/// </summary>
    public struct TagRaw
    {
		/// <summary>
		/// Protocol control.
		/// </summary>
        public EPC_PC PC { get; set; }
        //public uint N { get; set; } // same as PC.PC_EPC_Length * 8
        //public uint A { get; set; }  // same as PC.Attribs; HasA = PC.Toggle
		/// <summary>
		/// The rest of memory bank contents, as-is.
		/// </summary>
        public byte[] V { get; set; }

		/// <summary>
		/// Get the Raw URI, a representation suitable for storing in a database or sending over a network.
		/// 
		/// Ref: EPC TDS 1.9, 15.2.1
		/// </summary>
		/// <param name="builder">the builder that will hold the data</param>
		public void GetURI(StringBuilder builder)
        {
            builder.Append("urn:epc:raw:");

            PC.GetURIpart(builder);

            builder.Append(PC.PC_EPC_Length * 8); // PC+EPC length in bits
            builder.Append('.');

            if (PC.Toggle && PC.Attribs > 0) {
                builder.Append('x');
                builder.AppendFormat("{0:X2}", PC.Attribs);
                builder.Append('.');
            }

            builder.Append('x');
			foreach (var b in V)
			{
				builder.AppendFormat("{0:X2}", b);
			}
        }
    }
}
