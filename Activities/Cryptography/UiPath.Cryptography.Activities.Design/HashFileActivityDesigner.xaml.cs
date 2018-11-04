using UiPath.Shared;

namespace UiPath.Cryptography.Activities.Design
{
    // Interaction logic for HashFileActivityDesigner.xaml
    public partial class HashFileActivityDesigner
    {
        public HashFileActivityDesigner()
        {
            InitializeComponent();

            cbAlgorithms.ItemsSource = LocalizedEnum<HashAlgorithms>.GetLocalizedValues();
            cbAlgorithms.DisplayMemberPath = nameof(LocalizedEnum.Name);
            cbAlgorithms.SelectedValuePath = nameof(LocalizedEnum.Value);
        }
    }
}
