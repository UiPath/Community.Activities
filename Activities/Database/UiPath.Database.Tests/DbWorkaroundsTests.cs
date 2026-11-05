using System;
using System.IO;
using Shouldly;
using Xunit;

namespace UiPath.Database.Tests
{
    public class DbWorkaroundsTests : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "dbworkarounds-" + Guid.NewGuid().ToString("N"));

        public DbWorkaroundsTests() => Directory.CreateDirectory(_root);

        public void Dispose()
        {
            try { Directory.Delete(_root, true); } catch (IOException) { }
        }

        private string AddPackage(string root, string package, string version)
        {
            var path = Path.Combine(root, package, version);
            Directory.CreateDirectory(path);
            return path;
        }

        [Fact]
        public void FindPackageVersionPath_VulnerableAndPatchedSqliteInstalled_ChoosesPatched()
        {
            var oldRoot = Path.Combine(_root, "old");
            var newRoot = Path.Combine(_root, "new");
            AddPackage(oldRoot, DbWorkarounds.Sqlite.Package, "2.1.6");
            var patched = AddPackage(newRoot, DbWorkarounds.Sqlite.Package, "2.1.14");

            var result = DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { oldRoot, newRoot });

            result.ShouldBe(patched);
        }

        [Fact]
        public void FindPackageVersionPath_OnlyVulnerableSqliteInstalled_Throws()
        {
            var oldRoot = Path.Combine(_root, "old");
            AddPackage(oldRoot, DbWorkarounds.Sqlite.Package, "2.1.6");

            Should.Throw<DirectoryNotFoundException>(() =>
                DbWorkarounds.FindPackageVersionPath(DbWorkarounds.Sqlite.Package, DbWorkarounds.Sqlite.MinVersion, new[] { oldRoot }));
        }

        [Fact]
        public void SqliteMinVersion_IsNotVulnerable()
        {
            Version.Parse(DbWorkarounds.Sqlite.MinVersion).ShouldBeGreaterThanOrEqualTo(new Version(2, 1, 12));
        }
    }
}
