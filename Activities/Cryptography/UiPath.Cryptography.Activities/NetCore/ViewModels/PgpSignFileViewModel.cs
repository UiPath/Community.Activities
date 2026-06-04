using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Diagnostics.CodeAnalysis;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;
using UiPath.Platform.ResourceHandling;

namespace UiPath.Cryptography.Activities
{
    [ViewModelClass(typeof(PgpSignFileViewModel))]
    public partial class PgpSignFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    [ExcludeFromCodeCoverage]
    public class PgpSignFileViewModel : PgpSignViewModelBase
    {
        public PgpSignFileViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignOutArgument<ILocalResource> SignedFile { get; set; } = new DesignOutArgument<ILocalResource>();

        protected override void InitializeOutputProperty(int orderIndex)
        {
            SignedFile.IsPrincipal = false;
            SignedFile.OrderIndex = orderIndex;
            SignedFile.Category = Resources.Output;
        }
    }
}
