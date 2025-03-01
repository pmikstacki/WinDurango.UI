using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WinDurango.UI.Utils
{
    public static class FSHelper
    {
        public static long GetDirectorySize(this DirectoryInfo dir)
        {
            long size = 0;

            foreach (FileInfo file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                size += file.Length;
            }

            return size;
        }

        public static string GetSizeString(this long size)
        {
            string[] types = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            int i = 0;
            double dSize = size;

            while (dSize >= 1024 && i < types.Length - 1)
            {
                dSize /= 1024;
                i++;
            }

            return $"{dSize:0.00} {types[i]}";
        }

        public static void OpenFolder(string folder)
        {
            Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
        }

        public static string FindFileOnPath(string file)
        {
            return Environment
                .GetEnvironmentVariable("PATH")
                .Split(Path.PathSeparator)
                .FirstOrDefault(p => File.Exists(Path.Combine(p, file)));
        }
    }
}
