using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using UiPath.Cryptography.Activities.NetCore.ViewModels;
using UiPath.Cryptography.Activities.Properties;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Encrypts a string with a key based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(EncryptTextViewModel))]
    public partial class EncryptText
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class EncryptTextViewModel : EncryptCryptoViewModelBase
    {
        public EncryptTextViewModel(IDesignServices services) : base(services)
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
            Input.Category = Resources.Input;

            ConfigureAlgorithmAndKeyProperties(ref orderIndex);

            Result.IsPrincipal = false;
            Result.OrderIndex = orderIndex++;
            Result.Category = Resources.Output;

            ConfigureTailProperties(ref orderIndex);
            ConfigureKeyInputModeMenuActions();
            ConfigurePublicKeyFileMenuActions();
            ConfigurePassphraseInputModeMenuActions();
        }
    }
}
