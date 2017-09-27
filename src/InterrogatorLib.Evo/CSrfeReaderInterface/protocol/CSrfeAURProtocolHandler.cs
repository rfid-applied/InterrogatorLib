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
using CSrfeReaderInterface.protocol;

namespace CSrfeReaderInterface.rfe.protocol
{
    public class CSrfeAURprotocolHandler : CSrfeProtocolHandler
    {
        /// <summary>
        /// Constructs an instnace of the protocol handler for the given device.
        /// </summary>
        /// <param name="device">Device to which the reader is connected</param>
        public CSrfeAURprotocolHandler(device.IProtocolDeviceInterface device)
            : base(device)
        {
        }

        #region Tag-Functions

        public bool acknowledgeTag(byte[] tagId)
        {
            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add((byte)tagId.Count());
            payloadList.AddRange(tagId);
            payload = payloadList.ToArray();

            byte[] resultData;

            if (!customTagCommand((byte)0x01, payload, out resultData))
                return false;

            return true;
        }

        #endregion
    }
}
