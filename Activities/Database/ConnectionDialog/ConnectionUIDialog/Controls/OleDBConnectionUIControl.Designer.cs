namespace Microsoft.Data.ConnectionUI
{
	public partial class OleDBConnectionUIControl
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OleDBConnectionUIControl));
			this.providerLabel = new System.Windows.Forms.Label();
			this.providerTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.providerComboBox = new System.Windows.Forms.ComboBox();
			this.dataLinksButton = new System.Windows.Forms.Button();
			this.dataSourceGroupBox = new System.Windows.Forms.GroupBox();
			this.dataSourceTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.dataSourceLabel = new System.Windows.Forms.Label();
			this.dataSourceTextBox = new System.Windows.Forms.TextBox();
			this.locationLabel = new System.Windows.Forms.Label();
			this.locationTextBox = new System.Windows.Forms.TextBox();
			this.logonGroupBox = new System.Windows.Forms.GroupBox();
			this.loginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.subLoginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.userNameLabel = new System.Windows.Forms.Label();
			this.userNameTextBox = new System.Windows.Forms.TextBox();
			this.passwordLabel = new System.Windows.Forms.Label();
			this.passwordTextBox = new System.Windows.Forms.TextBox();
			this.subSubLoginTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.blankPasswordCheckBox = new System.Windows.Forms.CheckBox();
			this.allowSavingPasswordCheckBox = new System.Windows.Forms.CheckBox();
			this.nativeSecurityRadioButton = new System.Windows.Forms.RadioButton();
			this.integratedSecurityRadioButton = new System.Windows.Forms.RadioButton();
			this.initialCatalogLabel = new System.Windows.Forms.Label();
			this.initialCatalogComboBox = new System.Windows.Forms.ComboBox();
			this.providerTableLayoutPanel.SuspendLayout();
			this.dataSourceGroupBox.SuspendLayout();
			this.dataSourceTableLayoutPanel.SuspendLayout();
			this.logonGroupBox.SuspendLayout();
			this.loginTableLayoutPanel.SuspendLayout();
			this.subLoginTableLayoutPanel.SuspendLayout();
			this.subSubLoginTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// providerLabel
			// 
			resources.ApplyResources(this.providerLabel, "providerLabel");
			this.providerLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.providerLabel.Name = "providerLabel";
			// 
			// providerTableLayoutPanel
			// 
			resources.ApplyResources(this.providerTableLayoutPanel, "providerTableLayoutPanel");
			this.providerTableLayoutPanel.Controls.Add(this.providerComboBox, 0, 0);
			this.providerTableLayoutPanel.Controls.Add(this.dataLinksButton, 1, -1);
			this.providerTableLayoutPanel.Name = "providerTableLayoutPanel";
			// 
			// providerComboBox
			// 
			resources.ApplyResources(this.providerComboBox, "providerComboBox");
			this.providerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.providerComboBox.FormattingEnabled = true;
			this.providerComboBox.Name = "providerComboBox";
			this.providerComboBox.Sorted = true;
			this.providerComboBox.SelectedIndexChanged += new System.EventHandler(this.SetProvider);
			this.providerComboBox.DropDown += new System.EventHandler(this.SetProviderDropDownWidth);
			// 
			// dataLinksButton
			// 
			resources.ApplyResources(this.dataLinksButton, "dataLinksButton");
			this.dataLinksButton.MinimumSize = new System.Drawing.Size(83, 23);
			this.dataLinksButton.Name = "dataLinksButton";
			this.dataLinksButton.Click += new System.EventHandler(this.ShowDataLinks);
			// 
			// dataSourceGroupBox
			// 
			resources.ApplyResources(this.dataSourceGroupBox, "dataSourceGroupBox");
			this.dataSourceGroupBox.Controls.Add(this.dataSourceTableLayoutPanel);
			this.dataSourceGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataSourceGroupBox.Name = "dataSourceGroupBox";
			this.dataSourceGroupBox.TabStop = false;
			// 
			// dataSourceTableLayoutPanel
			// 
			resources.ApplyResources(this.dataSourceTableLayoutPanel, "dataSourceTableLayoutPanel");
			this.dataSourceTableLayoutPanel.Controls.Add(this.dataSourceLabel, 0, 0);
			this.dataSourceTableLayoutPanel.Controls.Add(this.dataSourceTextBox, 1, 0);
			this.dataSourceTableLayoutPanel.Controls.Add(this.locationLabel, 0, 1);
			this.dataSourceTableLayoutPanel.Controls.Add(this.locationTextBox, 1, 1);
			this.dataSourceTableLayoutPanel.Name = "dataSourceTableLayoutPanel";
			// 
			// dataSourceLabel
			// 
			resources.ApplyResources(this.dataSourceLabel, "dataSourceLabel");
			this.dataSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataSourceLabel.Name = "dataSourceLabel";
			// 
			// dataSourceTextBox
			// 
			resources.ApplyResources(this.dataSourceTextBox, "dataSourceTextBox");
			this.dataSourceTextBox.Name = "dataSourceTextBox";
			this.dataSourceTextBox.Leave += new System.EventHandler(this.TrimControlText);
			this.dataSourceTextBox.TextChanged += new System.EventHandler(this.SetDataSource);
			// 
			// locationLabel
			// 
			resources.ApplyResources(this.locationLabel, "locationLabel");
			this.locationLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.locationLabel.Name = "locationLabel";
			// 
			// locationTextBox
			// 
			resources.ApplyResources(this.locationTextBox, "locationTextBox");
			this.locationTextBox.Name = "locationTextBox";
			this.locationTextBox.Leave += new System.EventHandler(this.TrimControlText);
			this.locationTextBox.TextChanged += new System.EventHandler(this.SetLocation);
			// 
			// logonGroupBox
			// 
			resources.ApplyResources(this.logonGroupBox, "logonGroupBox");
			this.logonGroupBox.Controls.Add(this.loginTableLayoutPanel);
			this.logonGroupBox.Controls.Add(this.nativeSecurityRadioButton);
			this.logonGroupBox.Controls.Add(this.integratedSecurityRadioButton);
			this.logonGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.logonGroupBox.Name = "logonGroupBox";
			this.logonGroupBox.TabStop = false;
			// 
			// loginTableLayoutPanel
			// 
			resources.ApplyResources(this.loginTableLayoutPanel, "loginTableLayoutPanel");
			this.loginTableLayoutPanel.Controls.Add(this.subLoginTableLayoutPanel, 0, 0);
			this.loginTableLayoutPanel.Controls.Add(this.subSubLoginTableLayoutPanel, 0, 1);
			this.loginTableLayoutPanel.Name = "loginTableLayoutPanel";
			// 
			// subLoginTableLayoutPanel
			// 
			resources.ApplyResources(this.subLoginTableLayoutPanel, "subLoginTableLayoutPanel");
			this.subLoginTableLayoutPanel.Controls.Add(this.userNameLabel, 0, 0);
			this.subLoginTableLayoutPanel.Controls.Add(this.userNameTextBox, 1, 0);
			this.subLoginTableLayoutPanel.Controls.Add(this.passwordLabel, 0, 1);
			this.subLoginTableLayoutPanel.Controls.Add(this.passwordTextBox, 1, 1);
			this.subLoginTableLayoutPanel.Name = "subLoginTableLayoutPanel";
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
			// subSubLoginTableLayoutPanel
			// 
			resources.ApplyResources(this.subSubLoginTableLayoutPanel, "subSubLoginTableLayoutPanel");
			this.subSubLoginTableLayoutPanel.Controls.Add(this.blankPasswordCheckBox, 0, 0);
			this.subSubLoginTableLayoutPanel.Controls.Add(this.allowSavingPasswordCheckBox, 1, 0);
			this.subSubLoginTableLayoutPanel.Name = "subSubLoginTableLayoutPanel";
			// 
			// blankPasswordCheckBox
			// 
			resources.ApplyResources(this.blankPasswordCheckBox, "blankPasswordCheckBox");
			this.blankPasswordCheckBox.Name = "blankPasswordCheckBox";
			this.blankPasswordCheckBox.CheckedChanged += new System.EventHandler(this.SetBlankPassword);
			// 
			// allowSavingPasswordCheckBox
			// 
			resources.ApplyResources(this.allowSavingPasswordCheckBox, "allowSavingPasswordCheckBox");
			this.allowSavingPasswordCheckBox.Name = "allowSavingPasswordCheckBox";
			this.allowSavingPasswordCheckBox.CheckedChanged += new System.EventHandler(this.SetAllowSavingPassword);
			// 
			// nativeSecurityRadioButton
			// 
			resources.ApplyResources(this.nativeSecurityRadioButton, "nativeSecurityRadioButton");
			this.nativeSecurityRadioButton.Name = "nativeSecurityRadioButton";
			this.nativeSecurityRadioButton.CheckedChanged += new System.EventHandler(this.SetSecurityOption);
			// 
			// integratedSecurityRadioButton
			// 
			resources.ApplyResources(this.integratedSecurityRadioButton, "integratedSecurityRadioButton");
			this.integratedSecurityRadioButton.Name = "integratedSecurityRadioButton";
			this.integratedSecurityRadioButton.CheckedChanged += new System.EventHandler(this.SetSecurityOption);
			// 
			// initialCatalogLabel
			// 
			resources.ApplyResources(this.initialCatalogLabel, "initialCatalogLabel");
			this.initialCatalogLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.initialCatalogLabel.Name = "initialCatalogLabel";
			// 
			// initialCatalogComboBox
			// 
			resources.ApplyResources(this.initialCatalogComboBox, "initialCatalogComboBox");
			this.initialCatalogComboBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Append;
			this.initialCatalogComboBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.initialCatalogComboBox.FormattingEnabled = true;
			this.initialCatalogComboBox.Name = "initialCatalogComboBox";
			this.initialCatalogComboBox.Leave += new System.EventHandler(this.TrimControlText);
			this.initialCatalogComboBox.TextChanged += new System.EventHandler(this.SetInitialCatalog);
			this.initialCatalogComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.HandleComboBoxDownKey);
			this.initialCatalogComboBox.DropDown += new System.EventHandler(this.EnumerateCatalogs);
			// 
			// OleDBConnectionUIControl
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.initialCatalogComboBox);
			this.Controls.Add(this.initialCatalogLabel);
			this.Controls.Add(this.logonGroupBox);
			this.Controls.Add(this.dataSourceGroupBox);
			this.Controls.Add(this.providerTableLayoutPanel);
			this.Controls.Add(this.providerLabel);
			this.MinimumSize = new System.Drawing.Size(350, 323);
			this.Name = "OleDBConnectionUIControl";
			this.providerTableLayoutPanel.ResumeLayout(false);
			this.providerTableLayoutPanel.PerformLayout();
			this.dataSourceGroupBox.ResumeLayout(false);
			this.dataSourceGroupBox.PerformLayout();
			this.dataSourceTableLayoutPanel.ResumeLayout(false);
			this.dataSourceTableLayoutPanel.PerformLayout();
			this.logonGroupBox.ResumeLayout(false);
			this.logonGroupBox.PerformLayout();
			this.loginTableLayoutPanel.ResumeLayout(false);
			this.loginTableLayoutPanel.PerformLayout();
			this.subLoginTableLayoutPanel.ResumeLayout(false);
			this.subLoginTableLayoutPanel.PerformLayout();
			this.subSubLoginTableLayoutPanel.ResumeLayout(false);
			this.subSubLoginTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label providerLabel;
		private System.Windows.Forms.TableLayoutPanel providerTableLayoutPanel;
		private System.Windows.Forms.ComboBox providerComboBox;
		private System.Windows.Forms.Button dataLinksButton;
		private System.Windows.Forms.GroupBox dataSourceGroupBox;
		private System.Windows.Forms.TableLayoutPanel dataSourceTableLayoutPanel;
		private System.Windows.Forms.Label dataSourceLabel;
		private System.Windows.Forms.TextBox dataSourceTextBox;
		private System.Windows.Forms.Label locationLabel;
		private System.Windows.Forms.TextBox locationTextBox;
		private System.Windows.Forms.GroupBox logonGroupBox;
		private System.Windows.Forms.RadioButton integratedSecurityRadioButton;
		private System.Windows.Forms.RadioButton nativeSecurityRadioButton;
		private System.Windows.Forms.TableLayoutPanel loginTableLayoutPanel;
		private System.Windows.Forms.TableLayoutPanel subLoginTableLayoutPanel;
		private System.Windows.Forms.Label userNameLabel;
		private System.Windows.Forms.TextBox userNameTextBox;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.TextBox passwordTextBox;
		private System.Windows.Forms.TableLayoutPanel subSubLoginTableLayoutPanel;
		private System.Windows.Forms.CheckBox blankPasswordCheckBox;
		private System.Windows.Forms.CheckBox allowSavingPasswordCheckBox;
		private System.Windows.Forms.Label initialCatalogLabel;
		private System.Windows.Forms.ComboBox initialCatalogComboBox;
	}
}
