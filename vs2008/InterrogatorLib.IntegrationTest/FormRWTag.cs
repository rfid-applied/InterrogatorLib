using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace InterrogatorLib.IntegrationTest
{
    public partial class FormRWTag : Form
    {
        public FormRWTag()
        {
            InitializeComponent();
            this.Closing += new CancelEventHandler(FormRWTag_Closing);
        }

        void FormRWTag_Closing(object sender, CancelEventArgs e)
        {
            Deinit();
        }
        ReaderService _service;

        void ReportException(Exception e)
        {
            System.Diagnostics.Debug.Write(e.Message + "\n" + e.StackTrace);
            MessageBox.Show("Error initializing: " + e.Message);
        }

        void Subscribe(bool sub)
        {
            if (sub)
            {
                _service.TagRead += new EventHandler<GenericEventArgs<IReadResult>>(_service_TagRead);
                _service.TagWritten += new EventHandler<GenericEventArgs<bool>>(_service_TagWritten);
                _service.OverwriteStatus += new EventHandler<GenericEventArgs<string>>(_service_OverwriteStatus);
            }
            else
            {
                _service.TagRead -= new EventHandler<GenericEventArgs<IReadResult>>(_service_TagRead);
                _service.TagWritten -= new EventHandler<GenericEventArgs<bool>>(_service_TagWritten);
                _service.OverwriteStatus -= new EventHandler<GenericEventArgs<string>>(_service_OverwriteStatus);
            }
        }

        void _service_TagWritten(object sender, GenericEventArgs<bool> e)
        {
            if (e.Value)
                MessageBox.Show("Success!");
            else
                MessageBox.Show("Failure!");
        }

        void Deinit()
        {
            if (_service != null)
            {
                try
                {
                    Subscribe(false);
                    _service.Shutdown();
                    _service = null;
                }
                catch (Exception e)
                {
                    ReportException(e);
                    return;
                }
            }
        }

        void DoInvoke(Action act)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(act);
            }
            else
                act();
        }

        void _service_OverwriteStatus(object sender, GenericEventArgs<string> e)
        {
            DoInvoke(() =>
            {
                statusBar1.Text = e.Value;
            });
        }

        void _service_TagRead(object sender, GenericEventArgs<IReadResult> e)
        {
            if (!string.IsNullOrEmpty(e.Value.Reason))
                MessageBox.Show("Failed due to: " + e.Value.Reason);
            else
                MessageBox.Show("OK: EPC URI is: " + e.Value.URI);
        }

        void InitM3Orange()
        {
            Deinit();

            try
            {
                var reader = new RFIDApplied.InterrogatorLib.M3ReaderLowLevel();
                reader.Initialize();

                _service = new ReaderService(reader);
                Subscribe(true);
            }
            catch (Exception e)
            {
                _service = null;
                ReportException(e);
            }
        }

        void InitM3OrangeUHFGun()
        {
            Deinit();

            try
            {
                var reader = new RFIDApplied.InterrogatorLib.M3ReaderGunLowLevel();
                reader.Initialize();

                _service = new ReaderService(reader);
                Subscribe(true);
            }
            catch (Exception e)
            {
                _service = null;
                ReportException(e);
            }
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            // M3 Orange driver chosen
            InitM3Orange();
        }

        private void menuItem3_Click(object sender, EventArgs e)
        {
            // M3 Orange UHF Gun driver chosen
            InitM3OrangeUHFGun();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_service == null)
            {
                MessageBox.Show("Initialize the reader first");
                return;
            }
            _service.Read();
        }
    }
}