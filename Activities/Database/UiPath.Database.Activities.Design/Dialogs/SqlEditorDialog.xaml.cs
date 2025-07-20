using P = UiPath.Database.Activities.Design.Properties;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
using System.Windows;

namespace UiPath.Database.Activities.Design
{
    // Interaction logic for SqlEditorDialog.xaml
    public partial class SqlEditorDialog : WorkflowElementDialog
    {
        public SqlEditorDialog(ModelItem modelItem)
        {
            ModelItem = modelItem;
            Context = modelItem.GetEditingContext();
            InitializeComponent();
        }

        void OnParametersButtonClicked(object sender, RoutedEventArgs e)
        {
            DynamicArgumentDesignerOptions dadOptions = new DynamicArgumentDesignerOptions
            {
                Title = P.Resources.Parameters
            };
            DynamicArgumentDialog.ShowDialog(ModelItem, ModelItem.Properties["Parameters"].Value, ModelItem.GetEditingContext(), this, dadOptions);
        }
    }
}
