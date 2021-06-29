namespace Microsoft.Data.ConnectionUI
{
	public partial class AccessConnectionUIControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AccessConnectionUIControl));
			this.databaseFileLabel = new System.Windows.Forms.Label();
			this.databaseFileTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.databaseFileTextBox = new System.Windows.Forms.TextBox();
			this.browseButton = new System.Windows.Forms.Button();
			this.logonGroupBox = new System.Windows.Forms.GroupBox();
			this.loginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.userNameLabel = new System.Windows.Forms.Label();
			this.userNameTextBox = new System.Windows.Forms.TextBox();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.passwordTextBox = new System.Windows.Forms.TextBox();
			this.savePasswordCheckBox = new System.Windows.Forms.CheckBox();
			this.databaseFileTableLayoutPanel.SuspendLayout();
			this.logonGroupBox.SuspendLayout();
			this.loginTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// databaseFileLabel
			// 
			resources.ApplyResources(this.databaseFileLabel, "databaseFileLabel");
			this.databaseFileLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.databaseFileLabel.Name = "databaseFileLabel";
			// 
			// databaseFileTableLayoutPanel
			// 
			resources.ApplyResources(this.databaseFileTableLayoutPanel, "databaseFileTableLayoutPanel");
			this.databaseFileTableLayoutPanel.Controls.Add(this.databaseFileTextBox, 0, 0);
			this.databaseFileTableLayoutPanel.Controls.Add(this.browseButton, 1, 0);
			this.databaseFileTableLayoutPanel.Name = "databaseFileTableLayoutPanel";
			// 
			// databaseFileTextBox
			// 
			resources.ApplyResources(this.databaseFileTextBox, "databaseFileTextBox");
			this.databaseFileTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.databaseFileTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
			this.databaseFileTextBox.Name = "databaseFileTextBox";
			this.databaseFileTextBox.Leave += new System.EventHandler(this.TrimControlText);
			this.databaseFileTextBox.TextChanged += new System.EventHandler(this.SetDatabaseFile);
			// 
			// browseButton
			// 
			resources.ApplyResources(this.browseButton, "browseButton");
			this.browseButton.Name = "browseButton";
			this.browseButton.Click += new System.EventHandler(this.Browse);
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
			this.userNameTextBox.Leave += new System.EventHandler(this.TrimControlText);
			this.userNameTextBox.TextChanged += new System.EventHandler(this.SetUserName);
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
			// AccessConnectionUIControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.logonGroupBox);
			this.Controls.Add(this.databaseFileTableLayoutPanel);
			this.Controls.Add(this.databaseFileLabel);
			this.MinimumSize = new System.Drawing.Size(300, 148);
			this.Name = "AccessConnectionUIControl";
			this.databaseFileTableLayoutPanel.ResumeLayout(false);
			this.databaseFileTableLayoutPanel.PerformLayout();
			this.logonGroupBox.ResumeLayout(false);
			this.logonGroupBox.PerformLayout();
			this.loginTableLayoutPanel.ResumeLayout(false);
			this.loginTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label databaseFileLabel;
		private System.Windows.Forms.TableLayoutPanel databaseFileTableLayoutPanel;
		private System.Windows.Forms.TextBox databaseFileTextBox;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.GroupBox logonGroupBox;
		private System.Windows.Forms.TableLayoutPanel loginTableLayoutPanel;
		private System.Windows.Forms.Label userNameLabel;
		private System.Windows.Forms.TextBox userNameTextBox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.CheckBox savePasswordCheckBox;

	}
}
