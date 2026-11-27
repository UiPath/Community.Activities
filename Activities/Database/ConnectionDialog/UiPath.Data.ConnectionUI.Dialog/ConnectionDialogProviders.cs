using System;
using System.Collections.Generic;
using UiPath.Database.Extensibility;

namespace UiPath.Data.ConnectionUI.Dialog
{
    /// <summary>
    /// A database the connection dialog offers in addition to the built-in ones, for example Oracle from the extended
    /// package. Declare it with <see cref="ConnectionDialogProviderAttribute"/> on the assembly that contains it.
    /// </summary>
    /// <remarks>The type must be a public, non-abstract class with a public parameterless constructor.</remarks>
    public interface IConnectionDialogProvider
    {
        /// <summary>The provider with its connection properties and UI control.</summary>
        DataProvider DataProvider { get; }

        /// <summary>The data source that lists <see cref="DataProvider"/>.</summary>
        DataSource DataSource { get; }
    }

    /// <summary>
    /// Marks an <see cref="IConnectionDialogProvider"/> of an assembly:
    /// <c>[assembly: ConnectionDialogProvider(typeof(MyDialogProvider))]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class ConnectionDialogProviderAttribute : Attribute
    {
        public ConnectionDialogProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        public Type ProviderType { get; }
    }

    /// <summary>Finds the dialog providers that installed packages bring.</summary>
    public static class ConnectionDialogProviders
    {
        /// <summary>Assemblies loaded by name when they are not loaded yet (a missing one means there is none).</summary>
        internal static readonly string[] WellKnownAssemblies = { "UiPath.Database.Activities.Extended.Design" };

        private static readonly ExtensionCatalog<IConnectionDialogProvider, ConnectionDialogProviderAttribute> Catalog =
            new ExtensionCatalog<IConnectionDialogProvider, ConnectionDialogProviderAttribute>(
                attribute => attribute.ProviderType,
                WellKnownAssemblies,
                typeof(ConnectionDialogProviderAttribute).Assembly);

        /// <summary>Returns every dialog provider that is installed now.</summary>
        public static IReadOnlyList<IConnectionDialogProvider> GetAll() => Catalog.GetAll();
    }
}
