namespace Microsoft.Data.ConnectionUI
{
	public partial class OracleConnectionUIControl
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OracleConnectionUIControl));
            this.hostnameLabel = new System.Windows.Forms.Label();
            this.hostnameTextBox = new System.Windows.Forms.TextBox();
            this.logonGroupBox = new System.Windows.Forms.GroupBox();
            this.loginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.userNameLabel = new System.Windows.Forms.Label();
            this.userNameTextBox = new System.Windows.Forms.TextBox();
            this.passwordLabel = new System.Windows.Forms.Label();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.savePasswordCheckBox = new System.Windows.Forms.CheckBox();
            this.portTextBox = new System.Windows.Forms.TextBox();
            this.portLabel = new System.Windows.Forms.Label();
            this.serviceNameTextBox = new System.Windows.Forms.TextBox();
            this.serviceNameLabel = new System.Windows.Forms.Label();
            this.logonGroupBox.SuspendLayout();
            this.loginTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // hostnameLabel
            // 
            resources.ApplyResources(this.hostnameLabel, "hostnameLabel");
            this.hostnameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.hostnameLabel.Name = "hostnameLabel";
            // 
            // hostnameTextBox
            // 
            resources.ApplyResources(this.hostnameTextBox, "hostnameTextBox");
            this.hostnameTextBox.Name = "hostnameTextBox";
            this.hostnameTextBox.TextChanged += new System.EventHandler(this.SetAtribute);
            this.hostnameTextBox.Leave += new System.EventHandler(this.TrimControlText);
            // 
            // logonGroupBox
            // 
            resources.ApplyResources(this.logonGroupBox, "logonGroupBox");
            this.logonGroupBox.Controls.Add(this.loginTableLayoutPanel);
            this.logonGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.logonGroupBox.Name = "logonGroupBox";
            this.logonGroupBox.TabStop = false;
            // 
            // loginTableLayoutPanel
            // 
            resources.ApplyResources(this.loginTableLayoutPanel, "loginTableLayoutPanel");
            this.loginTableLayoutPanel.Controls.Add(this.userNameLabel, 0, 0);
            this.loginTableLayoutPanel.Controls.Add(this.userNameTextBox, 1, 0);
            this.loginTableLayoutPanel.Controls.Add(this.passwordLabel, 0, 1);
            this.loginTableLayoutPanel.Controls.Add(this.passwordTextBox, 1, 1);
            this.loginTableLayoutPanel.Controls.Add(this.savePasswordCheckBox, 1, 2);
            this.loginTableLayoutPanel.Name = "loginTableLayoutPanel";
            // 
            // userNameLabel
            // 
            resources.ApplyResources(this.userNameLabel, "userNameLabel");
            this.userNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.userNameLabel.Name = "userNameLabel";
            // 
            // userNameTextBox
            // 
            resources.ApplyResources(this.userNameTextBox, "userNameTextBox");
            this.userNameTextBox.Name = "userNameTextBox";
            this.userNameTextBox.TextChanged += new System.EventHandler(this.SetUserName);
            this.userNameTextBox.Leave += new System.EventHandler(this.TrimControlText);
            // 
            // passwordLabel
            // 
            resources.ApplyResources(this.passwordLabel, "passwordLabel");
            this.passwordLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.passwordLabel.Name = "passwordLabel";
            // 
            // passwordTextBox
            // 
            resources.ApplyResources(this.passwordTextBox, "passwordTextBox");
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.UseSystemPasswordChar = true;
            this.passwordTextBox.TextChanged += new System.EventHandler(this.SetPassword);
            // 
            // savePasswordCheckBox
            // 
            resources.ApplyResources(this.savePasswordCheckBox, "savePasswordCheckBox");
            this.savePasswordCheckBox.Name = "savePasswordCheckBox";
            this.savePasswordCheckBox.CheckedChanged += new System.EventHandler(this.SetSavePassword);
            // 
            // portTextBox
            // 
            resources.ApplyResources(this.portTextBox, "portTextBox");
            this.portTextBox.Name = "portTextBox";
            this.portTextBox.TextChanged += new System.EventHandler(this.SetAtribute);
            // 
            // portLabel
            // 
            resources.ApplyResources(this.portLabel, "portLabel");
            this.portLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.portLabel.Name = "portLabel";
            // 
            // serviceNameTextBox
            // 
            resources.ApplyResources(this.serviceNameTextBox, "serviceNameTextBox");
            this.serviceNameTextBox.Name = "serviceNameTextBox";
            this.serviceNameTextBox.TextChanged += new System.EventHandler(this.SetAtribute);
            // 
            // serviceNameLabel
            // 
            resources.ApplyResources(this.serviceNameLabel, "serviceNameLabel");
            this.serviceNameLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.serviceNameLabel.Name = "serviceNameLabel";
            // 
            // OracleConnectionUIControl
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.serviceNameTextBox);
            this.Controls.Add(this.serviceNameLabel);
            this.Controls.Add(this.portTextBox);
            this.Controls.Add(this.portLabel);
            this.Controls.Add(this.logonGroupBox);
            this.Controls.Add(this.hostnameTextBox);
            this.Controls.Add(this.hostnameLabel);
            this.Name = "OracleConnectionUIControl";
            this.logonGroupBox.ResumeLayout(false);
            this.logonGroupBox.PerformLayout();
            this.loginTableLayoutPanel.ResumeLayout(false);
            this.loginTableLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label hostnameLabel;
		private System.Windows.Forms.GroupBox logonGroupBox;
		private System.Windows.Forms.TableLayoutPanel loginTableLayoutPanel;
		private System.Windows.Forms.Label userNameLabel;
		private System.Windows.Forms.TextBox userNameTextBox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.CheckBox savePasswordCheckBox;
        private System.Windows.Forms.TextBox hostnameTextBox;
        private System.Windows.Forms.TextBox portTextBox;
        private System.Windows.Forms.Label portLabel;
        private System.Windows.Forms.TextBox serviceNameTextBox;
        private System.Windows.Forms.Label serviceNameLabel;
    }
}
