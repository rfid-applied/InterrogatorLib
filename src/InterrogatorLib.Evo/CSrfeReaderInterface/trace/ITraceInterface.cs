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

namespace CSrfeReaderInterface.trace
{
    /// <summary>
    /// This abstract class provides a trace interface for the RFE-Protocol Handler. To change the way of 
    /// tracing/logging just subclass this class and change the used instance.
    /// </summary>
    public abstract class ITraceInterface
    {
        /// <summary>
        /// Property that holds the used trace level.
        /// If the specified trace level is lesser than or the same as the specified trace level, the trace will be written.
        /// </summary>
        public int TraceLevel { get; set; }

        /// <summary>
        /// Writes the given trace message after verifying the trace level.
        /// </summary>
        /// <param name="level">Trace level of the message</param>
        /// <param name="text">Trace message</param>
        public void trc(int level, string text)
        {
            if (level > TraceLevel)
                return;

            string msg = "[" + DateTime.Now.ToString("HH:mm:ss.ffff") + "] - " + text;
            write(msg);
        }

        /// <summary>
        /// Abstract method to write to a specific output.
        /// </summary>
        /// <param name="text">Trace message</param>
        public abstract void write(string text);
    }
}
