using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.OfflineSync.Models
{
    public class OfflineRequest
    {
        public string? Url { get; set; }
        public string? Method { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
