using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace UiPath.Shared.Service.Client
{
    internal class HostWrapper : IDisposable
    {
        private const int MaxBufferedLines = 512;
        private const long LogFileSizeCapBytes = 50 * 1024 * 1024; // 50 MB
        private const int MaxLogFileCount = 128;

        private static readonly object _logFileCleanupLock = new object();

        private bool _disposed;

        private readonly object _disposeLock = new object();

        private readonly object _outputLock = new object();
        private readonly object _errorLock = new object();
        private readonly object _logFileLock = new object();

        private readonly Queue<string> _stdoutBuffer = new Queue<string>(MaxBufferedLines + 1);
        private readonly Queue<string> _stderrBuffer = new Queue<string>(MaxBufferedLines + 1);

        private int _logTrace = 0;
        private int _initialized = 0;

        private StreamWriter _logFile;
        private bool _logFileOpenAttempted;

        /// <summary>
        /// When true, stdout/stderr lines are written to a per-host diagnostic log file in the
        /// local UiPath logs folder. Intended for diagnosis only — does NOT flow to Orchestrator.
        /// </summary>
        internal bool LogTrace
        {
            get => Interlocked.CompareExchange(ref _logTrace, 0, 0) == 1;
            set => Interlocked.Exchange(ref _logTrace, value ? 1 : 0);
        }

        /// <summary>
        /// Set to true by Controller once the service is ready.
        /// </summary>
        internal bool Initialized
        {
            get => Interlocked.CompareExchange(ref _initialized, 0, 0) == 1;
            set => Interlocked.Exchange(ref _initialized, value ? 1 : 0);
        }

        internal Process Proc { get; set; }

        internal NamedPipeClientStream Pipe { get; set; }

        internal void AppendStdout(string line)
        {
            lock (_outputLock)
            {
                _stdoutBuffer.Enqueue(line);
                if (_stdoutBuffer.Count > MaxBufferedLines)
                    _stdoutBuffer.Dequeue();
            }
            TryAppendToLogFile("stdout", line);
        }

        internal void AppendStderr(string line)
        {
            lock (_errorLock)
            {
                _stderrBuffer.Enqueue(line);
                if (_stderrBuffer.Count > MaxBufferedLines)
                    _stderrBuffer.Dequeue();
            }
            TryAppendToLogFile("stderr", line);
        }

        public void Dispose()
        {
            lock (_disposeLock)
            {
                if (_disposed)
                    return;
                _disposed = true;
            }

            try
            {
                Pipe?.Dispose();
                if (Proc != null)
                {
                    if (!HostProcessHasExited())
                    {
                        Proc.Kill(entireProcessTree: true);
                        if (!Proc.WaitForExit(5000))
                            Trace.TraceWarning($"Host process {Proc.Id} did not exit within 5 seconds after Kill.");
                    }
                    Proc.Dispose();
                }
            }
            catch
            {
                //ignore exceptions on dispose, we don't care if the process is already killed or if the pipe is already closed
            }
            Pipe = null;
            Proc = null;

            lock (_outputLock)
                _stdoutBuffer.Clear();
            lock (_errorLock)
                _stderrBuffer.Clear();

            lock (_logFileLock)
            {
                try { _logFile?.Dispose(); }
                catch { /* best-effort */ }
                _logFile = null;
            }
        }

        internal bool HostProcessHasExited()
        {
            try
            {
                return Proc?.HasExited ?? true;
            }
            catch
            {
                //For wathever reason if HasExited throws, we assume that the process has not exited
                //Error will be thrown when timeout expires
                return false;
            }
        }

        internal bool GetHostProcessId(out int processId)
        {
            processId = 0;
            try
            {
                processId = Proc.Id;
                return processId > 0;
            }
            catch
            {
                //If the id is not set, it will throw
                return false;
            }
        }

        internal void ThrowIfProcessHasExited()
        {
            if (Proc != null && HostProcessHasExited())
            {
                // Drain any remaining lines still buffered in the async readers
                Proc.WaitForExit();

                string output;
                string error;

                lock (_outputLock)
                    output = string.Join(Environment.NewLine, _stdoutBuffer);
                lock (_errorLock)
                    error = string.Join(Environment.NewLine, _stderrBuffer);

                throw new InvalidOperationException($"Host process has exited!\n latest output: {output} \n latest error: {error} \n");
            }
        }

        // Opens the diagnostic log file on first call when LogTrace is enabled and the host
        // PID is known. Subsequent calls reuse the same writer. On any IO failure the writer
        // is disposed and file logging is permanently disabled for this host's lifetime —
        // we don't want a logging issue to take the scope down.
        private void TryAppendToLogFile(string stream, string line)
        {
            if (!LogTrace) return;

            lock (_logFileLock)
            {
                if (_logFile == null)
                {
                    if (_logFileOpenAttempted) return;
                    _logFileOpenAttempted = true;
                    _logFile = OpenDiagnosticLogFile();
                    if (_logFile == null) return;
                }

                try
                {
                    if (_logFile.BaseStream.Position >= LogFileSizeCapBytes)
                    {
                        Trace.TraceInformation("Python host diagnostic log reached size cap; disabling further writes.");
                        try { _logFile.Dispose(); } catch { /* best-effort */ }
                        _logFile = null;
                        return;
                    }

                    _logFile.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{stream}] {line}");
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Disabling Python host diagnostic log after write failure: {ex.Message}");
                    try { _logFile.Dispose(); } catch { /* best-effort */ }
                    _logFile = null;
                }
            }
        }

        private StreamWriter OpenDiagnosticLogFile()
        {
            try
            {
                var pid = Proc?.Id ?? 0;
                if (pid == 0)
                {
                    // host PID not assigned yet — try again on the next line
                    _logFileOpenAttempted = false;
                    return null;
                }

                var dir = ResolveUiPathLogsFolder();
                Directory.CreateDirectory(dir);
                EnforceLogFileRetention(dir);

                var path = Path.Combine(dir, $"python-host-{DateTime.Now:yyyy-MM-ddTHHmmss}-{pid}.log");
                return new StreamWriter(path, append: false) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Trace.TraceWarning($"Could not open Python host diagnostic log: {ex.Message}");
                return null;
            }
        }

        // Deletes the oldest python-host-*.log files beyond MaxLogFileCount.
        // Uses a static lock so concurrent sessions don't race destructively — worst case
        // two sessions each open one new file while the other is sweeping, which is benign.
        private static void EnforceLogFileRetention(string dir)
        {
            lock (_logFileCleanupLock)
            {
                try
                {
                    var files = new DirectoryInfo(dir)
                        .GetFiles("python-host-*.log");

                    // Timestamp prefix makes lexicographic name sort equivalent to age sort.
                    Array.Sort(files, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

                    for (int i = 0; i < files.Length - MaxLogFileCount; i++)
                    {
                        try { files[i].Delete(); }
                        catch (Exception ex)
                        {
                            Trace.TraceWarning($"Could not delete old Python host log '{files[i].Name}': {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning($"Could not enforce Python host log retention: {ex.Message}");
                }
            }
        }

        // Test/integration hook: when set, this folder is used as the base.
        // this in initialization and reset to null on teardown. The "python" subfolder is
        // still appended on top.
        internal static string LogsFolderOverride { get; set; }

        // Resolves the folder where Python host diagnostic logs are written: the standard
        // Studio/Robot log folder + "python" subfolder. Base folder is LogsFolderOverride
        // when set, otherwise Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)\UiPath\Logs.
        private static string ResolveUiPathLogsFolder()
        {
            var baseFolder = LogsFolderOverride
                          ?? Path.Combine(
                                 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                 "UiPath", "Logs");
            return Path.Combine(baseFolder, "python");
        }
    }
}
