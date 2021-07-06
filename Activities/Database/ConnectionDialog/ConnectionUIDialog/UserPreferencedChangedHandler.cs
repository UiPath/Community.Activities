//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Forms.Design;
using Microsoft.Win32;

namespace Microsoft.Data.ConnectionUI
{
	internal sealed class UserPreferenceChangedHandler : IComponent
	{
		public UserPreferenceChangedHandler(Form form)
		{
			Debug.Assert(form != null);
			SystemEvents.UserPreferenceChanged += new UserPreferenceChangedEventHandler(HandleUserPreferenceChanged);
			_form = form;
		}

		~UserPreferenceChangedHandler()
		{
			Dispose(false);
		}

		public ISite Site
		{
			get
			{
				return _form.Site;
			}
			set
			{
				// This shouldn't be called
			}
		}

		public event EventHandler Disposed;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void HandleUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
		{
			// Need to update the font
			IUIService uiService = (_form.Site != null) ? _form.Site.GetService(typeof(IUIService)) as IUIService : null;
			if (uiService != null)
			{
				Font newFont = uiService.Styles["DialogFont"] as Font;
				if (newFont != null)
				{
					_form.Font = newFont;
				}
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				SystemEvents.UserPreferenceChanged -= new UserPreferenceChangedEventHandler(HandleUserPreferenceChanged);
                Disposed?.Invoke(this, EventArgs.Empty);
            }
		}

		private Form _form;
	}
}
