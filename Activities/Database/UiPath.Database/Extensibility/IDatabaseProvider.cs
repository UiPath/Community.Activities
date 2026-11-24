using System;
using System.Data.Common;
using UiPath.Database.BulkOps;

namespace UiPath.Database.Extensibility
{
    /// <summary>
    /// A database provider that is not part of the base package, for example the Oracle provider of the extended
    /// package. Declare it with <see cref="DatabaseProviderAttribute"/> on the assembly that contains it; the base
    /// package finds it with <see cref="DatabaseProviders"/>.
    /// </summary>
    /// <remarks>
    /// The type must be a public, non-abstract class with a public parameterless constructor. One instance is created
    /// per process and reused.
    /// <para>
    /// This interface ships in the base package and is implemented by extension packages that can be installed at a
    /// different version than the base package. Never add, remove or change a member: a mismatch fails to load the
    /// extension with a <see cref="TypeLoadException"/>. The same holds for every other type the extension implements,
    /// such as <see cref="BulkOps.IBulkOperations"/>. Add new capabilities as separate, optional interfaces that the
    /// base package tests for with <c>is</c>.
    /// </para>
    /// </remarks>
    public interface IDatabaseProvider
    {
        /// <summary>The ADO.NET provider name, for example <c>Oracle.ManagedDataAccess.Client</c>.</summary>
        string ProviderName { get; }

        /// <summary>The ADO.NET factory of the provider.</summary>
        DbProviderFactory Factory { get; }

        /// <summary>Provider-wide setup. Called before the provider is used and may be called more than once.</summary>
        void Initialize();

        /// <summary>Whether <paramref name="connection"/> is a connection of this provider.</summary>
        bool Owns(DbConnection connection);

        /// <summary>Whether the provider can insert and update data tables in bulk.</summary>
        bool SupportsBulk { get; }

        /// <summary>Creates the bulk operations of the provider, or null when <see cref="SupportsBulk"/> is false.</summary>
        IBulkOperations CreateBulkOperations();

        /// <summary>
        /// Gives <paramref name="parameter"/> the provider-specific type that matches <paramref name="clrType"/>.
        /// Returns false when the parameter does not belong to the provider or no mapping exists.
        /// </summary>
        bool TryUpdateParameterType(DbParameter parameter, Type clrType);

        /// <summary>
        /// The statement that creates the temporary table used by bulk updates, or null when the provider does not
        /// need one.
        /// </summary>
        /// <param name="tempTableName">The name of the temporary table.</param>
        /// <param name="columnList">The comma separated columns to copy.</param>
        /// <param name="sourceTable">The table whose structure is copied.</param>
        string GetCreateTempTableSql(string tempTableName, string columnList, string sourceTable);
    }

    /// <summary>
    /// Marks an <see cref="IDatabaseProvider"/> of an assembly so that the base package finds it automatically:
    /// <c>[assembly: DatabaseProvider(typeof(MyProvider))]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class DatabaseProviderAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="DatabaseProviderAttribute"/> class.</summary>
        /// <param name="providerType">The type that implements <see cref="IDatabaseProvider"/>.</param>
        public DatabaseProviderAttribute(Type providerType)
        {
            ProviderType = providerType;
        }

        /// <summary>The type that implements <see cref="IDatabaseProvider"/>.</summary>
        public Type ProviderType { get; }
    }
}
