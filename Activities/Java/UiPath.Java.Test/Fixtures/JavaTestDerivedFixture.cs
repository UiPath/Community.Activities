using System;
using System.IO;
using System.Threading.Tasks;

namespace UiPath.Java.Test.Fixtures
{
    public class JavaTestDerivedFixture : JavaTestBaseFixture
    {
        public JavaTestDerivedFixture()
        {
            OneTimeSetup().Wait();
        }

        private async Task OneTimeSetup()
        {
            await Invoker.LoadJar(Path.Combine(JavaFilesPath, "TestProgram.Jar"), Ct);
            await Invoker.LoadJar(Path.Combine(JavaFilesPath, "ImportCoordinate.Jar"), Ct);
        }
    }
}
