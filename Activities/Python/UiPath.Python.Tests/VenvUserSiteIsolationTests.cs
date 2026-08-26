using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UiPath.TestUtils;
using Xunit;
using Assert = Xunit.Assert;

namespace UiPath.Python.Tests
{
    // Regression test for STUD-81085: pointing Python Scope's Path at a venv must stop the
    // interpreter's PEP-370 user-site directory (%APPDATA%\Roaming\Python\PythonXY\site-packages)
    // from being processed at all. pywin32 registers its native DLL search directory
    // (os.add_dll_directory) from a .pth file in whichever site directory it's installed into;
    // if the user-site copy still gets processed alongside the venv's own copy, a differently
    // built pywin32 there collides with the venv's copy and surfaces as
    // "DLL load failed while importing win32api: The specified procedure could not be found."
    //
    // This test doesn't need real pywin32 binaries: the bug is that the user-site .pth runs at
    // all when a venv is configured, so a .pth marker is a faithful, hermetic reproduction of the
    // exact mechanism pywin32 relies on. Runs out-of-process since that's the real production
    // default path (PythonScope.Isolated defaults to true).
    public class VenvUserSiteIsolationTests : IDisposable
    {
        private const string Category = "Python";

        private static readonly string EmbeddedRuntimePath = EmbeddedPythonRuntimeBootstrap.EnsureRuntimePath();
        private static readonly string EmbeddedLibraryPath = EmbeddedPythonRuntimeBootstrap.GetPythonLibraryPath(EmbeddedRuntimePath);

        private readonly string _rootDir;
        private readonly string _venvDir;
        private readonly string _userBaseDir;
        private readonly string _markerFile;
        private readonly string _previousUserBase;
        private readonly string _previousNoUserSite;

        public VenvUserSiteIsolationTests()
        {
            _rootDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "venv-usersite-tests", Guid.NewGuid().ToString("N"))).FullName;
            _venvDir = Path.Combine(_rootDir, ".venv");
            _userBaseDir = Path.Combine(_rootDir, "userbase");
            _markerFile = Path.Combine(_rootDir, "marker.txt");

            _previousUserBase = Environment.GetEnvironmentVariable("PYTHONUSERBASE");
            _previousNoUserSite = Environment.GetEnvironmentVariable("PYTHONNOUSERSITE");

