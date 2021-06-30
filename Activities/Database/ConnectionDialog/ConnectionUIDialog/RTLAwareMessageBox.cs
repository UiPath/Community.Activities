//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Windows.Forms;
using System.Globalization;

namespace Microsoft.Data.ConnectionUI
{
	internal sealed class RTLAwareMessageBox
	{
		private RTLAwareMessageBox()
		{
		}

		public static DialogResult Show(string caption, string text, MessageBoxIcon icon)
		{
			MessageBoxOptions options = 0;
			if (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft)
			{
				options = MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading;
			}
			return MessageBox.Show(text, caption, MessageBoxButtons.OK, icon, MessageBoxDefaultButton.Button1, options);
		}
	}
}
