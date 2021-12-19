using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UiPath.ZenDesk.Models
{
    public class TicketFieldOptionResponse
    {
        [JsonProperty("custom_field_options")]
        public IList<TicketFieldOption> TicketFieldOptions { get; set; }

        [JsonProperty("nex_page")]
        public string NextPage { get; set; }

        [JsonProperty("previous_page")]
        public string PreviousPage { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }
}
