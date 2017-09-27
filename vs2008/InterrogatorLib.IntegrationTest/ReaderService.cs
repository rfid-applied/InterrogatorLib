//#define CHATTY_READER // define if you want to receive debug output in the file CHATTY_OUTPUT_FILE

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using InterrogatorLib;
using System.Diagnostics;

namespace InterrogatorLib.IntegrationTest
{
    public interface IReadResult
    {
        // error message
        string Reason { get; }
        // if read succeeded, then this should contain the Pure Identity EPC URI
        string URI { get; }
    }
    public class GenericEventArgs<T> : EventArgs
    {
        public GenericEventArgs(T v) { _value = v; }
        T _value;
        public T Value { get { return _value; } }
    }

    public interface IReaderService
    {
        bool Ready { get; }
        void Shutdown();
        void Read();
        event EventHandler<GenericEventArgs<IReadResult>> TagRead;

        void Write(IReadResult result, string epc);
        event EventHandler<GenericEventArgs<bool>> TagWritten;

        // overwrite the next tag with the new EPC, generated via MCS
        void Overwrite(string GTIN14, int? serial);
        // NOTE: will raise TagRead, TagWritten events!
        event EventHandler<GenericEventArgs<string>> OverwriteStatus;
    }

    public class ReadResult : IReadResult
    {
        public ReadResult() { }
        public ReadResult(string error)
        {
            Reason = error;
        }
        // error message
        public string Reason { get; set; }
        // if read succeeded, then this should contain the Pure Identity EPC URI
        public string URI { get; set; }
    }

    public class ReaderService : IReaderService
    {
        IReaderLowLevel _reader;

        const string CHATTY_OUTPUT_FILE = "service.reader.txt";

        public ReaderService(IReaderLowLevel reader)
        {
            _reader = reader;

            _reader.Initialize();

#if CHATTY_READER
        Trace.Listeners.Add(new TextWriterTraceListener(CHATTY_OUTPUT_FILE));
#endif

        /*
         * NOTE: from https://www.dataloggerinc.com/downloads/caenrfid/CAEN_RFID_API_UserMan_rev_04.pdf
         */
            /*
            {
                double Gain = 8.0;
                double Loss = 1.5;
                double ERPPower = 1500.0;
                int OutPower;
                OutPower = (int)(ERPPower / Math.Pow(10, ((Gain - Loss - 2.14) / 10)));
                M3Client.reader.SetPower(OutPower);

                Console.WriteLine("Set effective radiate power to {0} mW", ERPPower);
            }
            {
                double Gain = 8.0;
                double Loss = 1.5;
                double ERPPower;
                int OutPower;
                OutPower = M3Client.reader.GetPower();
                ERPPower = ((double)OutPower) * ((double)Math.Pow(10, ((Gain - Loss - 2.14) / 10)));
                Console.WriteLine("Current effective radiate power, mW: {0}", ERPPower);
            }*/
        }

        public bool Ready
        {
            get
            {
                return _reader.IsReady();
            }
        }

        public void Shutdown()
        {
            if (_reader != null)
            {
                _reader.Dispose();
                _reader = null;
            }
        }

        public void Read()
        {
            // fork an async task...
            object req = null;
            var w = new System.Threading.WaitCallback(MyWorkerThread_Read);
            System.Threading.ThreadPool.QueueUserWorkItem(w, req);
        }
        public event EventHandler<GenericEventArgs<IReadResult>> TagRead;

