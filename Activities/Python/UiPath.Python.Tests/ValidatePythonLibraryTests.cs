using System;
using System.IO;
using System.Runtime.InteropServices;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    public class ValidatePythonLibraryTests
    {
        private const string Category = "Python";

        private static readonly string EmbeddedRuntimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
        private static readonly string EmbeddedLibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(EmbeddedRuntimePath);

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void SupportedLibrary_DoesNotThrow()
        {
            // The embedded runtime is 64-bit Python 3.14, which is in the supported set.
            var ex = Record.Exception(() => EngineProvider.ValidatePythonLibrary(EmbeddedLibraryPath));
            Assert.Null(ex);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void UnsupportedVersionName_ThrowsNotSupported()
        {
            // A real 64-bit library (so the bitness check passes) but named for an unsupported
            // version — the version is derived from the file name, so this must be rejected.
            var dir = NewTempDir();
            try
            {
                var unsupported = Path.Combine(dir, "python36.dll");
                File.Copy(EmbeddedLibraryPath, unsupported);

                var ex = Assert.Throws<NotSupportedException>(
                    () => EngineProvider.ValidatePythonLibrary(unsupported));
                Assert.Contains("3.6", ex.Message);
            }
            finally
            {
                TryDeleteDir(dir);
            }
        }

        [SkippableFact]
        [Trait(TestCategories.Category, Category)]
        public void ThirtyTwoBitLibrary_ThrowsPlatformNotSupported()
        {
            // Bitness is read from the PE header on Windows; on other platforms the PE file is not
            // recognized as ELF and the check no-ops, so this assertion is Windows-specific.
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var dir = NewTempDir();
            try
            {
                // Supported version name so only the 32-bit check can trigger the failure.
                var lib32 = Path.Combine(dir, "python311.dll");
                WriteMinimalPortableExecutable(lib32, is64Bit: false);

                Assert.Throws<PlatformNotSupportedException>(
                    () => EngineProvider.ValidatePythonLibrary(lib32));
            }
            finally
            {
                TryDeleteDir(dir);
            }
        }

        [SkippableFact]
        [Trait(TestCategories.Category, Category)]
        public void SixtyFourBitLibrary_PassesBitnessCheck()
        {
            // Counterpart to the 32-bit test: a fabricated 64-bit PE with an unsupported version
            // name must fail on the version (NotSupported), not on bitness (PlatformNotSupported).
            // This proves the PE generator's 64-bit path is detected correctly.
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            var dir = NewTempDir();
            try
            {
                var lib64 = Path.Combine(dir, "python36.dll");
                WriteMinimalPortableExecutable(lib64, is64Bit: true);

                Assert.Throws<NotSupportedException>(
                    () => EngineProvider.ValidatePythonLibrary(lib64));
            }
            finally
            {
                TryDeleteDir(dir);
            }
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void MissingOrEmptyPath_DoesNotThrow()
        {
            // Fail-open: nothing to inspect means validation must never block a run.
            Assert.Null(Record.Exception(() => EngineProvider.ValidatePythonLibrary(null)));
            Assert.Null(Record.Exception(() => EngineProvider.ValidatePythonLibrary("   ")));
            Assert.Null(Record.Exception(() => EngineProvider.ValidatePythonLibrary(
                Path.Combine(Path.GetTempPath(), "does-not-exist-" + Guid.NewGuid().ToString("N") + ".dll"))));
        }

        #region Helpers

        private static string NewTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "validate-lib-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void TryDeleteDir(string dir)
        {
            try { Directory.Delete(dir, true); } catch { /* best effort */ }
        }

        /// <summary>
        /// Writes the smallest PE file the framework's PEReader will parse: a DOS header, the PE
        /// signature, a COFF header with no sections, and an optional header whose only meaningful
        /// field is the magic (PE32 vs PE32+). NumberOfRvaAndSizes stays zero, so there are no data
        /// directories and the header is self-consistent.
        /// </summary>
        private static void WriteMinimalPortableExecutable(string filePath, bool is64Bit)
        {
            const int peHeaderOffset = 0x80;
            ushort optionalHeaderSize = (ushort)(is64Bit ? 112 : 96);

            using var ms = new MemoryStream();
            using var w = new BinaryWriter(ms);

            // DOS header: 'MZ', then padding, with e_lfanew (PE offset) at 0x3C.
            w.Write((byte)'M');
            w.Write((byte)'Z');
            w.Write(new byte[0x3C - 2]);
            w.Write((uint)peHeaderOffset);          // e_lfanew
            w.Write(new byte[peHeaderOffset - 0x40]); // pad up to the PE header

            // PE signature "PE\0\0".
            w.Write(new byte[] { (byte)'P', (byte)'E', 0, 0 });

            // COFF header (20 bytes).
            w.Write((ushort)(is64Bit ? 0x8664 : 0x014C)); // Machine
            w.Write((ushort)0);                           // NumberOfSections
            w.Write((uint)0);                             // TimeDateStamp
            w.Write((uint)0);                             // PointerToSymbolTable
            w.Write((uint)0);                             // NumberOfSymbols
            w.Write(optionalHeaderSize);                  // SizeOfOptionalHeader
            w.Write((ushort)0x2002);                      // Characteristics: EXECUTABLE_IMAGE | DLL

            // Optional header: only Magic matters; the rest stays zero.
            w.Write((ushort)(is64Bit ? 0x20B : 0x10B));   // Magic: PE32+ / PE32
            w.Write(new byte[optionalHeaderSize - 2]);

            File.WriteAllBytes(filePath, ms.ToArray());
        }

        #endregion Helpers
    }
}
