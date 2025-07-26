using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.OfflineSync.Models
{

    public class StoredHttpResponse
    {
        public int StatusCode { get; set; }
        public string ReasonPhrase { get; set; } = "OK";
        public string Version { get; set; } = "1.1";
        public string Body { get; set; } = string.Empty;
        public Dictionary<string, string[]> Headers { get; set; } = new();
        public Dictionary<string, string[]> ContentHeaders { get; set; } = new();
    }
}
