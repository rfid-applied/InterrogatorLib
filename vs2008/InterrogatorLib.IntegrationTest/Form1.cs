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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private InterrogatorLib.IInterrogator _interrogator;

        private void HandleClick(object sender, EventArgs e)
        {
            try
            {
                if (sender == FClearEPCList)
                {
                    FEPCListView.Items.Clear();
                }
                else if (sender == FStartInventory || sender == FStopInventory || sender == FShutdownReader)
                {
                    if (_interrogator == null)
                    {
                        MessageBox.Show("Init a reader first!");
                        return;
                    }
                    if (sender == FStartInventory)
                    {
                        _interrogator.StartInventory(new byte[0], 0, 0, Convert.ToInt32(FTimeout.Value));
                    }
                    else if (sender == FStopInventory)
                    {
                        _interrogator.StopInventory();
                    }
                    else if (sender == FShutdownReader)
                    {
                        _interrogator.Dispose();
                        _interrogator = null;
                    }
                }
                else if (sender == FInitDotel || sender == FInitM3)
                {
                    if (_interrogator != null)
                    {
                        MessageBox.Show("Shutdown a reader first!");
                        return;
                    }
                    if (sender == FInitDotel)
                    {
#if false
                        _interrogator = new InterrogatorLib.DotelInterrogator();
#else
                        MessageBox.Show("DOTEL not supported!");
                        return;
#endif
                    }
                    else
                    if (sender == FInitM3)
                    {
                        _interrogator = new InterrogatorLib.M3OrangeInterrogator();
                    }
                    InitInterrogator();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void InitInterrogator()
        {
            _interrogator.EPCEvent += new InterrogatorLib.EPCEventHandler(OnEpcEvent);
            FPowerSlider.Value = _interrogator.Power;
        }

        void OnEpcEvent(object sender, InterrogatorLib.EPCEventArgs args)
        {
            foreach (var epc in args.EPC)
            {
                FEPCListView.Items.Add(new ListViewItem() { Text = epc });
            }
        }

        private void QuitClicked(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}