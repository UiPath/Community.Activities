//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Data.ConnectionUI
{
	public class ContextHelpEventArgs : HelpEventArgs
	{
		public ContextHelpEventArgs(DataConnectionDialogContext context, Point mousePos) : base(mousePos)
		{
			_context = context;
		}

		public DataConnectionDialogContext Context
		{
			get
			{
				return _context;
			}
		}

		private DataConnectionDialogContext _context;
	}
}
