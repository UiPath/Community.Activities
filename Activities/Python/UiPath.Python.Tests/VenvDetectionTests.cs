using System;
using System.IO;
using UiPath.Python.Impl;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // Direct, fast tests for VenvDetection.GetVenvInfo — no Python engine involved. Covers the
    // detection shapes discussed during the STUD-81085 review: venv root, its Scripts/bin
    // launcher folder, and the false-positive an unbounded ancestor walk used to allow (an
    // unrelated, fully-standalone installation merely sitting near someone else's pyvenv.cfg).
    public class VenvDetectionTests : IDisposable
    {
        private const string Category = "Python";

        private readonly string _rootDir;

        public VenvDetectionTests()
        {
            _rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "venv-detection-tests", Guid.NewGuid().ToString("N"))).FullName;
        }

        public void Dispose()
        {
            try { Directory.Delete(_rootDir, true); } catch { /* best effort cleanup */ }
        }

        private string WriteVenvCfg(string venvDir, string home = @"C:\FakeBase", string extra = null)
        {
            Directory.CreateDirectory(venvDir);
            var content = $"home = {home}{Environment.NewLine}version = 3.13.0{Environment.NewLine}{extra}";
            File.WriteAllText(Path.Combine(venvDir, "pyvenv.cfg"), content);
            return venvDir;
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void VenvRoot_Is_Detected()
        {
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "myvenv"));

            var venv = VenvDetection.GetVenvInfo(venvDir);

            Assert.NotNull(venv);
            Assert.Equal(venvDir, venv.Root);
            Assert.Equal(@"C:\FakeBase", venv.Home);
        }

        [Theory]
        [InlineData("Scripts")]
        [InlineData("bin")]
        [Trait(TestCategories.Category, Category)]
        public void LauncherSubfolder_Is_Detected(string folderName)
        {
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "myvenv"));
            var launcherDir = Directory.CreateDirectory(Path.Combine(venvDir, folderName)).FullName;

            var venv = VenvDetection.GetVenvInfo(launcherDir);

            Assert.NotNull(venv);
            Assert.Equal(venvDir, venv.Root);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void UnrelatedFolder_OneLevelBelow_WrongName_Is_Not_Detected()
        {
            // pyvenv.cfg one level up, but the intermediate folder isn't a real venv launcher
            // name — a fully standalone install could legitimately live here and must not be
            // mistaken for being inside someone else's venv.
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "someones_venv"));
            var standaloneDir = Directory.CreateDirectory(Path.Combine(venvDir, "runtime")).FullName;

            var venv = VenvDetection.GetVenvInfo(standaloneDir);

            Assert.Null(venv);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void TwoLevelsUp_Is_Not_Detected()
        {
            // Even with the right launcher name one level further up, detection deliberately
            // doesn't walk a second level — no real venv layout ever needs it.
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "myvenv"));
            var scriptsDir = Directory.CreateDirectory(Path.Combine(venvDir, "Scripts")).FullName;
            var nestedDir = Directory.CreateDirectory(Path.Combine(scriptsDir, "nested")).FullName;

            var venv = VenvDetection.GetVenvInfo(nestedDir);

            Assert.Null(venv);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void StrayFile_Without_HomeOrVersion_Is_Not_Treated_As_Venv()
        {
            var dir = Directory.CreateDirectory(Path.Combine(_rootDir, "notavenv")).FullName;
            File.WriteAllText(Path.Combine(dir, "pyvenv.cfg"), "some-unrelated-key = value" + Environment.NewLine);

            var venv = VenvDetection.GetVenvInfo(dir);

            Assert.Null(venv);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void SystemSitePackages_Flag_Is_Captured()
        {
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "myvenv"), extra: "include-system-site-packages = true" + Environment.NewLine);

            var venv = VenvDetection.GetVenvInfo(venvDir);

            Assert.NotNull(venv);
            Assert.True(venv.IncludeSystemSitePackages);
            Assert.False(venv.ShouldDisableUserSite);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public void Default_DoesNotIncludeSystemSitePackages_ShouldDisableUserSite()
        {
            var venvDir = WriteVenvCfg(Path.Combine(_rootDir, "myvenv"));

            var venv = VenvDetection.GetVenvInfo(venvDir);

            Assert.NotNull(venv);
            Assert.False(venv.IncludeSystemSitePackages);
            Assert.True(venv.ShouldDisableUserSite);
        }
    }
}
