using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for HashTextActivityDesigner.xaml
    public partial class HashTextActivityDesigner
    {
        public HashTextActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<HashAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
