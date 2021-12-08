using System.Activities.DesignViewModels;
using System.Activities.ViewModels;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UiPath.Cryptography.Activities.NetCore.ViewModels;

namespace UiPath.Cryptography.Activities
{
    /// <summary>
    /// Hashes a file with a key using a specified algorithm and encoding format 
    /// and returns the hexadecimal string representation of the resulting hash.
    /// </summary>
    [ViewModelClass(typeof(KeyedHashFileViewModel))]
    public partial class KeyedHashFile
    {
    }
}

namespace UiPath.Cryptography.Activities.NetCore.ViewModels
{
    public partial class KeyedHashFileViewModel : DesignPropertiesViewModel
    {
        /// <summary>
        /// Basic constructor
        /// </summary>
        /// <param name="services"></param>
        public KeyedHashFileViewModel(IDesignServices services) : base(services)
        {
        }

        /// <summary>
        /// A drop-down which enables you to select the keyed hashing algorithm you want to use.
        /// </summary>
        public DesignProperty<KeyedHashAlgorithms> Algorithm { get; set; } = new DesignProperty<KeyedHashAlgorithms>();

        /// <summary>
        /// The path to the file you want to hash.
        /// </summary>
        public DesignInArgument<string> FilePath { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The key that you want to use to hash the specified file.
        /// </summary>
        public DesignInArgument<string> Key { get; set; } = new DesignInArgument<string>();

        /// <summary>
        /// The secure string used to hash the provided file.
        /// </summary>
        public DesignInArgument<SecureString> KeySecureString { get; set; } = new DesignInArgument<SecureString>();

        /// <summary>
        /// The encoding used to interpret the key specified in the Key property.
        /// </summary>
        public DesignInArgument<Encoding> Encoding { get; set; } = new DesignInArgument<Encoding>();

        /// <summary>
        /// The hashed file, stored in a String variable.
        /// </summary>
        public DesignOutArgument<string> Result { get; set; } = new DesignOutArgument<string>();

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

            FilePath.IsPrincipal = true;
            FilePath.OrderIndex = propertyOrderIndex++;
            FilePath.Widget = new DefaultWidget { Type = ViewModelWidgetType.Browser };

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
