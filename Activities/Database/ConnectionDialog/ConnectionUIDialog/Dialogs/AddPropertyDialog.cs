//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Microsoft.Data.ConnectionUI
{
    internal partial class AddPropertyDialog : Form
    {
        public AddPropertyDialog()
        {
            InitializeComponent();

            // Make sure we handle a user preference change
            if (components == null)
            {
                components = new System.ComponentModel.Container();
            }
            components.Add(new UserPreferenceChangedHandler(this));
        }

        public AddPropertyDialog(DataConnectionDialog mainDialog)
            : this()
        {
            Debug.Assert(mainDialog != null);

            _mainDialog = mainDialog;
        }

        public string PropertyName
        {
            get
            {
                return propertyTextBox.Text;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            propertyTextBox.Width =
                buttonsTableLayoutPanel.Right -
                propertyTextBox.Left;

            // Resize the dialog appropriately so that OK/Cancel buttons fit
            int preferredClientWidth =
                Padding.Left +
                buttonsTableLayoutPanel.Margin.Left +
                buttonsTableLayoutPanel.Width +
                buttonsTableLayoutPanel.Margin.Right +
                Padding.Right;
            if (ClientSize.Width < preferredClientWidth)
            {
                ClientSize = new Size(preferredClientWidth, ClientSize.Height);
            }
        }

        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            // Get the active control
            Control activeControl = HelpUtils.GetActiveControl(this);

            // Figure out the context
            DataConnectionDialogContext context = DataConnectionDialogContext.AddProperty;
            if (activeControl == propertyTextBox)
            {
                context = DataConnectionDialogContext.AddPropertyTextBox;
            }
            if (activeControl == okButton)
            {
                context = DataConnectionDialogContext.AddPropertyOkButton;
            }
            if (activeControl == cancelButton)
            {
                context = DataConnectionDialogContext.AddPropertyCancelButton;
            }

            // Call OnContextHelpRequested
            ContextHelpEventArgs e = new ContextHelpEventArgs(context, hevent.MousePos);
            _mainDialog.OnContextHelpRequested(e);
            hevent.Handled = e.Handled;
            if (!e.Handled)
            {
                base.OnHelpRequested(hevent);
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (_mainDialog.TranslateHelpButton && HelpUtils.IsContextHelpMessage(ref m))
            {
                // Force the ? in the title bar to invoke the help topic
                HelpUtils.TranslateContextHelpMessage(this, ref m);
            }
            base.WndProc(ref m);
        }

        private void SetOkButtonStatus(object sender, EventArgs e)
        {
            okButton.Enabled = (propertyTextBox.Text.Trim().Length > 0);
        }

        private DataConnectionDialog _mainDialog;
    }
}