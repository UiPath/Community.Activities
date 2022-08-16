using System.Windows;
using res = UiPath.Scripting.Activities.Design.Properties.Resources;

namespace UiPath.Scripting.Activities.Design
{
    /// <summary>
    /// Resource Dictionary for Custom Editors
    /// </summary>
    public partial class EditorResources
    {
        private static ResourceDictionary _resources;

        internal static ResourceDictionary GetResources()
        {
            if (_resources == null)
            {
                _resources = new EditorResources();
            }
            return _resources;
        }

        public EditorResources()
        {
            InitializeComponent();
        }
    }
}
