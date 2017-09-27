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

namespace CSrfeReaderInterface.protocol
{
    /// <summary>
    /// This class implements the RF-Embedded Reader-Host-Protocol.
    /// It provides each protocol command in an own function. All functions are blocking and wait for the response of the reader.
    /// </summary>
    public class CSrfeProtocolHandler
    {
        public class TagEvent
        {
            public byte[] tagId;

            public bool hasAntenna;
            public byte antennaId;

            public bool hasRSSI;
            public byte[] rssi;

            public bool hasReadFrequency;
            public ulong readFrequency;

            public bool hasMemory;
            public byte memBank;
            public ushort memAddr;
            public byte[] memData;

            public bool hasTrigger;
            public byte trigger;

            public bool hasHandle;
            public byte[] handle;

            public bool hasState;
            public ushort state;

            public bool hasBattery;
            public byte battery;

            public TagEvent()
            {
                tagId = new byte[0];
                hasAntenna = false;
                hasRSSI = false;
                hasReadFrequency = false;
                hasMemory = false;
                hasTrigger = false;
                hasHandle = false;
                hasState = false;
                hasBattery = false;
            }

            public TagEvent(TagEvent other)
            {
                tagId = new byte[other.tagId.Length];
                other.tagId.CopyTo(tagId, 0);

                hasAntenna = other.hasAntenna;
                if (hasAntenna)
                {
                    antennaId = other.antennaId;
                }

                hasRSSI = other.hasRSSI;
                if (hasRSSI)
                {
                    rssi = new byte[other.rssi.Length];
                    other.rssi.CopyTo(rssi, 0);
                }

                hasReadFrequency = other.hasReadFrequency;
                if (hasReadFrequency)
                {
                    readFrequency = other.readFrequency;
                }

                hasMemory = other.hasMemory;
                if (hasMemory)
                {
                    memBank = other.memBank;
                    memAddr = other.memAddr;
                    memData = new byte[other.memData.Length];
                    other.memData.CopyTo(memData, 0);
                }

                hasTrigger = other.hasTrigger;
                if (hasTrigger)
                {
                    trigger = other.trigger;
                }

                hasHandle = other.hasHandle;
                if (hasHandle)
                {
                    handle = new byte[other.handle.Length];
                    other.handle.CopyTo(handle, 0);
                }

                hasState = other.hasState;
                if (hasState)
                {
                    state = other.state;
                }

                hasBattery = other.hasBattery;
                if (hasBattery)
                {
                    battery = other.battery;
                }
            }
        };

        /// <summary>
        /// Private member to store received messages according to the message id.
        /// </summary>
        private CSrfeMessageQueue _MessageQueue;

        /// <summary>
        /// Private member to store received pending messages according to the message id.
        /// </summary>
        private CSrfeMessageQueue _PendingMessageQueue;

        /// <summary>
        /// Instance of the device that is used to communicate with the reader. 
        /// </summary>
        private device.IProtocolDeviceInterface _Device;

        /// <summary>
        /// The last return code of the reader.
        /// </summary>
        private Constants.eRFE_RET_VALUE _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

        /// <summary>
        /// The time out in ms to wait for the response of the reader.
        /// </summary>
        private int _ResponseTimeOut = 1000;

        /// <summary>
        /// Option to block interrupts from cyclic inventory.
        /// </summary>
        private bool _BlockCyclicInventoryInterrupts = false;


        /// <summary>
        /// Getter and setter for the response time out.
        /// </summary>
        public int ResponseTimeOut
        {
            get { return _ResponseTimeOut; }
            set { _ResponseTimeOut = value; }
        }

        /// <summary>
        /// Getter for the last return code of the raeder.
        /// </summary>
        public Constants.eRFE_RET_VALUE LastReturnCode
        {
            get { return _LastReturnCode; }
        }

        /// <summary>
        /// Getter and setter for blocking interrupts.
        /// </summary>
        public bool BlockCyclicInventoryInterrupts
        {
            get { return _BlockCyclicInventoryInterrupts; }
            set { _BlockCyclicInventoryInterrupts = value; }
        }


        /// <summary>
        /// Constructs an instnace of the protocol handler for the given device.
        /// </summary>
        /// <param name="device">Device to which the reader is connected</param>
        public CSrfeProtocolHandler(device.IProtocolDeviceInterface device)
        {
            _Device = device;
            _Device.DataRead += new device.IProtocolDeviceInterface.DataReadHandler(parseData);

            _MessageQueue = new CSrfeMessageQueue();
            _PendingMessageQueue = new CSrfeMessageQueue();
        }

        public void Dispose()
        {
            if (_Device != null)
            {
                _Device.Close();
                _Device.DataRead -= new device.IProtocolDeviceInterface.DataReadHandler(parseData);
                _Device = null;
            }
            if (_MessageQueue != null)
            {
                _MessageQueue.Dispose();
                _MessageQueue = null;
            }
            if (_PendingMessageQueue != null)
            {
                _PendingMessageQueue.Dispose();
                _PendingMessageQueue = null;
            }
        }


        #region Commands

        #region Reader-Common

