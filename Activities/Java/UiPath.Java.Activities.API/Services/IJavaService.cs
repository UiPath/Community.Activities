using System.Threading;
using System.Threading.Tasks;
using UiPath.Java.Activities.API.Models;

namespace UiPath.Java.Activities.API
{
    /// <summary>
    /// Provides Java interop capabilities for coded workflows.
    /// </summary>
    public interface IJavaService
    {
        /// <summary>
        /// Creates and initializes a Java scope configured with the given options.
        /// </summary>
        /// <param name="options">Options for configuring the Java scope.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>An <see cref="IJavaScopeHandle"/> representing the active Java scope.</returns>
        Task<IJavaScopeHandle> UseJavaScope(JavaScopeOptions options, CancellationToken ct = default);
    }
}
