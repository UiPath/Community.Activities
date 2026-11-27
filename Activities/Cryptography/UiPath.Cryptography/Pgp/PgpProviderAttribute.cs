using System;

namespace UiPath.Cryptography.Pgp
{
    /// <summary>
    /// Marks the <see cref="IPgpProvider"/> of an assembly, for example the extended package:
    /// <c>[assembly: PgpProvider(typeof(MyProvider))]</c>. The type must be a public, non-abstract class with a public
    /// parameterless constructor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class PgpProviderAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="PgpProviderAttribute"/> class.</summary>
        /// <param name="providerType">The type that implements <see cref="IPgpProvider"/>.</param>
        public PgpProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        /// <summary>Gets the type that implements <see cref="IPgpProvider"/>.</summary>
        public Type ProviderType { get; }
    }
}
