using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InterrogatorLib
{
    public enum MemoryBankType
    {
        Other = -1,
        Reserved = 0,
        EPC = 1,
        TID = 2,
        USER = 3
    };

    public class EPCEventArgs : EventArgs
    {
        /// <summary>EPC code. </summary>
        public string[] EPC { get; set; }
    }

    public delegate void EPCEventHandler(object sender, EPCEventArgs args);

    public interface IInterrogator : IDisposable
    {
        /// <summary>
        /// Start asynchronous inventory process.
        /// </summary>
        /// <param name="AMask">Selection mask</param>
        /// <param name="AMaskLength">Length of AMask, in bytes</param>
        /// <param name="AOffset">Start position AMask filter will be applied to, in bytes</param>
        /// <param name="ATimeout">Timeout, in seconds</param>
        /// <returns>True on success, false on failure</returns>
        /// <example>
        /// // starts an inventory process, filtering no tags
        /// // will time out after 1 sec interval
        /// StartInventory(new byte[] { 0 }, 0, 0, 1);
        /// </example>
        bool StartInventory(byte[] AMask, int AMaskLength, int AOffset, int ATimeout);

        /// <summary>
        /// Stops running inventory process. If no inventory is running, does nothing.
        /// </summary>
        void StopInventory();

        /// <summary>
        /// Fires on each tag detected during inventory process.
        /// May contain duplicates.
        /// </summary>
        event EPCEventHandler EPCEvent;

//        bool IsBusy { get; }
        int Power { get; set; }
    }
}
