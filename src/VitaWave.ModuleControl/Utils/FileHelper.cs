using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuleControl.Utils
{
    public static class FileHelper
    {
        //yes this is AI code. it just moves up directories until it finds the first .cfg file
        public static string[] FindAndReadConfigFile()
        {
            string currentDirectory = Directory.GetCurrentDirectory();

            while (true)
            {
                // Check if current directory has .cfg files
                string[] configFiles = Directory.GetFiles(currentDirectory, "*.cfg");
                if (configFiles.Length > 0)
                {
                    // Found a .cfg file, read all lines and return them
                    return File.ReadAllLines(configFiles[0]);
                }

                // Check if directory has .sln or .exe files
                if (Directory.GetFiles(currentDirectory, "*.sln").Length > 0 ||
                    Directory.GetFiles(currentDirectory, "*.exe").Length > 0)
                {
                    // Found .sln or .exe file, stop searching
                    return Array.Empty<string>();
                }

                // Get parent directory
                DirectoryInfo parent = Directory.GetParent(currentDirectory)!;
                if (parent == null)
                {
                    // Reached root directory, no .cfg files found
                    return Array.Empty<string>();
                }

                // Move up to parent directory
                currentDirectory = parent.FullName;
            }
        }

    }
}
