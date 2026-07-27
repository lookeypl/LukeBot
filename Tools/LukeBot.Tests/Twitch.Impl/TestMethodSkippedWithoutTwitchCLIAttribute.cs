using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace LukeBot.Tests.Twitch.Impl
{
    /**
     * Additional attribute for EventSub tests. Will attempt to find twitch.exe in PATH and, if
     * located, will attempt to call `twitch help` to make sure this is the tool we need.
     *
     * Failure to locate Twitch CLI in PATH (or a different unrelated binary) will cause the test
     * method to be skipped.
     */
    public class TestMethodSkippedWithoutTwitchCLIAttribute: IgnorableTestMethodAtribute
    {
        private static string mTwitchCLIFullPath = null;

        public static string GetCLIPath()
        {
            return mTwitchCLIFullPath;
        }

        private bool IsBinaryActuallyTwitchCLI(string path)
        {
            ProcessStartInfo startInfo = new();
            startInfo.FileName = path;
            startInfo.Arguments = "help";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.CreateNoWindow = true;

            Process twitchCLI = new Process();
            twitchCLI.StartInfo = startInfo;

            try
            {
                twitchCLI.Start();
                string firstLine = twitchCLI.StandardOutput.ReadLine();
                if (!firstLine.Contains("A simple CLI tool for the New Twitch API"))
                    return false;
                else
                    return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override bool ShouldIgnore(ITestMethod testMethod)
        {
            if (mTwitchCLIFullPath == null)
            {
                // test if twitch binary exists in PATH
                string str = Environment.GetEnvironmentVariable("PATH");
                if (str == null)
                    return true;

                string twitchBinaryName = "twitch";
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    twitchBinaryName += ".exe";

                string[] paths = str.Split(System.IO.Path.PathSeparator);
                foreach (string path in paths)
                {
                    string fullPath = System.IO.Path.Combine(path, twitchBinaryName);
                    if (File.Exists(fullPath) && IsBinaryActuallyTwitchCLI(fullPath))
                    {
                        mTwitchCLIFullPath = fullPath;
                        return false;
                    }
                }

                // not found, fill in empty string
                mTwitchCLIFullPath = "";
                return true;
            }

            if (mTwitchCLIFullPath.Length > 0)
                return false;
            else
                return true;
        }
    }
}
