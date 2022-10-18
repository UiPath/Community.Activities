using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Converters;
using System.Activities.Presentation.Model;
using System.Activities.Presentation.PropertyEditing;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace UiPath.Scripting.Activities.Design
{
    /// <summary>
    /// Argument Dictionary Editor
    /// </summary>
    internal class ArgumentDictionaryEditor : DialogPropertyValueEditor
    {
        public ArgumentDictionaryEditor()
        {
            this.InlineEditorTemplate = (DataTemplate)EditorResources.GetResources()["ArgumentDictionaryEditor"];
        }

        public override void ShowDialog(PropertyValue propertyValue, IInputElement commandSource)
        {
            string propertyName = propertyValue.ParentProperty.PropertyName;
            string displayName = propertyValue.ParentProperty.DisplayName ?? propertyName;

            var ownerActivity = (new ModelPropertyEntryToOwnerActivityConverter()).Convert(propertyValue.ParentProperty, typeof(ModelItem), false, null) as ModelItem;
            EditingContext context = ownerActivity.GetEditingContext();

            DynamicArgumentDesignerOptions options = new DynamicArgumentDesignerOptions()
            {
                Title = displayName
            };

            ModelItem modelItem = ownerActivity.Properties[propertyName].Dictionary;
            if (modelItem == null)
            {
                // If the cast to Dictionary failed, we attempt to consider the propery as Collection,
                // given that the DynamicArgumentDialog supports both Dictionaries and Collections.
                modelItem = ownerActivity.Properties[propertyName].Collection;
            }

            using (ModelEditingScope change = modelItem.BeginEdit(propertyName + "Editing"))
            {
                if (DynamicArgumentDialog.ShowDialog(ownerActivity, modelItem, context, ownerActivity.View, options))
                {
                    change.Complete();

                    //to trigger ModelItem property changed event
                    if (modelItem.GetCurrentValue() is Dictionary<string, Argument> d)
                    {
                        ownerActivity.Properties[propertyName].SetValue(d.ToDictionary(e => e.Key, e => e.Value));
                    }
                }
                else
                {
                    change.Revert();
                }
            }
        }
    }
}
