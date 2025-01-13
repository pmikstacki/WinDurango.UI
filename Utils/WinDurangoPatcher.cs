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

namespace WinDurango.UI.Utils
{
    public class GitHubRelease
    {
        public string Name { get; set; }
        public string DownloadLink { get; set; }
    }

    public class WinDurangoPatcher
    {
        private static GitHubRelease wdRelease;
        private static string patchDir = Path.Combine(App.DataDir, "WinDurangoCore");

        public static async Task<bool> PatchPackage(Windows.ApplicationModel.Package package, bool forceRedownload, Func<string, int, Task> statusCallback)
        {
            Logger.WriteInformation($"Patching package {package.Id.FamilyName}");
            statusCallback($"Patching {package.Id.FamilyName}", 0);
            string curDate = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            var instPkg = InstalledPackages.GetInstalledPackage(package.Id.FamilyName);

            string installPath = package.InstalledPath;

            statusCallback($"Getting release", 10);
            await GetOrReuseRelease(); // don't use the return value since wdRelease is set regardless
            

            if (!Path.Exists(patchDir) || forceRedownload)
            {
                if (forceRedownload && Path.Exists(patchDir))
                    Directory.Delete(patchDir, true);

                string zip = Path.Combine(App.DataDir, "WinDurangoCore.zip");

                try
                {
                    using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true }))
                    {
                        statusCallback($"Downloading WinDurango release \"{wdRelease.Name}\"", 20);
                        Logger.WriteInformation($"Downloading WinDurango release \"{wdRelease.Name}\"");
                        using (HttpResponseMessage res = await client.GetAsync(wdRelease.DownloadLink, HttpCompletionOption.ResponseHeadersRead))
                        {
                            res.EnsureSuccessStatusCode();

                            using (FileStream stream = new FileStream(zip, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                statusCallback($"Writing release zip", 30);
                                await res.Content.CopyToAsync(stream);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    return false;
                }
            

                try
                {
                    Directory.CreateDirectory(patchDir);
                    Logger.WriteInformation($"Unzipping");
                    statusCallback($"Unzipping", 40);
                    ZipFile.ExtractToDirectory(zip, patchDir);
                }
                catch (Exception ex)
                {
                    Logger.WriteException(ex);
                    return false;
                }
            }

            statusCallback($"Patching", 50);
            if (Path.Exists(patchDir))
            {
                DirectoryInfo unzipped = new DirectoryInfo(patchDir);

                var unzippedFiles = unzipped.GetFiles("*.dll");
                float prog = (90 - 50) / (float)unzippedFiles.Length;
                DirectoryInfo pkg = new DirectoryInfo(installPath);

                foreach (var file in unzippedFiles)
                {
                    int progress = (int)Math.Round(50 + prog * Array.IndexOf(unzippedFiles, file));
                    statusCallback($"Copying {file.Name}", progress);
                    var pkgFName = pkg.GetFiles("*.dll").FirstOrDefault(pkgFName => pkgFName.Name == file.Name);
                    if (pkgFName != null)
                    {
                        Logger.WriteInformation($"Backing up old {pkgFName.Name}");
                        string DllBackup = Path.Combine(installPath, "WDDllBackup", curDate);
                        if (!Path.Exists(DllBackup))
                            Directory.CreateDirectory(DllBackup);

                        File.Move(pkgFName.FullName, Path.Combine(DllBackup, pkgFName.Name));
                        if (!instPkg.Value.installedPackage.OriginalDLLs.Contains(Path.Combine(DllBackup, pkgFName.Name)))
                            instPkg.Value.installedPackage.OriginalDLLs.Add(Path.Combine(DllBackup, pkgFName.Name));
                    }

                    string symPath = Path.Combine(installPath, file.Name);
                    Logger.WriteInformation($"Added {file.Name}");
                    File.Copy(file.FullName, Path.Combine(installPath, file.Name));
                    if (!instPkg.Value.installedPackage.SymlinkedDLLs.Contains(symPath))
                        instPkg.Value.installedPackage.SymlinkedDLLs.Add(symPath);
                }
            } else
            {
                Logger.WriteError("How did this happen???? patchDir should exist.");
                return false;
            }

            statusCallback($"Updating package list", 95);
            instPkg.Value.installedPackage.IsPatched = true;
            InstalledPackages.UpdateInstalledPackage(instPkg.Value.familyName, instPkg.Value.installedPackage);
            statusCallback($"Done!", 100);
            return true;
        }

        public static async Task<bool> UnpatchPackage(Windows.ApplicationModel.Package package, Func<string, int, Task> statusCallback)
        {
            Logger.WriteInformation($"Unpatching package {package.Id.FamilyName}");
            statusCallback($"Unpatching {package.Id.FamilyName}", 0);
            var instPkg = InstalledPackages.GetInstalledPackage(package.Id.FamilyName);
            var dlls = instPkg.Value.installedPackage.SymlinkedDLLs.ToArray();
            var origDlls = instPkg.Value.installedPackage.OriginalDLLs.ToArray();

            string installPath = package.InstalledPath;

            float delProg = (0 - 50) / (float)instPkg.Value.installedPackage.SymlinkedDLLs.Count;
            foreach (var dll in dlls)
            {
                int progress = (int)Math.Round(50 + delProg * Array.IndexOf(instPkg.Value.installedPackage.SymlinkedDLLs.ToArray(), dll));
                statusCallback($"Removing {dll}", progress);
                try
                {
                    File.Delete(dll);
                } catch
                {
                    Logger.WriteError($"Failed to delete {dll}.");
                }
                if (instPkg.Value.installedPackage.SymlinkedDLLs.Contains(dll))
                    instPkg.Value.installedPackage.SymlinkedDLLs.Remove(dll);
            };

            float repProg = (99 - 50) / (float)instPkg.Value.installedPackage.OriginalDLLs.Count;
            foreach (var dll in origDlls)
            {
                int progress = (int)Math.Round(50 + delProg * Array.IndexOf(origDlls, dll));
                statusCallback($"Placing back original DLL \"{dll}\"", progress);
                try
                {
                    File.Copy(dll, Path.Combine(installPath, dll));
                }
                catch
                {
                    Logger.WriteError($"Failed to copy {dll}.");
                }
                if (instPkg.Value.installedPackage.OriginalDLLs.Contains(dll))
                    instPkg.Value.installedPackage.OriginalDLLs.Remove(dll);
            };

            instPkg.Value.installedPackage.IsPatched = false;

            statusCallback($"Updating package list", 99);
            InstalledPackages.UpdateInstalledPackage(instPkg.Value.familyName, instPkg.Value.installedPackage);
            statusCallback($"Done!", 100);
            return true;
        }

        // don't wanna use all the api shits lol
        public static async Task<GitHubRelease> GetOrReuseRelease()
        {
            if (wdRelease.GetType() != typeof(GitHubRelease))
                await GetLatestRelease();

            return wdRelease;
        }

        public static async Task<GitHubRelease> GetLatestRelease()
        {
            GitHubRelease release = new GitHubRelease();

            string url = $"https://api.github.com/repos/WinDurango/WinDurango/releases";

            System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubAPI/1.0)");
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            JsonDocument document = JsonDocument.Parse(json);
            var releases = document.RootElement.EnumerateArray();

            if (!releases.MoveNext())
                throw new Exception("Couldn't find any releases?????");

            JsonElement newestRelease = releases.Current;

            string name = newestRelease.GetProperty("name").GetString();

            release.Name = name;

            var assets = newestRelease.GetProperty("assets").EnumerateArray();

            if (!assets.MoveNext())
                throw new Exception("Couldn't find any assets?????");

            string download = assets.Current.GetProperty("browser_download_url").GetString();

            release.DownloadLink = download;

            wdRelease = release;
            return release;
        }
    }
}
