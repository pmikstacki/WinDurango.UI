using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinDurango.UI.Dialogs;
using WinDurango.UI.Settings;
using Package = Windows.ApplicationModel.Package;

namespace WinDurango.UI.Utils
{
    public class GitHubRelease
    {
        public string Name { get; set; }
        public string DownloadLink { get; set; }
    }

    // yes it's the wrong term but it sounds good to my head
    public static class WinDurangoPatcher
    {
        private static readonly HttpClient httpClient = new(new HttpClientHandler { AllowAutoRedirect = true });
        private static GitHubRelease wdRelease;

        static WinDurangoPatcher()
        {
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubAPI/1.0)");
        }

        public static async Task<bool> UnpatchPackage(Package package, ProgressController controller)
        {
            installedPackage pkg = App.InstalledPackages.GetPackage(package);
            if (pkg == null)
                return false;

            return await UnpatchPackage(pkg, controller);
        }

        public static async Task<bool> PatchPackage(Package package, bool forceRedownload,
            ProgressController controller)
        {
            installedPackage pkg = App.InstalledPackages.GetPackage(package);
            if (pkg == null)
                return false;

            return await PatchPackage(pkg, forceRedownload, controller);
        }

        // todo: clean this up
        public static async Task<bool> PatchPackage(installedPackage package, bool forceRedownload,
            ProgressController controller)
        {
            string patchesPath = Path.Combine(App.DataDir, "WinDurangoCore");
            controller?.Update($"Patching {package.FamilyName}", 0);
            string curDate = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss");
            string installPath = package.InstallPath;

            controller?.Update("Getting latest release", 10);
            await GetOrReuseRelease(); // don't use the return value since wdRelease is set regardless
            string dlLink = wdRelease.DownloadLink;
            string relName = wdRelease.Name;
            string dlPath = $"WinDurangoCore.zip";

            // see this is quite messy but just needed to get it to work
            if (App.Settings.Settings.DownloadSource == UiConfigData.PatchSource.Artifact)
            {
                dlLink = "https://nightly.link/WinDurango/WinDurango/workflows/msbuild/main/WinDurango-DEBUG.zip";
                dlPath = $"WinDurangoCore-ARTIFACT.zip";
                patchesPath = Path.Combine(App.DataDir, "WinDurangoCore-ARTIFACT");
                relName = $"latest GitHub Actions artifact";
            }

            if (!Path.Exists(patchesPath) || forceRedownload)
            {
                if (Path.Exists(patchesPath))
                    Directory.Delete(patchesPath, true);

                string archivePath = Path.Combine(App.DataDir, dlPath);

                try
                {
                    controller?.Update($"Downloading WinDurango {relName}", 20);

                    await using Stream httpStream = await httpClient.GetStreamAsync(dlLink);
                    await using FileStream stream = new(archivePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    controller?.Update("Writing release zip", 30);
                    // why so slow?
                    await httpStream.CopyToAsync(stream);
                }
                catch (Exception ex)
                {
                    await controller.Fail(ex.Message, "Failed to download/write zip");
                    return false;
                }

                try
                {
                    Directory.CreateDirectory(patchesPath);
                    controller?.Update($"Extracting", 40);
                    ZipFile.ExtractToDirectory(archivePath, patchesPath);
                }
                catch (Exception ex)
                {
                    await controller.Fail(ex.Message, "Failed to extract");
                    return false;
                }
            }

            controller?.Update("Extracting", 50);
            if (Path.Exists(patchesPath))
            {
                DirectoryInfo patchesDir = new(patchesPath);

                FileInfo[] patchFiles = patchesDir.GetFiles("*.dll");
                float progressPerFile = (90 - 50) / (float)patchFiles.Length;
                DirectoryInfo packageDir = new(installPath);

                foreach (FileInfo file in patchFiles)
                {
                    int progress = (int)Math.Round(50 + progressPerFile * Array.IndexOf(patchFiles, file));

                    controller?.Update($"Copying {file.Name}", progress);
                    FileInfo oldFile = packageDir.GetFiles("*.dll").FirstOrDefault(f => f.Name == file.Name);
                    if (oldFile != null)
                    {
                        Logger.WriteInformation($"Backing up old {oldFile.Name}");
                        string dllBackup = Path.Combine(installPath, "WDDllBackup", curDate);
                        if (!Path.Exists(dllBackup))
                            Directory.CreateDirectory(dllBackup);

                        try
                        {
                            File.Move(oldFile.FullName, Path.Combine(dllBackup, oldFile.Name));
                        }
                        catch (Exception e)
                        {
                            await controller.Fail(e.Message, "Failed to move backed-up file " + oldFile.Name);
                            return false;
                        }

                        if (!package.OriginalDlls.Contains(Path.Combine(dllBackup, oldFile.Name)))
                            package.OriginalDlls.Add(Path.Combine(dllBackup, oldFile.Name));
                    }

                    string patchPath = Path.Combine(installPath, file.Name);
                    if (Path.Exists(Path.Combine(installPath, file.Name)))
                        File.Delete(Path.Combine(installPath, file.Name));

                    try
                    {
                        File.Copy(file.FullName, Path.Combine(installPath, file.Name));
                    }
                    catch (Exception e)
                    {
                        await controller.Fail(e.Message, "Failed to copy file " + file.Name);
                        return false;
                    }

                    Logger.WriteInformation($"Added {file.Name}");

                    if (!package.PatchedDlls.Contains(patchPath))
                        package.PatchedDlls.Add(patchPath);
                }
            }
            else
            {
                Logger.WriteError("How did this happen???? patchDir should exist.");
                return false;
            }

            controller?.Update($"Writing patch txt", 93);
            StringBuilder builder = new();
            builder.AppendLine($"# This package was patched by WinDurango.UI with WinDurango release \"{relName}\".");
            builder.AppendLine("# If you want to unpatch manually, delete this file and edit %appdata%\\WinDurango\\UI\\InstalledPackages.json and set IsPatched to false.");
            builder.AppendLine("# Format is ReleaseName;VerPacked");
            builder.AppendLine($"{relName.Replace(";", "-")};{App.VerPacked}");
            await File.WriteAllTextAsync(Path.Combine(installPath, "installed.txt"), builder.ToString());

            controller?.Update($"Updating package list", 95);
            package.IsPatched = true;
            App.InstalledPackages.UpdatePackage(package);
            controller?.Update($"Done!", 100);
            return true;
        }

        public static async Task<bool> UnpatchPackage(installedPackage package,
            ProgressController controller)
        {
            controller?.Update($"Unpatching {package.FamilyName}", 0);

            string[] dlls = package.PatchedDlls.ToArray();
            string[] originalDlls = package.OriginalDlls.ToArray();

            string installPath = package.InstallPath;

            float progressPerRemove = (0 - 50) / (float)package.PatchedDlls.Count;
            foreach (string dll in dlls)
            {
                int progress = (int)Math.Round(50 + progressPerRemove * Array.IndexOf(package.PatchedDlls.ToArray(), dll));
                controller?.Update($"Removing {dll}", progress);
                try
                {
                    File.Delete(dll);
                    package.PatchedDlls.Remove(dll);
                }
                catch
                {
                    Logger.WriteError($"Failed to delete {dll}.");
                }
            };

            float progressPerRevert = (98 - 50) / (float)package.OriginalDlls.Count;
            foreach (string dll in originalDlls)
            {
                int progress = (int)Math.Round(50 + progressPerRevert * Array.IndexOf(originalDlls, dll));
                controller?.Update($"Placing back original DLL \"{dll}\"", progress);
                try
                {
                    File.Copy(dll, Path.Combine(installPath, dll));
                    package.OriginalDlls.Remove(dll);
                }
                catch
                {
                    Logger.WriteError($"Failed to copy {dll}.");
                }
            };

            package.IsPatched = false;

            controller?.Update($"Removing patched.txt", 99);
            if (Path.Exists(Path.Combine(installPath, "installed.txt")))
                File.Delete(Path.Combine(installPath, "installed.txt"));

            controller?.Update($"Updating package list", 99);
            App.InstalledPackages.UpdatePackage(package);
            controller?.Update($"Done!", 100);
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