using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpClearsignFileViewModel))]
    public partial class PgpClearsignFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpClearsignFileViewModel : PgpSignViewModelBase
    {
        public PgpClearsignFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignOutArgument<ILocalResource> ClearSignedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeOutputProperty(int orderIndex)
        {
            ClearSignedFile.IsPrincipal = false;
            ClearSignedFile.OrderIndex = orderIndex;
            ClearSignedFile.Category = Resources.Output;
        }
    }
}
