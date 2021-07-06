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
	public partial class AccessConnectionUIControl : UserControl, IDataConnectionUIControl
	{
		public AccessConnectionUIControl()
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
			if (connectionProperties == null)
			{
				throw new ArgumentNullException("connectionProperties");
			}

			if (!(connectionProperties is OleDBAccessConnectionProperties))
			{
				throw new ArgumentException(Strings.AccessConnectionUIControl_InvalidConnectionProperties);
			}

			if (connectionProperties is OdbcConnectionProperties)
			{
				// ODBC does not support saving the password
				savePasswordCheckBox.Enabled = false;
			}

			_connectionProperties = connectionProperties;
		}

		public void LoadProperties()
		{
			_loading = true;

			databaseFileTextBox.Text = Properties[DatabaseFileProperty] as string;
			userNameTextBox.Text = Properties[UserNameProperty] as string;
			if (userNameTextBox.Text.Length == 0)
			{
				userNameTextBox.Text = "Admin";
			}
			passwordTextBox.Text = Properties[PasswordProperty] as string;
			if (!(Properties is OdbcConnectionProperties))
			{
				savePasswordCheckBox.Checked = (bool)Properties["Persist Security Info"];
			}
			else
			{
				savePasswordCheckBox.Checked = false;
			}

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
			}
			else
			{
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
				Properties[DatabaseFileProperty] = (databaseFileTextBox.Text.Trim().Length > 0) ? databaseFileTextBox.Text.Trim() : null;
			}
		}

		private void Browse(object sender, EventArgs e)
		{
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Title = Strings.AccessConnectionUIControl_BrowseFileTitle,
                Multiselect = false,
                RestoreDirectory = true,
                Filter = Strings.AccessConnectionUIControl_BrowseFileFilter,
                DefaultExt = Strings.AccessConnectionUIControl_BrowseFileDefaultExt
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

		private void SetUserName(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties[UserNameProperty] = (userNameTextBox.Text.Trim().Length > 0) ? userNameTextBox.Text.Trim() : null;
				if ((Properties[UserNameProperty] as string).Equals("Admin"))
				{
					Properties[UserNameProperty] = null;
				}
			}
		}

		private void SetPassword(object sender, EventArgs e)
		{
			if (!_loading)
			{
				Properties[PasswordProperty] = (passwordTextBox.Text.Length > 0) ? passwordTextBox.Text : null;
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

		private string DatabaseFileProperty
		{
			get
			{
				if (!(Properties is OdbcConnectionProperties))
				{
					return "Data Source";
				}
				else
				{
					return "DBQ";
				}
			}
		}

		private string UserNameProperty
		{
			get
			{
				if (!(Properties is OdbcConnectionProperties))
				{
					return "User ID";
				}
				else
				{
					return "UID";
				}
			}
		}

		private string PasswordProperty
		{
			get
			{
				if (!(Properties is OdbcConnectionProperties))
				{
					return "Jet OLEDB:Database Password";
				}
				else
				{
					return "PWD";
				}
			}
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
