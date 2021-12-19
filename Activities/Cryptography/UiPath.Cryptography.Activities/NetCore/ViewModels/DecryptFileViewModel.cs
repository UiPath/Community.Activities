﻿using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.NetCore.ViewModels;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Decrypts a file based on a specified key encoding and algorithm.
    /// </summary>
    [ViewModelClass(typeof(DecryptFileViewModel))]
    public partial class DecryptFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class DecryptFileViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public DecryptFileViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// A drop-down which enables you to select the decryption algorithm you want to use.
        /// </summary>
        public DesignProperty<SymmetricAlgorithms> Algorithm { get; set; } = new DesignProperty<SymmetricAlgorithms>();

        /// <summary>
        /// The path to the file that you want to decrypt.
        /// </summary>
        public DesignInArgument<string> InputFilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to decrypt the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to decrypt the input file.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// The encoding used to interpret the key specified in the Key property.
        /// </summary>
        public DesignInArgument<Encoding> KeyEncoding { get; set; } = new DesignInArgument<Encoding>();

        /// <summary>
        /// The path where you want to save the decrypted file.
        /// </summary>
        public DesignInArgument<string> OutputFilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// If a file already exists at the path specified in the Output path field, selecting this check box overwrites it. 
        /// </summary>
        public DesignProperty<bool> Overwrite { get; set; } = new DesignProperty<bool>();

        /// <summary>
        /// Specifies if the automation should continue even when the activity throws an error.
        /// </summary>
        public DesignInArgument<bool> ContinueOnError { get; set; } = new DesignInArgument<bool>();

        protected override void InitializeModel()
        {
            base.InitializeModel();
            int propertyOrderIndex = 1;

            Algorithm.IsPrincipal = true;
            Algorithm.OrderIndex = propertyOrderIndex++;
            Algorithm.Widget = new DefaultWidget { Type = ViewModelWidgetType.Dropdown };

            InputFilePath.IsPrincipal = true;
            InputFilePath.OrderIndex = propertyOrderIndex++;

            Key.IsPrincipal = true;
            Key.OrderIndex = propertyOrderIndex++;

            OutputFilePath.IsPrincipal = true;
            OutputFilePath.OrderIndex = propertyOrderIndex++;

            KeySecureString.IsPrincipal = false;
            KeySecureString.OrderIndex = propertyOrderIndex++;

            KeyEncoding.IsPrincipal = false;
            KeyEncoding.OrderIndex = propertyOrderIndex++;
            KeyEncoding.Value = null;
            KeyEncoding.IsRequired = true;

            Overwrite.IsPrincipal = false;
            Overwrite.OrderIndex = propertyOrderIndex++;
            Overwrite.Widget = new DefaultWidget { Type = ViewModelWidgetType.Toggle };

            ContinueOnError.IsPrincipal = false;
            ContinueOnError.OrderIndex = propertyOrderIndex++;
            ContinueOnError.Widget = new DefaultWidget { Type = ViewModelWidgetType.NullableBoolean };
            ContinueOnError.Value = false;
        }
    }
}
