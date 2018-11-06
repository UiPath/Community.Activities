using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for KeyedHashFileActivityDesigner.xaml
    public partial class KeyedHashFileActivityDesigner
    {
        public KeyedHashFileActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<KeyedHashAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
