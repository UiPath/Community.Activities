using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for DecryptFileActivityDesigner.xaml
    public partial class DecryptFileActivityDesigner
    {
        public DecryptFileActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<SymmetricAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
