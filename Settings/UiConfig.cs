using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Settings;

public class UiConfigData
{
    public enum ThemeSetting
    {
        Fluent,
        FluentThin,
        Mica,
        MicaAlt,
        System
    }

    public enum PatchSource
    {
        Release,
        Artifact
    }

    public uint SaveVersion { get; set; } = App.VerPacked;
    public ThemeSetting Theme { get; set; } = ThemeSetting.Fluent;
    public bool DebugLoggingEnabled { get; set; } = false;
    public bool AppViewIsHorizontalScrolling { get; set; } = false;

    public string Language { get; set; } = "en-US";

    public string DownloadedWdVer { get; set; } = String.Empty;
    public PatchSource DownloadSource { get; set; } = PatchSource.Release;

    public bool ShowDevNotice { get; set; } = true;
}

// TODO: fix type init exception
public class UiConfig : IConfig
{
    private readonly string _settingsFile = Path.Combine(App.DataDir, "settings.json");
    public UiConfigData Settings { get; private set; }

    public UiConfig()
    {
        Settings = new UiConfigData();
        if (!Directory.Exists(App.DataDir))
            Directory.CreateDirectory(App.DataDir);

        if (!File.Exists(_settingsFile))
        {
            Logger.WriteWarning($"Settings file doesn't exist");
            Generate();
            return;
        }

        try
        {
            string json = File.ReadAllText(_settingsFile);
            UiConfigData loadedSettings = JsonSerializer.Deserialize<UiConfigData>(json);

            if (loadedSettings == null)
            {
                Logger.WriteWarning("loadedSettings is null... wtf?");
                return;
            }

            if (loadedSettings.SaveVersion > App.VerPacked)
            {
                Reset();
                Logger.WriteInformation($"Settings were reset due to the settings file version being too new. ({loadedSettings.SaveVersion})");
            }

            loadedSettings = JsonSerializer.Deserialize<UiConfigData>(json);
            Settings = loadedSettings;
        }
        catch (Exception ex)
        {
            Logger.WriteError("Error loading settings: " + ex.Message);
            Generate();
        }
    }

    public void Reset()
    {
        Backup();
        Generate();
        Logger.WriteInformation($"Settings have been reset");
    }

    public void Backup()
    {
        string settingsBackup = _settingsFile + ".old_" + ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds().ToString();
        Logger.WriteInformation($"Backing up settings.json to {settingsBackup}");
        File.Move(_settingsFile, settingsBackup);
    }

    public void Generate()
    {
        Logger.WriteInformation($"Generating settings file...");
        Settings = new UiConfigData();
        Save();
    }

    public void Save()
    {
        try
        {
            Logger.WriteInformation($"Saving settings...");
            Settings.SaveVersion = App.VerPacked;
            JsonSerializerOptions options = new();
            options.WriteIndented = true;
            File.WriteAllText(_settingsFile, JsonSerializer.Serialize(Settings, options));
            /* FIXME: For some reason unknown to me, App.MainWindow is null here, but only if the Settings were generated before e.g. on first launch
             * No biggy in that case as nothing has been customized yet, but depending on the reason this might cause problems.
            */
            App.MainWindow.LoadSettings();
        }
        catch (Exception ex)
        {
            Logger.WriteError("Error saving settings: " + ex.Message);
        }
    }

    public void Set(string setting, object value)
    {
        PropertyInfo property = typeof(UiConfigData).GetProperty(setting);

        if (property == null || !property.CanWrite)
        {
            Logger.WriteError($"Setting {setting} does not exist... this shouldn't happen.");
            return;
        }

        property.SetValue(Settings, Convert.ChangeType(value, property.PropertyType));
        Save();
    }

}