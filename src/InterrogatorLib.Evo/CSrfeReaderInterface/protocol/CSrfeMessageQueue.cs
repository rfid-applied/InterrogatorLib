/*
 * Copyright (c) 2008-2013, RF-Embedded GmbH
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 *  1. Redistributions of source code must retain the above copyright notice, 
 *     this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright notice, 
 *     this list of conditions and the following disclaimer in the 
 *     documentation and/or other materials provided with the distribution.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY 
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT 
 * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT 
 * OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR 
 * TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS 
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CSrfeReaderInterface.protocol
{
    /// <summary>
    /// Class to store recieved messages.
    /// </summary>
    class CSrfeMessageQueue : IDisposable
    {
        /// <summary>
        /// The stored messages
        /// </summary>
        private Dictionary<int, byte[]> m_messageQueue;

        /// <summary>
        /// Mutex to secure the access
        /// </summary>
        private Mutex m_mutex;

        /// <summary>
        /// Constructs a new message queue
        /// </summary>
        public CSrfeMessageQueue()
        {
            m_messageQueue = new Dictionary<int, byte[]>();
            m_mutex = new Mutex();
        }

        public void Dispose()
        {
            if (m_mutex != null)
            {
                m_mutex.Close();
                m_mutex = null;
            }
        }

        /// <summary>
        /// Enqueues the given message with the given id. If a message with this id is already present, it is replaced.
        /// </summary>
        /// <param name="id">Message id</param>
        /// <param name="message">Message</param>
        public void enqueueMessage(int id, byte[] message)
        {
            m_mutex.WaitOne();
            m_messageQueue.Remove(id);
            m_messageQueue.Add(id, message);
            m_mutex.ReleaseMutex();
        }

        /// <summary>
        /// Waits for a message with the given message id for the given time.
        /// </summary>
        /// <param name="id">Message id to wait for</param>
        /// <param name="msecs">Maximum time to wait for the message</param>
        /// <param name="ok">Result of the waiting</param>
        /// <returns>The message, if the operation did not succeeed, null is returned.</returns>
        public byte[] waitForMessage(int id, int msecs, out bool ok)
        {
            int i = 0;
            int cycles = msecs / 10;

            ok = false;

            // check for the flag for the given time
            while (i++ < cycles)
            {
                m_mutex.WaitOne();
                ok = m_messageQueue.ContainsKey(id);
                m_mutex.ReleaseMutex();

                if (ok == true)
                    break;

                Thread.Sleep(10);
            }

            byte[] ret;

            m_mutex.WaitOne();
            m_messageQueue.TryGetValue(id, out ret);
            m_messageQueue.Remove(id);
            m_mutex.ReleaseMutex();

            return ret;
        }

    }
}
