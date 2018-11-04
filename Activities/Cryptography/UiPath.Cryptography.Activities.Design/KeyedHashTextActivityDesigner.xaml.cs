using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for KeyedHashTextActivityDesigner.xaml
    public partial class KeyedHashTextActivityDesigner
    {
        public KeyedHashTextActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<KeyedHashAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
