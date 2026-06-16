using System;
using System.Activities;
using System.IO;
using Shouldly;
using UiPath.Cryptography.Activities;
using UiPath.Cryptography.Enums;
using Xunit;

namespace UiPath.Cryptography.Activities.Tests
{
    /// <summary>
    /// Branches of <see cref="PgpClearSignFile"/> not covered by the round-trip tests
    /// in <see cref="PgpStandaloneTests"/>: the class rename, async cancellation,
    /// and the IResource property surface.
    /// </summary>
    public class PgpClearSignFileBranchTests : PgpTestBase
    {
        // The class is exposed under the new PascalCase name. If a future tooling pass
        // accidentally reintroduced the old camelCase name (or removed it without an alias),
        // this test would break loudly.
        [Fact]
        public void PgpClearSignFile_TypeName_IsPascalCase()
        {
            Type t = typeof(PgpClearSignFile);
            t.Name.ShouldBe("PgpClearSignFile");
            t.FullName.ShouldBe("UiPath.Cryptography.Activities.PgpClearSignFile");

            // No stale camelCase type lingering in the assembly.
            Type stale = t.Assembly.GetType("UiPath.Cryptography.Activities.PgpClearsignFile", throwOnError: false);
            stale.ShouldBeNull("the old PgpClearsignFile (camelCase) name should not exist after the rename");
        }

        // The activity is async — moved to AsyncTaskCodeActivity on this branch. Pin the base type
        // so a regression that silently reverts to CodeActivity (and loses the async path) fails here.
        [Fact]
        public void PgpClearSignFile_DerivesFromAsyncTaskCodeActivity()
        {
            Type t = typeof(PgpClearSignFile);
            Type expectedBase = typeof(UiPath.Shared.Activities.AsyncTaskCodeActivity);
            expectedBase.IsAssignableFrom(t).ShouldBeTrue($"expected {t} to derive from {expectedBase}");
        }

        // The activity exposes both string-path and IResource properties — pin both so a
        // future rename of one accidentally removes the other.
        [Fact]
        public void PgpClearSignFile_PropertySurface_HasPairedInputs()
        {
            Type t = typeof(PgpClearSignFile);
            t.GetProperty(nameof(PgpClearSignFile.InputFilePath)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.InputFile)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.PrivateKeyFilePath)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.PrivateKeyFile)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.Passphrase)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.PassphraseSecureString)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.OutputFilePath)).ShouldNotBeNull();
            t.GetProperty(nameof(PgpClearSignFile.ClearSignedFile)).ShouldNotBeNull();
        }

        // Default ContinueOnError (false) — missing input file must surface as a runtime exception
        // (mirror of PgpClearSignFile_ContinueOnError_SwallowsResolveFailure with the opposite flag).
        [Fact]
        public void PgpClearSignFile_DefaultContinueOnError_MissingInput_Throws()
        {
            string missingInput = Path.Combine(Path.GetTempPath(), $"missing_throw_{Guid.NewGuid():N}.txt");
            string outputPath = Path.Combine(Path.GetTempPath(), $"missing_throw_out_{Guid.NewGuid():N}.asc");

            var activity = new PgpClearSignFile
            {
                InputFilePath = new InArgument<string>(missingInput),
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                OutputFilePath = new InArgument<string>(outputPath),
                Overwrite = true,
            };

            try
            {
                Should.Throw<Exception>(() => WorkflowInvoker.Invoke(activity));
                File.Exists(outputPath).ShouldBeFalse("no output should have been written when input is missing");
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }

        // ContinueOnError swallows exceptions during ExecuteAsync — non-existent input file
        // exercises the catch block in the async method.
        [Fact]
        public void PgpClearSignFile_ContinueOnError_SwallowsResolveFailure()
        {
            string missingInput = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.txt");
            string outputPath = Path.Combine(Path.GetTempPath(), $"out_{Guid.NewGuid():N}.asc");

            var activity = new PgpClearSignFile
            {
                InputFilePath = new InArgument<string>(missingInput),
                PrivateKeyFilePath = new InArgument<string>(_privateKeyPath),
                Passphrase = new InArgument<string>(Passphrase),
                OutputFilePath = new InArgument<string>(outputPath),
                Overwrite = true,
                ContinueOnError = new InArgument<bool>(true),
            };

            try
            {
                Should.NotThrow(() => WorkflowInvoker.Invoke(activity));
                File.Exists(outputPath).ShouldBeFalse("no output should have been written when input is missing");
            }
            finally
            {
                if (File.Exists(outputPath)) File.Delete(outputPath);
            }
        }
    }
}
