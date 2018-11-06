using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for DecryptTextActivityDesigner.xaml
    public partial class DecryptTextActivityDesigner
    {
        public DecryptTextActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<SymmetricAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
