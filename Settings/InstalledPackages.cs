using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Windows.ApplicationModel;
using WinDurango.UI.Utils;


namespace WinDurango.UI.Settings
{
    public class installedPackage
    {
        public string FullName { get; set; }
        public string FamilyName { get; set; }
        public string Version { get; set; }
        public string InstallPath { get; set; }
        public List<string> PatchedDlls { get; set; }
        public List<string> OriginalDlls { get; set; }
        public bool IsPatched { get; set; }
    }

    public class InstalledPackages
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            WriteIndented = true
        };

        private readonly List<installedPackage> _installedPackages;

        public InstalledPackages()
        {
            if (!Directory.Exists(App.DataDir))
            {
                Directory.CreateDirectory(App.DataDir);
            }

            string filePath = Path.Combine(App.DataDir, "InstalledPackages.json");

            if (!File.Exists(filePath))
            {
                Logger.WriteInformation("Creating empty InstalledPackages.json");
                File.WriteAllText(filePath, "{}");
                _installedPackages = [];
            }
            else
            {
                string json = File.ReadAllText(filePath);

                _installedPackages = JsonSerializer.Deserialize<List<installedPackage>>(json)
                                     ?? [];
            }
        }

        public void RemovePackage(Package pkg)
        {
            installedPackage package = _installedPackages.Find(p => p.FamilyName == pkg.Id.FamilyName);
            if (package != null && package.FullName == pkg.Id.FullName)
            {
                _installedPackages.Remove(package);
            }
            else
            {
                Logger.WriteError($"Couldn't uninstall {pkg.Id.FamilyName} as it was not found in the package list.");
                return;
            }

            Save();
            Logger.WriteInformation($"Removed {pkg.DisplayName} ({pkg.Id.FamilyName}) from the InstalledPackages list.");
        }

        public List<installedPackage> GetPackages()
        {
            return _installedPackages;
        }

        public installedPackage? GetPackage(Package pkg)
        {
            return _installedPackages.Find(p => p.FamilyName == pkg.Id.FamilyName);
        }

        public installedPackage? GetPackage(string familyName)
        {
            return _installedPackages.Find(p => p.FamilyName == familyName);
        }

        public void UpdatePackage(installedPackage package)
        {
            int index = _installedPackages.FindIndex(p => p.FamilyName == package.FamilyName);
            if (index < 0)
            {
                _installedPackages.Add(package);
            }
            else
            {
                _installedPackages[index] = package;
            }
            Save();
        }

        public void Save()
        {
            string json = JsonSerializer.Serialize(_installedPackages, JsonSerializerOptions);
            File.WriteAllText(Path.Combine(App.DataDir, "InstalledPackages.json"), json);
            Logger.WriteDebug("Saved InstalledPackages.json");
        }

        public void AddPackage(Package package)
        {
            if (_installedPackages.Exists(p => p.FamilyName == package.Id.FamilyName))
            {
                Logger.WriteError($"Couldn't add {package.DisplayName} as it already exists.");
                return;
            }

            _installedPackages.Add(new installedPackage
            {
                FullName = package.Id.FullName,
                FamilyName = package.Id.FamilyName,
                Version =
                    $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}",
                InstallPath = package.InstalledPath,
                PatchedDlls = [],
                OriginalDlls = [],
                IsPatched = Path.Exists(package.InstalledPath)
            });

            Save();
        }
    }
}