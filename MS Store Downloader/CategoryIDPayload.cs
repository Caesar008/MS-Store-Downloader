using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace MS_Store_Downloader
{
    public class CategoryIDData
    {
        [JsonProperty("Payload")]
        public CategoyIDPayload Payload { get; set; }
        [JsonProperty("ExpiryUtc")]
        public string ExpiryUtc { get; set; }
        [JsonProperty("Path")]
        public string Path { get; set; }
    }

    public class CategoyIDPayload
    {
        [JsonProperty("ApproximateSizeInBytes")]
        public long ApproximateSizeInBytes { get; set; }
        [JsonProperty("HasFreeTrial")]
        public bool HasFreeTrial { get; set; }
        [JsonProperty("Subtitle")]
        public string Subtitle { get; set; }
        [JsonProperty("Price")]
        public double Price { get; set; }
        [JsonProperty("IsDownloadable")]
        public bool IsDownloadable { get; set; }
        [JsonProperty("ContainsDownloadPackage")]
        public bool ContainsDownloadPackage { get; set; }
        [JsonProperty("SupportUris")]
        public List<UriObject> SupportUris { get; set; }
        [JsonProperty("PackageFamilyNames")]
        public List<string> PackageFamilyNames { get; set; }
        [JsonProperty("HasAlternateEditions")]
        public bool HasAlternateEditions { get; set; }
        [JsonProperty("ShortTitle")]
        public string ShortTitle { get; set; }
        [JsonProperty("SubcategoryName")]
        public string SubcategoryName { get; set; }
        [JsonProperty("AvailableDevicesDisplayText")]
        public string AvailableDevicesDisplayText { get; set; }
        [JsonProperty("CatalogSource")]
        public string CatalogSource { get; set; }
        [JsonProperty("Description")]
        public string Description { get; set; }
        [JsonProperty("Skus")]
        public List<SKU> Skus { get; set; }
    }

    public class FulfillmentData
    {
        [JsonProperty("ProductId")]
        public string ProductId { get; set; }
        [JsonProperty("WuCategoryId")]
        public string WuCategoryId { get; set; }
    }

    public class SKU
    {
        [JsonProperty("FulfillmentData")]
        public string FulfillmentData { get; set; }
    }

    public class UriObject
    {
        [JsonProperty("Uri")]
        public string Uri { get; set; }
    }
}
