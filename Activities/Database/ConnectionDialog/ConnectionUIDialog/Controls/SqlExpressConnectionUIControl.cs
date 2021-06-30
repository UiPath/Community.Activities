//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

namespace Microsoft.Data.ConnectionUI
{
	public partial class SqlFileConnectionUIControl : UserControl, IDataConnectionUIControl
	{
		public SqlFileConnectionUIControl()
		{
			InitializeComponent();
			RightToLeft = RightToLeft.Inherit;

			int requiredHeight = LayoutUtils.GetPreferredCheckBoxHeight(savePasswordCheckBox);
			if (savePasswordCheckBox.Height < requiredHeight)
			{
				savePasswordCheckBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom;
				loginTableLayoutPanel.Height += loginTableLayoutPanel.Margin.Bottom;
				loginTableLayoutPanel.Margin = new Padding(loginTableLayoutPanel.Margin.Left, loginTableLayoutPanel.Margin.Top, loginTableLayoutPanel.Margin.Right, 0);
			}
		}

		public void Initialize(IDataConnectionProperties connectionProperties)
		{
			if (!(connectionProperties is SqlFileConnectionProperties))
			{
				throw new ArgumentException(Strings.SqlFileConnectionUIControl_InvalidConnectionProperties);
			}

			_connectionProperties = connectionProperties;
		}

		public void LoadProperties()
		{
			_loading = true;

			databaseFileTextBox.Text = Properties["AttachDbFilename"] as string;
			string myDocumentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			if (databaseFileTextBox.Text.StartsWith(myDocumentsDir, StringComparison.OrdinalIgnoreCase))
			{
				databaseFileTextBox.Text = databaseFileTextBox.Text.Substring(myDocumentsDir.Length + 1);
			}
			if ((bool)Properties["Integrated Security"])
			{
				windowsAuthenticationRadioButton.Checked = true;
			}
			else
			{
				sqlAuthenticationRadioButton.Checked = true;
			}
			userNameTextBox.Text = Properties["User ID"] as string;
			passwordTextBox.Text = Properties["Password"] as string;
			savePasswordCheckBox.Checked = (bool)Properties["Persist Security Info"];

			_loading = false;
		}

		// Simulate RTL mirroring
		protected override void OnRightToLeftChanged(EventArgs e)
		{
			base.OnRightToLeftChanged(e);
			if (ParentForm != null &&
				ParentForm.RightToLeftLayout == true &&
				RightToLeft == RightToLeft.Yes)
			{
				LayoutUtils.MirrorControl(databaseFileLabel, databaseFileTableLayoutPanel);
				LayoutUtils.MirrorControl(windowsAuthenticationRadioButton);
				LayoutUtils.MirrorControl(sqlAuthenticationRadioButton);
				LayoutUtils.MirrorControl(loginTableLayoutPanel);
			}
			else
			{
				LayoutUtils.UnmirrorControl(loginTableLayoutPanel);
				LayoutUtils.UnmirrorControl(sqlAuthenticationRadioButton);
				LayoutUtils.UnmirrorControl(windowsAuthenticationRadioButton);
				LayoutUtils.UnmirrorControl(databaseFileLabel, databaseFileTableLayoutPanel);
			}
		}

		protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
		{
			Size baseSize = Size;
			MinimumSize = Size.Empty;
			base.ScaleControl(factor, specified);
			MinimumSize = new Size(
				(int)Math.Round(baseSize.Width * factor.Width),
				(int)Math.Round(baseSize.Height * factor.Height));
		}

		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			if (Parent == null)
			{
				OnFontChanged(e);
			}
		}

		private void SetDatabaseFile(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["AttachDbFilename"] = (databaseFileTextBox.Text.Trim().Length > 0) ? databaseFileTextBox.Text.Trim() : null;
			}
		}

		private void UpdateDatabaseFile(object sender, EventArgs e)
		{
			if (!_loading)
			{
				string attachDbFilename = (databaseFileTextBox.Text.Trim().Length > 0) ? databaseFileTextBox.Text.Trim() : null;
				if (attachDbFilename != null)
				{
					if (!attachDbFilename.EndsWith(".mdf", StringComparison.OrdinalIgnoreCase))
					{
						attachDbFilename += ".mdf";
					}
					try
					{
						if (!System.IO.Path.IsPathRooted(attachDbFilename))
						{
							// Simulate a default directory as My Documents by appending this to the front
							attachDbFilename = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), attachDbFilename);
						}
					}
					catch { }
				}
				Properties["AttachDbFilename"] = attachDbFilename;
			}
		}

		private void Browse(object sender, EventArgs e)
		{
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = Strings.SqlConnectionUIControl_BrowseFileTitle,
                Multiselect = false,
                CheckFileExists = false,
                RestoreDirectory = true,
                Filter = Strings.SqlConnectionUIControl_BrowseFileFilter,
                DefaultExt = Strings.SqlConnectionUIControl_BrowseFileDefaultExt,
                FileName = Properties["AttachDbFilename"] as string
            };
            if (Container != null)
			{
				Container.Add(fileDialog);
			}
			try
			{
				DialogResult result = fileDialog.ShowDialog(ParentForm);
				if (result == DialogResult.OK)
				{
					databaseFileTextBox.Text = fileDialog.FileName.Trim();
				}
			}
			finally
			{
				if (Container != null)
				{
					Container.Remove(fileDialog);
				}
				fileDialog.Dispose();
			}
		}

		private void SetAuthenticationOption(object sender, EventArgs e)
		{
			if (windowsAuthenticationRadioButton.Checked)
			{
				if (!_loading)
				{
					Properties["Integrated Security"] = true;
					Properties.Reset("User ID");
					Properties.Reset("Password");
					Properties.Reset("Persist Security Info");
				}
				loginTableLayoutPanel.Enabled = false;
			}
			else /* if (sqlAuthenticationRadioButton.Checked) */
			{
				if (!_loading)
				{
					Properties["Integrated Security"] = false;
					SetUserName(sender, e);
					SetPassword(sender, e);
					SetSavePassword(sender, e);
				}
				loginTableLayoutPanel.Enabled = true;
			}
		}

		private void SetUserName(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["User ID"] = (userNameTextBox.Text.Trim().Length > 0) ? userNameTextBox.Text.Trim() : null;
			}
		}

		private void SetPassword(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Password"] = (passwordTextBox.Text.Length > 0) ? passwordTextBox.Text : null;
				passwordTextBox.Text = passwordTextBox.Text; // forces reselection of all text
			}
		}

		private void SetSavePassword(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties["Persist Security Info"] = savePasswordCheckBox.Checked;
			}
		}

		private void TrimControlText(object sender, EventArgs e)
		{
			Control c = sender as Control;
			c.Text = c.Text.Trim();
		}

		private IDataConnectionProperties Properties
		{
			get
			{
				return _connectionProperties;
			}
		}

		private bool _loading;
		private IDataConnectionProperties _connectionProperties;
	}
}
