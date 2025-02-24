using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using Windows.Gaming.Input;
using WinDurango.UI.Utils;

namespace WinDurango.UI.Settings
{
    public class BindingsConverter : JsonConverter<Dictionary<GamepadButtons, Key>>
    {
        public override Dictionary<GamepadButtons, Key> Read(ref Utf8JsonReader reader, Type conv, JsonSerializerOptions options)
        {
            Dictionary<GamepadButtons, Key> dic = new();
            JsonElement val = JsonDocument.ParseValue(ref reader).RootElement;

            foreach (JsonProperty property in val.EnumerateObject())
            {
                if (Enum.TryParse(property.Name, out GamepadButtons button))
                {
                    dic[button] = Enum.Parse<Key>(property.Value.GetString()!);
                }
            }
            return dic;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<GamepadButtons, Key> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<GamepadButtons, Key> kvs in value)
            {
                writer.WritePropertyName(kvs.Key.ToString());
                writer.WriteStringValue(kvs.Value.ToString());
            }
            writer.WriteEndObject();
        }
    }

    public class CoreConfigData
    {
        public class User
        {
            public string Name { get; set; } = "durangler";
            public ulong Id { get; set; } = 0;
        }

        public class ControllerKeybind
        {
            public GamepadButtons? ControllerBind { get; set; }
            public Key? KeyBind { get; set; }
        }

        public string Version { get; set; } = "unset"; // to be set by core if for some reason the config already exists
        public bool EnableConsole { get; set; } = false;
        public bool DebugLogging { get; set; } = false;
        public bool LogToFile { get; set; } = true;
        public Dictionary<GamepadButtons, Key> GamepadBindings { get; set; } =
            Enum.GetValues(typeof(GamepadButtons))
                .Cast<GamepadButtons>()
                .ToDictionary(button => button, button => Key.None);

        public List<User> Users { get; set; } = [
            new User() {
                Name = "durangler",
                Id = 0
            },
            new User() {
                Name = "durangled",
                Id = 1
            },
            new User() {
                Name = "durangler2",
                Id = 2
            },
            new User() {
                Name = "durangled2",
                Id = 3
            },

        ];

    }

    public class CoreConfig : IConfig
    {
        private readonly string _settingsFile = Path.Combine(App.CoreDataDir, "settings.json");
        public CoreConfigData Settings { get; set; }

        public CoreConfig()
        {
            Settings = new CoreConfigData();

            if (!Directory.Exists(App.DataDir))
                Directory.CreateDirectory(App.DataDir);

            if (!File.Exists(_settingsFile))
            {
                Logger.WriteWarning("Core settings file doesn't exist");
                Generate();
                return;
            }

            try
            {
                string json = File.ReadAllText(_settingsFile);
                Settings = JsonSerializer.Deserialize<CoreConfigData>(json);

                if (Settings == null)
                    throw new Exception("loadedSettings is null... wtf?");
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error loading core settings: " + ex.Message);
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
            Logger.WriteInformation($"Backing up core settings.json to {settingsBackup}");
            File.Move(_settingsFile, settingsBackup);
        }

        public void Generate()
        {
            Logger.WriteInformation($"Generating core settings file...");
            Settings = new CoreConfigData();
            Save();
        }

        public void Save()
        {
            try
            {
                Logger.WriteInformation($"Saving core settings...");
                JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true, Converters = { new BindingsConverter() } };
                File.WriteAllText(_settingsFile, JsonSerializer.Serialize(Settings, options));
            }
            catch (Exception ex)
            {
                Logger.WriteError("Error saving settings: " + ex.Message);
            }
        }

        public void Set(string setting, object value)
        {
            PropertyInfo property = typeof(CoreConfigData).GetProperty(setting);

            if (property == null || !property.CanWrite)
            {
                Logger.WriteError($"Setting {setting} does not exist... this shouldn't happen.");
                return;
            }

            property.SetValue(Settings, Convert.ChangeType(value, property.PropertyType));
            Save();
        }
    }
}