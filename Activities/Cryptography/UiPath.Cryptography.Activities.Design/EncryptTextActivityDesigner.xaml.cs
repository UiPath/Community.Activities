using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for EncryptTextActivityDesigner.xaml
    public partial class EncryptTextActivityDesigner
    {
        public EncryptTextActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<SymmetricAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
