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

namespace CSrfeReaderInterface.protocol
{
    public static class Constants
    {
        public const byte RFE_DATA_BASE_INDEX = 9;		/*!< @brief Base index of the payload in package */

        public const byte RFE_START_INDEX_1 = 0;		/*!< @brief Index of the first start byte */
        public const byte RFE_START_INDEX_2 = 1;		/*!< @brief Index of the second start byte */
        public const byte RFE_START_INDEX_3 = 2;		/*!< @brief Index of the third start byte */
        public const byte RFE_COMMAND_START_INDEX = 3;		/*!< @brief Index of the command start byte */
        public const byte RFE_COMMAND_INDEX_1 = 4;		/*!< @brief Index of the first command */
        public const byte RFE_COMMAND_INDEX_2 = 5;		/*!< @brief Index of the second command */
        public const byte RFE_LENGTH_START_INDEX = 6;		/*!< @brief Index of the length start byte */
        public const byte RFE_LENGTH_INDEX = 7;		/*!< @brief Index of the length */

        public const byte RFE_PAYLOAD_START_INDEX = 8;		/*!< @brief Index of the payload start byte */
        public const byte RFE_PAYLOAD_INDEX = 9;		/*!< @brief Index of the payload */

        public const byte RFE_CS_START_WITHOUT_PAYLOAD = 8;		/*!< @brief Index of the checksum start byte if no payload is in the package*/
        public const byte RFE_CS_WITHOUT_PAYLOAD_INDEX = 9;		/*!< @brief Index of the checksum if no payload is in the package*/


        public const byte RFE_START_BYTE_1 = 0x52;		/*!< @brief Value of the first start byte = 'R' */
        public const byte RFE_START_BYTE_2 = 0x46;		/*!< @brief Value of the second start byte = 'F' */
        public const byte RFE_START_BYTE_3 = 0x45;		/*!< @brief Value of the third start byte = 'E' */
        public const byte RFE_COMMAND_START_BYTE = 0x01;		/*!< @brief Value of the command start byte */
        public const byte RFE_LENGTH_START_BYTE = 0x02;		/*!< @brief Value of the length start byte */
        public const byte RFE_PAYLOAD_START_BYTE = 0x03;		/*!< @brief Value of the payload start byte */
        public const byte RFE_CHECKSUM_START_BYTE = 0x04;		/*!< @brief Value of the checksum start byte */

        public const byte RFE_INVENTORY_ROUND_ENDED = 0x00;		/*!< @brief Identifier that says that the current inventory round ended */
        public const byte RFE_TAG_ID_START_BYTE = 0x01;		/*!< @brief Value of the tagId start byte in an inventory answer */
        public const byte RFE_RSSI_START_BYTE = 0x02;		/*!< @brief Value of the rssi start byte in an inventory answer */
        public const byte RFE_USERMEM_START_BYTE = 0x03;		/*!< @brief Value of the user memory start byte in an inventory answer */
        public const byte RFE_TRIGGER_START_BYTE = 0x04;		/*!< @brief Value of the trigger start byte in an inventory answer */
        public const byte RFE_ANTENNA_ID_START_BYTE = 0x05;		/*!< @brief Value of the antenna id start byte in an inventory answer */
        public const byte RFE_READ_FREQU_START_BYTE = 0x06;		/*!< @brief Value of the frequency start byte in an inventory answer */
        public const byte RFE_GEN2_HANDLE_START_BYTE = 0x07;		/*!< @brief Value of the handle in an inventory answer */
        public const byte RFE_STATE_START_BYTE = 0x08;		/*!< @brief Value of the state start byte in an inventory answer */
        public const byte RFE_BATTERY_START_BYTE = 0x09;		/*!< @brief Value of the battery start byte in an inventory answer */

