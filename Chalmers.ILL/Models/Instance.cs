using Newtonsoft.Json;
using System;

namespace Chalmers.ILL.Models
{
    public class Instance
    {
        public string Id { get; set; }
        public string Hrid { get; set; }
        public string Source { get; set; }
        public string Title { get; set; }
        public object[] AlternativeTitles { get; set; }
        public object[] Editions { get; set; }
        public object[] Series { get; set; }
        public Identifier[] Identifiers { get; set; }
        public object[] Contributors { get; set; }
        public object[] Subjects { get; set; }
        public object[] Classifications { get; set; }
        public object[] Publication { get; set; }
        public object[] PublicationFrequency { get; set; }
        public object[] PublicationRange { get; set; }
        public object[] ElectronicAccess { get; set; }
        public string InstanceTypeId { get; set; }
        public object[] InstanceFormatIds { get; set; }
        public object[] InstanceFormats { get; set; }
        public object[] PhysicalDescriptions { get; set; }
        public object[] Languages { get; set; }
        public object[] Notes { get; set; }
        public bool DiscoverySuppress { get; set; }
        public object[] StatisticalCodeIds { get; set; }
        public string StatusId { get; set; }
        public Metadata Metadata { get; set; }
        public object[] HoldingsRecords2 { get; set; }
        public object[] NatureOfContentTermIds { get; set; }
    }

    public class Metadata
    {
        public DateTime CreatedDate { get; set; }
        public string CreatedByUserId { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedByUserId { get; set; }
    }

    public class Identifier
    {
        public string Value { get; set; }
        public string IdentifierTypeId { get; set; }
    }
}