        /// <summary>
        /// Retrieves the reader ID of the reader.
        /// </summary>
        /// <param name="readerID">Retrieved reader ID</param>
        /// <returns>Succes of the operation</returns>
        public bool getReaderID(out uint readerID)
        {
            Global.trc(3, "Get Reader ID - Trying to get Reader ID");

            readerID = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON, Constants.RFE_COM2_GET_SERIAL_NUMBER);
            if (!res)
            {
                Global.trc(3, "Get Reader ID - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_SERIAL_NUMBER), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Reader ID - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 4)
            {
                Global.trc(3, "Get Reader ID - NOK - Payl");
                return false;
            }

            readerID = 0;
            readerID |= (((uint)payl[0]) << 24);
            readerID |= (((uint)payl[1]) << 16);
            readerID |= (((uint)payl[2]) << 8);
            readerID |= (uint)payl[3];

            Global.trc(3, "Get Reader ID - OK : Reader ID = " + String.Format("{0:X08}", readerID));

            return true;
        }

        /// <summary>
        /// Retrieves the reader type of the reader.
        /// </summary>
        /// <param name="readerType">Retrieved reader type</param>
        /// <returns>Succes of the operation</returns>
        public bool getReaderType(out uint readerType)
        {
            Global.trc(3, "Get Reader Type - Trying to get Reader Type");

            readerType = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_READER_TYPE);
            if (!res)
            {
                Global.trc(3, "Get Reader Type - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_READER_TYPE), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Reader Type - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 4)
            {
                Global.trc(3, "Get Reader Type - NOK - Payl");
                return false;
            }

            readerType = 0;
            readerType |= (((uint)payl[0]) << 24);
            readerType |= (((uint)payl[1]) << 16);
            readerType |= (((uint)payl[2]) << 8);
            readerType |= (uint)payl[3];

            Global.trc(3, "Get Reader Type - OK : Reader Type = " + String.Format("{0:X08}", readerType));

            return true;
        }

        /// <summary>
        /// Retrieves the hardware revision of the reader.
        /// </summary>
        /// <param name="hardwareRevision">Retrieved hardware revision</param>
        /// <returns>Succes of the operation</returns>
        public bool getHardwareRevision(out uint hardwareRevision)
        {
            Global.trc(3, "Get Hardware Rev - Trying to get hardware ID");

            hardwareRevision = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_HARDWARE_REVISION);
            if (!res)
            {
                Global.trc(3, "Get Hardware Rev - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_HARDWARE_REVISION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Hardware Rev - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 4)
            {
                Global.trc(3, "Get Hardware Rev - NOK - Payl");
                return false;
            }

            // set the variable to the new value
            hardwareRevision = 0;
            hardwareRevision |= (((uint)payl[0]) << 24);
            hardwareRevision |= (((uint)payl[1]) << 16);
            hardwareRevision |= (((uint)payl[2]) << 8);
            hardwareRevision |= (uint)payl[3];

            Global.trc(3, "Get Hardware Rev - OK : Hardware Revision = " + String.Format("{0:X08}", hardwareRevision));

            return true;
        }

        /// <summary>
        /// Retrieves the software revision of the reader.
        /// </summary>
        /// <param name="softwareRevision">Retrieved software revision</param>
        /// <returns>Succes of the operation</returns>
        public bool getSoftwareRevision(out uint softwareRevision)
        {
            Global.trc(3, "Get Software Rev - Trying to get software ID");

            softwareRevision = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_SOFTWARE_REVISION);
            if (res != true)
            {
                Global.trc(3, "Get Software Rev - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_SOFTWARE_REVISION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Software Rev - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 4)
            {
                Global.trc(3, "Get Software Rev - NOK - Payl");
                return false;
            }

            // set the variable to the new value
            softwareRevision = 0;
            softwareRevision |= (((uint)payl[0]) << 24);
            softwareRevision |= (((uint)payl[1]) << 16);
            softwareRevision |= (((uint)payl[2]) << 8);
            softwareRevision |= (uint)payl[3];

            Global.trc(3, "Get Software Rev - OK : Software Revision = " + String.Format("{0:X08}", softwareRevision));

            return true;
        }

        /// <summary>
        /// Retrieves the bootloader revision of the reader.
        /// </summary>
        /// <param name="bootloaderRevision">Retrieved bootloader revision</param>
        /// <returns>Succes of the operation</returns>
        public bool getBootloaderRevision(out uint bootloaderRevision)
        {
            Global.trc(3, "Get Bootloader Rev - Trying to get bootloader ID");

            bootloaderRevision = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_BOOTLOADER_REVISION);
            if (res != true)
            {
                Global.trc(3, "Get Bootloader Rev - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_BOOTLOADER_REVISION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Bootloader Rev - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 4)
            {
                Global.trc(3, "Get Bootloader Rev - NOK - Payl");
                return false;
            }

            // set the variable to the new value
            bootloaderRevision = 0;
            bootloaderRevision |= (((uint)payl[0]) << 24);
            bootloaderRevision |= (((uint)payl[1]) << 16);
            bootloaderRevision |= (((uint)payl[2]) << 8);
            bootloaderRevision |= (uint)payl[3];

            Global.trc(3, "Get Bootloader Rev - OK : Bootloader Revision = " + String.Format("{0:X08}", bootloaderRevision));

            return true;
        }

        /// <summary>
        /// Retrieves the current running system of the reader.
        /// </summary>
        /// <param name="currentSystem">The current running sytem of the reader</param>
        /// <returns>Succes of the operation</returns>
        public bool getCurrentSystem(out Constants.eRFE_CURRENT_SYSTEM currentSystem)
        {
            Global.trc(3, "Get Current System - Trying to get current System");

            currentSystem = Constants.eRFE_CURRENT_SYSTEM.RFE_SYS_FIRMWARE;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_CURRENT_SYSTEM);
            if (res != true)
            {
                Global.trc(3, "Get Current System - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_CURRENT_SYSTEM), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Current System - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 1)
            {
                Global.trc(3, "Get Current System - NOK - Payl");
                return false;
            }

            currentSystem = (Constants.eRFE_CURRENT_SYSTEM)payl[0];

            Global.trc(3, "Get Current System - OK : System " + currentSystem.ToString());

            return true;
        }

        /// <summary>
        /// Retrieves the current state of the reader.
        /// </summary>
        /// <param name="currentState">Current state of the reader</param>
        /// <returns>Succes of the operation</returns>
        public bool getCurrentState(out Constants.eRFE_CURRENT_READER_STATE currentState)
        {
            Global.trc(3, "Get Current State - Trying to get current State");

            currentState = Constants.eRFE_CURRENT_READER_STATE.RFE_STATE_IDLE;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_CURRENT_STATE);
            if (res != true)
            {
                Global.trc(3, "Get Current State - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_CURRENT_STATE), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Current State - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 1)
            {
                Global.trc(3, "Get Current State - NOK - Payl");
                return false;
            }

            currentState = (Constants.eRFE_CURRENT_READER_STATE)payl[0];

            Global.trc(3, "Get Current State - OK : State " + currentState.ToString());

            return true;
        }

        /// <summary>
        /// Retrieves the status register of the reader.
        /// </summary>
        /// <param name="statusRegister">Status register of the reader</param>
        /// <returns>Succes of the operation</returns>
        public bool getStatusRegister(out ulong statusRegister)
        {
            Global.trc(3, "Get Status Register - Trying to get current State");

            statusRegister = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_STATUS_REGISTER);
            if (res != true)
            {
                Global.trc(3, "Get Status Register - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_STATUS_REGISTER), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Status Register - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 8)
            {
                Global.trc(3, "Get Status Register - NOK - Payl");
                return false;
            }

            statusRegister = 0;
            statusRegister |= (((ulong)payl[0]) << 56);
            statusRegister |= (((ulong)payl[1]) << 48);
            statusRegister |= (((ulong)payl[2]) << 40);
            statusRegister |= (((ulong)payl[3]) << 32);
            statusRegister |= (((ulong)payl[4]) << 24);
            statusRegister |= (((ulong)payl[5]) << 16);
            statusRegister |= (((ulong)payl[6]) << 8);
            statusRegister |= (ulong)payl[7];

            Global.trc(3, "Get Status Register - OK : Status Register = " + String.Format("{0:X016}", statusRegister));

            return true;
        }

        /// <summary>
        /// Retrieves the antenna count of the reader.
        /// </summary>
        /// <param name="count">Antenna count of the reader</param>
        /// <returns>Succes of the operation</returns>
        public bool getAntennaCount(out byte count)
        {
            Global.trc(3, "Get Antenna Count - Trying to get antenna count");

            count = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_ANTENNA_COUNT);
            if (res != true)
            {
                Global.trc(3, "Get Antenna Count - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_COMMON,
                    Constants.RFE_COM2_GET_ANTENNA_COUNT), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Antenna Count - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 1)
            {
                Global.trc(3, "Get Antenna Count - NOK - Payl");
                return false;
            }

            count = (byte)payl[0];

            Global.trc(3, "Get Antenna Count - OK : Count " + String.Format("{0}", count));

            return true;
        }

        #endregion Reader-Common

        #region Reader-RF

        /// <summary>
        /// Retrieves the attenuation settings of the reader.
        /// </summary>
        /// <param name="maxAttenuation">Maximum settable value for the attenuation</param>
        /// <param name="currentAttenuation">Current set value for the attenuation</param>
        /// <returns>Succes of the operation</returns>
        public bool getAttenuation(out ushort maxAttenuation, out ushort currentAttenuation)
        {
            Global.trc(3, "Get Attenuation - Trying to attenuation");

            maxAttenuation = currentAttenuation = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_ATTENUATION);
            if (res != true)
            {
                Global.trc(3, "Get Attenuation - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_ATTENUATION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Attenuation - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 5 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Attenuation - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            // set the variable to the new value
            maxAttenuation = 0;
            maxAttenuation |= (ushort)(((ushort)payl[1]) << 8);
            maxAttenuation |= (ushort)payl[2];

            currentAttenuation = 0;
            currentAttenuation |= (ushort)(((ushort)payl[3]) << 8);
            currentAttenuation |= (ushort)payl[4];

            Global.trc(3, "Get Attenuation - OK : Max(" + String.Format("{0}", maxAttenuation) +
                                         ") - Cur(" + String.Format("{0}", currentAttenuation) + ")");

            return true;
        }

        /// <summary>
        /// Retrieves the frequency table and the current set mode of the reader.
        /// </summary>
        /// <param name="mode">Current used mode</param>
        /// <param name="maxFrequencyCount">Maximum count of frequency table entries</param>
        /// <param name="frequencies">Frequency table</param>
        /// <returns>Succes of the operation</returns>
        public bool getFrequency(out byte mode, out byte maxFrequencyCount, out List<uint> frequencies)
        {
            Global.trc(3, "Get Frequency - Trying to frequency");

            mode = maxFrequencyCount = 0;
            frequencies = new List<uint>();

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_FREQUENCY);
            if (res != true)
            {
                Global.trc(3, "Get Frequency - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_FREQUENCY), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Frequency - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length < 5 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Frequency - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            mode = payl[1];
            maxFrequencyCount = payl[2];
            frequencies.Clear();
            byte count = payl[3];
            byte index = 4;
            for (int i = 0; i < count; i++)
            {
                uint freq = 0;
                freq |= (((uint)payl[index++]) << 16);
                freq |= (((uint)payl[index++]) << 8);
                freq |= (uint)payl[index++];

                frequencies.Add(freq);
            }

            Global.trc(3, "Get Frequency - OK : Mode(" + String.Format("{0}", mode) + ") - Max(" + String.Format("{0}", maxFrequencyCount) + ")");
            foreach (uint f in frequencies)
                Global.trc(3, "Get Frequency - OK :     Freq(" + String.Format("{0}", f) + "kHz)");

            return true;
        }

        /// <summary>
        /// Retrieves the sensitivity settings of the reader.
        /// </summary>
        /// <param name="maxSensitivity">Maximum settable sensitivity</param>
        /// <param name="minSensitivity">Mininim settable sensitivity</param>
        /// <param name="currentSensitivity">Current set sensitivity</param>
        /// <returns>Succes of the operation</returns>
        public bool getSensitivity(out short maxSensitivity, out short minSensitivity, out short currentSensitivity)
        {
            Global.trc(3, "Get Sensitivity - Trying to get Sensitivity");

            maxSensitivity = minSensitivity = currentSensitivity = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_SENSITIVITY);
            if (res != true)
            {
                Global.trc(3, "Get Sensitivity - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_SENSITIVITY), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Sensitivity - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 7 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Sensitivity - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            // set the variable to the new value
            maxSensitivity = 0;
            maxSensitivity |= (short)(((short)payl[1]) << 8);
            maxSensitivity |= (short)payl[2];

            minSensitivity = 0;
            minSensitivity |= (short)(((short)payl[3]) << 8);
            minSensitivity |= (short)payl[4];

            currentSensitivity = 0;
            currentSensitivity |= (short)(((short)payl[5]) << 8);
            currentSensitivity |= (short)payl[6];

            Global.trc(3, "Get Sensitivity - OK : Max(" + String.Format("{0}", maxSensitivity) +
                                         ") - Min(" + String.Format("{0}", minSensitivity) +
                                         ") - Cur" + String.Format("{0}", currentSensitivity) + ")");

            return true;
        }

        /// <summary>
        /// Retrieves the LBT params of the reader.
        /// </summary>
        /// <param name="listenTime">Listen time in msecs</param>
        /// <param name="idleTime">Idle time in msecs</param>
        /// <param name="maxAllocTime">Maximum allocation time in msecs</param>
        /// <param name="rssiThreshold">RSSI threshold</param>
        /// <returns>Succes of the operation</returns>
        public bool getLbtParams(out ushort listenTime, out ushort idleTime, out ushort maxAllocTime, out short rssiThreshold)
        {
            Global.trc(3, "Get Lbt Params - Trying to get Lbt Params");

            listenTime = idleTime = maxAllocTime = 0;
            rssiThreshold = 0;

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_LBT_PARAMS);
            if (res != true)
            {
                Global.trc(3, "Get Lbt Params - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_GET_LBT_PARAMS), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Lbt Params - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length != 9 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Lbt Params - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            // set the variable to the new value
            listenTime = 0;
            listenTime |= (ushort)(((ushort)payl[1]) << 8);
            listenTime |= (ushort)payl[2];

            idleTime = 0;
            idleTime |= (ushort)(((ushort)payl[3]) << 8);
            idleTime |= (ushort)payl[4];

            maxAllocTime = 0;
            maxAllocTime |= (ushort)(((ushort)payl[5]) << 8);
            maxAllocTime |= (ushort)payl[6];

            rssiThreshold = 0;
            rssiThreshold |= (short)(((short)payl[7]) << 8);
            rssiThreshold |= (short)payl[8];

            Global.trc(3, "Get Lbt Params - OK : Listen Time " + String.Format("{0}", listenTime) +
                                         " Idle Time " + String.Format("{0}", idleTime) +
                                         " Maximum Allocation Time " + String.Format("{0}", maxAllocTime) +
                                         " RSSI Threshold " + String.Format("{0}", rssiThreshold));

            return true;
        }


        /// <summary>
        /// Sets the attenuation setting of the reader
        /// </summary>
        /// <param name="value">The new attenuation value</param>
        /// <returns>Succes of the operation</returns>
        public bool setAttenuation(ushort value)
        {
            Global.trc(3, "Set Attenuation - Trying to set output power to " + String.Format("{0}", value));

            byte[] payload;
            payload = new byte[2];
            payload[0] = (byte)(value >> 8);
            payload[1] = (byte)(value);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_ATTENUATION, payload);
            if (res != true)
            {
                Global.trc(3, "Set Attenuation - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_ATTENUATION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Attenuation - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Attenuation - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Set Attenuation - OK");

            return true;
        }

        /// <summary>
        /// Sets the frequency table of the reader
        /// </summary>
        /// <param name="mode">The mode to hop through the table</param>
        /// <param name="frequencies">The new frequency table</param>
        /// <returns>Succes of the operation</returns>
        public bool setFrequency(byte mode, List<uint> frequencies)
        {
            Global.trc(3, "Set Frequency - Trying to set frequency");

            List<byte> payloadList = new List<byte>();
            payloadList.Add(mode);
            payloadList.Add((byte)frequencies.Count);

            for (int i = 0; i < frequencies.Count; i++)
            {
                payloadList.Add((byte)(frequencies[i] >> 16));
                payloadList.Add((byte)(frequencies[i] >> 8));
                payloadList.Add((byte)(frequencies[i]));
            }
            byte[] payload = payloadList.ToArray();

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_FREQUENCY, payload);
            if (res != true)
            {
                Global.trc(3, "Set Frequency - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_FREQUENCY), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Frequency - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Frequency - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            Global.trc(3, "Set Frequency - OK");

            return true;
        }

        /// <summary>
        /// Sets the sensitivity setting of the reader
        /// </summary>
        /// <param name="targetValue">The targeted sensitivity value</param>
        /// <param name="actualValue">The actual set sensitivity value</param>
        /// <returns>Succes of the operation</returns>
        public bool setSensitivity(short targetValue, out short actualValue)
        {
            Global.trc(3, "Set Sensitivity - Trying to set sensitvity to " + String.Format("{0}", targetValue));

            actualValue = 0;

            byte[] payload;
            payload = new byte[2];
            payload[0] = (byte)(targetValue >> 8);
            payload[1] = (byte)(targetValue);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_SENSITIVITY, payload);
            if (res != true)
            {
                Global.trc(3, "Set Sensitivity - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_SENSITIVITY), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Sensitivity - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Sensitivity - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            actualValue = 0;
            actualValue |= (short)(((short)payl[1]) << 8);
            actualValue |= (short)payl[2];

            Global.trc(3, "Set Sensitivity - OK : Set to " + String.Format("{0}", actualValue));

            return true;
        }

        /// <summary>
        /// Sets the LBT params of the reader
        /// </summary>
        /// <param name="listenTime">Listen time in msecs</param>
        /// <param name="idleTime">Idle time in msecs</param>
        /// <param name="maxAllocTime">Maximum allocation time in msecs</param>
        /// <param name="rssiThreshold">RSSI threshold</param>
        /// <returns>Succes of the operation</returns>
        public bool setLbtParams(ushort listenTime, ushort idleTime, ushort maxAllocTime, short rssiThreshold)
        {
            Global.trc(3, "Set LBT-Params - Trying to set lbt params: Listen Time " + String.Format("{0}", listenTime) +
                                                                        " Idle Time " + String.Format("{0}", idleTime) +
                                                                        " Maximum Allocation Time " + String.Format("{0}", maxAllocTime) +
                                                                        " RSSI Threshold " + String.Format("{0}", rssiThreshold));

            byte[] payload;
            payload = new byte[2];
            payload[0] = (byte)(listenTime >> 8);
            payload[1] = (byte)(listenTime);
            payload[2] = (byte)(idleTime >> 8);
            payload[3] = (byte)(idleTime);
            payload[4] = (byte)(maxAllocTime >> 8);
            payload[5] = (byte)(maxAllocTime);
            payload[6] = (byte)(rssiThreshold >> 8);
            payload[7] = (byte)(rssiThreshold);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_LBT_PARAMS, payload);
            if (res != true)
            {
                Global.trc(3, "Set LBT-Params - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_RF,
                    Constants.RFE_COM2_SET_LBT_PARAMS), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set LBT-Params - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set LBT-Params - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Set LBT-Params - OK");

            return true;
        }



        #endregion Reader-RF

        #region Reader-Control

        /// <summary>
        /// Reboots the reader
        /// </summary>
        /// <returns>Succes of the operation</returns>
        public bool reboot()
        {
            Global.trc(3, "Reboot - Trying to reboot");

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_REBOOT);
            if (res != true)
            {
                Global.trc(3, "Reboot - NOK - Send");
                return res;
            }

            System.Threading.Thread.Sleep(50);

            return true;
        }

        /// <summary>
        /// Sets the heartbeat settings of the reader
        /// </summary>
        /// <param name="on">Specifies if the reader should send a heartbeat</param>
        /// <param name="interval">Specifies the interval of the heartbeat</param>
        /// <returns>Succes of the operation</returns>
        public bool setHeartBeat(bool on, ushort interval)
        {
            Global.trc(3, "Set Heartbeat - Trying to set heartbeat " + ((on) ? "ON" : "OFF"));

            byte[] payload = new byte[0];
            if (interval == 0)
            {
                payload = new byte[1];
                payload[0] = (byte)((on) ? Constants.eRFE_HEARTBEAT_SIGNAL.HEARTBEAT_ON : Constants.eRFE_HEARTBEAT_SIGNAL.HEARTBEAT_OFF);
            }
            if (interval != 0)
            {
                payload = new byte[3];
                payload[0] = (byte)((on) ? Constants.eRFE_HEARTBEAT_SIGNAL.HEARTBEAT_ON : Constants.eRFE_HEARTBEAT_SIGNAL.HEARTBEAT_OFF);
                payload[1] = (byte)(interval >> 8);
                payload[2] = (byte)(interval);
            }

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_HEARTBEAT, payload);
            if (res != true)
            {
                Global.trc(3, "Set Heartbeat - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_HEARTBEAT), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Heartbeat - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Heartbeat - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            Global.trc(3, "Set Heartbeat - OK");

            return true;

        }

        /// <summary>
        /// Sets the antenna power of the reader
        /// </summary>
        /// <param name="on">Specifies if the antenna power should be activated</param>
        /// <returns>Succes of the operation</returns>
        public bool setAntennaPower(bool on)
        {
            Global.trc(3, "Set Antenna - Trying to set antenna power " + ((on) ? "ON" : "OFF"));

            byte[] payload;
            payload = new byte[1];
            payload[0] = (byte)((on) ? Constants.eRFE_ANTENNA_POWER.ANTENNA_ON : Constants.eRFE_ANTENNA_POWER.ANTENNA_OFF);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_ANTENNA_POWER, payload);
            if (res != true)
            {
                Global.trc(3, "Set Antenna - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_ANTENNA_POWER), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Antenna - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Antenna - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            Global.trc(3, "Set Antenna - OK");

            return true;
        }

        /// <summary>
        /// Saves the settings permanent on the reader
        /// </summary>
        /// <returns>Succes of the operation</returns>
        public bool saveSettingsPermanent()
        {
            Global.trc(3, "Save Settings - Trying save settings permanent");

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SAVE_SETTINGS_PERMANENT);
            if (res != true)
            {
                Global.trc(3, "Save Settings - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SAVE_SETTINGS_PERMANENT), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Save Settings - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Save Settings - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Save Settings - OK");

            return true;

        }

        /// <summary>
        /// Restores the factory settings of the reader
        /// </summary>
        /// <returns>Succes of the operation</returns>
        public bool restoreFactorySettings()
        {
            Global.trc(3, "Restore Settings - Trying to restore settings");

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_RESTORE_FACTORY_SETTINGS);
            if (res != true)
            {
                Global.trc(3, "Restore Settings - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_RESTORE_FACTORY_SETTINGS), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Restore Settings - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Restore Settings - NOK - Payl");
                _LastReturnCode
                        = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Restore Settings - OK");

            return true;
        }

        /// <summary>
        /// Retrieves a parameter from the reader at the given address
        /// </summary>
        /// <param name="address">Address of the paremeter</param>
        /// <param name="value">Retrieved value of the parameter</param>
        /// <returns>Succes of the operation</returns>
        public bool getParam(ushort address, out byte[] value)
        {
            Global.trc(3, "Get Param - Trying to get param of address " + String.Format("{0}", address));

            value = new byte[0];

            byte[] payload;
            payload = new byte[2];
            payload[0] = (byte)(address >> 8);
            payload[1] = (byte)(address);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_PARAM, payload);
            if (res != true)
            {
                Global.trc(3, "Get Param - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_PARAM), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Param - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Param - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            value = new byte[payl[1]];
            Array.Copy(payl, 2, value, 0, value.Length);

            Global.trc(3, "Get Param - OK : " + BitConverter.ToString(value));

            return true;
        }

        /// <summary>
        /// Sets the value of a parameter of the reader
        /// </summary>
        /// <param name="address">Address of the value</param>
        /// <param name="value">The new value of the parameter</param>
        /// <returns>Succes of the operation</returns>
        public bool setParam(ushort address, byte[] value)
        {
            Global.trc(3, "Set Param - Trying to set param at address "
                    + String.Format("{0}", address) + " to " + BitConverter.ToString(value));

            byte[] payload;
            payload = new byte[3 + value.Length];
            payload[0] = (byte)(address >> 8);
            payload[1] = (byte)(address);
            payload[2] = (byte)value.Length;
            value.CopyTo(payload, 3);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_PARAM, payload);
            if (res != true)
            {
                Global.trc(3, "Set Param - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_PARAM), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Param - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Param - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Set Param - OK");

            return true;
        }

        /// <summary>
        /// Retrieves a the stored device name of the reader
        /// </summary>
        /// <param name="name">Stored device name</param>
        /// <returns>Succes of the operation</returns>
        public bool getDeviceName(out string name)
        {
            Global.trc(3, "Get Device Name - Trying to get device name");

            name = "";

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_DEVICE_NAME);
            if (res != true)
            {
                Global.trc(3, "Get Device Name - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_DEVICE_NAME), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Device Name - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Device Name - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            name = enc.GetString(payl, 1, payl.Length - 1);

            Global.trc(3, "Get Device Name - OK : " + name);

            return true;
        }

        /// <summary>
        /// Sets the device name of the reader
        /// </summary>
        /// <param name="name">The device name</param>
        /// <returns>Succes of the operation</returns>
        public bool setDeviceName(string name)
        {
            Global.trc(3, "Set Device Name - Trying to set device name to " + name);

            byte[] payload;

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            payload = enc.GetBytes(name);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_DEVICE_NAME, payload);
            if (res != true)
            {
                Global.trc(3, "Set Device Name - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_DEVICE_NAME), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Device Name - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Device Name - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            Global.trc(3, "Set Device Name - OK");

            return true;
        }

        /// <summary>
        /// Retrieves a the stored device location of the reader
        /// </summary>
        /// <param name="location">Stored device location</param>
        /// <returns>Succes of the operation</returns>
        public bool getDeviceLocation(out string location)
        {
            Global.trc(3, "Get Device Location - Trying to get device name");

            location = "";

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_DEVICE_LOCATION);
            if (res != true)
            {
                Global.trc(3, "Get Device Location - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_GET_DEVICE_LOCATION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Device Location - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Device Location - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            location = enc.GetString(payl, 1, payl.Length - 1);

            Global.trc(3, "Get Device Location - OK : " + location);

            return true;
        }

        /// <summary>
        /// Sets the device location of the reader
        /// </summary>
        /// <param name="location">The device location</param>
        /// <returns>Succes of the operation</returns>
        public bool setDeviceLocation(string location)
        {
            Global.trc(3, "Set Device Location - Trying to set device location to " + location);

            byte[] payload;

            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            payload = enc.GetBytes(location);

            // reset last return code
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_DEVICE_LOCATION, payload);
            if (res != true)
            {
                Global.trc(3, "Set Device Location - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_CONTROL,
                    Constants.RFE_COM2_SET_DEVICE_LOCATION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Device Location - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Device Location - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)(byte)payl[0];
                return false;
            }

            Global.trc(3, "Set Device Location - OK");

            return true;
        }

        #endregion Reader-Control

        #region GPIO

        /// <summary>
        /// Retrieves the GPIO capabillities of the reader
        /// </summary>
        /// <param name="mask">Bit mask of available GPIOs</param>
        /// <param name="output">Bit mask of GPIOs that are available as output</param>
        /// <param name="input">Bit mask of GPIOs that are available as input</param>
        /// <returns>Succes of the operation</returns>
        public bool getGPIOCaps(out ulong mask, out ulong output, out ulong input)
        {
            mask = output = input = 0;

            Global.trc(6, "Get GPIO Caps - Trying to get GPIO Caps");

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO_CAPS);
            if (res != true)
            {
                Global.trc(3, "Get GPIO Caps - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO_CAPS), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get GPIO Caps - NOK - Resp");
                return false;
            }

            if (payl.Length != 12)
            {
                Global.trc(3, "Get GPIO Caps - NOK - Payl");
                return false;
            }

            mask = 0;
            mask |= (((ulong)(byte)payl[0]) << 24);
            mask |= (((ulong)(byte)payl[1]) << 16);
            mask |= (((ulong)(byte)payl[2]) << 8);
            mask |= (ulong)(byte)payl[3];

            output = 0;
            output |= (((ulong)(byte)payl[4]) << 24);
            output |= (((ulong)(byte)payl[5]) << 16);
            output |= (((ulong)(byte)payl[6]) << 8);
            output |= (ulong)(byte)payl[7];

            input = 0;
            input |= (((ulong)(byte)payl[8]) << 24);
            input |= (((ulong)(byte)payl[9]) << 16);
            input |= (((ulong)(byte)payl[10]) << 8);
            input |= (ulong)(byte)payl[11];

            Global.trc(6, "Get GPIO Caps - OK");

            return true;
        }

        /// <summary>
        /// Retrieves the current set GPIO direction
        /// </summary>
        /// <param name="direction">Bit mask of the current direction, 1 = output, 2 = input</param>
        /// <returns>Succes of the operation</returns>
        public bool getGPIODirection(out ulong direction)
        {
            direction = 0;

            Global.trc(3, "Get GPIO Direction - Trying to get GPIO Direction");

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO_DIRECTION);
            if (res != true)
            {
                Global.trc(3, "Get GPIO Direction - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO_DIRECTION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get GPIO Direction - NOK - Resp");
                return false;
            }

            if (payl.Length != 4)
            {
                Global.trc(3, "Get GPIO Direction - NOK - Payl");
                return false;
            }

            direction = 0;
            direction |= (((ulong)(byte)payl[0]) << 24);
            direction |= (((ulong)(byte)payl[1]) << 16);
            direction |= (((ulong)(byte)payl[2]) << 8);
            direction |= (ulong)(byte)payl[3];

            Global.trc(3, "Get GPIO Direction - OK");

            return true;
        }

        /// <summary>
        /// Set the direction of GPIO pins 
        /// </summary>
        /// <param name="direction">The bits that are high are configured as output, the others as input</param>
        /// <returns>Succes of the operation</returns>
        public bool setGPIODirection(ulong direction)
        {
            Global.trc(3, "Set GPIO Direction - Trying to set GPIO direction");

            byte[] payload;
            payload = new byte[4];
            payload[0] = (byte)(direction >> 24);
            payload[1] = (byte)(direction >> 16);
            payload[2] = (byte)(direction >> 8);
            payload[3] = (byte)(direction);

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_SET_GPIO_DIRECTION, payload);
            if (res != true)
            {
                Global.trc(3, "Set GPIO Direction - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_SET_GPIO_DIRECTION), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set GPIO Direction - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set GPIO Direction - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "Set GPIO Direction - OK");

            return true;
        }

        /// <summary>
        /// Retrieves the current level of the GPIO pins
        /// </summary>
        /// <param name="mask">Bit mask of the current level</param>
        /// <returns>Succes of the operation</returns>
        public bool getGPIO(out ulong mask)
        {
            mask = 0;

            Global.trc(3, "Get GPIO - Trying to get GPIO");

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO);
            if (res != true)
            {
                Global.trc(3, "Get GPIO - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_GET_GPIO), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get GPIO - NOK - Resp");
                return false;
            }

            if (payl.Length != 4)
            {
                Global.trc(3, "Get GPIO - NOK - Payl");
                return false;
            }

            mask = 0;
            mask |= (((ulong)(byte)payl[0]) << 24);
            mask |= (((ulong)(byte)payl[1]) << 16);
            mask |= (((ulong)(byte)payl[2]) << 8);
            mask |= (ulong)(byte)payl[3];

            Global.trc(3, "Get GPIO Direction - OK");

            return true;

        }

        /// <summary>
        /// Set the current level of output GPIO pins to high
        /// </summary>
        /// <param name="mask">Bit mask of GPIO pins that should be set high</param>
        /// <returns>Succes of the operation</returns>
        public bool setGPIO(ulong mask)
        {
            Global.trc(3, "Set GPIO - Trying to set GPIO");

            byte[] payload;
            payload = new byte[4];
            payload[0] = (byte)(mask >> 24);
            payload[1] = (byte)(mask >> 16);
            payload[2] = (byte)(mask >> 8);
            payload[3] = (byte)(mask);

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_SET_GPIO, payload);
            if (res != true)
            {
                Global.trc(3, "Set GPIO - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_SET_GPIO), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set GPIO - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set GPIO - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "Set GPIO - OK");

            return true;
        }

        /// <summary>
        /// Set the current level of output GPIO pins to low
        /// </summary>
        /// <param name="mask">Bit mask of GPIO pins that should be set low</param>
        /// <returns>Succes of the operation</returns>
        public bool clearGPIO(ulong mask)
        {
            Global.trc(3, "Clear GPIO - Trying to clear GPIO");

            byte[] payload;
            payload = new byte[4];
            payload[0] = (byte)(mask >> 24);
            payload[1] = (byte)(mask >> 16);
            payload[2] = (byte)(mask >> 8);
            payload[3] = (byte)(mask);

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_CLEAR_GPIO, payload);
            if (res != true)
            {
                Global.trc(3, "Clear GPIO - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_CLEAR_GPIO), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Clear GPIO - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Clear GPIO - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "Clear GPIO - OK ");

            return true;
        }

        /// <summary>
        /// First clears and then sets the specified masks
        /// </summary>
        /// <param name="clearMask">Bit mask of GPIO pins that should be set low</param>
        /// <param name="setMask">Bit mask of GPIO pins that should be set high</param>
        /// <returns>Succes of the operation</returns>
        public bool clearSetGPIO(ulong clearMask, ulong setMask)
        {
            Global.trc(3, "ClearSet GPIO - Trying to clear GPIO");

            byte[] payload;
            payload = new byte[8];
            payload[0] = (byte)(clearMask >> 24);
            payload[1] = (byte)(clearMask >> 16);
            payload[2] = (byte)(clearMask >> 8);
            payload[3] = (byte)(clearMask);
            payload[4] = (byte)(setMask >> 24);
            payload[5] = (byte)(setMask >> 16);
            payload[6] = (byte)(setMask >> 8);
            payload[7] = (byte)(setMask);

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_CLEAR_SET_GPIO, payload);
            if (res != true)
            {
                Global.trc(3, "ClearSet GPIO - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_GPIO,
                    Constants.RFE_COM2_CLEAR_SET_GPIO), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "ClearSet GPIO - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "ClearSet GPIO - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "ClearSet GPIO - OK ");

            return true;
        }

        #endregion GPIO

        #region Antenna

        /// <summary>
        /// Set the antenna sequence
        /// </summary>
        /// <param name="sequence">The antenna sequence [index,time]</param>
        /// <returns>Succes of the operation</returns>
        public bool setAntennaSequence(List<Pair<byte, ulong>> sequence)
        {
            Global.trc(3, "Set Antenna Sequence - Trying to set antenna sequence");

            byte[] payload;
            payload = new byte[1 + (5 * sequence.Count)];
            payload[0] = (byte)(sequence.Count);

            int index = 1;
            for (int i = 0; i < sequence.Count; i++)
            {
                payload[index++] = sequence[i].First;
                payload[index++] = (byte)(sequence[i].Second >> 24);
                payload[index++] = (byte)(sequence[i].Second >> 16);
                payload[index++] = (byte)(sequence[i].Second >> 8);
                payload[index++] = (byte)(sequence[i].Second);
            }

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_SET_ANTENNA_SEQUENCE, payload);
            if (res != true)
            {
                Global.trc(3, "Set Antenna Sequence - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_SET_ANTENNA_SEQUENCE), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Antenna Sequence - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Antenna Sequence - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "Set Antenna Sequence - OK");

            return true;
        }

        /// <summary>
        /// Get the antenna sequence
        /// </summary>
        /// <param name="sequence">The antenna sequence [index,time]</param>
        /// <returns>Succes of the operation</returns>
        public bool getAntennaSequence(out List<Pair<byte, ulong>> sequence)
        {
            Global.trc(3, "Get Antenna Sequence - Trying to get antenna sequence");

            sequence = new List<Pair<byte, ulong>>();

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_GET_WORKING_ANTENNA);
            if (res != true)
            {
                Global.trc(3, "Get Antenna Sequence - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_GET_WORKING_ANTENNA), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Antenna Sequence - NOK - Resp");
                return false;
            }

            if (payl.Length < 2 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Antenna Sequence - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            byte sequenceCount = payl[1];

            if (payl.Length != ((sequenceCount * 5) + 2))
            {
                Global.trc(0, "Get Antenna Sequence - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            byte index = 2;
            for (byte i = 0; i < sequenceCount; i++)
            {
                byte antennaIndex = payl[index++];

                ulong time = 0;
                time += (((ulong)payl[index++]) << 24);
                time += (((ulong)payl[index++]) << 16);
                time += (((ulong)payl[index++]) << 8);
                time += ((ulong)payl[index++]);

                Pair<byte, ulong> p = new Pair<byte, ulong>(antennaIndex, time);
                sequence.Add(p);
            }


            Global.trc(3, "Get Antenna Sequence - OK");

            return true;
        }

        /// <summary>
        /// Set the working antenna 
        /// </summary>
        /// <param name="index">Index of the working antenna</param>
        /// <returns>Succes of the operation</returns>
        public bool setWorkingAntenna(byte index)
        {
            Global.trc(3, "Set Working Antenna - Trying to set working antenna");

            byte[] payload;
            payload = new byte[1];
            payload[0] = index;

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_SET_WORKING_ANTENNA, payload);
            if (res != true)
            {
                Global.trc(3, "Set Working Antenna - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_SET_WORKING_ANTENNA), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Set Working Antenna - NOK - Resp");
                return false;
            }

            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Set Working Antenna - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            Global.trc(3, "Set Working Antenna - OK");

            return true;
        }

        /// <summary>
        /// Get the working antenna 
        /// </summary>
        /// <param name="index">Index of the working antenna</param>
        /// <returns>Succes of the operation</returns>
        public bool getWorkingAntenna(out byte index)
        {
            Global.trc(3, "Get Working Antenna - Trying to get working antenna");

            index = 0;

            // reset the flag
            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_GET_WORKING_ANTENNA);
            if (res != true)
            {
                Global.trc(3, "Get Working Antenna - NOK - Send");
                return res;
            }

            // wait for either the flag or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_READER_ANTENNA,
                    Constants.RFE_COM2_GET_WORKING_ANTENNA), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Get Working Antenna - NOK - Resp");
                return false;
            }

            if (payl.Length < 2 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Get Working Antenna - NOK - Payl");
                _LastReturnCode = ((Constants.eRFE_RET_VALUE)payl[0]);
                return false;
            }

            index = payl[1];

            Global.trc(3, "Get Working Antenna - OK");

            return true;
        }


        #endregion Antenna

        #region Tag-Functions

        /// <summary>
        /// Executes a single inventory at the reader.
        /// </summary>
        /// <param name="epc">List of found tags</param>
        /// <returns>Succes of the operation</returns>
        public bool doSingleInventory(out List<byte[]> epc)
        {
            Global.trc(3, "Single Inventory - Trying to do an inventory");

            epc = new List<byte[]>();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_INVENTORY_SINGLE);
            if (res != true)
            {
                Global.trc(3, "Single Inventory - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_INVENTORY_SINGLE), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Single Inventory - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Single Inventory - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            byte epcsInMessage = payl[2];
            byte index = 3;
            for (int i = 0; i < epcsInMessage; i++)
            {
                //byte tagInfoSize = msg[index);
                byte epcLength = payl[index + 2];
                byte[] temp = new byte[epcLength];
                Array.Copy(payl, index + 3, temp, 0, epcLength);
                epc.Add(temp);
                index += payl[index];
                index++;
            }

            Global.trc(3, "Single Inventory - OK : Count(" + epcsInMessage + ")");
            foreach (byte[] t in epc)
                Global.trc(3, "Single Inventory - OK :     EPC: " + BitConverter.ToString(t));

            return true;
        }

        /// <summary>
        /// Sets the cyclic inventory on or off
        /// </summary>
        /// <param name="on">Cyclic inventory mode</param>
        /// <returns>Succes of the operation</returns>
        public bool setCyclicInventory(bool on)
        {
            Global.trc(3, "Cyclic Inventory - Trying to set cyclic inventory to " + String.Format("{0}", on));

            byte[] payload;
            payload = new byte[1];
            if (on)
                payload[0] = (byte)Constants.eRFE_INVENTORY_MODE.INVENTORY_ON;
            else
                payload[0] = (byte)Constants.eRFE_INVENTORY_MODE.INVENTORY_OFF;

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_INVENTORY_CYCLIC, payload);
            if (res != true)
            {
                Global.trc(3, "Cyclic Inventory - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_INVENTORY_CYCLIC), _ResponseTimeOut, out result);
            if (!result)
            {
                Global.trc(3, "Cyclic Inventory - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Cyclic Inventory - NOK - Payl");
                _LastReturnCode
                        = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Cyclic Inventory - OK");

            return true;
        }

        /// <summary>
        /// Reads data from a tag.
        /// </summary>
        /// <param name="epc">EPC of the specified tag</param>
        /// <param name="mem_bank">Memory bank where to read data from</param>
        /// <param name="address">Address within the memory bank</param>
        /// <param name="passwd">The access password to read from the tag</param>
        /// <param name="count">The count of data that should be read</param>
        /// <param name="data">The read data</param>
        /// <returns>Succes of the operation</returns>
        public bool readFromTag(byte[] epc, byte mem_bank, ushort address, byte[] passwd, byte count, out byte[] data)
        {
            Global.trc(3, "Read From Tag - Trying to read from tag " + BitConverter.ToString(epc) + " from memory bank "
                    + String.Format("{0}", mem_bank) + " and address " + String.Format("{0}", address) + " the count " + String.Format("{0}", count));

            data = new byte[0];

            if (passwd.Length != 4)
            {
                Global.trc(3, "Read From Tag - NOK - Data");
                return false;
            }


            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add((byte)epc.Length);
            payloadList.AddRange(epc);

            payloadList.Add((byte)mem_bank);
            payloadList.Add((byte)(address >> 8));
            payloadList.Add((byte)address);
            payloadList.AddRange(passwd);

            payloadList.Add(count);
            payload = payloadList.ToArray();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_READ_FROM_TAG, payload);
            if (res != true)
            {
                Global.trc(3, "Read From Tag - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_READ_FROM_TAG), _ResponseTimeOut * 4, out result);
            if (!result)
            {
                Global.trc(3, "Read From Tag - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || (((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS) && (((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_RESULT_PENDING))
            {
                Global.trc(3, "Read From Tag - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            if (((Constants.eRFE_RET_VALUE)payl[0]) == Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                data = new byte[payl[1]];
                Array.Copy(payl, 2, data, 0, payl[1]);
            }
            else if (((Constants.eRFE_RET_VALUE)payl[0]) == Constants.eRFE_RET_VALUE.RFE_RET_RESULT_PENDING)
            {
                if (payl.Count() != 2)
                {
                    Global.trc(0, "Read From Tag - NOK - Payl");
                    _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                    return false;
                }

                bool p_result = false;
                byte[] pendingResp = _PendingMessageQueue.waitForMessage(payl[1], _ResponseTimeOut * 10, out p_result);
                if (!p_result)
                {
                    Global.trc(3, "Read From Tag - NOK - Resp");
                    return false;
                }

                if (pendingResp.Length == 0 || ((Constants.eRFE_RET_VALUE)pendingResp[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
                {
                    Global.trc(3, "Read From Tag - NOK - Payl");
                    _LastReturnCode = (Constants.eRFE_RET_VALUE)pendingResp[0];
                    return false;
                }

                data = new byte[pendingResp[1]];
                Array.Copy(pendingResp, 2, data, 0, pendingResp[1]);
            }

            Global.trc(3, "Read From Tag - OK : Read the data from the tag:" + BitConverter.ToString(data));

            return true;
        }

        /// <summary>
        /// Writes data to the a tag.
        /// </summary>
        /// <param name="epc">EPC of the specified tag</param>
        /// <param name="mem_bank">Memory bank where data should be written to</param>
        /// <param name="address">Address within the memory bank</param>
        /// <param name="passwd">The access password to write to the tag</param>
        /// <param name="data">The data that should be written</param>
        /// <returns>Succes of the operation</returns>
        public bool writeToTag(byte[] epc, byte mem_bank, ushort address, byte[] passwd, byte[] data)
        {
            Global.trc(3, "Write To Tag - Trying to write to tag " + BitConverter.ToString(epc) + " at bank "
                    + String.Format("{0}", mem_bank) + " and address " + String.Format("{0}", address) + " the bytes " + BitConverter.ToString(data));

            if (passwd.Length != 4)
            {
                Global.trc(3, "Write To Tag - NOK - Data");
                return false;
            }

            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add((byte)epc.Length);
            payloadList.AddRange(epc);

            payloadList.Add((byte)mem_bank);
            payloadList.Add((byte)(address >> 8));
            payloadList.Add((byte)address);
            payloadList.AddRange(passwd);

            payloadList.Add((byte)data.Length);
            payloadList.AddRange(data);
            payload = payloadList.ToArray();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_WRITE_TO_TAG, payload);
            if (res != true)
            {
                Global.trc(3, "Write To Tag - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_WRITE_TO_TAG), _ResponseTimeOut * 4, out result);
            if (!result)
            {
                Global.trc(3, "Write To Tag - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Write To Tag - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Write To Tag - OK");

            return true;
        }

        /// <summary>
        /// Locks a specfied memory region of a tag.
        /// </summary>
        /// <param name="epc">EPC of the specified tag</param>
        /// <param name="mode">The lock mode</param>
        /// <param name="memory">The memory region</param>
        /// <param name="password">The access password to lock the tag</param>
        /// <returns>Succes of the operation</returns>
        public bool lockTag(byte[] epc, byte mode, byte memory, byte[] password)
        {
            Global.trc(3, "Lock Tag - Trying to lock tag " + BitConverter.ToString(epc) + " with the mode "
                    + String.Format("{0}", mode) + " the memory " + String.Format("{0}", memory));

            if (password.Length != 4)
            {
                Global.trc(3, "Lock Tag - NOK - Data");
                return false;
            }

            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add((byte)epc.Length);
            payloadList.AddRange(epc);

            payloadList.Add((byte)mode);
            payloadList.Add((byte)memory);
            payloadList.AddRange(password);
            payload = payloadList.ToArray();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_LOCK_TAG, payload);
            if (res != true)
            {
                Global.trc(3, "Lock Tag - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_LOCK_TAG), _ResponseTimeOut * 2, out result);
            if (!result)
            {
                Global.trc(3, "Lock Tag - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Lock Tag - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Lock Tag - OK");

            return true;
        }

        /// <summary>
        /// Kills a tag
        /// </summary>
        /// <param name="epc">EPC of the specified tag</param>
        /// <param name="rfu">rfu</param>
        /// <param name="recom">recom</param>
        /// <param name="password">The kill password to kill the tag</param>
        /// <returns>Succes of the operation</returns>
        public bool killTag(byte[] epc, byte rfu, byte recom, byte[] password)
        {
            Global.trc(3, "Kill Tag - Trying to kill tag " + BitConverter.ToString(epc) + " with the rfu "
                    + String.Format("{0}", rfu) + " the recom " + String.Format("{0}", recom));

            if (password.Length != 4)
            {
                Global.trc(3, "Kill Tag - NOK - Data");
                return false;
            }

            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add((byte)epc.Length);
            payloadList.AddRange(epc);
            payloadList.Add((byte)(((byte)((rfu & 0x0F) << 4)) | ((byte)(recom & 0x0F))));
            payloadList.AddRange(password);
            payload = payloadList.ToArray();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_KILL_TAG, payload);
            if (res != true)
            {
                Global.trc(3, "Kill Tag - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_KILL_TAG), _ResponseTimeOut * 2, out result);
            if (!result)
            {
                Global.trc(3, "Kill Tag - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0])
                    != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Kill Tag - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            Global.trc(3, "Kill Tag - OK");

            return true;

        }

        /// <summary>
        /// Executes a custom tag command on the reader
        /// </summary>
        /// <param name="commandId">Id of the command</param>
        /// <param name="data">Command payload</param>
        /// <param name="resultData">Result paylod</param>
        /// <returns>Succes of the operation</returns>
        public bool customTagCommand(byte commandId, byte[] data, out byte[] resultData)
        {
            Global.trc(3, "Custom Tag Command - Trying to execute custom tag command " + String.Format("{0}", commandId)
                    + " with data " + BitConverter.ToString(data));

            resultData = new byte[0];

            byte[] payload;
            List<byte> payloadList = new List<byte>();
            payloadList.Add(commandId);
            payloadList.AddRange(data);
            payload = payloadList.ToArray();

            _LastReturnCode = Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS;

            // send the command
            bool res = send2Reader(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_CUSTOM_TAG_COMMAND, payload);
            if (res != true)
            {
                Global.trc(3, "Custom Tag Command - NOK - Send");
                return res;
            }

            // wait for either the response or a timeout
            bool result = false;
            byte[] payl = _MessageQueue.waitForMessage(messageId(Constants.RFE_COM1_TAG_FUNCTIONS,
                    Constants.RFE_COM2_CUSTOM_TAG_COMMAND), _ResponseTimeOut * 2, out result);
            if (!result)
            {
                Global.trc(3, "Custom Tag Command - NOK - Resp");
                return false;
            }

            // parse the response
            if (payl.Length == 0 || ((Constants.eRFE_RET_VALUE)payl[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
            {
                Global.trc(3, "Custom Tag Command - NOK - Payl");
                _LastReturnCode = (Constants.eRFE_RET_VALUE)payl[0];
                return false;
            }

            resultData = new byte[payl.Length - 1];
            Array.Copy(payl, 1, resultData, 0, payl.Length - 1);

            Global.trc(3, "Custom Tag Command - OK : Result data: " + BitConverter.ToString(resultData));

            return true;

        }

        #endregion Tag-Functions

        #endregion Commands

        #region Delegates

        /// <summary>
        /// Delegate is called every time a heratbeat signal of the reader is received.
        /// </summary>
        public delegate void HeartBeatHandler(byte[] data);
        /// <summary>
        /// The event is emitted every time a heratbeat signal of the reader is received.
        /// </summary>
        public event HeartBeatHandler HeartBeat;

        /// <summary>
        /// Delegate is called everytime a result of the cyclic inventory is received.
        /// </summary>
        /// <param name="tagEvent">The data of the tag event</param>
        public delegate void CyclicInventoryHandler(TagEvent tagEvent);
        /// <summary>
        /// Event is emitted everytime a result of the cyclic inventory is received.
        /// </summary>
        public event CyclicInventoryHandler CyclicInventory;

        /// <summary>
        /// Delegate is called everytime the reader changed its state.
        /// </summary>
        /// <param name="newState">The new state of the reader</param>
        public delegate void StateChangedHandler(Constants.eRFE_CURRENT_READER_STATE newState);
        /// <summary>
        /// Event is emitted everytime the reader changed its state.
        /// </summary>
        public event StateChangedHandler StateChanged;

        /// <summary>
        /// Delegate is called everytime the status register of the reader changed.
        /// </summary>
        /// <param name="statusRegister">New value of the status register</param>
        public delegate void StatusRegisterChangedHandler(ulong statusRegister);
        /// <summary>
        /// Event is emitted everytime the status register of the reader changed.
        /// </summary>
        public event StatusRegisterChangedHandler StatusRegisterChanged;

        /// <summary>
        /// Delegate is called everytime the status register of the reader changed.
        /// </summary>
        /// <param name="gpioValues">New value of the status register</param>
        public delegate void GpioValuesChangedHandler(ulong gpioValues);
        /// <summary>
        /// Event is emitted everytime the status register of the reader changed.
        /// </summary>
        public event GpioValuesChangedHandler GpioValuesChanged;

        #endregion Delegates

        #region OutputCreator

        /// <summary>
        /// Builds up a message that is sent to the reader
        /// </summary>
        /// <param name="com1">Command byte 1</param>
        /// <param name="com2">Command byte 2</param>
        /// <returns>Succes of the operation</returns>
        bool send2Reader(byte com1, byte com2)
        {
            byte[] msg = new byte[10];
            msg[0] = Constants.RFE_START_BYTE_1;
            msg[1] = Constants.RFE_START_BYTE_2;
            msg[2] = Constants.RFE_START_BYTE_3;
            msg[3] = Constants.RFE_COMMAND_START_BYTE;
            msg[4] = com1;
            msg[5] = com2;
            msg[6] = Constants.RFE_LENGTH_START_BYTE;
            msg[7] = 0;
            msg[8] = Constants.RFE_CHECKSUM_START_BYTE;
            msg[9] = calcXORCS(msg.ToArray());

            Global.trc(8, "<- SinglMessage " + BitConverter.ToString(msg));

            return _Device.Send(msg);
        }

        /// <summary>
        /// Builds up a message that is sent to the reader
        /// </summary>
        /// <param name="com1">Command byte 1</param>
        /// <param name="com2">Command byte 2</param>
        /// <param name="payload">Paylod of the message</param>
        /// <returns>Succes of the operation</returns>
        bool send2Reader(byte com1, byte com2, byte[] payload)
        {
            List<byte> msg = new List<byte>();
            msg.Add(Constants.RFE_START_BYTE_1);
            msg.Add(Constants.RFE_START_BYTE_2);
            msg.Add(Constants.RFE_START_BYTE_3);
            msg.Add(Constants.RFE_COMMAND_START_BYTE);
            msg.Add(com1);
            msg.Add(com2);
            msg.Add(Constants.RFE_LENGTH_START_BYTE);
            msg.Add((byte)payload.Length);

            if (payload.Length > 0)
            {
                msg.Add(Constants.RFE_PAYLOAD_START_BYTE);
                msg.AddRange(payload);
            }

            msg.Add(Constants.RFE_CHECKSUM_START_BYTE);
            msg.Add(calcXORCS(msg.ToArray()));

            Global.trc(8, "<- SinglMessage " + BitConverter.ToString(msg.ToArray()));

            return _Device.Send(msg.ToArray());
        }


        #endregion OutputCreator

        #region InputParser

        /// <summary>
        /// Parses the recieved paylod for the heartbeat interrupt
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void heartBeatISR(byte[] payload)
        {
            if (payload.Length < 1 || ((Constants.eRFE_RET_VALUE)payload[0]) != Constants.eRFE_RET_VALUE.RFE_RET_SUCCESS)
                return;

            if (HeartBeat != null)
            {
                byte[] data = new byte[payload.Length - 1];
                Array.Copy(payload, 1, data, 0, payload.Length - 1);
                HeartBeat(data);
            }
        }

        /// <summary>
        /// Parses the recieved payload for the cyclic inventory interrupt
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void cyclicInventoryISR(byte[] payload)
        {
            if (_BlockCyclicInventoryInterrupts == true)
                return;

            Constants.eInventoryMessageState state = Constants.eInventoryMessageState.START;

            TagEvent tagEvent = new TagEvent();

            byte tagIdIndex = 0;
            byte memIndex = 0;

            foreach (byte c in payload)
            {
                switch (state)
                {
                    case Constants.eInventoryMessageState.START:
                        if (c == Constants.RFE_TAG_ID_START_BYTE)
                            state = Constants.eInventoryMessageState.TAGID_LENGTH;
                        else if (c == Constants.RFE_RSSI_START_BYTE)
                            state = Constants.eInventoryMessageState.RSSI1;
                        else if (c == Constants.RFE_USERMEM_START_BYTE)
                            state = Constants.eInventoryMessageState.MEM_BANK;
                        else if (c == Constants.RFE_TRIGGER_START_BYTE)
                            state = Constants.eInventoryMessageState.TRIGGER;
                        else if (c == Constants.RFE_ANTENNA_ID_START_BYTE)
                            state = Constants.eInventoryMessageState.ANTENNA;
                        else if (c == Constants.RFE_READ_FREQU_START_BYTE)
                            state = Constants.eInventoryMessageState.FREQUENCY1;
                        else if (c == Constants.RFE_GEN2_HANDLE_START_BYTE)
                            state = Constants.eInventoryMessageState.HANDLE1;
                        else if (c == Constants.RFE_STATE_START_BYTE)
                            state = Constants.eInventoryMessageState.STATE1;
                        else if (c == Constants.RFE_BATTERY_START_BYTE)
                            state = Constants.eInventoryMessageState.BATTERY;
                        else
                            state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.TAGID_LENGTH:
                        tagEvent.tagId = new byte[c];
                        state = Constants.eInventoryMessageState.TAGID;
                        break;
                    case Constants.eInventoryMessageState.TAGID:
                        tagEvent.tagId[tagIdIndex++] = c;
                        if (tagEvent.tagId.Length == tagIdIndex)
                            state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.RSSI1:
                        tagEvent.rssi = new byte[2];
                        tagEvent.rssi[0] = c;
                        state = Constants.eInventoryMessageState.RSSI2;
                        break;
                    case Constants.eInventoryMessageState.RSSI2:
                        tagEvent.rssi[1] = c;
                        tagEvent.hasRSSI = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.MEM_BANK:
                        tagEvent.memBank = c;
                        state = Constants.eInventoryMessageState.MEM_ADDR1;
                        break;
                    case Constants.eInventoryMessageState.MEM_ADDR1:
                        tagEvent.memAddr = 0;
                        tagEvent.memAddr += (ushort)(((ushort)c) << 8);
                        state = Constants.eInventoryMessageState.MEM_ADDR2;
                        break;
                    case Constants.eInventoryMessageState.MEM_ADDR2:
                        tagEvent.memAddr += c;
                        state = Constants.eInventoryMessageState.MEM_SIZE;
                        break;
                    case Constants.eInventoryMessageState.MEM_SIZE:
                        tagEvent.memData = new byte[c];
                        state = Constants.eInventoryMessageState.MEM_DATA;
                        break;
                    case Constants.eInventoryMessageState.MEM_DATA:
                        tagEvent.memData[memIndex++] = c;
                        if (tagEvent.memData.Length == memIndex)
                        {
                            state = Constants.eInventoryMessageState.START;
                            tagEvent.hasMemory = true;
                        }
                        break;

                    case Constants.eInventoryMessageState.TRIGGER:
                        tagEvent.trigger = c;
                        tagEvent.hasTrigger = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.ANTENNA:
                        tagEvent.antennaId = c;
                        tagEvent.hasAntenna = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.FREQUENCY1:
                        tagEvent.readFrequency = 0;
                        tagEvent.readFrequency += (((ulong)c) << 16);
                        state = Constants.eInventoryMessageState.FREQUENCY2;
                        break;
                    case Constants.eInventoryMessageState.FREQUENCY2:
                        tagEvent.readFrequency += (((ulong)c) << 8);
                        state = Constants.eInventoryMessageState.FREQUENCY3;
                        break;
                    case Constants.eInventoryMessageState.FREQUENCY3:
                        tagEvent.readFrequency += (((ulong)c) << 0);
                        tagEvent.hasReadFrequency = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.STATE1:
                        tagEvent.state = 0;
                        tagEvent.state += (ushort)(((ushort)c) << 8);
                        state = Constants.eInventoryMessageState.STATE2;
                        break;
                    case Constants.eInventoryMessageState.STATE2:
                        tagEvent.state += (ushort)(((ushort)c) << 0);
                        tagEvent.hasState = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.HANDLE1:
                        tagEvent.handle = new byte[2];
                        tagEvent.handle[0] = c;
                        state = Constants.eInventoryMessageState.HANDLE2;
                        break;
                    case Constants.eInventoryMessageState.HANDLE2:
                        tagEvent.handle[1] = c;
                        tagEvent.hasHandle = true;
                        state = Constants.eInventoryMessageState.START;
                        break;

                    case Constants.eInventoryMessageState.BATTERY:
                        tagEvent.battery = c;
                        tagEvent.hasBattery = true;
                        state = Constants.eInventoryMessageState.START;
                        break;
                }
            }

            if (tagEvent.tagId.Length == 0)
                return;

            if (CyclicInventory != null)
                CyclicInventory(tagEvent);
        }

        /// <summary>
        /// Parses the recieved payload for the state changed interrupt
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void stateChangedISR(byte[] payload)
        {
            if (payload.Length != 1)
                return;

            Constants.eRFE_CURRENT_READER_STATE state = (Constants.eRFE_CURRENT_READER_STATE)payload[0];

            if (StateChanged != null)
                StateChanged(state);
        }

        /// <summary>
        /// Parses the recieved payload for the status register changed interrupt
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void statusRegChangedISR(byte[] payload)
        {
            if (payload.Length != 8)
                return;

            ulong statusRegister = 0;
            statusRegister |= (((ulong)payload[0]) << 56);
            statusRegister |= (((ulong)payload[1]) << 48);
            statusRegister |= (((ulong)payload[2]) << 40);
            statusRegister |= (((ulong)payload[3]) << 32);
            statusRegister |= (((ulong)payload[4]) << 24);
            statusRegister |= (((ulong)payload[5]) << 16);
            statusRegister |= (((ulong)payload[6]) << 8);
            statusRegister |= (ulong)payload[7];

            if (StatusRegisterChanged != null)
                StatusRegisterChanged(statusRegister);
        }

        /// <summary>
        /// Parses the recieved payload for the gpio values changed interrupt
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void gpioValuesChangedISR(byte[] payload)
        {
            if (payload.Length != 4)
                return;

            ulong gpioValues = 0;
            gpioValues |= (((ulong)payload[0]) << 24);
            gpioValues |= (((ulong)payload[1]) << 16);
            gpioValues |= (((ulong)payload[2]) << 8);
            gpioValues |= (ulong)payload[3];

            if (GpioValuesChanged != null)
                GpioValuesChanged(gpioValues);
        }

        /// <summary>
        /// Parses the recieved payload for the operation result
        /// </summary>
        /// <param name="payload">The received payload</param>
        private void operationResultISR(byte[] payload)
        {
            if (payload.Count() < 2)
                return;

            byte id = (byte)payload[0];
            byte[] data = new byte[payload.Count() - 1];
            Array.Copy(payload, 1, data, 0, payload.Length - 1);

            _PendingMessageQueue.enqueueMessage(id, data);
        }

        /// <summary>
        /// State of the state machine to parse received data
        /// </summary>
        private Constants.eMessageState _state = Constants.eMessageState.START_BYTE_1;
        /// <summary>
        /// Current single message
        /// </summary>
        private List<byte> _singleMsg = new List<byte>();
        /// <summary>
        /// Current payload index
        /// </summary>
        private int _payloadIndex = 0;
        /// <summary>
        /// Current paylod length
        /// </summary>
        private int _payloadLength = 0;

        /// <summary>
        /// Parses the received bytes from the device and splits it up to sngle messages.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="data">Data that where sent by the device</param>
        private void parseData(object sender, byte[] data)
        {
            if (sender != _Device)
                return;

            Global.trc(10, "-> RawMessage: " + BitConverter.ToString(data));

            foreach (byte c in data)
            {
                switch (_state)
                {
                    case Constants.eMessageState.START_BYTE_1:
                        if (c == Constants.RFE_START_BYTE_1)
                        {
                            _state = Constants.eMessageState.START_BYTE_2;
                            _singleMsg.Clear();
                            _payloadIndex = 0;
                            _payloadLength = 0;
                        }
                        break;

                    case Constants.eMessageState.START_BYTE_2:
                        if (c == Constants.RFE_START_BYTE_2)
                            _state = Constants.eMessageState.START_BYTE_3;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.START_BYTE_3:
                        if (c == Constants.RFE_START_BYTE_3)
                            _state = Constants.eMessageState.COMMAND_START_BYTE;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.COMMAND_START_BYTE:
                        if (c == Constants.RFE_COMMAND_START_BYTE)
                            _state = Constants.eMessageState.COMMAND_1;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.COMMAND_1:
                        _state = Constants.eMessageState.COMMAND_2;
                        break;

                    case Constants.eMessageState.COMMAND_2:
                        _state = Constants.eMessageState.LENGTH_START_BYTE;
                        break;

                    case Constants.eMessageState.LENGTH_START_BYTE:
                        if (c == Constants.RFE_LENGTH_START_BYTE)
                            _state = Constants.eMessageState.LENGTH;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.LENGTH:
                        _payloadLength = c;
                        _payloadIndex = 0;
                        if (_payloadLength == 0)
                            _state = Constants.eMessageState.CHECKSUM_START_BYTE;
                        else
                            _state = Constants.eMessageState.PAYLOAD_START_BYTE;
                        break;

                    case Constants.eMessageState.PAYLOAD_START_BYTE:
                        if (c == Constants.RFE_PAYLOAD_START_BYTE)
                            _state = Constants.eMessageState.PAYLOAD;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.PAYLOAD:
                        if (++_payloadIndex >= _payloadLength)
                            _state = Constants.eMessageState.CHECKSUM_START_BYTE;

                        break;

                    case Constants.eMessageState.CHECKSUM_START_BYTE:
                        if (c == Constants.RFE_CHECKSUM_START_BYTE)
                            _state = Constants.eMessageState.CHECKSUM;
                        else
                            _state = Constants.eMessageState.START_BYTE_1;
                        break;

                    case Constants.eMessageState.CHECKSUM:
                        {
                            _state = Constants.eMessageState.START_BYTE_1;
                            byte[] msg = _singleMsg.ToArray();
                            if (c != calcXORCS(msg))
                            {
                                Global.trc(0, "CHECKSUM NOK!!");
                                break;
                            }

                            Global.trc(9, "-> SingleMessage: " + BitConverter.ToString(msg));

                            computeMessage(msg);

                            break;
                        }

                }
                _singleMsg.Add(c);
            }

        }

        /// <summary>
        /// Computes a single message and stores it in the message queue or computes it.
        /// </summary>
        /// <param name="msg">A single message from the reader</param>
        private void computeMessage(byte[] msg)
        {
            byte command1 = msg[Constants.RFE_COMMAND_INDEX_1];
            byte command2 = msg[Constants.RFE_COMMAND_INDEX_2];

            byte[] payload = new byte[msg[Constants.RFE_LENGTH_INDEX]];
            Array.Copy(msg, Constants.RFE_PAYLOAD_INDEX, payload, 0, msg[Constants.RFE_LENGTH_INDEX]);

            switch (command1)
            {
                case Constants.RFE_COM1_INTERRUPT: // Interrupts
                    switch (command2)
                    {
                        case Constants.RFE_COM2_HEARTBEAT_INTERRUPT:
                            heartBeatISR(payload);
                            break;
                        case Constants.RFE_COM2_INVENTORY_CYCLIC_INTERRUPT:
                            cyclicInventoryISR(payload);
                            break;
                        case Constants.RFE_COM2_STATE_CHANGED_INTERRUPT:
                            stateChangedISR(payload);
                            break;
                        case Constants.RFE_COM2_STATUS_REG_CHANGED_INTERRUPT:
                            statusRegChangedISR(payload);
                            break;
                        case Constants.RFE_COM2_GPIO_PINS_CHANGED:
                            gpioValuesChangedISR(payload);
                            break;
                        case Constants.RFE_COM2_OPERATION_RESULT_INTERRUPT:
                            operationResultISR(payload);
                            break;
                        default:
                            _MessageQueue.enqueueMessage(messageId(command1, command2), payload);
                            break;
                    }
                    break;
                default:
                    _MessageQueue.enqueueMessage(messageId(command1, command2), payload);
                    break;
            }
        }

        #endregion InputParser

        #region Helper

        /// <summary>
        /// Generates a unique message id for the command bytes
        /// </summary>
        /// <param name="command1">Command byte 1</param>
        /// <param name="command2">Command byte 2</param>
        /// <returns>Unique message id</returns>
        private int messageId(byte command1, byte command2)
        {
            return (((int)command1) << 8) | command2;
        }

        /// <summary>
        /// Caclulates the XOR checksum for the given data.
        /// </summary>
        /// <param name="data">The data to calc checksum</param>
        /// <returns>The XOR checksum</returns>
        private byte calcXORCS(byte[] data)
        {
            byte result = 0;
            foreach (byte c in data)
            {
                result ^= c;
            }
            return result;
        }

        #endregion Helper
    }
}
