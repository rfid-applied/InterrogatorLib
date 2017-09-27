namespace InterrogatorLib.IntegrationTest
{
    partial class FormRWTag
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
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItemM3Orange = new System.Windows.Forms.MenuItem();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.statusBar1 = new System.Windows.Forms.StatusBar();
            this.SuspendLayout();
            // 
            // mainMenu1
            // 
            this.mainMenu1.MenuItems.Add(this.menuItem1);
            // 
            // menuItem1
            // 
            this.menuItem1.MenuItems.Add(this.menuItemM3Orange);
            this.menuItem1.MenuItems.Add(this.menuItem3);
            this.menuItem1.Text = "Driver";
            // 
            // menuItemM3Orange
            // 
            this.menuItemM3Orange.Text = "M3 Orange";
            this.menuItemM3Orange.Click += new System.EventHandler(this.menuItem2_Click);
            // 
            // menuItem3
            // 
            this.menuItem3.Text = "M3 Orange UHF Gun";
            this.menuItem3.Click += new System.EventHandler(this.menuItem3_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(75, 93);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(89, 54);
            this.button1.TabIndex = 0;
            this.button1.Text = "Read EPC";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // statusBar1
            // 
            this.statusBar1.Location = new System.Drawing.Point(0, 246);
            this.statusBar1.Name = "statusBar1";
            this.statusBar1.Size = new System.Drawing.Size(240, 22);
            // 
            // FormRWTag
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(240, 268);
            this.Controls.Add(this.statusBar1);
            this.Controls.Add(this.button1);
            this.Menu = this.mainMenu1;
            this.Name = "FormRWTag";
            this.Text = "Read/Write tag";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItemM3Orange;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.StatusBar statusBar1;
    }
}