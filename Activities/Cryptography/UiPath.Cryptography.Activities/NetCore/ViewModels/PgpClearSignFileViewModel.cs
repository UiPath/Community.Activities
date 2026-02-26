using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpClearSignFileViewModel))]
    public partial class PgpClearSignFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpClearSignFileViewModel : PgpSignViewModelBase
    {
        public PgpClearSignFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignOutArgument<ILocalResource> ClearSignedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeOutputProperty(int orderIndex)
        {
            ClearSignedFile.IsPrincipal = false;
            ClearSignedFile.OrderIndex = orderIndex;
        }
    }
}
