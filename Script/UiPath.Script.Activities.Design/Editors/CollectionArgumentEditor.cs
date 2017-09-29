using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using System.Windows;

namespace UiPath.Script.Activities.Design.Editors
{
    /// <summary>
    /// Interaction logic for ArgumentDictionaryEditor.xaml
    /// </summary>
    public partial class CollectionArgumentEditor : DialogPropertyValueEditor
    {
        private static DataTemplate EditorTemplate = (DataTemplate)new EditorTemplates()["CollectionArgumentEditor"];

        public CollectionArgumentEditor()
        {
            this.InlineEditorTemplate = EditorTemplate;
        }

        public override void ShowDialog(PropertyValue propertyValue, IInputElement commandSource)
        {
            string propertyName = propertyValue.ParentProperty.PropertyName;

            var ownerActivity = (new ModelPropertyEntryToOwnerActivityConverter()).Convert(
                propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;

            DynamicArgumentDesignerOptions options = new DynamicArgumentDesignerOptions()
            {
                Title = propertyName
            };

            ModelItem modelItem = ownerActivity.Properties[propertyName].Collection;

            using (ModelEditingScope change = modelItem.BeginEdit(propertyName + "Editing"))
            {
                if (DynamicArgumentDialog.ShowDialog(ownerActivity, modelItem, ownerActivity.GetEditingContext(), ownerActivity.View, options))
                {
                    change.Complete();
                }
                else
                {
                    change.Revert();
                }
            }
        }
    }
}
