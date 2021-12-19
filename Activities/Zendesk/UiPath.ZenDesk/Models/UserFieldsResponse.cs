using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UiPath.ZenDesk.Models
{
    public class UserFieldsResponse
    {
        [JsonProperty("user_fields")]
        public IList<UserField> UserFields { get; set; }
    }
}
