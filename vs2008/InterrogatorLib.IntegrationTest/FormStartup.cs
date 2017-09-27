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
    public partial class FormStartup : Form
    {
        public FormStartup()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (var form = new Form1())
            {
                form.ShowDialog();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var form = new FormRWTag())
            {
                form.ShowDialog();
            }
        }
    }
}