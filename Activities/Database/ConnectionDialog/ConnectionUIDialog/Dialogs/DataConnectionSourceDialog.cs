//------------------------------------------------------------------------------
// <copyright company="Microsoft Corporation">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using UiPath.Database.Workaround;

namespace Microsoft.Data.ConnectionUI
{
    internal partial class DataConnectionSourceDialog : Form
    {
        public DataConnectionSourceDialog()
        {
            InitializeComponent();

            DbWorkarounds.SNILoadWorkaround(); 
            // Make sure we handle a user preference change
            if (components == null)
            {
                components = new System.ComponentModel.Container();
            }
            components.Add(new UserPreferenceChangedHandler(this));
        }

        public DataConnectionSourceDialog(DataConnectionDialog mainDialog)
            : this()
        {
            Debug.Assert(mainDialog != null);

            _mainDialog = mainDialog;
        }

        public string Title
        {
            get
            {
                return Text;
            }
            set
            {
                Text = value;
            }
        }

        public string HeaderLabel
        {
            get
            {
                return (_headerLabel != null) ? _headerLabel.Text : string.Empty;
            }
            set
            {
                if (_headerLabel == null && (value == null || value.Length == 0))
                {
                    return;
                }
                if (_headerLabel != null && value == _headerLabel.Text)
                {
                    return;
                }
                if (value != null)
                {
                    if (_headerLabel == null)
                    {
                        _headerLabel = new Label
                        {
                            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                            FlatStyle = FlatStyle.System,
                            Location = new Point(12, 12),
                            Margin = new Padding(3),
                            Name = "dataSourceLabel",
                            Width = mainTableLayoutPanel.Width,
                            TabIndex = 100
                        };
                        Controls.Add(_headerLabel);
                    }
                    _headerLabel.Text = value;
                    MinimumSize = Size.Empty;
                    _headerLabel.Height = LayoutUtils.GetPreferredLabelHeight(_headerLabel);
                    int dy =
                        _headerLabel.Bottom +
                        _headerLabel.Margin.Bottom +
                        mainTableLayoutPanel.Margin.Top -
                        mainTableLayoutPanel.Top;
                    mainTableLayoutPanel.Anchor &= ~AnchorStyles.Bottom;
                    Height += dy;
                    mainTableLayoutPanel.Anchor |= AnchorStyles.Bottom;
                    mainTableLayoutPanel.Top += dy;
                    MinimumSize = Size;
                }
                else
                {
                    if (_headerLabel != null)
                    {
                        int dy = _headerLabel.Height;
                        try
                        {
                            Controls.Remove(_headerLabel);
                        }
                        finally
                        {
                            _headerLabel.Dispose();
                            _headerLabel = null;
                        }
                        MinimumSize = Size.Empty;
                        mainTableLayoutPanel.Top -= dy;
                        mainTableLayoutPanel.Anchor &= ~AnchorStyles.Bottom;
                        Height -= dy;
                        mainTableLayoutPanel.Anchor |= AnchorStyles.Bottom;
                        MinimumSize = Size;
                    }
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            // If a main dialog was associated with this dialog, get its data sources
            if (_mainDialog != null)
            {
                foreach (DataSource dataSource in _mainDialog.DataSources)
                {
                    if (dataSource == _mainDialog.UnspecifiedDataSource)
                    {
                        continue;
                    }
                    dataSourceListBox.Items.Add(dataSource);
                }
                if (_mainDialog.DataSources.Contains(_mainDialog.UnspecifiedDataSource))
                {
                    // We want to put the unspecified data source at the end of the list
                    dataSourceListBox.Sorted = false;
                    dataSourceListBox.Items.Add(_mainDialog.UnspecifiedDataSource);
                }

                // Figure out the correct width for the data source list box and size dialog
                int dataSourceListBoxWidth = dataSourceListBox.Width - (dataSourceListBox.Width - dataSourceListBox.ClientSize.Width);
                foreach (object item in dataSourceListBox.Items)
                {
                    Size size = TextRenderer.MeasureText((item as DataSource).DisplayName, dataSourceListBox.Font);
                    size.Width += 3; // otherwise text is crammed up against right edge
                    dataSourceListBoxWidth = Math.Max(dataSourceListBoxWidth, size.Width);
                }
                dataSourceListBoxWidth = dataSourceListBoxWidth + (dataSourceListBox.Width - dataSourceListBox.ClientSize.Width);
                dataSourceListBoxWidth = Math.Max(dataSourceListBoxWidth, dataSourceListBox.MinimumSize.Width);
                int dx = dataSourceListBoxWidth - dataSourceListBox.Size.Width;
                Width += dx * 2; // * 2 because the description group box resizes as well
                MinimumSize = Size;

                if (_mainDialog.SelectedDataSource != null)
                {
                    dataSourceListBox.SelectedItem = _mainDialog.SelectedDataSource;
                    if (_mainDialog.SelectedDataProvider != null)
                    {
                        dataProviderComboBox.SelectedItem = _mainDialog.SelectedDataProvider;
                    }
                }

                // Configure the initial data provider selections
                foreach (DataSource dataSource in dataSourceListBox.Items)
                {
                    DataProvider selectedProvider = _mainDialog.GetSelectedDataProvider(dataSource);
                    if (selectedProvider != null)
                    {
                        _providerSelections[dataSource] = selectedProvider;
                    }
                }
            }

            // Set the save selection check box
            saveSelectionCheckBox.Checked = _mainDialog.SaveSelection;

            SetOkButtonStatus();

            base.OnLoad(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            dataProviderComboBox.Top =
                leftPanel.Height -
                leftPanel.Padding.Bottom -
                dataProviderComboBox.Margin.Bottom -
                dataProviderComboBox.Height;
            dataProviderLabel.Top =
                dataProviderComboBox.Top -
                dataProviderComboBox.Margin.Top -
                dataProviderLabel.Margin.Bottom -
                dataProviderLabel.Height;

            int dx =
                (saveSelectionCheckBox.Right + saveSelectionCheckBox.Margin.Right) -
                (buttonsTableLayoutPanel.Left - buttonsTableLayoutPanel.Margin.Left);
            if (dx > 0)
            {
                Width += dx;
                MinimumSize = new Size(MinimumSize.Width + dx, MinimumSize.Height);
            }
            mainTableLayoutPanel.Anchor &= ~AnchorStyles.Bottom;
            saveSelectionCheckBox.Anchor &= ~AnchorStyles.Bottom;
            saveSelectionCheckBox.Anchor |= AnchorStyles.Top;
            buttonsTableLayoutPanel.Anchor &= ~AnchorStyles.Bottom;
            buttonsTableLayoutPanel.Anchor |= AnchorStyles.Top;
            int height =
                buttonsTableLayoutPanel.Top +
                buttonsTableLayoutPanel.Height +
                buttonsTableLayoutPanel.Margin.Bottom +
                Padding.Bottom;
            int dy = Height - SizeFromClientSize(new Size(0, height)).Height;
            MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height - dy);
            Height -= dy;
            buttonsTableLayoutPanel.Anchor &= ~AnchorStyles.Top;
            buttonsTableLayoutPanel.Anchor |= AnchorStyles.Bottom;
            saveSelectionCheckBox.Anchor &= ~AnchorStyles.Top;
            saveSelectionCheckBox.Anchor |= AnchorStyles.Bottom;
            mainTableLayoutPanel.Anchor |= AnchorStyles.Bottom;
        }

        protected override void OnRightToLeftLayoutChanged(EventArgs e)
        {
            base.OnRightToLeftLayoutChanged(e);
            if (RightToLeftLayout == true &&
                RightToLeft == RightToLeft.Yes)
            {
                LayoutUtils.MirrorControl(dataSourceLabel, dataSourceListBox);
                LayoutUtils.MirrorControl(dataProviderLabel, dataProviderComboBox);
            }
            else
            {
                LayoutUtils.UnmirrorControl(dataProviderLabel, dataProviderComboBox);
                LayoutUtils.UnmirrorControl(dataSourceLabel, dataSourceListBox);
            }
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            if (RightToLeftLayout == true &&
                RightToLeft == RightToLeft.Yes)
            {
                LayoutUtils.MirrorControl(dataSourceLabel, dataSourceListBox);
                LayoutUtils.MirrorControl(dataProviderLabel, dataProviderComboBox);
            }
            else
            {
                LayoutUtils.UnmirrorControl(dataProviderLabel, dataProviderComboBox);
                LayoutUtils.UnmirrorControl(dataSourceLabel, dataSourceListBox);
            }
        }

        protected override void OnHelpRequested(HelpEventArgs hevent)
        {
            // Get the active control
            Control activeControl = HelpUtils.GetActiveControl(this);

            // Figure out the context
            DataConnectionDialogContext context = DataConnectionDialogContext.Source;
            if (activeControl == dataSourceListBox)
            {
                context = DataConnectionDialogContext.SourceListBox;
            }
            if (activeControl == dataProviderComboBox)
            {
                context = DataConnectionDialogContext.SourceProviderComboBox;
            }
            if (activeControl == okButton)
            {
                context = DataConnectionDialogContext.SourceOkButton;
            }
            if (activeControl == cancelButton)
            {
                context = DataConnectionDialogContext.SourceCancelButton;
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

        private void FormatDataSource(object sender, ListControlConvertEventArgs e)
        {
            if (e.DesiredType == typeof(string))
            {
                e.Value = (e.ListItem as DataSource).DisplayName;
            }
        }

        private void ChangeDataSource(object sender, EventArgs e)
        {
            DataSource newDataSource = dataSourceListBox.SelectedItem as DataSource;
            dataProviderComboBox.Items.Clear();
            if (newDataSource != null)
            {
                foreach (DataProvider dataProvider in newDataSource.Providers)
                {
                    dataProviderComboBox.Items.Add(dataProvider);
                }
                if (!_providerSelections.ContainsKey(newDataSource))
                {
                    _providerSelections.Add(newDataSource, newDataSource.DefaultProvider);
                }
                dataProviderComboBox.SelectedItem = _providerSelections[newDataSource];
            }
            else
            {
                dataProviderComboBox.Items.Add(string.Empty);
            }
            ConfigureDescription();
            SetOkButtonStatus();
        }

        private void SelectDataSource(object sender, EventArgs e)
        {
            if (okButton.Enabled)
            {
                DialogResult = DialogResult.OK;
                DoOk(sender, e);
                Close();
            }
        }

        private void FormatDataProvider(object sender, ListControlConvertEventArgs e)
        {
            if (e.DesiredType == typeof(string))
            {
                e.Value = (e.ListItem is DataProvider) ? (e.ListItem as DataProvider).DisplayName : e.ListItem.ToString();
            }
        }

        private void SetDataProviderDropDownWidth(object sender, EventArgs e)
        {
            if (dataProviderComboBox.Items.Count > 0 &&
                !(dataProviderComboBox.Items[0] is string))
            {
                int largestWidth = 0;
                using (Graphics g = Graphics.FromHwnd(dataProviderComboBox.Handle))
                {
                    foreach (DataProvider dataProvider in dataProviderComboBox.Items)
                    {
                        int width = TextRenderer.MeasureText(
                            g,
                            dataProvider.DisplayName,
                            dataProviderComboBox.Font,
                            new Size(int.MaxValue, int.MaxValue),
                            TextFormatFlags.WordBreak
                        ).Width;
                        if (width > largestWidth)
                        {
                            largestWidth = width;
                        }
                    }
                }
                dataProviderComboBox.DropDownWidth = largestWidth + 3; // give a little extra margin
                if (dataProviderComboBox.Items.Count > dataProviderComboBox.MaxDropDownItems)
                {
                    dataProviderComboBox.DropDownWidth += SystemInformation.VerticalScrollBarWidth;
                }
            }
            else
            {
                dataProviderComboBox.DropDownWidth = dataProviderComboBox.Width;
            }
        }

        private void ChangeDataProvider(object sender, EventArgs e)
        {
            if (dataSourceListBox.SelectedItem != null)
            {
                _providerSelections[dataSourceListBox.SelectedItem as DataSource] = dataProviderComboBox.SelectedItem as DataProvider;
            }
            ConfigureDescription();
            SetOkButtonStatus();
        }

        private void ConfigureDescription()
        {
            if (dataProviderComboBox.SelectedItem is DataProvider)
            {
                if (dataSourceListBox.SelectedItem == _mainDialog.UnspecifiedDataSource)
                {
                    descriptionLabel.Text = (dataProviderComboBox.SelectedItem as DataProvider).Description;
                }
                else
                {
                    descriptionLabel.Text = (dataProviderComboBox.SelectedItem as DataProvider).GetDescription(dataSourceListBox.SelectedItem as DataSource);
                }
            }
            else
            {
                descriptionLabel.Text = null;
            }
        }

        private void SetSaveSelection(object sender, EventArgs e)
        {
            _mainDialog.SaveSelection = saveSelectionCheckBox.Checked;
        }

        private void SetOkButtonStatus()
        {
            okButton.Enabled =
                dataSourceListBox.SelectedItem is DataSource &&
                dataProviderComboBox.SelectedItem is DataProvider;
        }

        private void DoOk(object sender, EventArgs e)
        {
            _mainDialog.SetSelectedDataSourceInternal(dataSourceListBox.SelectedItem as DataSource);
            foreach (DataSource dataSource in dataSourceListBox.Items)
            {
                DataProvider selectedProvider = (_providerSelections.ContainsKey(dataSource)) ? _providerSelections[dataSource] : null;
                _mainDialog.SetSelectedDataProviderInternal(dataSource, selectedProvider);
            }
        }

        private Label _headerLabel;
        private Dictionary<DataSource, DataProvider> _providerSelections = new Dictionary<DataSource, DataProvider>();
        private DataConnectionDialog _mainDialog;
    }
}