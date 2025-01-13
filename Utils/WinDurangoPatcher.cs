using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Packaging;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinDurango.UI.Settings;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Package = Windows.ApplicationModel.Package;

namespace WinDurango.UI.Utils
{
    public class GitHubRelease
    {
        public string Name { get; set; }
        public string DownloadLink { get; set; }
    }

    public static class WinDurangoPatcher
    {
        private static readonly HttpClient httpClient = new(new HttpClientHandler { AllowAutoRedirect = true });
        private static GitHubRelease wdRelease;
        private static string patchesPath = Path.Combine(App.DataDir, "WinDurangoCore");

        static WinDurangoPatcher()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubAPI/1.0)");
        }

        public static async Task<bool> UnpatchPackage(Package package, Func<string, int, Task> statusCallback)
        {
            installedPackage pkg = App.InstalledPackages.GetPackage(package);
            if (pkg == null)
                return false;
            
            return await UnpatchPackage(pkg, statusCallback);
        }
        
        public static async Task<bool> PatchPackage(Package package, bool forceRedownload,
            Func<string, int, Task> statusCallback)
        {
            installedPackage pkg = App.InstalledPackages.GetPackage(package);
            if (pkg == null)
                return false;
            
            return await PatchPackage(pkg, forceRedownload, statusCallback);
        }
        
        public static async Task<bool> PatchPackage(installedPackage package, bool forceRedownload,
            Func<string, int, Task> statusCallback)
        {
            statusCallback($"Patching {package.FamilyName}", 0);
            string curDate = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            string installPath = package.InstallPath;

            statusCallback("Getting latest release", 10);
            await GetOrReuseRelease(); // don't use the return value since wdRelease is set regardless

            if (!Path.Exists(patchesPath) || forceRedownload)
            {
                if (Path.Exists(patchesPath))
                    Directory.Delete(patchesPath, true);

                string archivePath = Path.Combine(App.DataDir, "WinDurangoCore.zip");

                try
                {
                    statusCallback($"Downloading release \"{wdRelease.Name}\"", 20);

                    // using HttpResponseMessage res = await httpClient.GetAsync(wdRelease.DownloadLink,
                    //     HttpCompletionOption.ResponseHeadersRead);
                    // res.EnsureSuccessStatusCode();
                    await using Stream httpStream = await httpClient.GetStreamAsync(wdRelease.DownloadLink);
                    await using FileStream stream = new(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    statusCallback("Writing release zip", 30);
                    await httpStream.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    return false;
                }

                try
                {
                    Directory.CreateDirectory(patchesPath);
                    statusCallback($"Extracting", 40);
                    ZipFile.ExtractToDirectory(archivePath, patchesPath);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    return false;
                }
            }

            statusCallback("Patching", 50);
            if (Path.Exists(patchesPath))
            {
                DirectoryInfo patchesDir = new(patchesPath);

                FileInfo[] patchFiles = patchesDir.GetFiles("*.dll");
                float progressPerFile = (90 - 50) / (float)patchFiles.Length;
                DirectoryInfo packageDir = new(installPath);

                foreach (FileInfo file in patchFiles)
                {
                    int progress = (int)Math.Round(50 + progressPerFile * Array.IndexOf(patchFiles, file));

                    statusCallback($"Copying {file.Name}", progress);
                    FileInfo oldFile = packageDir.GetFiles("*.dll").FirstOrDefault(f => f.Name == file.Name);
                    if (oldFile != null)
                    {
                        Logger.WriteInformation($"Backing up old {oldFile.Name}");
                        string DllBackup = Path.Combine(installPath, "WDDllBackup", curDate);
                        if (!Path.Exists(DllBackup))
                            Directory.CreateDirectory(DllBackup);

                        File.Move(oldFile.FullName, Path.Combine(DllBackup, oldFile.Name));
                        if (!package.OriginalDlls.Contains(Path.Combine(DllBackup, oldFile.Name)))
                            package.OriginalDlls.Add(Path.Combine(DllBackup, oldFile.Name));
                    }

                    string patchPath = Path.Combine(installPath, file.Name);
                    Logger.WriteInformation($"Added {file.Name}");
                    File.Copy(file.FullName, Path.Combine(installPath, file.Name));
                    
                    if (!package.PatchedDlls.Contains(patchPath))
                        package.PatchedDlls.Add(patchPath);
                }
            }
            else
            {
                Logger.WriteError("How did this happen???? patchDir should exist.");
                return false;
            }

            statusCallback($"Writing patch txt", 93);
            StringBuilder builder = new();
            builder.AppendLine($"# This file was patched by WinDurango.UI with WinDurango release \"{wdRelease.Name}\".");
            builder.AppendLine("# If you want to unpatch manually, delete this file and edit %appdata%\\WinDurango\\UI\\InstalledPackages.json and set IsPatched to false.");
            builder.AppendLine("# Format is ReleaseName;VerPacked");
            builder.AppendLine($"{wdRelease.Name.Replace(";","-")};{App.VerPacked}");
            await File.WriteAllTextAsync(Path.Combine(installPath, "installed.txt"), builder.ToString());
            
            statusCallback($"Updating package list", 95);
            package.IsPatched = true;
            App.InstalledPackages.UpdatePackage(package);
            statusCallback($"Done!", 100);
            return true;
        }

        public static async Task<bool> UnpatchPackage(installedPackage package,
            Func<string, int, Task> statusCallback)
        {
            statusCallback($"Unpatching {package.FamilyName}", 0);

            string[] dlls = package.PatchedDlls.ToArray();
            string[] originalDlls = package.OriginalDlls.ToArray();

            string installPath = package.InstallPath;

            float progressPerRemove = (0 - 50) / (float)package.PatchedDlls.Count;
            foreach (string dll in dlls)
            {
                int progress = (int)Math.Round(50 + progressPerRemove * Array.IndexOf(package.PatchedDlls.ToArray(), dll));
                statusCallback($"Removing {dll}", progress);
                try
                {
                    File.Delete(dll);
                }
                catch
                {
                    Logger.WriteError($"Failed to delete {dll}.");
                }

                package.PatchedDlls.Remove(dll);
            };

            float progressPerRevert = (98 - 50) / (float)package.OriginalDlls.Count;
            foreach (string dll in originalDlls)
            {
                int progress = (int)Math.Round(50 + progressPerRevert * Array.IndexOf(originalDlls, dll));
                statusCallback($"Placing back original DLL \"{dll}\"", progress);
                try
                {
                    File.Copy(dll, Path.Combine(installPath, dll));
                }
                catch
                {
                    Logger.WriteError($"Failed to copy {dll}.");
                }

                package.OriginalDlls.Remove(dll);
            };

            package.IsPatched = false;

            statusCallback($"Removing patched.txt", 99);
            if (Path.Exists(Path.Combine(installPath, "installed.txt")))
                File.Delete(Path.Combine(installPath, "installed.txt"));
            
            statusCallback($"Updating package list", 99);
            App.InstalledPackages.UpdatePackage(package);
            statusCallback($"Done!", 100);
            return true;
        }

        // don't wanna use all the api shits lol
        private static async Task<GitHubRelease> GetOrReuseRelease()
        {
            if (wdRelease is null)
                await GetLatestRelease();

            return wdRelease;
        }

        private static async Task<GitHubRelease> GetLatestRelease()
        {
            GitHubRelease release = new();

            const string url = $"https://api.github.com/repos/WinDurango/WinDurango/releases";

            HttpResponseMessage response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            JsonDocument document = JsonDocument.Parse(json);
            JsonElement.ArrayEnumerator releases = document.RootElement.EnumerateArray();

            if (!releases.MoveNext())
                throw new Exception("Couldn't find any releases?????");

            JsonElement newestRelease = releases.Current;

            string name = newestRelease.GetProperty("name").GetString();

            release.Name = name;

            JsonElement.ArrayEnumerator assets = newestRelease.GetProperty("assets").EnumerateArray();

            if (!assets.MoveNext())
                throw new Exception("Couldn't find any assets?????");

            string download = assets.Current.GetProperty("browser_download_url").GetString();

            release.DownloadLink = download;

            wdRelease = release;
            return release;
        }
    }
}