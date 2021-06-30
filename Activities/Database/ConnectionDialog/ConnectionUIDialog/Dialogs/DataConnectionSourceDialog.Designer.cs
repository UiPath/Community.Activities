namespace Microsoft.Data.ConnectionUI
{
	internal partial class DataConnectionSourceDialog
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataConnectionSourceDialog));
			this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.leftPanel = new System.Windows.Forms.Panel();
			this.dataSourceLabel = new System.Windows.Forms.Label();
			this.dataSourceListBox = new System.Windows.Forms.ListBox();
			this.dataProviderLabel = new System.Windows.Forms.Label();
			this.dataProviderComboBox = new System.Windows.Forms.ComboBox();
			this.descriptionGroupBox = new System.Windows.Forms.GroupBox();
			this.descriptionLabel = new System.Windows.Forms.Label();
			this.saveSelectionCheckBox = new System.Windows.Forms.CheckBox();
			this.buttonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.mainTableLayoutPanel.SuspendLayout();
			this.leftPanel.SuspendLayout();
			this.descriptionGroupBox.SuspendLayout();
			this.buttonsTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTableLayoutPanel
			// 
			resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
			this.mainTableLayoutPanel.Controls.Add(this.leftPanel, 0, 0);
			this.mainTableLayoutPanel.Controls.Add(this.descriptionGroupBox, 1, 0);
			this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
			// 
			// leftPanel
			// 
			this.leftPanel.Controls.Add(this.dataSourceLabel);
			this.leftPanel.Controls.Add(this.dataSourceListBox);
			this.leftPanel.Controls.Add(this.dataProviderLabel);
			this.leftPanel.Controls.Add(this.dataProviderComboBox);
			resources.ApplyResources(this.leftPanel, "leftPanel");
			this.leftPanel.Name = "leftPanel";
			// 
			// dataSourceLabel
			// 
			resources.ApplyResources(this.dataSourceLabel, "dataSourceLabel");
			this.dataSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataSourceLabel.Name = "dataSourceLabel";
			// 
			// dataSourceListBox
			// 
			resources.ApplyResources(this.dataSourceListBox, "dataSourceListBox");
			this.dataSourceListBox.FormattingEnabled = true;
			this.dataSourceListBox.MinimumSize = new System.Drawing.Size(200, 108);
			this.dataSourceListBox.Name = "dataSourceListBox";
			this.dataSourceListBox.Sorted = true;
			this.dataSourceListBox.DoubleClick += new System.EventHandler(this.SelectDataSource);
			this.dataSourceListBox.SelectedIndexChanged += new System.EventHandler(this.ChangeDataSource);
			this.dataSourceListBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.FormatDataSource);
			// 
			// dataProviderLabel
			// 
			resources.ApplyResources(this.dataProviderLabel, "dataProviderLabel");
			this.dataProviderLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataProviderLabel.Name = "dataProviderLabel";
			// 
			// dataProviderComboBox
			// 
			resources.ApplyResources(this.dataProviderComboBox, "dataProviderComboBox");
			this.dataProviderComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dataProviderComboBox.FormattingEnabled = true;
			this.dataProviderComboBox.Items.AddRange(new object[] {
            resources.GetString("dataProviderComboBox.Items")});
			this.dataProviderComboBox.Name = "dataProviderComboBox";
			this.dataProviderComboBox.Sorted = true;
			this.dataProviderComboBox.SelectedIndexChanged += new System.EventHandler(this.ChangeDataProvider);
			this.dataProviderComboBox.DropDown += new System.EventHandler(this.SetDataProviderDropDownWidth);
			this.dataProviderComboBox.Format += new System.Windows.Forms.ListControlConvertEventHandler(this.FormatDataProvider);
			// 
			// descriptionGroupBox
			// 
			resources.ApplyResources(this.descriptionGroupBox, "descriptionGroupBox");
			this.descriptionGroupBox.Controls.Add(this.descriptionLabel);
			this.descriptionGroupBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.descriptionGroupBox.Name = "descriptionGroupBox";
			this.descriptionGroupBox.TabStop = false;
			// 
			// descriptionLabel
			// 
			resources.ApplyResources(this.descriptionLabel, "descriptionLabel");
			this.descriptionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.descriptionLabel.Name = "descriptionLabel";
			// 
			// saveSelectionCheckBox
			// 
			resources.ApplyResources(this.saveSelectionCheckBox, "saveSelectionCheckBox");
			this.saveSelectionCheckBox.Name = "saveSelectionCheckBox";
			this.saveSelectionCheckBox.CheckedChanged += new System.EventHandler(this.SetSaveSelection);
			// 
			// buttonsTableLayoutPanel
			// 
			resources.ApplyResources(this.buttonsTableLayoutPanel, "buttonsTableLayoutPanel");
			this.buttonsTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
			this.buttonsTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
			this.buttonsTableLayoutPanel.Name = "buttonsTableLayoutPanel";
			// 
			// okButton
			// 
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.okButton.Name = "okButton";
			this.okButton.Click += new System.EventHandler(this.DoOk);
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.cancelButton.Name = "cancelButton";
			// 
			// DataConnectionSourceDialog
			// 
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.mainTableLayoutPanel);
			this.Controls.Add(this.saveSelectionCheckBox);
			this.Controls.Add(this.buttonsTableLayoutPanel);
			this.HelpButton = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DataConnectionSourceDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.mainTableLayoutPanel.ResumeLayout(false);
			this.leftPanel.ResumeLayout(false);
			this.leftPanel.PerformLayout();
			this.descriptionGroupBox.ResumeLayout(false);
			this.buttonsTableLayoutPanel.ResumeLayout(false);
			this.buttonsTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
		private System.Windows.Forms.Panel leftPanel;
		private System.Windows.Forms.Label dataSourceLabel;
		private System.Windows.Forms.ListBox dataSourceListBox;
		private System.Windows.Forms.Label dataProviderLabel;
		private System.Windows.Forms.ComboBox dataProviderComboBox;
		private System.Windows.Forms.GroupBox descriptionGroupBox;
		private System.Windows.Forms.Label descriptionLabel;
		private System.Windows.Forms.CheckBox saveSelectionCheckBox;
		private System.Windows.Forms.TableLayoutPanel buttonsTableLayoutPanel;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
	}
}
