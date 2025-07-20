using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using System.Windows;

namespace UiPath.Activities.Presentation.Editors
{
    /// <summary>
    /// Interaction logic for ArgumentDictionaryEditor.xaml
    /// </summary>
    public partial class ArgumentDictionaryEditor : DialogPropertyValueEditor
    {
        private static readonly DataTemplate EditorTemplate = (DataTemplate)new EditorTemplates()["ArgumentDictionaryEditor"];

        public ArgumentDictionaryEditor()
        {
            InlineEditorTemplate = EditorTemplate; 
        }

        public override void ShowDialog(PropertyValue propertyValue, IInputElement commandSource)
        {
            string propertyName = propertyValue.ParentProperty.PropertyName;

            var ownerActivity = (new ModelPropertyEntryToOwnerActivityConverter()).Convert(
                propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;

            DynamicArgumentDesignerOptions options = new DynamicArgumentDesignerOptions()
            {
                Title = propertyValue.ParentProperty.DisplayName
            };

            ModelItem modelItem = ownerActivity.Properties[propertyName].Dictionary;

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
