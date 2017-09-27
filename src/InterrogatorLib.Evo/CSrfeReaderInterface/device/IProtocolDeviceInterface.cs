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

namespace CSrfeReaderInterface.device
{

    /// <summary>
    /// Interface which is used by the protocol handler to communicate with the reader.
    /// </summary>
    public abstract class IProtocolDeviceInterface
    {
        /// <summary>
        /// Opens the device
        /// </summary>
        /// <returns>true if opening was successful, otherwise false</returns>
        abstract public bool Open();

        /// <summary>
        /// Closes the device
        /// </summary>
        /// <returns>true if closing was successful, otherwise false</returns>
        abstract public bool Close();

        /// <summary>
        /// Sends the given bytes to the device
        /// </summary>
        /// <param name="data">Data to be sent</param>
        /// <returns></returns>
        abstract public bool Send(byte[] data);

        /// <summary>
        /// Delegate is called if data is received.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="data">Received data</param>
        public delegate void DataReadHandler(object sender, byte[] data);

        /// <summary>
        /// Event is emitted if data is received.
        /// </summary>
        public event DataReadHandler DataRead;

        /// <summary>
        /// Raises the DataRead event 
        /// </summary>
        /// <param name="data">The read data</param>
        protected void RaiseDataReadEvent(byte[] data)
        {
            if (DataRead != null)
                DataRead(this, data);
        }
    }
}