        void MyWorkerThread_Read(object request)
        {
            if (!_reader.IsReady())
                _reader.Initialize();

            object mtag = null;
            // TODO: wait for a signal to cancel?
            // - how to implement such a signal?
            if (OverwriteStatus != null)
                OverwriteStatus(this, new GenericEventArgs<string>("Singulating tag..."));
            const int RETRIES = 4;
            {
                int ntries = 0;
                while (mtag == null && ntries < RETRIES) // AS-20160718: decreasing the retries greatly
                {
                    mtag = _reader.SingulateTag();
                    System.Threading.Thread.Sleep(100);
                    ntries++;
                }
            }
            if (mtag == null)
            {
                if (TagRead != null)
                    TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to singulate tag!")));
                return;
            }

            if (OverwriteStatus != null)
                OverwriteStatus(this, new GenericEventArgs<string>("Tag singulated. Reading EPC..."));

            string uri;
            var res = DoWithRetries<string>(RETRIES, out uri, (out string uri0) =>
            {
                DecodeError err;

                // NOTE: when reading, be prepared to supply your password!
                // how to handle that??

                // try to make sense of the tag first
                TagEPCURI_SGTIN sgtin0;
                err = _reader.ReadEPC_SGTIN(mtag, out sgtin0);
                if (err == DecodeError.None)
                {
                    uri0 = sgtin0.Identity.URI;
                    return err;
                }

                // try to fallback to raw URI
                TagRaw raw0;
                err = _reader.ReadEPC_Raw(mtag, out raw0);
                if (err == DecodeError.None)
                {
                    var sbuf = new StringBuilder();
                    raw0.GetURI(sbuf);
                    uri0 = sbuf.ToString();
                    return err;
                }

                uri0 = null;
                return err;
            });
            switch (res)
            {
                case DecodeError.IO:
                    // could not read the EPC after the retries, bail out
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to read EPC! Try again.")));
                    return;
                case DecodeError.Invalid:
                    // invalid tag, have to use the GTIN provided by the server
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to decode EPC (empty or invalid format)")));
                    return;
                case DecodeError.None:
                    // good tag, let's roll
                    break;
            }
            if (TagRead != null)
            {
                TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult() { URI = uri }));
            }
        }

        public void Write(IReadResult result, string epc)
        {
            /*
            var res = (ReadResult)result;

            var data = M3Client.HexStringToByteArray(epc);
            if (data.Length > MEM_SIZE_BYTES)
                throw new InvalidOperationException("Write tag: size of EPC is expected to be " + MEM_SIZE_BYTES.ToString() + " but it is " + data.Length.ToString());

            var r = M3Client.WriteTag(res.Tag, MEMBANK, MEM_OFFSET, data.Length, data);*/
            var r = false;
            if (TagWritten != null)
                TagWritten(this, new GenericEventArgs<bool>(r));
        }

        public event EventHandler<GenericEventArgs<bool>> TagWritten;

        public event EventHandler<GenericEventArgs<string>> OverwriteStatus;

        public struct OverwriteRequest
        {
            public OverwriteRequest(string gtin14, int? serial)
            {
                _gtin14 = gtin14;
                _serial = serial;
            }
            string _gtin14;
            int? _serial;
            public string GTIN14 { get { return _gtin14; } set { _gtin14 = value; } }
            public int? Serial { get { return _serial; } set { _serial = value; } }
        }

        public void Overwrite(string GTIN14, int? serial)
        {
            // fork an async task...
            var req = new OverwriteRequest(GTIN14, serial);
            var w = new System.Threading.WaitCallback(MyWorkerThread);
            System.Threading.ThreadPool.QueueUserWorkItem(w, req);
        }

        delegate V MyDelegate<U, V>(out U output);
        static DecodeError DoWithRetries<A>(int maxRetries, out A datum, MyDelegate<A, DecodeError> f)
        {
            int retries = 0;
            bool stop = false;
            DecodeError res = DecodeError.Invalid;
            datum = default(A);
            while (!stop)
            {
                res = f(out datum);
                switch (res)
                {
                    case DecodeError.IO:
                        if (retries < maxRetries)
                        {
                            retries++;
                            //System.Threading.Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            stop = true; // failed hard
                        }
                        break;
                    default: // invalid or no error
                        stop = true;
                        break;
                }
            }
            return res;
        }
        static bool WithRetries<A>(int maxRetries, A datum, Func<A, bool> f)
        {
            int retries = 0;
            bool stop = false;
            bool res = false;
            while (!stop)
            {
                res = f(datum);
                if (!res)
                {
                    if (retries < maxRetries)
                    {
                        retries++;
                        //System.Threading.Thread.Sleep(100);
                        continue;
                    }
                    else
                    {
                        stop = true;
                    }
                }
                else
                {
                    stop = true;
                }
            }
            return res;
        }

        void MyWorkerThread(object request)
        {
            var req0 = (OverwriteRequest)request;
            // parse GTIN14 string, how?
            // we need to extract CompanyPrefix (as long) and ItemRef (as long)
            var GTIN14AndGCPLen = req0.GTIN14;
            IdentityEPCURI_SGTIN primedURI;
            {
                var ix = GTIN14AndGCPLen.IndexOf(';');
                if (ix == -1)
                {
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Invalid GTIN-14: " + GTIN14AndGCPLen)));
                    return;
                }
                else
                {
                    string GTIN14;
                    int GCPLen;
                    GTIN14 = GTIN14AndGCPLen.Substring(0, ix);
                    GCPLen = int.Parse(GTIN14AndGCPLen.Substring(ix + 1, GTIN14AndGCPLen.Length - ix - 1));
                    //Console.WriteLine("{0}, {1}", GTIN14, GPCLen);

                    if (!InterrogatorLib.Decoder.ParseFromGTIN14(GTIN14, GCPLen, out primedURI))
                    {
                        if (TagRead != null)
                            TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to parse GTIN-14! Unable to continue.")));
                        return;
                    }
                }
            }

            if (!_reader.IsReady())
                _reader.Initialize();

            // perform reads, then immediately writes, until you finally do it
            object mtag = null;
            // TODO: wait for a signal to cancel?
            // - how to implement such a signal?
            if (OverwriteStatus != null)
                OverwriteStatus(this, new GenericEventArgs<string>("Singulating tag..."));
            const int RETRIES = 4;
            {
                int ntries = 0;
                while (mtag == null && ntries < RETRIES) // AS-20160618: decreasing greatly
                {
                    mtag = _reader.SingulateTag();
                    System.Threading.Thread.Sleep(100);
                    ntries++;
                }
            }
            if (mtag == null)
            {
                if (TagRead != null)
                    TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to singulate tag!")));
                return;
            }

            if (OverwriteStatus != null)
                OverwriteStatus(this, new GenericEventArgs<string>("Tag singulated, reading TID"));

            TagEPCURI_SGTIN epc1;
            if (req0.Serial == null)
            {
                TID tid;
                var res = DoWithRetries<TID>(RETRIES, out tid, (out TID tid0) =>
                {
                    return _reader.ReadTID(mtag, out tid0);
                });
                switch (res)
                {
                    case DecodeError.Invalid:
                        // signal to the upper layer that this tag is bad, and bail out
                        if (TagRead != null)
                            TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Chip unsupported! Unable to continue.")));
                        return;
                    case DecodeError.IO:
                        // signal to the user that this tag could not be read, and bail out
                        if (TagRead != null)
                            TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to decode TID! Try again.")));
                        return;
                    case DecodeError.None:
                        break; // OK
                }
                if (!tid.IsSerialized)
                {
                    // signal to the user that this tag does not support MCS, and bail out
                    // TODO: we could probably handle this too...
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Tag chip does is not serialized! Unable to continue")));
                    return;
                }

                if (OverwriteStatus != null)
                    OverwriteStatus(this, new GenericEventArgs<string>("TID read. Reading EPC..."));

                TagEPCURI_SGTIN uri;
                res = DoWithRetries<TagEPCURI_SGTIN>(RETRIES, out uri, (out TagEPCURI_SGTIN uri0) =>
                {
                    // NOTE: when reading, be prepared to supply your password!
                    // how to handle that??
                    return _reader.ReadEPC_SGTIN(mtag, out uri0);
                });
                switch (res)
                {
                    case DecodeError.IO:
                    // could not the EPC after the retries, bail out
                    //if (TagRead != null)
                    //    TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to decode EPC! Try again.")));
                    //return;
                    case DecodeError.Invalid:
                        // invalid tag, have to use the GTIN provided by the server
                        uri.Identity = primedURI;
                        break;
                    case DecodeError.None:
                        // good tag, let's roll
                        break;
                }

                if (!MCS.GenerateEPC(ref tid, ref uri, out epc1))
                {
                    // failed to assign a new EPC
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to assign new EPC! Unable to continue.")));
                    return;
                }
            }
            else
            {
                // serial supplied, write it as-is
                var serial = req0.Serial.Value;
                if (serial > int.MaxValue)
                {
                    // overflowed...
                    if (TagRead != null)
                        TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to write new EPC: serial too big! Unable to continue.")));
                    return;
                }

                epc1 = default(TagEPCURI_SGTIN);
                epc1.Attribs = 0x0; // ??? not used
                epc1.Filter = 0x0; // 0: all others

                // C# needs some Rust-style borrowing stuff for value types!
                // (or, some linear types would be even nicer, as they are simpler)
                var identity = primedURI;
                Trace.WriteLine(string.Format("identity EPC before serial assignment {0}", identity.URI));
                identity.SetSerial((long)serial);
                epc1.Identity = identity;
                Trace.WriteLine(string.Format("identity EPC after serial assignment {0}", identity.URI));
            }

#if CHATTY_READER
            Trace.WriteLine(string.Format("MCS: TID serial {0}, EPC {1}, result EPC {2}", BitConverter.ToString(tid.Serial), uri.Identity.URI, epc1.Identity.URI));
#endif
            //System.Windows.Forms.MessageBox.Show(String.Format("New EPC SGTIN Pure Identity Tag URI {0}", epc1.Identity.URI));

            if (OverwriteStatus != null)
                OverwriteStatus(this, new GenericEventArgs<string>("Writing EPC..."));

            // TODO: write the new tag and permalock it!
            bool write_res = WithRetries<TagEPCURI_SGTIN>(RETRIES, epc1, (uri1) =>
            {
                return _reader.WriteEPC(mtag, uri1);
            });
            if (!write_res)
            {
                // unable to write the new data, bail out
                if (TagRead != null)
                    TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult("Unable to write new EPC! Try again.")));
                return;
            }

            // permalock and report success
            if (TagRead != null)
                TagRead(this, new GenericEventArgs<IReadResult>(new ReadResult() { URI = epc1.Identity.URI }));
        }
    }
}
