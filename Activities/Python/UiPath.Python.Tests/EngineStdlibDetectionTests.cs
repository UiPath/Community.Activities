using System;
using System.IO;
using UiPath.Python.Impl;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // Direct, fast tests for Engine's stdlib-detection helpers (HasStdlib and friends) — no
    // Python engine involved. "Lib" (Windows) vs "lib" (POSIX) is the real, fixed folder name
    // each platform's CPython build/installer produces, not a style choice — WindowsStdlibLandmark
    // and PosixStdlibDirectory assert on the exact, case-sensitive string each platform's check is
    // built from, since a real Directory.Exists call on this repo's Windows-only CI can't tell
    // "Lib" apart from "lib" (NTFS resolves both to the same directory by default), so filesystem
    // behavior alone can't prove the check asks for the right casing on either platform.
    public class EngineStdlibDetectionTests : IDisposable
    {
        private const string Category = "Python";

        private readonly string _rootDir;

        public EngineStdlibDetectionTests()
        {
            _rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "engine-stdlib-tests", Guid.NewGuid().ToString("N"))).FullName;
        }

        public void Dispose()
        {
            try { Directory.Delete(_rootDir, true); } catch { /* best effort cleanup */ }
            GC.SuppressFinalize(this);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void WindowsStdlibLandmark_Uses_CapitalL_Lib_Segment()
        {
            var landmark = Engine.WindowsStdlibLandmark(_rootDir);

            Assert.Equal(Path.Combine(_rootDir, "Lib", "encodings"), landmark, StringComparer.Ordinal);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void PosixStdlibDirectory_Uses_Lowercase_Lib_Segment()
        {
            var libDir = Engine.PosixStdlibDirectory(_rootDir);

            Assert.Equal(Path.Combine(_rootDir, "lib"), libDir, StringComparer.Ordinal);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void WindowsAndPosix_Segments_Differ_In_Case()
        {
            // Pins the two platforms' checks to genuinely different, non-interchangeable strings
            // — guards against a future edit accidentally making both branches check the same
            // (either) casing, which real Directory.Exists calls on this Windows-only CI would
            // never catch on their own (NTFS folds the case difference away).
            var windowsSegment = Path.GetFileName(Path.GetDirectoryName(Engine.WindowsStdlibLandmark(_rootDir)));
            var posixSegment = Path.GetFileName(Engine.PosixStdlibDirectory(_rootDir));

            Assert.Equal("Lib", windowsSegment, StringComparer.Ordinal);
            Assert.Equal("lib", posixSegment, StringComparer.Ordinal);
            Assert.NotEqual(windowsSegment, posixSegment, StringComparer.Ordinal);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void HasStdlib_True_When_Landmark_Present()
        {
            // Runs on this repo's actual CI/dev OS (Windows), exercising the real branch HasStdlib
            // takes here — confirms the happy path independent of the case-sensitivity question
            // above.
            Directory.CreateDirectory(Path.Combine(_rootDir, "Lib", "encodings"));

            Assert.True(EngineHasStdlib(_rootDir));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void HasStdlib_False_When_No_Lib_Folder_At_All()
        {
            // Mirrors a Microsoft Store Python's app-execution-alias folder: exists, but carries
            // no Lib folder whatsoever.
            Assert.False(EngineHasStdlib(_rootDir));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void HasStdlib_False_For_NullOrEmpty()
        {
            Assert.False(EngineHasStdlib(null));
            Assert.False(EngineHasStdlib(string.Empty));
        }

        // Engine.HasStdlib itself is private (only ConfigureRuntime should call it directly) —
        // reached here via reflection so this suite doesn't need to widen that method's
        // visibility just for testing, unlike WindowsStdlibLandmark/PosixStdlibDirectory, whose
        // whole purpose is to be asserted on directly.
        private static bool EngineHasStdlib(string pythonHome)
        {
            var method = typeof(Engine).GetMethod("HasStdlib", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (bool)method.Invoke(null, new object[] { pythonHome });
        }
    }
}
