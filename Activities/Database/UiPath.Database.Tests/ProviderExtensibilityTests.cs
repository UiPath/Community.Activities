using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.Data.Sqlite;
using UiPath.Database;
using UiPath.Database.BulkOps;
using UiPath.Database.Extensibility;
using UiPath.Database.Tests;
using Xunit;

// A provider of this test assembly: the base assembly finds it the way it finds the Oracle provider of the extended package.
[assembly: DatabaseProvider(typeof(FakeDatabaseProvider))]

// Extensions for the catalog tests: one that works and ones the catalog must ignore.
[assembly: TestExtension(typeof(GoodTestExtension))]
[assembly: TestExtension(typeof(AbstractTestExtension))]
[assembly: TestExtension(typeof(NoDefaultConstructorTestExtension))]
[assembly: TestExtension(typeof(NotAnExtension))]
[assembly: TestExtension(typeof(ThrowingTestExtension))]

namespace UiPath.Database.Tests
{
    public interface ITestExtension
    {
        string Name { get; }
    }

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TestExtensionAttribute : Attribute
    {
        public TestExtensionAttribute(Type type) => Type = type;

        public Type Type { get; }
    }

    public class GoodTestExtension : ITestExtension
    {
        public string Name => "good";
    }

    public abstract class AbstractTestExtension : ITestExtension
    {
        public string Name => "abstract";
    }

    public class NoDefaultConstructorTestExtension : ITestExtension
    {
        public NoDefaultConstructorTestExtension(int value)
        {
        }

        public string Name => "no default constructor";
    }

    public class NotAnExtension
    {
    }

    public class ThrowingTestExtension : ITestExtension
    {
        public ThrowingTestExtension() => throw new InvalidOperationException("cannot be created");

        public string Name => "throwing";
    }

    public class FakeDatabaseProvider : IDatabaseProvider
    {
        public static int InitializeCalls;

        public string ProviderName => "Fake.Provider";

        public DbProviderFactory Factory => SqliteFactory.Instance;

        public bool SupportsBulk => true;

        public void Initialize() => InitializeCalls++;

        public bool Owns(DbConnection connection) => false;

        public IBulkOperations CreateBulkOperations() => null;

        public bool TryUpdateParameterType(DbParameter parameter, Type clrType) => false;

        public string GetCreateTempTableSql(string tempTableName, string columnList, string sourceTable) => null;
    }

    public class ExtensionCatalogTests
    {
        private static readonly Assembly TestAssembly = typeof(ExtensionCatalogTests).Assembly;

        // The attribute lives in the base assembly in production; here the test assembly plays the extension package.
        private static ExtensionCatalog<ITestExtension, TestExtensionAttribute> CatalogOf(
            Func<IEnumerable<Assembly>> loaded,
            string[] wellKnown = null,
            Func<string, Assembly> load = null) =>
            new ExtensionCatalog<ITestExtension, TestExtensionAttribute>(
                attribute => attribute.Type,
                wellKnown ?? new string[0],
                typeof(DatabaseConnection).Assembly,
                loaded,
                load ?? (name => throw new System.IO.FileNotFoundException(name)));

        [Fact]
        public void Catalog_FindsTheExtensionsOfALoadedAssembly()
        {
            var catalog = CatalogOf(() => new[] { TestAssembly });

            ITestExtension found = catalog.Find(extension => extension.Name == "good");

            Assert.IsType<GoodTestExtension>(found);
        }

        [Fact]
        public void Catalog_IgnoresTypesThatAreNotUsableExtensions()
        {
            var catalog = CatalogOf(() => new[] { TestAssembly });

            IReadOnlyList<ITestExtension> all = catalog.GetAll();

            Assert.Single(all);
            Assert.IsType<GoodTestExtension>(all[0]);
        }

        [Fact]
        public void Catalog_KeepsOneInstancePerType()
        {
            var catalog = CatalogOf(() => new[] { TestAssembly });

            Assert.Same(catalog.GetAll().Single(), catalog.GetAll().Single());
        }

        [Fact]
        public void Catalog_DoesNotRememberAMiss()
        {
            // Studio loads assemblies lazily: the assembly of the extension may be loaded after the first lookup.
            var loaded = new List<Assembly>();
            var catalog = CatalogOf(() => loaded.ToList());

            Assert.Null(catalog.Find(extension => extension.Name == "good"));

            loaded.Add(TestAssembly);

            Assert.NotNull(catalog.Find(extension => extension.Name == "good"));
        }

