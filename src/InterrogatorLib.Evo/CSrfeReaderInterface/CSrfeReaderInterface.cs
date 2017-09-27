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

namespace CSrfeReaderInterface
{
    public static class Global
    {
        /// <summary>
        /// Instance of the used tracer. Change this to change the trace.
        /// </summary>
        public static CSrfeReaderInterface.trace.ITraceInterface m_tracer = null;

        /// <summary>
        /// Sends the given trace message with the trace level to the used trace instance.
        /// </summary>
        /// <param name="level">Trace level of the message</param>
        /// <param name="text">Trace message</param>
        public static void trc(int level, string text)
        {
            if (m_tracer != null)
                m_tracer.trc(level, text);
        }
    }

    public class Pair<T, U>
    {
        public Pair()
        {
        }

        public Pair(T first, U second)
        {
            this.First = first;
            this.Second = second;
        }

        public T First { get; set; }
        public U Second { get; set; }
    };
}
