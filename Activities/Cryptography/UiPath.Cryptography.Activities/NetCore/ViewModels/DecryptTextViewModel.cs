using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Cryptography.Activities.NetCore.ViewModels;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Decrypts text based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(DecryptTextViewModel))]
    public partial class DecryptText
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class DecryptTextViewModel : DecryptCryptoViewModelBase
    {
        public DecryptTextViewModel(IDesignServices services) : base(services)
        {
        }

        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            var orderIndex = 1;

            Input.IsPrincipal = true;
            Input.OrderIndex = orderIndex++;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);

            Result.IsPrincipal = false;
            Result.OrderIndex = orderIndex++;

            ConfigureTailProperties(ref orderIndex);
            ConfigureKeyInputModeMenuActions();
        }
    }
}
