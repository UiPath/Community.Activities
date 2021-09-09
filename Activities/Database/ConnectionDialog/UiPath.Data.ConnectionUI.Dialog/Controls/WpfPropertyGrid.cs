using System;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.View;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using UiPath.Data.ConnectionUI.Dialog.Dialogs;

namespace UiPath.Data.ConnectionUI.Dialog.Controls
{
    public enum PropertySort
    {
        NoSort = 0,
        Alphabetical = 1,
        Categorized = 2,
        CategorizedAlphabetical = 3
    };

    /// <summary>WPF Native PropertyGrid class, uses Workflow Foundation's PropertyInspector</summary>
    internal class WpfPropertyGrid : Grid
    {
        #region Private fields

        static WpfPropertyGrid current;
        private WorkflowDesigner Designer;
        private MethodInfo RefreshMethod;
        private MethodInfo OnSelectionChangedMethod;
        private MethodInfo IsInAlphaViewMethod;
        private Control PropertyToolBar;
        private Border ConStrText; 
        private Border ErrorText;
        private GridSplitter Splitter;
        private double TextBoxTextHeight = 60;

        #endregion

        #region Public properties

        /// <summary>Get or sets the selected object. Can be null.</summary>
        public object SelectedObject
        {
            get { return GetValue(SelectedObjectProperty); }
            set { SetValue(SelectedObjectProperty, value); }
        }

        /// <summary>Get or sets the selected object collection. Returns empty array by default.</summary>
        public object[] SelectedObjects
        {
            get { return GetValue(SelectedObjectsProperty) as object[]; }
            set { SetValue(SelectedObjectsProperty, value); }
        }

        /// <summary>XAML information with PropertyGrid's font and color information</summary>
        /// <seealso>Documentation for WorkflowDesigner.PropertyInspectorFontAndColorData</seealso>
        public string FontAndColorData
        {
            set
            {
                Designer.PropertyInspectorFontAndColorData = value;
            }
        }

        //makes the connection string section visible
        public bool ConStrVisible
        {
            get { return (bool)GetValue(ConStrVisibleProperty); }
            set { SetValue(ConStrVisibleProperty, value); }
        }

        /// <summary>Shows the tolbar on the top of the control</summary>
        public bool ToolbarVisible
        {
            get { return (bool)GetValue(ToolbarVisibleProperty); }
            set { SetValue(ToolbarVisibleProperty, value); }
        }

        /// <summary>The method of sorting</summary>
        public PropertySort PropertySort
        {
            get { return (PropertySort)GetValue(PropertySortProperty); }
            set { SetValue(PropertySortProperty, value); }
        }
        #endregion

        #region Dependency properties registration
        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register("SelectedObject", typeof(object), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectPropertyChanged));

        public static readonly DependencyProperty SelectedObjectsProperty =
            DependencyProperty.Register("SelectedObjects", typeof(object[]), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(new object[0], FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, SelectedObjectsPropertyChanged, CoerceSelectedObjects));

