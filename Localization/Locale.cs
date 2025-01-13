using System;
using System.Collections.Generic;
using System.Diagnostics;
using WinDurango.UI.Utils;
using WinUI3Localizer;

namespace WinDurango.UI.Localization
{
    public static class Locale
    {
        public static string GetLocalizedText(string name, params object[] args)
        {
            String translated = Localizer.Get().GetLocalizedString(name);
            if (translated == "")
                Logger.Write(LogLevel.Warning, $"String {name} not found in string resources.");
            return string.Format(translated == "" ? $"LOCALIZATION ERROR: String {name} not found in string resources." : translated, args);
        }
    }
}
