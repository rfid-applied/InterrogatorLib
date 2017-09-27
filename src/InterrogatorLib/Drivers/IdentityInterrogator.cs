using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace InterrogatorLib
{
#if NET35_CF
	/// <summary>
	/// An interrogator that does nothing.
	/// </summary>
	public class IdentityInterrogator : IInterrogator
    {
        public IdentityInterrogator() { }

        public void Dispose() { }

        public bool StartInventory(byte[] AMask, int AMaskLength, int AOffset, int ATimeout)
        {
            return false;
        }

        public void StopInventory() { }

		// implemented like this to prevent CS0067
        public event EPCEventHandler EPCEvent {
			add { }
			remove { }
		}

        public int Power { get; set; }
    }
#endif
}
