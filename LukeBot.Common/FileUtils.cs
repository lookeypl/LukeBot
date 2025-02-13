using System.IO;

namespace LukeBot.Common
{
    public class FileUtils
    {
        // We set CWD to be at exe directory
        // That way the project will work the same way after publish
        public static void SetUnifiedCWD()
        {
            string exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            Directory.SetCurrentDirectory(exeDir);
        }

        public static bool Exists(string path)
        {
            return Directory.Exists(path) || File.Exists(path);
        }
    }
}
