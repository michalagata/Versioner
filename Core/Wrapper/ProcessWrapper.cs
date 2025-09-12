using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Helper;
using Serilog;

namespace AnubisWorks.Tools.Versioner
{
    public static class ProcessWrapper
    {
        public static (string soutput, string err) RunProccess(string filePath, string args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            ILogger log = Log.Logger.ForContext("Context", nameof(ProcessWrapper), true);
            string s1 = null, serr = null;
            log.Verbose("Preparing process: {process_file_name} {process_args}", filePath, args);

            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(filePath, args)
                {
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                }
            };

            //if (PlatformDetector.GetOperatingSystem() == OSPlatform.Linux || PlatformDetector.GetOperatingSystem() == OSPlatform.OSX)
            //{
            //    if(!string.IsNullOrEmpty(workingDirectory)) p.StartInfo.WorkingDirectory = workingDirectory.ToLinuxPath();
            //}

            try
            {
                bool started = p.Start();
                if (started)
            {
                var waitForExitTask = Task.Factory.StartNew(() => { p.WaitForExit(); });

                var readOutputTask = Task.Factory.StartNew(() =>
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    s1 = outp;
                });

                var readErrortTask = Task.Factory.StartNew(() =>
                {
                    string err = p.StandardError.ReadToEnd();
                    if (err?.Length > 0)
                    {
                        serr = err;
                    }
                });

                Task.WaitAll(readOutputTask, readErrortTask, waitForExitTask);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Failed to start process: {process_file_name} {process_args}", filePath, args);
                serr = ex.Message;
            }
            finally
            {
                sw.Stop();
                log.Verbose("Process executed in {proces_elapsed_time} ms", sw.ElapsedMilliseconds);
            }
            return (s1, serr);
        }
    }
}