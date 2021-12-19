﻿using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Security;
using System.Text;
using System.Threading.Tasks;
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
    public partial class DecryptTextViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic consructor
        /// </summary>
        /// <param name="services"></param>
        public DecryptTextViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// A drop-down which enables you to select the decryption algorithm you want to use.
        /// </summary>
        public DesignProperty<SymmetricAlgorithms> Algorithm { get; set; } = new DesignProperty<SymmetricAlgorithms>();

        /// <summary>
        /// The text that you want to decrypt.
        /// </summary>
        public DesignInArgument<string> Input { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to decrypt the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to decrypt the input string.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// The encoding used to interpret the input text and the key specified in the Key property.
        /// </summary>
        public DesignInArgument<Encoding> Encoding { get; set; } = new DesignInArgument<Encoding>();

        /// <summary>
        /// The decrypted text, stored in a String variable.
        /// </summary>
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

        /// <summary>
        /// Specifies if the automation should continue even if the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            Input.IsPrincipal = true;
            Input.OrderIndex = propertyOrderIndex++;

            Key.IsPrincipal = true;
            Key.OrderIndex = propertyOrderIndex++;

            Encoding.IsPrincipal = false;
            Encoding.OrderIndex = propertyOrderIndex++;
            Encoding.Value = null;
            Encoding.IsRequired = true;

            KeySecureString.IsPrincipal = false;
            KeySecureString.OrderIndex = propertyOrderIndex++;

            Result.IsPrincipal = false;
            Result.OrderIndex = propertyOrderIndex++;

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;
        }
    }
}
