using System.Threading;
using System.Threading.Tasks;
using UiPath.Python.Activities.API.Models;

namespace UiPath.Python.Activities.API
{
    /// <summary>
    /// Provides Python scripting capabilities for coded workflows.
    /// </summary>
    public interface IPythonService
    {
        /// <summary>
        /// Creates and initializes a Python scope configured with the given options.
        /// </summary>
        /// <param name="options">Options for configuring the Python scope.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An <see cref="IPythonScopeHandle"/> representing the active Python scope.</returns>
        Task<IPythonScopeHandle> UsePythonScope(PythonScopeOptions options, CancellationToken ct = default);
    }
}
