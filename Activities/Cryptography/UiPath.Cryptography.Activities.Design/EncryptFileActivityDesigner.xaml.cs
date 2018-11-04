using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for EncryptFileActivityDesigner.xaml
    public partial class EncryptFileActivityDesigner
    {
        public EncryptFileActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<SymmetricAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