        public static readonly DependencyProperty ConStrVisibleProperty =
            DependencyProperty.Register("ConStrVisible", typeof(bool), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ConStrVisiblePropertyChanged));

        public static readonly DependencyProperty ToolbarVisibleProperty =
            DependencyProperty.Register("ToolbarVisible", typeof(bool), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ToolbarVisiblePropertyChanged));

        public static readonly DependencyProperty PropertySortProperty =
            DependencyProperty.Register("PropertySort", typeof(PropertySort), typeof(WpfPropertyGrid),
            new FrameworkPropertyMetadata(PropertySort.CategorizedAlphabetical, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, PropertySortPropertyChanged));
        #endregion

        /// <summary>Default constructor, creates the UIElements including a PropertyInspector</summary>
        public WpfPropertyGrid()
        {
            this.ColumnDefinitions.Add(new ColumnDefinition());
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });
            this.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0) });

            this.Designer = new WorkflowDesigner();

            TextBlock error = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFFFECED"),
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFBC316A")
            };

            TextBlock descrip = new TextBlock()
            {
                Visibility = Visibility.Visible,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            DockPanel dock = new DockPanel()
            {
                Visibility = Visibility.Visible,
                LastChildFill = true,
                Margin = new Thickness(3, 0, 3, 0)
            };

            descrip.SetValue(DockPanel.DockProperty, Dock.Top);

            dock.Children.Add(descrip);
            this.ErrorText = new Border()
            {
                Visibility = Visibility.Visible,
                BorderBrush = SystemColors.ActiveBorderBrush,
                Background = SystemColors.ControlBrush,
                BorderThickness = new Thickness(1),
                Child = error
            };
            this.ConStrText = new Border()
            {
                Visibility = Visibility.Visible,
                BorderBrush = SystemColors.ActiveBorderBrush,
                Background = SystemColors.ControlBrush,
                BorderThickness = new Thickness(1),
                Child = dock
            };
            this.Splitter = new GridSplitter()
            {
                Visibility = Visibility.Visible,
                ResizeDirection = GridResizeDirection.Rows,
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var inspector = Designer.PropertyInspectorView;
            inspector.Visibility = Visibility.Visible;
            inspector.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);


            this.Splitter.SetValue(Grid.RowProperty, 1);
            this.Splitter.SetValue(Grid.ColumnProperty, 0);

            this.ConStrText.SetValue(Grid.RowProperty, 2);
            this.ConStrText.SetValue(Grid.ColumnProperty, 0);

            this.ErrorText.SetValue(Grid.RowProperty, 3);
            this.ErrorText.SetValue(Grid.ColumnProperty, 0);

            Binding binding = new Binding("Parent.Background");
            descrip.SetBinding(BackgroundProperty, binding);

            this.Children.Add(inspector);
            this.Children.Add(this.Splitter);
            this.Children.Add(this.ConStrText);
            this.Children.Add(this.ErrorText);

            Type inspectorType = inspector.GetType();
            var props = inspectorType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            var methods = inspectorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);

            this.RefreshMethod = inspectorType.GetMethod("RefreshPropertyList",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.IsInAlphaViewMethod = inspectorType.GetMethod("set_IsInAlphaView",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            this.OnSelectionChangedMethod = inspectorType.GetMethod("OnSelectionChanged",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            this.PropertyToolBar = inspectorType.GetMethod("get_PropertyToolBar",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.DeclaredOnly).Invoke(inspector, new object[0]) as Control;

            //inspectorType.GetEvent("GotFocus").AddEventHandler(this,
            //    Delegate.CreateDelegate(typeof(RoutedEventHandler), this, "GotFocusHandler", false));
            //inspectorType.GetEvent("LostFocus").AddEventHandler(this,
            //    Delegate.CreateDelegate(typeof(RoutedEventHandler), this, "LostFocusHandler", false));
            current = this;
        }

        #region Dependency properties events
        private static object CoerceSelectedObjects(DependencyObject d, object value)
        {
            WpfPropertyGrid pg = d as WpfPropertyGrid;

            object single = pg.GetValue(SelectedObjectsProperty);

            return single == null ? new object[0] : value;
        }

        private static void SelectedObjectPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.CoerceValue(SelectedObjectsProperty);

            if (e.NewValue == null)
            {
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { null });
            }
            else
            {
                var context = new EditingContext();
                var mtm = new ModelTreeManager(context);
                mtm.Load(e.NewValue);
                Selection selection = Selection.Select(context, mtm.Root);
                
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { selection });
                ((AdoDotNetConnectionProperties)pg.SelectedObject).PropertyValueChanged += WpfPropertyGrid_PropertyValueChanged;
                ((AdoDotNetConnectionProperties)pg.SelectedObject).ErrorValidating += WpfPropertyGrid_ErrorValidating;
            }

            pg.ChangeHelpText(((AdoDotNetConnectionProperties)e.NewValue).ToDisplayString());
        }

        private static void WpfPropertyGrid_ErrorValidating(object sender, EventArgs e)
        {
            AdoDotNetConnectionProperties conProp = (AdoDotNetConnectionProperties)sender;
            current.SetErrorText(conProp.ErrorProperties);
        }

        private static void WpfPropertyGrid_PropertyValueChanged(object sender, EventArgs e)
        {
            AdoDotNetConnectionProperties conProp = (AdoDotNetConnectionProperties)sender;
            var connString = conProp.ToDisplayString();
            if (!conProp.HasErrors)
            {
                current.ChangeHelpText(connString);
                current.RefreshPropertyList();
            }
        }

        private static void SelectedObjectsPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.CoerceValue(SelectedObjectsProperty);

            object[] collection = e.NewValue as object[];

            if (collection.Length == 0)
            {
                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { null });
               
            }
            else
            {
                bool same = true;
                Type first = null;

                var context = new EditingContext();
                var mtm = new ModelTreeManager(context);
                Selection selection = null;

                // Accumulates the selection and determines the type to be shown in the top of the PG
                for (int i = 0; i < collection.Length; i++)
                {
                    mtm.Load(collection[i]);
                    if (i == 0)
                    {
                        selection = Selection.Select(context, mtm.Root);
                        first = collection[0].GetType();
                    }
                    else
                    {
                        selection = Selection.Union(context, mtm.Root);
                        if (!collection[i].GetType().Equals(first))
                            same = false;
                    }
                }

                pg.OnSelectionChangedMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { selection });
            }

            pg.ChangeHelpText(string.Empty);
        }

        private static void ConStrVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;

            if (e.NewValue != e.OldValue)
            {
                if (e.NewValue.Equals(true))
                {
                    pg.RowDefinitions[1].Height = new GridLength(5);
                    pg.RowDefinitions[2].Height = new GridLength(pg.TextBoxTextHeight);
                }
                else
                {
                    pg.TextBoxTextHeight = pg.RowDefinitions[2].Height.Value;
                    pg.RowDefinitions[1].Height = new GridLength(0);
                    pg.RowDefinitions[2].Height = new GridLength(0);
                }
            }
        }

        private static void ToolbarVisiblePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            pg.PropertyToolBar.Visibility = e.NewValue.Equals(true) ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void PropertySortPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            WpfPropertyGrid pg = source as WpfPropertyGrid;
            PropertySort sort = (PropertySort)e.NewValue;

            bool isAlpha = (sort == PropertySort.Alphabetical || sort == PropertySort.NoSort);
            pg.IsInAlphaViewMethod.Invoke(pg.Designer.PropertyInspectorView, new object[] { isAlpha });
        }
        #endregion

        ///// <summary>Updates the PropertyGrid's properties</summary>
        public void RefreshPropertyList()
        {
            RefreshMethod.Invoke(Designer.PropertyInspectorView, new object[] { false });
        }

        //Hide default controls used for Workflow Designer that we don't need
        protected override void OnRender(DrawingContext dc)
        {
            var searchButton = this.PropertyToolBar.GetType().GetProperty("SearchClearButton", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(this.PropertyToolBar) as Control;
            var searchBox = this.PropertyToolBar.GetType().GetProperty("SearchTextBox", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(this.PropertyToolBar) as Control;
            var searchLabel = this.PropertyToolBar.GetType().GetProperty("SearchLabel", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).GetValue(this.PropertyToolBar) as TextBlock;
            searchButton.Visibility = Visibility.Hidden;
            searchBox.Visibility = Visibility.Hidden;
            searchLabel.Visibility = Visibility.Hidden;
        }

        /// <summary>Changes the text help area contents</summary>
        /// <param name="title">Title in bold</param>
        /// <param name="descrip">Description with ellipsis</param>
        public void ChangeHelpText(string descrip)
        {
            DockPanel dock = this.ConStrText.Child as DockPanel;
            (dock.Children[0] as TextBlock).Text = descrip;
        }

        /// <summary>Changes the error area contents</summary>
        /// <param name="errors">Collection containing the properties with error</param>
        public void SetErrorText(IEnumerable<string> errors)
        {
            if (errors.Count() == 0)
            {
                this.RowDefinitions[3].Height = new GridLength(0);
                ((DataConnectionAdvancedDialog)((Grid)current.Parent).Parent).ToggleOKButton(true);
            }
            else
            {
                this.RowDefinitions[3].Height = GridLength.Auto;
                ((DataConnectionAdvancedDialog)((Grid)current.Parent).Parent).ToggleOKButton(false);
            }
            TextBlock tb = this.ErrorText.Child as TextBlock;
            string error = string.Format("Properties with error:\n\t{0}", string.Join("\n\t", errors));
            tb.Text = error;
        }
    }
}
