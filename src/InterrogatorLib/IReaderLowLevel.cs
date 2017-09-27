using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterrogatorLib
{
	/// <summary>
	/// The low-level reader interface. Every tag is conceptually comprised of N memorybanks, each being a memory buffer.
	/// </summary>
    public interface IReaderLowLevel : IDisposable
    {
        /// do not call any other methods prior to initialization (IsReady
        /// has to be true)
        void Initialize();
        bool IsReady();

        /// I/O chunk size, in bytes (must be even number!)
        /// or &lt; 0 if chunking is not to be used
        int ChunkSize { get; }

		/// <summary>
		/// Try to singulate a tag.
		/// </summary>
		/// <returns>An opaque handle to the singulated tag (if non-null)</returns>
        object SingulateTag();
		/// <summary>
		/// Given a tag, and a memory bank index, read [count] bytes starting at [offset], into [arr]
		/// </summary>
		/// <param name="tag">The opaque tag handle. Only pass objects given by [SingulateTag]</param>
		/// <param name="membank">The index of memory bank to read from (0: RESERVED, 1: TID, 2: EPC, 3: USER)</param>
		/// <param name="offset">Offset (in bytes) to read from</param>
		/// <param name="count">Count (in bytes) to read</param>
		/// <param name="arr">Array to hold the results</param>
		/// <returns>True if succeeded</returns>
		bool ReadBytes(object tag, int membank, int offset, int count, out byte[] arr);
		/// <summary>
		/// Copy [count] bytes of [arr] into membank at [offset].
		/// </summary>
		/// <param name="tag">The opaque tag handle. Only pass objects given by [SingulateTag]</param>
		/// <param name="membank">The index of memory bank to read from (0: RESERVED, 1: TID, 2: EPC, 3: USER)</param>
		/// <param name="offset">Offset (in bytes) to write to (in memory bank, not the array)</param>
		/// <param name="count">Count (in bytes) to write</param>
		/// <param name="arr">Array with memory (should hold at least [count] bytes!)</param>
		/// <returns>True if succeeded</returns>
		bool WriteBytes(object tag, int membank, int offset, int count, byte[] arr);
    }
}
