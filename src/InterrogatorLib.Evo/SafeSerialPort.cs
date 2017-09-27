using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using System.IO.Ports;

namespace InterrogatorLib.Evo
{
    // taken from http://stackoverflow.com/questions/13408476/detecting-when-a-serialport-gets-disconnected
    public class SafeSerialPort : SerialPort
    {
        private Stream theBaseStream;

        public SafeSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
                : base(portName, baudRate, parity, dataBits, stopBits)
        {
        }

        public new void Open()
        {
            try
            {
                base.Open();
                theBaseStream = BaseStream;
                GC.SuppressFinalize(BaseStream);
            }
            catch
            {
                // eat it!
            }
        }

        public new void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (base.Container != null))
            {
                base.Container.Dispose();
            }
            try
            {
                if (theBaseStream.CanRead)
                {
                    theBaseStream.Close();
                    GC.ReRegisterForFinalize(theBaseStream);
                }
            }
            catch
            {
                // ignore exception - bug with USB-serial adapters
            }
            base.Dispose(disposing);
        }
    }
}
