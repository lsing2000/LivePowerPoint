using System;
using System.Web.Script.Serialization;
using LivePowerPoint.Core;
using PowerPoint = Microsoft.Office.Interop.PowerPoint;

namespace LivePowerPoint.AddIn
{
    /// <summary>Binding config in Shape.Tags. Identity = LP_BINDING_ID (a GUID), never shape index. No secrets here.</summary>
    public static class BindingStore
    {
        const string IdTag = "LP_BINDING_ID", CfgTag = "LP_CONFIG";
        static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        public static BindingConfig Read(PowerPoint.Shape s)
        {
            var id = s.Tags[IdTag];
            if (string.IsNullOrEmpty(id)) return null;
            var cfg = Json.Deserialize<BindingConfig>(s.Tags[CfgTag]);
            cfg.BindingId = id;
            return cfg;
        }
        public static void Write(PowerPoint.Shape s, BindingConfig cfg)
        {
            if (string.IsNullOrEmpty(cfg.BindingId)) cfg.BindingId = Guid.NewGuid().ToString("D");
            s.Tags.Add(IdTag, cfg.BindingId);
            s.Tags.Add(CfgTag, Json.Serialize(cfg));
        }
        public static void Remove(PowerPoint.Shape s) { s.Tags.Delete(IdTag); s.Tags.Delete(CfgTag); }
    }
}
