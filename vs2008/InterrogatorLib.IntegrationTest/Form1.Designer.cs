namespace InterrogatorLib.IntegrationTest
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.MainMenu mainMenu1;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainMenu1 = new System.Windows.Forms.MainMenu();
            this.FInitDotel = new System.Windows.Forms.Button();
            this.FInitM3 = new System.Windows.Forms.Button();
            this.FStartInventory = new System.Windows.Forms.Button();
            this.FStopInventory = new System.Windows.Forms.Button();
            this.FShutdownReader = new System.Windows.Forms.Button();
            this.FEPCListView = new System.Windows.Forms.ListView();
            this.EPC = new System.Windows.Forms.ColumnHeader();
            this.FClearEPCList = new System.Windows.Forms.Button();
            this.FQuit = new System.Windows.Forms.Button();
            this.FPowerSlider = new System.Windows.Forms.TrackBar();
            this.FPowerLabel = new System.Windows.Forms.Label();
            this.FTimeoutLabel = new System.Windows.Forms.Label();
            this.FTimeout = new System.Windows.Forms.NumericUpDown();
            this.SuspendLayout();
            // 
            // FInitDotel
            // 
            this.FInitDotel.Location = new System.Drawing.Point(13, 3);
            this.FInitDotel.Name = "FInitDotel";
            this.FInitDotel.Size = new System.Drawing.Size(180, 20);
            this.FInitDotel.TabIndex = 0;
            this.FInitDotel.Text = "Init DOTEL Reader";
            this.FInitDotel.Click += new System.EventHandler(this.HandleClick);
            // 
            // FInitM3
            // 
            this.FInitM3.Location = new System.Drawing.Point(13, 29);
            this.FInitM3.Name = "FInitM3";
            this.FInitM3.Size = new System.Drawing.Size(180, 20);
            this.FInitM3.TabIndex = 1;
            this.FInitM3.Text = "Init M3 Orange Reader";
            this.FInitM3.Click += new System.EventHandler(this.HandleClick);
            // 
            // FStartInventory
            // 
            this.FStartInventory.Location = new System.Drawing.Point(118, 149);
            this.FStartInventory.Name = "FStartInventory";
            this.FStartInventory.Size = new System.Drawing.Size(119, 20);
            this.FStartInventory.TabIndex = 2;
            this.FStartInventory.Text = "Start Inventory";
            this.FStartInventory.Click += new System.EventHandler(this.HandleClick);
            // 
            // FStopInventory
            // 
            this.FStopInventory.Location = new System.Drawing.Point(3, 149);
            this.FStopInventory.Name = "FStopInventory";
            this.FStopInventory.Size = new System.Drawing.Size(111, 20);
            this.FStopInventory.TabIndex = 3;
            this.FStopInventory.Text = "Stop Inventory";
            this.FStopInventory.Click += new System.EventHandler(this.HandleClick);
            // 
            // FShutdownReader
            // 
            this.FShutdownReader.Location = new System.Drawing.Point(13, 55);
            this.FShutdownReader.Name = "FShutdownReader";
            this.FShutdownReader.Size = new System.Drawing.Size(180, 20);
            this.FShutdownReader.TabIndex = 4;
            this.FShutdownReader.Text = "Shutdown Reader";
            this.FShutdownReader.Click += new System.EventHandler(this.HandleClick);
            // 
            // FEPCListView
            // 
            this.FEPCListView.Columns.Add(this.EPC);
            this.FEPCListView.Location = new System.Drawing.Point(4, 175);
            this.FEPCListView.Name = "FEPCListView";
            this.FEPCListView.Size = new System.Drawing.Size(233, 63);
            this.FEPCListView.TabIndex = 5;
            // 
            // EPC
            // 
            this.EPC.Text = "EPC";
            this.EPC.Width = 60;
            // 
            // FClearEPCList
            // 
            this.FClearEPCList.Location = new System.Drawing.Point(4, 245);
            this.FClearEPCList.Name = "FClearEPCList";
            this.FClearEPCList.Size = new System.Drawing.Size(233, 20);
            this.FClearEPCList.TabIndex = 6;
            this.FClearEPCList.Text = "Clear List";
            this.FClearEPCList.Click += new System.EventHandler(this.HandleClick);
            // 
            // FQuit
            // 
            this.FQuit.Location = new System.Drawing.Point(13, 81);
            this.FQuit.Name = "FQuit";
            this.FQuit.Size = new System.Drawing.Size(72, 20);
            this.FQuit.TabIndex = 7;
            this.FQuit.Text = "Quit";
            this.FQuit.Click += new System.EventHandler(this.QuitClicked);
            // 
            // FPowerSlider
            // 
            this.FPowerSlider.Location = new System.Drawing.Point(110, 124);
            this.FPowerSlider.Maximum = 200;
            this.FPowerSlider.Name = "FPowerSlider";
            this.FPowerSlider.Size = new System.Drawing.Size(127, 19);
            this.FPowerSlider.TabIndex = 8;
            // 
            // FPowerLabel
            // 
            this.FPowerLabel.Location = new System.Drawing.Point(4, 124);
            this.FPowerLabel.Name = "FPowerLabel";
            this.FPowerLabel.Size = new System.Drawing.Size(100, 20);
            this.FPowerLabel.Text = "Power, dB";
            // 
            // FTimeoutLabel
            // 
            this.FTimeoutLabel.Location = new System.Drawing.Point(4, 101);
            this.FTimeoutLabel.Name = "FTimeoutLabel";
            this.FTimeoutLabel.Size = new System.Drawing.Size(100, 20);
            this.FTimeoutLabel.Text = "Timeout, sec.";
            // 
            // FTimeout
            // 
            this.FTimeout.Location = new System.Drawing.Point(118, 101);
            this.FTimeout.Name = "FTimeout";
            this.FTimeout.Size = new System.Drawing.Size(119, 23);
            this.FTimeout.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.FTimeout);
            this.Controls.Add(this.FTimeoutLabel);
            this.Controls.Add(this.FPowerLabel);
            this.Controls.Add(this.FPowerSlider);
            this.Controls.Add(this.FQuit);
            this.Controls.Add(this.FClearEPCList);
            this.Controls.Add(this.FEPCListView);
            this.Controls.Add(this.FShutdownReader);
            this.Controls.Add(this.FStopInventory);
            this.Controls.Add(this.FStartInventory);
            this.Controls.Add(this.FInitM3);
            this.Controls.Add(this.FInitDotel);
            this.Menu = this.mainMenu1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button FInitDotel;
        private System.Windows.Forms.Button FInitM3;
        private System.Windows.Forms.Button FStartInventory;
        private System.Windows.Forms.Button FStopInventory;
        private System.Windows.Forms.Button FShutdownReader;
        private System.Windows.Forms.ListView FEPCListView;
        private System.Windows.Forms.ColumnHeader EPC;
        private System.Windows.Forms.Button FClearEPCList;
        private System.Windows.Forms.Button FQuit;
        private System.Windows.Forms.TrackBar FPowerSlider;
        private System.Windows.Forms.Label FPowerLabel;
        private System.Windows.Forms.Label FTimeoutLabel;
        private System.Windows.Forms.NumericUpDown FTimeout;
    }
}

