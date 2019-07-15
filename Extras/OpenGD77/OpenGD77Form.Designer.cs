namespace DMR
{
	partial class OpenGD77Form
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

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
			this.comboBoxCOMPorts = new System.Windows.Forms.ComboBox();
			this.lblCommPort = new System.Windows.Forms.Label();
			this.buttonRefreshCOMPortlist = new System.Windows.Forms.Button();
			this.btnSelectCommPort = new System.Windows.Forms.Button();
			this.btnBackupEEPROM = new System.Windows.Forms.Button();
			this.progressBar1 = new System.Windows.Forms.ProgressBar();
			this.btnBackupFlash = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// comboBoxCOMPorts
			// 
			this.comboBoxCOMPorts.FormattingEnabled = true;
			this.comboBoxCOMPorts.Location = new System.Drawing.Point(76, 10);
			this.comboBoxCOMPorts.Name = "comboBoxCOMPorts";
			this.comboBoxCOMPorts.Size = new System.Drawing.Size(121, 21);
			this.comboBoxCOMPorts.TabIndex = 0;
			// 
			// lblCommPort
			// 
			this.lblCommPort.AutoSize = true;
			this.lblCommPort.Location = new System.Drawing.Point(13, 13);
			this.lblCommPort.Name = "lblCommPort";
			this.lblCommPort.Size = new System.Drawing.Size(57, 13);
			this.lblCommPort.TabIndex = 1;
			this.lblCommPort.Text = "Comm port";
			// 
			// buttonRefreshCOMPortlist
			// 
			this.buttonRefreshCOMPortlist.Location = new System.Drawing.Point(298, 10);
			this.buttonRefreshCOMPortlist.Name = "buttonRefreshCOMPortlist";
			this.buttonRefreshCOMPortlist.Size = new System.Drawing.Size(75, 23);
			this.buttonRefreshCOMPortlist.TabIndex = 2;
			this.buttonRefreshCOMPortlist.Text = "Refresh ports list";
			this.buttonRefreshCOMPortlist.UseVisualStyleBackColor = true;
			this.buttonRefreshCOMPortlist.Click += new System.EventHandler(this.buttonRefreshCOMPortlist_Click);
			// 
			// btnSelectCommPort
			// 
			this.btnSelectCommPort.Location = new System.Drawing.Point(217, 10);
			this.btnSelectCommPort.Name = "btnSelectCommPort";
			this.btnSelectCommPort.Size = new System.Drawing.Size(75, 23);
			this.btnSelectCommPort.TabIndex = 2;
			this.btnSelectCommPort.Text = "Select";
			this.btnSelectCommPort.UseVisualStyleBackColor = true;
			this.btnSelectCommPort.Click += new System.EventHandler(this.btnSelectCommPort_Click);
			// 
			// btnBackupEEPROM
			// 
			this.btnBackupEEPROM.Location = new System.Drawing.Point(16, 48);
			this.btnBackupEEPROM.Name = "btnBackupEEPROM";
			this.btnBackupEEPROM.Size = new System.Drawing.Size(131, 23);
			this.btnBackupEEPROM.TabIndex = 3;
			this.btnBackupEEPROM.Text = "Backup EEPROM";
			this.btnBackupEEPROM.UseVisualStyleBackColor = true;
			this.btnBackupEEPROM.Click += new System.EventHandler(this.btnBackupEEPROM_Click);
			// 
			// progressBar1
			// 
			this.progressBar1.Location = new System.Drawing.Point(16, 251);
			this.progressBar1.Name = "progressBar1";
			this.progressBar1.Size = new System.Drawing.Size(466, 23);
			this.progressBar1.TabIndex = 4;
			// 
			// btnBackupFlash
			// 
			this.btnBackupFlash.Location = new System.Drawing.Point(16, 77);
			this.btnBackupFlash.Name = "btnBackupFlash";
			this.btnBackupFlash.Size = new System.Drawing.Size(131, 23);
			this.btnBackupFlash.TabIndex = 5;
			this.btnBackupFlash.Text = "Backup Flash";
			this.btnBackupFlash.UseVisualStyleBackColor = true;
			this.btnBackupFlash.Click += new System.EventHandler(this.btnBackupFlash_Click);
			// 
			// OpenGD77Form
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(505, 290);
			this.Controls.Add(this.btnBackupFlash);
			this.Controls.Add(this.progressBar1);
			this.Controls.Add(this.btnBackupEEPROM);
			this.Controls.Add(this.btnSelectCommPort);
			this.Controls.Add(this.buttonRefreshCOMPortlist);
			this.Controls.Add(this.lblCommPort);
			this.Controls.Add(this.comboBoxCOMPorts);
			this.Name = "OpenGD77Form";
			this.Text = "OpenGD77 Support";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox comboBoxCOMPorts;
		private System.Windows.Forms.Label lblCommPort;
		private System.Windows.Forms.Button buttonRefreshCOMPortlist;
		private System.Windows.Forms.Button btnSelectCommPort;
		private System.Windows.Forms.Button btnBackupEEPROM;
		private System.Windows.Forms.ProgressBar progressBar1;
		private System.Windows.Forms.Button btnBackupFlash;
	}
}