        public const byte RFE_COM1_READER_COMMON = 0x01;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_SERIAL_NUMBER = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_READER_TYPE = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_HARDWARE_REVISION = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_SOFTWARE_REVISION = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_BOOTLOADER_REVISION = 0x05;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_CURRENT_SYSTEM = 0x06;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_CURRENT_STATE = 0x07;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_STATUS_REGISTER = 0x08;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_ANTENNA_COUNT = 0x10;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_READER_RF = 0x02;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_ATTENUATION = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_FREQUENCY = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_SENSITIVITY = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_LBT_PARAMS = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_ATTENUATION = 0x81;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_FREQUENCY = 0x82;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_SENSITIVITY = 0x83;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_LBT_PARAMS = 0x84;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_READER_CONTROL = 0x03;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_REBOOT = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_HEARTBEAT = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_ANTENNA_POWER = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_RESTORE_FACTORY_SETTINGS = 0x20;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SAVE_SETTINGS_PERMANENT = 0x21;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_PARAM = 0x30;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_PARAM = 0x31;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_DEVICE_NAME = 0x32;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_DEVICE_NAME = 0x33;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_DEVICE_LOCATION = 0x34;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_DEVICE_LOCATION = 0x35;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SWITCH_SYSTEM = 0xFE;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_BOOTLOADER_COMMAND = 0xFF;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_READER_TAG_MODE = 0x04;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_TAG_MODE = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_CURRENT_TAG_MODE = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_TAG_FUNCTION_LIST = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_READER_GPIO = 0x05;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_GPIO_CAPS = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_GPIO_DIRECTION = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_GPIO_DIRECTION = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_GPIO = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_GPIO = 0x05;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_CLEAR_GPIO = 0x06;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_CLEAR_SET_GPIO = 0x07;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_READER_ANTENNA = 0x06;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_ANTENNA_SEQUENCE = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_ANTENNA_SEQUENCE = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_SET_WORKING_ANTENNA = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GET_WORKING_ANTENNA = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_TAG_FUNCTIONS = 0x50;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_INVENTORY_SINGLE = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_INVENTORY_CYCLIC = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_READ_FROM_TAG = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_WRITE_TO_TAG = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_LOCK_TAG = 0x05;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_KILL_TAG = 0x06;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_CUSTOM_TAG_COMMAND = 0x10;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_READ_MULTIPLE_FROM_TAG = 0x20;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_APPLICATION = 0x70;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_APPLICATION_CALL = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/

        public const byte RFE_COM1_INTERRUPT = 0x90;		/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_HEARTBEAT_INTERRUPT = 0x01;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_INVENTORY_CYCLIC_INTERRUPT = 0x02;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_STATE_CHANGED_INTERRUPT = 0x03;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_STATUS_REG_CHANGED_INTERRUPT = 0x04;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_BOOT_UP_FINISHED_INTERRUPT = 0x05;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_NOTIFICATION_INTERRUPT = 0x06;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_APPLICATION_INTERRUPT = 0x07;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_OPERATION_RESULT_INTERRUPT = 0x08;	/*!< @brief Command definition, can be found in the protocol description*/
        public const byte RFE_COM2_GPIO_PINS_CHANGED = 0x09;	/*!< @brief Command definition, can be found in the protocol description*/

        /*! @brief Return codes of the request handler that are also sent to the host */
        public enum eRFE_RET_VALUE
        {
            RFE_RET_NOTHING_TODO = -1,					/*!< @brief	Only internal return code that means there is nothing to do */
            RFE_RET_SUCCESS = 0x00,						/*!< @brief Operation was successfull */
            RFE_RET_RESULT_PENDING = 0x01,				/*!< @brief Operation result not present, is sent after operation done */
            RFE_RET_ERR_OP_NOT_SUPPORTED = 0x50,		/*!< @brief Operation is not supported */
            RFE_RET_ERR_UNKOWN_ERR = 0x51,				/*!< @brief An unknwon error occured */
            RFE_RET_ERR_ON_EXEC_OP = 0x52,				/*!< @brief While executing operation an error occured */
            RFE_RET_ERR_COULD_NOT_WRITE = 0x53,			/*!< @brief	The write operation could not be performed */
            RFE_RET_ERR_WRONG_PARAM_COUNT = 0x54,		/*!< @brief The param count of a request was wrong */
            RFE_RET_ERR_WRONG_PARAM = 0x55,				/*!< @brief A param of a request was wrong */
            RFE_RET_TMI_TAG_UNREACHABLE = 0xA0,		    /*!< @brief Gen2 Error: Tag is not in the range of the reader */
            RFE_RET_TMI_MEM_OVERRUN = 0xA1,			    /*!< @brief	Gen2 Error: Memory overrun -> address does not exist */
            RFE_RET_TMI_MEM_LOCKED = 0xA2,				/*!< @brief Gen2 Error: Memory of the tag is locked */
            RFE_RET_TMI_INSUFFICIENT_POWER = 0xA3,		/*!< @brief Gen2 Error: The tag has too less power */
            RFE_RET_TMI_WRONG_PASSWORD = 0xA4			/*!< @brief Gen2 Error: The tag's state was not taken to secured */
        };

