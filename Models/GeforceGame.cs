using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVIDIAGeForceNowEnabler
{
    public class GeforceGame
    {
        public int id { get; set; }
        public string title { get; set; }
        public string steamUrl { get; set; }
        public string store { get; set; }

        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("sortName")]
        public string SortName { get; set; }

        [JsonProperty("isFullyOptimized")]
        public bool IsFullyOptimized { get; set; }

        [JsonProperty("steamUrl")]
        public string SteamUrl { get; set; }

        [JsonProperty("store")]
        public string Store { get; set; }

        [JsonProperty("publisher")]
        public string Publisher { get; set; }

        [JsonProperty("genres")]
        public string[] Genres { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
