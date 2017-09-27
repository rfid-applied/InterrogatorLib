using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace InterrogatorLib
{
    public class MockTag
    {
        // NOTE: verbatim memory contents!
        public byte[] TID { get; set; }
        public byte[] EPC { get; set; }
        public byte[] User { get; set; }
    }

	/// <summary>
	/// Mock low-level reader.
	/// </summary>
    public class MockReaderLowLevel : IReaderLowLevel
    {
        int _currentTag = 0;

        public void Dispose()
        {
            // nop
        }

        public MockReaderLowLevel(IEnumerable<byte[]> epcs)
        {
            var lst = new List<MockTag>();
            foreach (var epc in epcs)
            {
                var tag = new MockTag();
                tag.EPC = epc;
                lst.Add(tag);
            }
            _tags = lst.ToArray();
        }

        public MockReaderLowLevel(string[] hexstringtags)
        {
            _tags = new MockTag[hexstringtags.Length];
            var i = 0;
            foreach (var hex in hexstringtags)
            {
                var s = hex.Split(';');
                var tag = new MockTag();
                tag.TID = StringToByteArray(s[0]);
                if (s.Length >= 2 && !String.IsNullOrEmpty(s[1]))
                {
                    tag.EPC = StringToByteArray(s[1]);
                }
                if (s.Length >= 3 && !String.IsNullOrEmpty(s[2]))
                {
                    tag.User = StringToByteArray(s[2]);
                }
                _tags[i] = tag;
                i++;
            }
        }
        public MockReaderLowLevel(MockTag[] mocktags)
        {
            _tags = mocktags;
        }
        MockTag[] _tags;

        public static IEnumerable<string> SplitToLines(string input)
        {
            if (input == null)
            {
                yield break;
            }

            using (System.IO.StringReader reader = new System.IO.StringReader(input))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        public byte[] StringToByteArray(string hex)
        {
            hex = hex.Replace("-", "");
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public object SingulateTagAssisted(int tagNr)
        {
            return _tags[tagNr];
        }

        public int ChunkSize
        {
            get { return -1; } // do not use chunking
        }

        public void Initialize()
        {
            // nop
        }

        public bool IsReady()
        {
            return true;
        }

        // try to singulate a tag
        public object SingulateTag()
        {
            if (_tags.Length == 0)
                return null;
            if (_currentTag > _tags.Length)
                _currentTag = 0; // wrap-around
            var res = _tags[_currentTag];
            _currentTag++;
            return res;
        }

        public static T[] SubArray<T>(T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        // only pass objects given by [SingulateTag]
        public bool ReadBytes(object tag0, int membank, int offset, int count, out byte[] arr)
        {
            var tag = (MockTag)tag0;

            arr = null;

            byte[] src = null;
            switch (membank)
            {
                case 0x0:
                    return false; // not implemented
                case 0x1: // EPC
                    src = tag.EPC;
                    break;
                case 0x2: // TID
                    src = tag.TID;
                    break;
                case 0x3: // USER
                    src = tag.User;
                    break;
                default:
                    return false;
            }
            if (src == null)
                return false;

            if (offset < 0)
                return false;
            if (offset + count > src.Length+1)
                return false;
            arr = SubArray<byte>(src, offset, count);
            return true;
        }

        public bool WriteBytes(object tag0, int membank, int offset, int count, byte[] arr)
        {
            var tag = (MockTag)tag0;

            byte[] src = null;
            switch (membank)
            {
                case 0x0:
                    return false; // not implemented
                case 0x1: // EPC
                    src = tag.EPC;
                    break;
                case 0x2: // TID
                    src = tag.TID;
                    break;
                case 0x3: // USER
                    src = tag.User;
                    break;
                default:
                    return false;
            }
            if (src == null)
                return false;

            if (offset < 0)
                return false;
            if (offset + count > src.Length+1)
                return false;
            Buffer.BlockCopy(src, offset, arr, 0, count);
            return true;
        }
    }
}
