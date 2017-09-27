using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace InterrogatorLib
{
    public class Utility
    {
		/// <summary>
		/// Converts a string containing hexadecimal characters to the corresponding byte array.
		/// Inverse: `ByteArrayToHexString`
		/// </summary>
		/// <param name="s">input string</param>
		/// <returns></returns>
		public static byte[] HexStringToByteArray(string s)
		{
			s = s.Replace(" ", "").Replace("-", "");
			byte[] buffer = new byte[s.Length / 2];
			for (int i = 0; i < s.Length; i += 2)
				buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
			return buffer;
		}

		/// <summary>
		/// Converts an array of bytes to the corresponding string containing hexadecimal characters.
		/// Inverse: `HexStringToByteArray`
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static string ByteArrayToHexString(byte[] data)
		{
			StringBuilder sb = new StringBuilder(data.Length * 3);
			foreach (byte b in data)
				sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
			return sb.ToString().ToUpper().Replace("-", "");
		}
	}
}
