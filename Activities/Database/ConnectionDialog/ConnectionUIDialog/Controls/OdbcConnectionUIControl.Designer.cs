namespace Microsoft.Data.ConnectionUI
{
	public partial class OdbcConnectionUIControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OdbcConnectionUIControl));
			this.dataSourceGroupBox = new System.Windows.Forms.GroupBox();
			this.connectionStringTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.connectionStringTextBox = new System.Windows.Forms.TextBox();
			this.buildButton = new System.Windows.Forms.Button();
			this.useConnectionStringRadioButton = new System.Windows.Forms.RadioButton();
			this.dataSourceNameTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.dataSourceNameComboBox = new System.Windows.Forms.ComboBox();
			this.refreshButton = new System.Windows.Forms.Button();
			this.useDataSourceNameRadioButton = new System.Windows.Forms.RadioButton();
			this.loginGroupBox = new System.Windows.Forms.GroupBox();
			this.loginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.userNameLabel = new System.Windows.Forms.Label();
			this.userNameTextBox = new System.Windows.Forms.TextBox();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.passwordTextBox = new System.Windows.Forms.TextBox();
			this.dataSourceGroupBox.SuspendLayout();
			this.connectionStringTableLayoutPanel.SuspendLayout();
			this.dataSourceNameTableLayoutPanel.SuspendLayout();
			this.loginGroupBox.SuspendLayout();
			this.loginTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// dataSourceGroupBox
			// 
			resources.ApplyResources(this.dataSourceGroupBox, "dataSourceGroupBox");
			this.dataSourceGroupBox.Controls.Add(this.connectionStringTableLayoutPanel);
			this.dataSourceGroupBox.Controls.Add(this.useConnectionStringRadioButton);
			this.dataSourceGroupBox.Controls.Add(this.dataSourceNameTableLayoutPanel);
			this.dataSourceGroupBox.Controls.Add(this.useDataSourceNameRadioButton);
			this.dataSourceGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataSourceGroupBox.Name = "dataSourceGroupBox";
			this.dataSourceGroupBox.TabStop = false;
			// 
			// connectionStringTableLayoutPanel
			// 
			resources.ApplyResources(this.connectionStringTableLayoutPanel, "connectionStringTableLayoutPanel");
			this.connectionStringTableLayoutPanel.Controls.Add(this.connectionStringTextBox, 0, 0);
			this.connectionStringTableLayoutPanel.Controls.Add(this.buildButton, 1, 0);
			this.connectionStringTableLayoutPanel.Name = "connectionStringTableLayoutPanel";
			// 
			// connectionStringTextBox
			// 
			resources.ApplyResources(this.connectionStringTextBox, "connectionStringTextBox");
			this.connectionStringTextBox.Name = "connectionStringTextBox";
			this.connectionStringTextBox.Leave += new System.EventHandler(this.SetConnectionString);
			// 
			// buildButton
			// 
			resources.ApplyResources(this.buildButton, "buildButton");
			this.buildButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.buildButton.Name = "buildButton";
			this.buildButton.Click += new System.EventHandler(this.BuildConnectionString);
			// 
			// useConnectionStringRadioButton
			// 
			resources.ApplyResources(this.useConnectionStringRadioButton, "useConnectionStringRadioButton");
			this.useConnectionStringRadioButton.Name = "useConnectionStringRadioButton";
			this.useConnectionStringRadioButton.CheckedChanged += new System.EventHandler(this.SetDataSourceOption);
			// 
			// dataSourceNameTableLayoutPanel
			// 
			resources.ApplyResources(this.dataSourceNameTableLayoutPanel, "dataSourceNameTableLayoutPanel");
			this.dataSourceNameTableLayoutPanel.Controls.Add(this.dataSourceNameComboBox, 0, 0);
			this.dataSourceNameTableLayoutPanel.Controls.Add(this.refreshButton, 1, 0);
			this.dataSourceNameTableLayoutPanel.Name = "dataSourceNameTableLayoutPanel";
			// 
			// dataSourceNameComboBox
			// 
			resources.ApplyResources(this.dataSourceNameComboBox, "dataSourceNameComboBox");
			this.dataSourceNameComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append;
			this.dataSourceNameComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.dataSourceNameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dataSourceNameComboBox.FormattingEnabled = true;
			this.dataSourceNameComboBox.Name = "dataSourceNameComboBox";
			this.dataSourceNameComboBox.Leave += new System.EventHandler(this.SetDataSourceName);
			this.dataSourceNameComboBox.SelectedIndexChanged += new System.EventHandler(this.SetDataSourceName);
			this.dataSourceNameComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HandleComboBoxDownKey);
			this.dataSourceNameComboBox.DropDown += new System.EventHandler(this.EnumerateDataSourceNames);
			// 
			// refreshButton
			// 
			resources.ApplyResources(this.refreshButton, "refreshButton");
			this.refreshButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.refreshButton.Name = "refreshButton";
			this.refreshButton.Click += new System.EventHandler(this.RefreshDataSourceNames);
			// 
			// useDataSourceNameRadioButton
			// 
			resources.ApplyResources(this.useDataSourceNameRadioButton, "useDataSourceNameRadioButton");
			this.useDataSourceNameRadioButton.Name = "useDataSourceNameRadioButton";
			this.useDataSourceNameRadioButton.CheckedChanged += new System.EventHandler(this.SetDataSourceOption);
			// 
			// loginGroupBox
			// 
			resources.ApplyResources(this.loginGroupBox, "loginGroupBox");
			this.loginGroupBox.Controls.Add(this.loginTableLayoutPanel);
			this.loginGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.loginGroupBox.Name = "loginGroupBox";
			this.loginGroupBox.TabStop = false;
			// 
			// loginTableLayoutPanel
			// 
			resources.ApplyResources(this.loginTableLayoutPanel, "loginTableLayoutPanel");
			this.loginTableLayoutPanel.Controls.Add(this.userNameLabel, 0, 0);
			this.loginTableLayoutPanel.Controls.Add(this.userNameTextBox, 1, 0);
			this.loginTableLayoutPanel.Controls.Add(this.passwordLabel, 0, 1);
			this.loginTableLayoutPanel.Controls.Add(this.passwordTextBox, 1, 1);
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
			this.userNameTextBox.Leave += new System.EventHandler(this.SetUserName);
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
			this.passwordTextBox.Leave += new System.EventHandler(this.SetPassword);
			// 
			// OdbcConnectionUIControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.loginGroupBox);
			this.Controls.Add(this.dataSourceGroupBox);
			this.MinimumSize = new System.Drawing.Size(350, 215);
			this.Name = "OdbcConnectionUIControl";
			this.dataSourceGroupBox.ResumeLayout(false);
			this.dataSourceGroupBox.PerformLayout();
			this.connectionStringTableLayoutPanel.ResumeLayout(false);
			this.connectionStringTableLayoutPanel.PerformLayout();
			this.dataSourceNameTableLayoutPanel.ResumeLayout(false);
			this.dataSourceNameTableLayoutPanel.PerformLayout();
			this.loginGroupBox.ResumeLayout(false);
			this.loginGroupBox.PerformLayout();
			this.loginTableLayoutPanel.ResumeLayout(false);
			this.loginTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox dataSourceGroupBox;
		private System.Windows.Forms.RadioButton useDataSourceNameRadioButton;
		private System.Windows.Forms.TableLayoutPanel dataSourceNameTableLayoutPanel;
		private System.Windows.Forms.ComboBox dataSourceNameComboBox;
		private System.Windows.Forms.Button refreshButton;
		private System.Windows.Forms.RadioButton useConnectionStringRadioButton;
		private System.Windows.Forms.TableLayoutPanel connectionStringTableLayoutPanel;
		private System.Windows.Forms.TextBox connectionStringTextBox;
		private System.Windows.Forms.Button buildButton;
		private System.Windows.Forms.GroupBox loginGroupBox;
		private System.Windows.Forms.TableLayoutPanel loginTableLayoutPanel;
		private System.Windows.Forms.Label userNameLabel;
		private System.Windows.Forms.TextBox userNameTextBox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox passwordTextBox;
	}
}
