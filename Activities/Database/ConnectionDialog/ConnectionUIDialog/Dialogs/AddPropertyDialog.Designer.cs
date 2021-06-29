namespace Microsoft.Data.ConnectionUI
{
	partial class AddPropertyDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddPropertyDialog));
			this.propertyLabel = new System.Windows.Forms.Label();
			this.propertyTextBox = new System.Windows.Forms.TextBox();
			this.buttonsTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.buttonsTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// propertyLabel
			// 
			resources.ApplyResources(this.propertyLabel, "propertyLabel");
			this.propertyLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.propertyLabel.Name = "propertyLabel";
			// 
			// propertyTextBox
			// 
			resources.ApplyResources(this.propertyTextBox, "propertyTextBox");
			this.propertyTextBox.Name = "propertyTextBox";
			this.propertyTextBox.TextChanged += new System.EventHandler(this.SetOkButtonStatus);
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
			// 
			// cancelButton
			// 
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.MinimumSize = new System.Drawing.Size(75, 23);
			this.cancelButton.Name = "cancelButton";
			// 
			// AddPropertyDialog
			// 
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.buttonsTableLayoutPanel);
			this.Controls.Add(this.propertyTextBox);
			this.Controls.Add(this.propertyLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.HelpButton = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddPropertyDialog";
			this.ShowInTaskbar = false;
			this.buttonsTableLayoutPanel.ResumeLayout(false);
			this.buttonsTableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label propertyLabel;
		private System.Windows.Forms.TextBox propertyTextBox;
		private System.Windows.Forms.TableLayoutPanel buttonsTableLayoutPanel;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
	}
}
