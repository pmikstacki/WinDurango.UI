using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace WinDurango.UI.Utils
{
    public class XHandler
    {
        public static List<Package> GetXPackages(List<Package> packages)
        {
            // first try implementation and it worked hell yeah
            List<Package> result = [];
            foreach (Package package in packages)
            {
                XbManifestInfo xbManifestInfo = package.GetXbProperties();
                ManifestInfo manifestInfo = package.GetProperties();
                if (xbManifestInfo.IsEra(manifestInfo))
                    result.Add(package);
            }
            Logger.WriteInformation($"Found {result.Count} Era/XbUWP packages");
            return result;
        }
    }
}
