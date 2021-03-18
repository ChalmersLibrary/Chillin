using Newtonsoft.Json;
using System;

namespace Chalmers.ILL.Models
{
    public class Instance
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("hrid")]
        public string Hrid { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("alternativeTitles")]
        public object[] AlternativeTitles { get; set; }

        [JsonProperty("editions")]
        public object[] Editions { get; set; }

        [JsonProperty("series")]
        public object[] Series { get; set; }

        [JsonProperty("identifiers")]
        public Identifier[] Identifiers { get; set; }

        [JsonProperty("contributors")]
        public object[] Contributors { get; set; }

        [JsonProperty("subjects")]
        public object[] Subjects { get; set; }

        [JsonProperty("classifications")]
        public object[] Classifications { get; set; }

        [JsonProperty("publication")]
        public object[] Publication { get; set; }

        [JsonProperty("publicationFrequency")]
        public object[] PublicationFrequency { get; set; }

        [JsonProperty("publicationRange")]
        public object[] PublicationRange { get; set; }

        [JsonProperty("electronicAccess")]
        public object[] ElectronicAccess { get; set; }

        [JsonProperty("instanceTypeId")]
        public string InstanceTypeId { get; set; }

        [JsonProperty("instanceFormatIds")]
        public object[] InstanceFormatIds { get; set; }

        [JsonProperty("instanceFormats")]
        public object[] InstanceFormats { get; set; }

        [JsonProperty("physicalDescriptions")]
        public object[] PhysicalDescriptions { get; set; }

        [JsonProperty("languages")]
        public object[] Languages { get; set; }

        [JsonProperty("notes")]
        public object[] Notes { get; set; }

        [JsonProperty("discoverySuppress")]
        public bool DiscoverySuppress { get; set; }

        [JsonProperty("statisticalCodeIds")]
        public object[] StatisticalCodeIds { get; set; }

        [JsonProperty("statusId")]
        public string StatusId { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }

        [JsonProperty("holdingsRecords2")]
        public object[] HoldingsRecords2 { get; set; }

        [JsonProperty("natureOfContentTermIds")]
        public object[] NatureOfContentTermIds { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonProperty("createdByUserId")]
        public string CreatedByUserId { get; set; }

        [JsonProperty("updatedDate")]
        public DateTime UpdatedDate { get; set; }

        [JsonProperty("updatedByUserId")]
        public string UpdatedByUserId { get; set; }
    }

    public class Identifier
    {
        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("identifierTypeId")]
        public string IdentifierTypeId { get; set; }
    }

}