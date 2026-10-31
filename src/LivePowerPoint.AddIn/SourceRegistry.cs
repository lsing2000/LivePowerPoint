using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace LivePowerPoint.AddIn
{
    /// <summary>Per-user source registry. API keys are DPAPI-encrypted (CurrentUser) and never written to the deck or logs.</summary>
    public static class SourceRegistry
    {
        static string PathFile => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LivePowerPoint", "sources.json");
        class Entry { public string BaseUrl; public string ProtectedKey; }
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        public static void Save(string datasetId, string baseUrl, string apiKey)
        {
            var all = Load();
            all[datasetId] = new Entry { BaseUrl = baseUrl, ProtectedKey = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(apiKey), null, DataProtectionScope.CurrentUser)) };
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PathFile));
            File.WriteAllText(PathFile, Json.Serialize(all));
        }
        public static SourceInfo Lookup(string datasetId)
        {
            if (!Load().TryGetValue(datasetId, out var e)) throw new InvalidOperationException("No source registered for dataset");
            return new SourceInfo { BaseUrl = e.BaseUrl, ApiKey = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(e.ProtectedKey), null, DataProtectionScope.CurrentUser)) };
        }
        static Dictionary<string, Entry> Load() => File.Exists(PathFile) ? Json.Deserialize<Dictionary<string, Entry>>(File.ReadAllText(PathFile)) : new Dictionary<string, Entry>();
    }
}
