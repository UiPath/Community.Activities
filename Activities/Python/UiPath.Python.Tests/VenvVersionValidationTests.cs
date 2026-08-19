using System;
using System.IO;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // Regression tests for EngineProvider.ValidateVenvVersion (called from the public
    // ValidateInstallation): a venv's declared Python version (its own pyvenv.cfg) must match the
    // version actually loaded from LibraryPath, or initialization would otherwise proceed with a
    // mismatched interpreter/site-packages pairing and fail later with a confusing error instead
    // of a clear one up front.
    public class VenvVersionValidationTests : IDisposable
    {
        private const string Category = "Python";

        private static readonly string EmbeddedRuntimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
        private static readonly string EmbeddedLibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(EmbeddedRuntimePath);

        private readonly string _rootDir;
        private readonly string _venvDir;

        public VenvVersionValidationTests()
        {
            _rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "venv-version-tests", Guid.NewGuid().ToString("N"))).FullName;
            _venvDir = Path.Combine(_rootDir, ".venv");
        }

        public void Dispose()
        {
            try { Directory.Delete(_rootDir, true); } catch { /* best effort cleanup */ }
        }

        private void WriteVenvCfg(string version)
        {
            Directory.CreateDirectory(_venvDir);
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}version = {version}{Environment.NewLine}");
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void MismatchedVenvVersion_Throws()
        {
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            // The embeddable runtime bootstrap is 3.14.5 — declare something else entirely.
            WriteVenvCfg("3.9.0");

            Assert.Throws<NotSupportedException>(() => EngineProvider.ValidateInstallation(_venvDir, EmbeddedLibraryPath));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void MatchingVenvVersion_DoesNotThrow()
        {
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            WriteVenvCfg("3.14.5");

            // Must not throw.
            EngineProvider.ValidateInstallation(_venvDir, EmbeddedLibraryPath);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void NonVenvPath_DoesNotThrow()
        {
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            // _venvDir has no pyvenv.cfg at all here — not a venv, so the version cross-check
            // must not even engage.
            Directory.CreateDirectory(_venvDir);

            EngineProvider.ValidateInstallation(_venvDir, EmbeddedLibraryPath);
        }
    }
}
