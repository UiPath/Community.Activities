using UiPath.Database;
using UiPath.Database.Activities.API.Models;

namespace UiPath.Database.Activities.API
{
    /// <summary>
    /// Provides database connectivity capabilities for coded workflows.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Opens a database connection configured with the given options.
        /// The returned <see cref="DatabaseConnection"/> must be disposed when no longer needed.
        /// </summary>
        /// <param name="options">Connection options (provider name and connection string).</param>
        /// <returns>An open <see cref="DatabaseConnection"/>.</returns>
        DatabaseConnection UseConnection(DatabaseScopeOptions options);
    }
}
