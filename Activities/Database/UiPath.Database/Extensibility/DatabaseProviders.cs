using System;
using System.Collections.Generic;
using System.Data.Common;

namespace UiPath.Database.Extensibility
{
    /// <summary>
    /// Finds the providers that installed packages bring, for example the Oracle provider of
    /// <c>UiPath.Database.Activities</c> (see <see cref="ExtensionCatalog{TExtension, TAttribute}"/>).
    /// </summary>
    public static class DatabaseProviders
    {
        /// <summary>
        /// Assemblies loaded by name when they are not loaded yet. Loading only succeeds when the assembly is installed;
        /// a missing assembly simply means there is no extra provider.
        /// </summary>
        internal static readonly string[] WellKnownAssemblies = { "UiPath.Database.Activities.Extended" };

        private static readonly ExtensionCatalog<IDatabaseProvider, DatabaseProviderAttribute> Catalog =
            new ExtensionCatalog<IDatabaseProvider, DatabaseProviderAttribute>(
                attribute => attribute.ProviderType,
                WellKnownAssemblies,
                typeof(DatabaseProviderAttribute).Assembly);

        /// <summary>Returns the provider with the given ADO.NET provider name, or null.</summary>
        public static IDatabaseProvider Find(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                return null;
            }

            return Catalog.Find(provider => string.Equals(provider.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Returns the provider that owns <paramref name="connection"/>, or null.</summary>
        public static IDatabaseProvider FindFor(DbConnection connection)
        {
            if (connection == null)
            {
                return null;
            }

            return Catalog.Find(provider => provider.Owns(connection));
        }

        /// <summary>
        /// The reason an installed provider package could not be loaded during the last lookup, or null when the package
        /// is simply not installed.
        /// </summary>
        public static Exception LastLoadError => Catalog.LastLoadError;

        /// <summary>Returns every provider that is installed now.</summary>
        public static IReadOnlyList<IDatabaseProvider> GetAll() => Catalog.GetAll();
    }
}
