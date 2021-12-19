using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UiPath.ZenDesk.Models
{
    public class CustomFieldOption
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("raw_name")]
        public string RawName { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

    }
}