            // Start every test from a known-clean baseline regardless of whatever the host
            // machine's own ambient environment happens to have for these two — otherwise a
            // machine/CI agent with e.g. PYTHONNOUSERSITE already set would make tests that expect
            // user-site processing fail for reasons unrelated to the behavior under test. The one
            // test that specifically wants an ambient value sets it itself, after this. Dispose
            // restores whatever was actually ambient when this instance was constructed.
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", null);
            Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", null);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _previousUserBase);
            Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", _previousNoUserSite);
            try { Directory.Delete(_rootDir, true); } catch { /* best effort cleanup */ }
            GC.SuppressFinalize(this);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_Does_Not_Process_UserSite_PthFiles()
        {
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            var venvSitePackages = Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages")).FullName;
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {EmbeddedRuntimePath}{Environment.NewLine}");
            WriteMarkerPth(venvSitePackages, "venv");

            var userSitePackages = Directory.CreateDirectory(Path.Combine(_userBaseDir, EmbeddedPythonRuntimeBootstrap.UserSiteVersionFolder, "site-packages")).FullName;
            WriteMarkerPth(userSitePackages, "usersite");
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _userBaseDir);

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile)
                ? File.ReadAllLines(_markerFile)
                : Array.Empty<string>();

            // The venv's own .pth must still run — this isn't about disabling site processing,
            // only about excluding the unrelated per-user directory.
            Assert.Contains("venv", markerLines);
            Assert.DoesNotContain("usersite", markerLines);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_With_SystemSitePackages_Still_Processes_UserSite_PthFiles()
        {
            // Mirrors CPython's own site.py venv() function: it only forces
            // ENABLE_USER_SITE = False when the venv was created *without*
            // --system-site-packages. A real, natively-activated --system-site-packages venv
            // leaves user-site enabled (the normal, non-venv computation applies instead) — so
            // this fix must not suppress it there either, or it would diverge from native parity
            // for a case that already accepts the same DLL-collision exposure as any other
            // non-venv interpreter.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            var venvSitePackages = Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages")).FullName;
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}include-system-site-packages = true{Environment.NewLine}");
            WriteMarkerPth(venvSitePackages, "venv");

            var userSitePackages = Directory.CreateDirectory(Path.Combine(_userBaseDir, EmbeddedPythonRuntimeBootstrap.UserSiteVersionFolder, "site-packages")).FullName;
            WriteMarkerPth(userSitePackages, "usersite");
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _userBaseDir);

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile)
                ? File.ReadAllLines(_markerFile)
                : Array.Empty<string>();

            Assert.Contains("venv", markerLines);
            Assert.Contains("usersite", markerLines);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_PythonHome_Resolves_To_Declared_Base_Install()
        {
            // Regression test for a separate bug found while exercising this fix: pointing
            // PythonHome directly at the venv root (rather than the base install its own
            // pyvenv.cfg declares) makes native init fail outright once a real installer-based
            // Python is used, since a venv has no standard library of its own. The embeddable test
            // runtime's own ._pth-based bootstrap resolves its stdlib independently of PythonHome,
            // so it can't catch that failure directly — but PythonHome still governs what the
            // interpreter reports as sys.base_prefix regardless, which is what this asserts.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {EmbeddedRuntimePath}{Environment.NewLine}");

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            string basePrefix;
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);

                var script = await engine.LoadScript(
                    "import sys\ndef check():\n    return sys.base_prefix\n",
                    CancellationToken.None);
                var result = await engine.InvokeMethod(script, "check", null, CancellationToken.None);
                basePrefix = (string)engine.Convert(result, typeof(string));
            }
            finally
            {
                await engine.Release();
            }

            Assert.Equal(
                Path.GetFullPath(EmbeddedRuntimePath).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(basePrefix).TrimEnd(Path.DirectorySeparatorChar),
                ignoreCase: true);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_ExitAndHelp_Builtins_Still_Resolve()
        {
            // Regression test: SetNoSiteFlag skips site.main() entirely for a default venv, which
            // does more than path setup — it also calls setquit()/setcopyright()/sethelper(),
            // installing the exit/quit/help/copyright/credits/license builtins. Without restoring
            // them, a script calling exit() (a common, if discouraged, RPA pattern) would die with
            // a NameError that gives no hint it's related to venv handling — it works today, works
            // natively in a venv, and works in every non-venv scope.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {EmbeddedRuntimePath}{Environment.NewLine}");

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            string result;
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);

                var script = await engine.LoadScript(
                    "import builtins\ndef check():\n    return f\"{hasattr(builtins, 'exit')},{hasattr(builtins, 'help')}\"\n",
                    CancellationToken.None);
                var invoked = await engine.InvokeMethod(script, "check", null, CancellationToken.None);
                result = (string)engine.Convert(invoked, typeof(string));
            }
            finally
            {
                await engine.Release();
            }

            Assert.Equal("True,True", result);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_Does_Not_Leak_BaseInstall_SitePackages()
        {
            // Regression test for a second, distinct leak path found by inspecting a real venv's
            // actual sys.path: CPython's site.main() unconditionally adds the base install's own
            // site-packages (and the bare base install prefix itself) unless something narrows
            // PREFIXES first — which never happened for the embedded interpreter, since site.py's
            // own venv() detection can't trigger for it. PYTHONNOUSERSITE never covered this; only
            // SetNoSiteFlag (skipping site.main() entirely for this case) does.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {EmbeddedRuntimePath}{Environment.NewLine}");

            var sysPath = await GetSysPath();
            var normalizedBase = Path.GetFullPath(EmbeddedRuntimePath).TrimEnd(Path.DirectorySeparatorChar);

            Assert.DoesNotContain(sysPath, p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar)
                .Equals(normalizedBase, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_With_SystemSitePackages_Still_Includes_BaseInstall_SitePackages()
        {
            // The --system-site-packages case must keep behaving exactly as it did before this
            // change: SetNoSiteFlag is only set for the default (ShouldDisableUserSite) case, so
            // site.main() still runs normally here and still adds the base install unconditionally
            // — which happens to already be correct for this specific flag.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}include-system-site-packages = true{Environment.NewLine}");

            var sysPath = await GetSysPath();
            var normalizedBase = Path.GetFullPath(EmbeddedRuntimePath).TrimEnd(Path.DirectorySeparatorChar);

            Assert.Contains(sysPath, p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar)
                .Equals(normalizedBase, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_SiteCustomize_Still_Runs()
        {
            // SetNoSiteFlag skips site.main() entirely for the default venv case, which would also
            // silently skip sitecustomize.py auto-import (some environments rely on it for
            // corporate setup) unless something restores it — Engine.PostInitializationVenvSetup
            // explicitly calls site.execsitecustomize() for exactly this reason.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            var venvSitePackages = Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages")).FullName;
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {EmbeddedRuntimePath}{Environment.NewLine}");

            var markerPath = _markerFile.Replace("\\", "/");
            File.WriteAllText(Path.Combine(venvSitePackages, "sitecustomize.py"),
                $"import codecs{Environment.NewLine}codecs.open('{markerPath}', 'a', encoding='utf-8').write('sitecustomize\\n'){Environment.NewLine}");

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile) ? File.ReadAllLines(_markerFile) : Array.Empty<string>();
            Assert.Contains("sitecustomize", markerLines);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_SiteCustomize_Still_Runs_From_BaseInstall_When_Venv_Has_None()
        {
            // Regression test: for a default venv (SetNoSiteFlag set) with no sitecustomize.py of
            // its own, the base install's Lib\sitecustomize.py (e.g. corporate proxy/logging setup)
            // must still be picked up — the stdlib paths remain on sys.path under Py_NoSiteFlag, and
            // this must be an unconditional call for the default-venv case, unlike the
            // --system-site-packages case where it's gated on the venv having its own copy.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            var fakeBase = Directory.CreateDirectory(Path.Combine(_rootDir, "fakebase")).FullName;
            Directory.CreateDirectory(Path.Combine(fakeBase, "Lib", "encodings"));
            var markerPath = _markerFile.Replace("\\", "/");
            File.WriteAllText(Path.Combine(fakeBase, "Lib", "sitecustomize.py"),
                $"import codecs{Environment.NewLine}codecs.open('{markerPath}', 'a', encoding='utf-8').write('base-sitecustomize\\n'){Environment.NewLine}");

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"), $"home = {fakeBase}{Environment.NewLine}");

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile) ? File.ReadAllLines(_markerFile) : Array.Empty<string>();
            Assert.Contains("base-sitecustomize", markerLines);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_With_SystemSitePackages_OwnSiteCustomize_Still_Runs()
        {
            // Regression test: for a --system-site-packages venv, SetNoSiteFlag isn't set, so
            // site.main() already ran during PythonEngine.Initialize() — before this fix, that
            // meant it could already have imported and cached "sitecustomize" from the user site
            // (enabled here via PYTHONUSERBASE, same as the ambient-PYTHONNOUSERSITE test below)
            // before the venv's own site-packages, added afterwards in
            // PostInitializationVenvSetup, ever got a chance to be searched. Without popping that
            // cached module first, the venv's own sitecustomize.py would never run at all — only
            // the user site's copy would. This asserts the venv's own copy does run.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            var venvSitePackages = Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages")).FullName;
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}include-system-site-packages = true{Environment.NewLine}");

            var markerPath = _markerFile.Replace("\\", "/");
            File.WriteAllText(Path.Combine(venvSitePackages, "sitecustomize.py"),
                $"import codecs{Environment.NewLine}codecs.open('{markerPath}', 'a', encoding='utf-8').write('venv-sitecustomize\\n'){Environment.NewLine}");

            var userSitePackages = Directory.CreateDirectory(Path.Combine(_userBaseDir, EmbeddedPythonRuntimeBootstrap.UserSiteVersionFolder, "site-packages")).FullName;
            File.WriteAllText(Path.Combine(userSitePackages, "sitecustomize.py"),
                $"import codecs{Environment.NewLine}codecs.open('{markerPath}', 'a', encoding='utf-8').write('usersite-sitecustomize\\n'){Environment.NewLine}");
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _userBaseDir);

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile) ? File.ReadAllLines(_markerFile) : Array.Empty<string>();
            Assert.Contains("venv-sitecustomize", markerLines);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_With_SystemSitePackages_And_No_Own_SiteCustomize_Does_Not_Rerun_UserSite_Copy()
        {
            // Regression test for a real bug in an earlier version of this fix: unconditionally
            // popping "sitecustomize" from sys.modules and re-importing, regardless of whether the
            // venv itself has its own copy, would still find the *same* user-site copy again when
            // the venv has none of its own — re-running its side effects a second time. Gating the
            // pop+reimport on the venv's own site-packages actually containing a sitecustomize.py
            // (see PostInitializationVenvSetup) avoids this: with no venv-local copy, the module
            // site.main() already cached is left untouched, so it runs exactly once.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages"));
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}include-system-site-packages = true{Environment.NewLine}");

            var markerPath = _markerFile.Replace("\\", "/");
            var userSitePackages = Directory.CreateDirectory(Path.Combine(_userBaseDir, EmbeddedPythonRuntimeBootstrap.UserSiteVersionFolder, "site-packages")).FullName;
            File.WriteAllText(Path.Combine(userSitePackages, "sitecustomize.py"),
                $"import codecs{Environment.NewLine}codecs.open('{markerPath}', 'a', encoding='utf-8').write('usersite-sitecustomize\\n'){Environment.NewLine}");
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _userBaseDir);

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile) ? File.ReadAllLines(_markerFile) : Array.Empty<string>();
            var single = Assert.Single(markerLines);
            Assert.Equal("usersite-sitecustomize", single);
        }

        [Fact]
        [Trait(TestCategories.Category, Category)]
        public async Task Venv_With_SystemSitePackages_Honors_Ambient_PYTHONNOUSERSITE()
        {
            // A --system-site-packages venv's own flag only says "also add the base install's
            // site-packages" — it says nothing about user-site, which native CPython computes
            // independently from PYTHONNOUSERSITE regardless of --system-site-packages. So an
            // ambient PYTHONNOUSERSITE=1 (e.g. an administrator's own workaround, exactly like the
            // one mentioned in the original ticket) must still suppress user-site here, matching
            // what a normally-activated --system-site-packages venv would do. ProcessStartInfo
            // already starts as a copy of this process's own environment, so this just needs
            // nothing in our own code to actively defeat it.
            Skip.IfNot(Directory.Exists(EmbeddedRuntimePath));

            Environment.SetEnvironmentVariable("PYTHONNOUSERSITE", "1");

            var venvSitePackages = Directory.CreateDirectory(Path.Combine(_venvDir, "Lib", "site-packages")).FullName;
            File.WriteAllText(Path.Combine(_venvDir, "pyvenv.cfg"),
                $"home = {EmbeddedRuntimePath}{Environment.NewLine}include-system-site-packages = true{Environment.NewLine}");
            WriteMarkerPth(venvSitePackages, "venv");

            var userSitePackages = Directory.CreateDirectory(Path.Combine(_userBaseDir, EmbeddedPythonRuntimeBootstrap.UserSiteVersionFolder, "site-packages")).FullName;
            WriteMarkerPth(userSitePackages, "usersite");
            Environment.SetEnvironmentVariable("PYTHONUSERBASE", _userBaseDir);

            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);
            }
            finally
            {
                await engine.Release();
            }

            var markerLines = File.Exists(_markerFile) ? File.ReadAllLines(_markerFile) : Array.Empty<string>();

            Assert.Contains("venv", markerLines);
            Assert.DoesNotContain("usersite", markerLines);
        }

        private async Task<string[]> GetSysPath()
        {
            var engine = EngineProvider.Get(Version.Python_310, _venvDir, EmbeddedLibraryPath, inProcess: false);
            try
            {
                await engine.Initialize(null, CancellationToken.None, 60);

                var script = await engine.LoadScript(
                    "import sys\ndef check():\n    return list(sys.path)\n",
                    CancellationToken.None);
                var result = await engine.InvokeMethod(script, "check", null, CancellationToken.None);
                return (string[])engine.Convert(result, typeof(string[]));
            }
            finally
            {
                await engine.Release();
            }
        }

        private void WriteMarkerPth(string siteDir, string tag)
        {
            var markerPath = _markerFile.Replace("\\", "/");
            var pthLine = $"import codecs; codecs.open('{markerPath}', 'a', encoding='utf-8').write('{tag}\\n')";
            File.WriteAllText(Path.Combine(siteDir, $"zzz_{tag}_marker.pth"), pthLine + Environment.NewLine);
        }
    }
}
