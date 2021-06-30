namespace Microsoft.Data.ConnectionUI
{
	partial class DataConnectionDialog
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataConnectionDialog));
			this.dataSourceLabel = new System.Windows.Forms.Label();
			this.dataSourceTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.dataSourceTextBox = new System.Windows.Forms.TextBox();
			this.changeDataSourceButton = new System.Windows.Forms.Button();
			this.containerControl = new System.Windows.Forms.ContainerControl();
			this.advancedButton = new System.Windows.Forms.Button();
			this.separatorPanel = new System.Windows.Forms.Panel();
			this.testConnectionButton = new System.Windows.Forms.Button();
			this.buttonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.acceptButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.dataProviderToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.dataSourceTableLayoutPanel.SuspendLayout();
			this.buttonsTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// dataSourceLabel
			// 
			resources.ApplyResources(this.dataSourceLabel, "dataSourceLabel");
			this.dataSourceLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.dataSourceLabel.Name = "dataSourceLabel";
			// 
			// dataSourceTableLayoutPanel
			// 
			resources.ApplyResources(this.dataSourceTableLayoutPanel, "dataSourceTableLayoutPanel");
			this.dataSourceTableLayoutPanel.Controls.Add(this.dataSourceTextBox, 0, 0);
			this.dataSourceTableLayoutPanel.Controls.Add(this.changeDataSourceButton, 1, 0);
			this.dataSourceTableLayoutPanel.Name = "dataSourceTableLayoutPanel";
			// 
			// dataSourceTextBox
			// 
			resources.ApplyResources(this.dataSourceTextBox, "dataSourceTextBox");
			this.dataSourceTextBox.Name = "dataSourceTextBox";
			this.dataSourceTextBox.ReadOnly = true;
			// 
			// changeDataSourceButton
			// 
			resources.ApplyResources(this.changeDataSourceButton, "changeDataSourceButton");
			this.changeDataSourceButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.changeDataSourceButton.Name = "changeDataSourceButton";
			this.changeDataSourceButton.Click += new System.EventHandler(this.ChangeDataSource);
			// 
			// containerControl
			// 
			resources.ApplyResources(this.containerControl, "containerControl");
			this.containerControl.Name = "containerControl";
			this.containerControl.SizeChanged += new System.EventHandler(this.SetConnectionUIControlDockStyle);
			// 
			// advancedButton
			// 
			resources.ApplyResources(this.advancedButton, "advancedButton");
			this.advancedButton.MinimumSize = new System.Drawing.Size(81, 23);
			this.advancedButton.Name = "advancedButton";
			this.advancedButton.Click += new System.EventHandler(this.ShowAdvanced);
			// 
			// separatorPanel
			// 
			resources.ApplyResources(this.separatorPanel, "separatorPanel");
			this.separatorPanel.Name = "separatorPanel";
			this.separatorPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.PaintSeparator);
			// 
			// testConnectionButton
			// 
			resources.ApplyResources(this.testConnectionButton, "testConnectionButton");
			this.testConnectionButton.MinimumSize = new System.Drawing.Size(101, 23);
			this.testConnectionButton.Name = "testConnectionButton";
			this.testConnectionButton.Click += new System.EventHandler(this.TestConnection);
			// 
			// buttonsTableLayoutPanel
			// 
			resources.ApplyResources(this.buttonsTableLayoutPanel, "buttonsTableLayoutPanel");
			this.buttonsTableLayoutPanel.Controls.Add(this.acceptButton, 0, 0);
			this.buttonsTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
			this.buttonsTableLayoutPanel.Name = "buttonsTableLayoutPanel";
			// 
			// acceptButton
			// 
			resources.ApplyResources(this.acceptButton, "acceptButton");
			this.acceptButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.acceptButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.acceptButton.Name = "acceptButton";
			this.acceptButton.Click += new System.EventHandler(this.HandleAccept);
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.cancelButton.Name = "cancelButton";
			// 
			// DataConnectionDialog
			// 
			this.AcceptButton = this.acceptButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.buttonsTableLayoutPanel);
			this.Controls.Add(this.testConnectionButton);
			this.Controls.Add(this.separatorPanel);
			this.Controls.Add(this.advancedButton);
			this.Controls.Add(this.containerControl);
			this.Controls.Add(this.dataSourceTableLayoutPanel);
			this.Controls.Add(this.dataSourceLabel);
			this.HelpButton = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DataConnectionDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.dataSourceTableLayoutPanel.ResumeLayout(false);
			this.dataSourceTableLayoutPanel.PerformLayout();
			this.buttonsTableLayoutPanel.ResumeLayout(false);
			this.buttonsTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label dataSourceLabel;
		private System.Windows.Forms.TableLayoutPanel dataSourceTableLayoutPanel;
		private System.Windows.Forms.TextBox dataSourceTextBox;
		private System.Windows.Forms.ToolTip dataProviderToolTip;
		private System.Windows.Forms.Button changeDataSourceButton;
		private System.Windows.Forms.ContainerControl containerControl;
		private System.Windows.Forms.Button advancedButton;
		private System.Windows.Forms.Panel separatorPanel;
		private System.Windows.Forms.Button testConnectionButton;
		private System.Windows.Forms.TableLayoutPanel buttonsTableLayoutPanel;
		private System.Windows.Forms.Button acceptButton;
		private System.Windows.Forms.Button cancelButton;
	}
}
