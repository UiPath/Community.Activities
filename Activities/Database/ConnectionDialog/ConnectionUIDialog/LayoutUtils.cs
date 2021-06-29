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
	internal sealed class LayoutUtils
	{
		private LayoutUtils()
		{
		}

		public static int GetPreferredLabelHeight(Label label)
		{
			return GetPreferredLabelHeight(label, label.Width);
		}

		public static int GetPreferredLabelHeight(Label label, int requiredWidth)
		{
			return GetPreferredHeight(label, label.UseCompatibleTextRendering, requiredWidth);
		}

		public static int GetPreferredCheckBoxHeight(CheckBox checkBox)
		{
			return GetPreferredHeight(checkBox, checkBox.UseCompatibleTextRendering, checkBox.Width);
		}

		public static void MirrorControl(Control c)
		{
			c.Left =
				c.Parent.Right -
				c.Parent.Padding.Left -
				c.Margin.Left -
				c.Width;
			if ((c.Anchor & AnchorStyles.Left) == 0 ||
				(c.Anchor & AnchorStyles.Right) == 0)
			{
				c.Anchor &= ~AnchorStyles.Left;
				c.Anchor |= AnchorStyles.Right;
			}
		}

		public static void MirrorControl(Control c, Control pivot)
		{
			c.Left = pivot.Right - c.Width;
			if ((c.Anchor & AnchorStyles.Left) == 0 ||
				(c.Anchor & AnchorStyles.Right) == 0)
			{
				c.Anchor &= ~AnchorStyles.Left;
				c.Anchor |= AnchorStyles.Right;
			}
		}

		public static void UnmirrorControl(Control c)
		{
			c.Left =
				c.Parent.Left +
				c.Parent.Padding.Left +
				c.Margin.Left;
			if ((c.Anchor & AnchorStyles.Left) == 0 ||
				(c.Anchor & AnchorStyles.Right) == 0)
			{
				c.Anchor &= ~AnchorStyles.Right;
				c.Anchor |= AnchorStyles.Left;
			}
		}

		public static void UnmirrorControl(Control c, Control pivot)
		{
			c.Left = pivot.Left;
			if ((c.Anchor & AnchorStyles.Left) == 0 ||
				(c.Anchor & AnchorStyles.Right) == 0)
			{
				c.Anchor &= ~AnchorStyles.Right;
				c.Anchor |= AnchorStyles.Left;
			}
		}

		private static int GetPreferredHeight(Control c, bool useCompatibleTextRendering, int requiredWidth)
		{
			using (Graphics g = Graphics.FromHwnd(c.Handle))
			{
				if (useCompatibleTextRendering)
				{
					return g.MeasureString(c.Text, c.Font, c.Width).ToSize().Height;
				}
				else
				{
					return TextRenderer.MeasureText(
						g,
						c.Text,
						c.Font,
						new Size(requiredWidth, int.MaxValue),
						TextFormatFlags.WordBreak
					).Height;
				}
			}
		}
	}
}