        /*! @brief Typedef for the current state */
        public enum eRFE_CURRENT_READER_STATE
        {
            RFE_STATE_IDLE = 0x00,		/*!< @brief Idle state */
            RFE_STATE_REBOOTING = 0x01,	/*!< @brief Rebooting state */
            RFE_STATE_SCANNING = 0x10,	/*!< @brief Scanning state */
            RFE_STATE_WRITING = 0x11,	/*!< @brief Scanning state */
            RFE_STATE_READING = 0x12,	/*!< @brief Scanning state */
        } ;


        /*! @brief Typedef for current system */
        public enum eRFE_CURRENT_SYSTEM
        {
            RFE_SYS_BOOTLOADER = 0x22,			/*!< @brief Bootloader system */
            RFE_SYS_FIRMWARE = 0xBB				/*!< @brief Firmware system */
        } ;

        /*! @brief Typedef for heartbeat parameter */
        public enum eRFE_HEARTBEAT_SIGNAL
        {
            HEARTBEAT_OFF = 0x00,						/*!< @brief Turns heartbeat off */
            HEARTBEAT_ON = 0x01,						/*!< @brief Turns heartbeat on */
            HEARTBEAT_DUPLEX = 0x02,					/*!< @brief Turns heartbeat duplex on */
            HEARTBEAT_STATE_ON = 0x03,					/*!< @brief Turns heartbeat with state on */
            HEARTBEAT_STATE_DUPLEX = 0x04				/*!< @brief Turns heartbeat duplex with state on */
        } ;


        /*! @brief Typedef for antenna power parameter */
        public enum eRFE_ANTENNA_POWER
        {
            ANTENNA_OFF = 0x00,							/*!< @brief Turns antenna power on */
            ANTENNA_ON = 0x01							/*!< @brief Turns antenna power off */
        } ;


        /*! @brief Typedef for tag types */
        public enum eRFE_TAG_MODE
        {
            ISO_18000_6_B = 0x40,						/*!< @brief ISO 6B tags */
            ISO_18000_6_C = 0x41,						/*!< @brief ISO 6C / Gen2 tags */
            RFE_ACTIVE_01 = 0xC0,						/*!< @brief RFE active tags */
            RFE_ACTIVE_02 = 0xC1						/*!< @brief RFE active tags */
        } ;


        /*! @brief Typedef for cyclic inventory parameter */
        public enum eRFE_INVENTORY_MODE
        {
            INVENTORY_OFF = 0x00,						/*!< @brief Turns cyclic inventory on */
            INVENTORY_ON = 0x01,						/*!< @brief Turns cyclic inventory off */
        } ;


        public enum eRFE_TRIGGER_SOURCE
        {
            TRIGGER_NONE = 0x00,
            TRIGGER_BUTTON = 0x01,
        } ;


        public enum eMessageState
        {
            START_BYTE_1,
            START_BYTE_2,
            START_BYTE_3,
            COMMAND_START_BYTE,
            COMMAND_1,
            COMMAND_2,
            LENGTH_START_BYTE,
            LENGTH,
            PAYLOAD_START_BYTE,
            PAYLOAD,
            CHECKSUM_START_BYTE,
            CHECKSUM
        };

        public enum eInventoryMessageState
        {
            START,
            INVENTORY_END_INDICATOR,
            TAGID_LENGTH,
            TAGID,
            RSSI1,
            RSSI2,
            MEM_BANK,
            MEM_ADDR1,
            MEM_ADDR2,
            MEM_SIZE,
            MEM_DATA,
            TRIGGER,
            ANTENNA,
            FREQUENCY1,
            FREQUENCY2,
            FREQUENCY3,
            HANDLE1,
            HANDLE2,
            STATE1,
            STATE2,
            BATTERY,
        };




    }
}