        [Fact]
        public void Catalog_LoadsAWellKnownAssemblyByNameWhenItIsNotLoadedYet()
        {
            var requested = new List<string>();
            var catalog = CatalogOf(
                () => new Assembly[0],
                new[] { "The.Extension.Assembly" },
                name =>
                {
                    requested.Add(name);
                    return TestAssembly;
                });

            Assert.NotNull(catalog.Find(extension => extension.Name == "good"));
            Assert.Equal(new[] { "The.Extension.Assembly" }, requested);
        }

        [Fact]
        public void Catalog_TreatsAMissingWellKnownAssemblyAsNoExtension()
        {
            var catalog = CatalogOf(() => new Assembly[0], new[] { "Not.Installed" });

            Assert.Null(catalog.Find(extension => true));
            Assert.Empty(catalog.GetAll());
        }

        [Fact]
        public void Catalog_SkipsAnExtensionWhoseLookupThrows()
        {
            var catalog = CatalogOf(() => new[] { TestAssembly });

            Assert.Null(catalog.Find(extension => throw new InvalidOperationException("broken")));
        }

        [Fact]
        public void Catalog_OnlyReadsAssembliesThatReferenceTheOwner()
        {
            // The assembly of System.Private.CoreLib does not reference UiPath.Database, so its attributes are not read.
            var catalog = CatalogOf(() => new[] { typeof(object).Assembly });

            Assert.Empty(catalog.GetAll());
        }
    }

    public class DatabaseProvidersTests
    {
        [Fact]
        public void Find_ReturnsAProviderDeclaredByALoadedAssembly()
        {
            IDatabaseProvider provider = DatabaseProviders.Find("fake.provider");

            Assert.IsType<FakeDatabaseProvider>(provider);
            Assert.Contains(DatabaseProviders.GetAll(), p => p is FakeDatabaseProvider);
        }

        [Fact]
        public void Find_ReturnsNullForAnUnknownProviderOrEmptyName()
        {
            Assert.Null(DatabaseProviders.Find("No.Such.Provider"));
            Assert.Null(DatabaseProviders.Find(null));
            Assert.Null(DatabaseProviders.Find(string.Empty));
            Assert.Null(DatabaseProviders.FindFor(null));
        }

        [Fact]
        public void OracleIsNotPartOfTheBaseAssembly()
        {
            // The base package must not ship or reference the Oracle driver: it carries its own cryptography.
            Assert.DoesNotContain(
                typeof(DatabaseConnection).Assembly.GetReferencedAssemblies(),
                reference => reference.Name.StartsWith("Oracle", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class DatabaseConnectionProviderTests
    {
        [Fact]
        public void Initialize_UsesAnExtraProviderOfAnInstalledPackage()
        {
            int before = FakeDatabaseProvider.InitializeCalls;

            using (var connection = new DatabaseConnection().Initialize("Data Source=:memory:", "Fake.Provider"))
            {
                Assert.Equal(System.Data.ConnectionState.Open, connection.State);
                Assert.True(connection.SupportsBulk());
            }

            Assert.True(FakeDatabaseProvider.InitializeCalls > before);
        }

        [Fact]
        public void Initialize_ExplainsThatTheOracleProviderComesWithTheExtendedPackage()
        {
            var exception = Assert.Throws<InvalidOperationException>(
                () => new DatabaseConnection().Initialize("User Id=x;Password=y;Data Source=z", DatabaseConstants.OracleProvider));

            Assert.Contains(DatabaseConstants.OracleProvider, exception.Message);
            Assert.Contains("UiPath.Database.Activities", exception.Message);
        }

        [Fact]
        public void Initialize_StillFailsForAnUnknownProvider()
        {
            Assert.Throws<ArgumentException>(() => new DatabaseConnection().Initialize("x", "No.Such.Provider"));
        }

        [Fact]
        public void SupportsBulk_IsFalseForAProviderWithoutBulkSupport()
        {
            using (var connection = new DatabaseConnection().Initialize(new SqliteConnection("Data Source=:memory:")))
            {
                Assert.False(connection.SupportsBulk());
            }
        }

        [Fact]
        public void BulkOperationsFactory_HasNoBulkOperationsForAConnectionWithoutAProvider()
        {
            Assert.Null(BulkOperationsFactory.Create(new SqliteConnection("Data Source=:memory:")));
        }
    }
}
