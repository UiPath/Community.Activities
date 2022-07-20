using System.Windows;
using res = UiPath.Scripting.Activities.Design.Properties.Resources;

namespace UiPath.Scripting.Activities.Design
{
    /// <summary>
    /// Resource Dictionary for Custom Editors
    /// </summary>
    public partial class EditorResources
    {
        private static ResourceDictionary s_resources;

        internal static ResourceDictionary GetResources()
        {
            if (s_resources == null)
            {
                s_resources = new EditorResources();
            }
            return s_resources;
        }

        public EditorResources()
        {
            InitializeComponent();
        }
    }
}