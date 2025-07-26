using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSH.OfflineSync.Models
{
    public partial class FSHApiOffline
    {
        public DateTime LastModified { get; set; }
        public bool IsDirty { get; set; } = true;
    }
}
