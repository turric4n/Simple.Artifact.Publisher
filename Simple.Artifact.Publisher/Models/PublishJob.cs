using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Simple.Artifact.Publisher.Models
{
    public class PublishJob
    {
        public string File { get; set; }
        public string Target { get; set; }
        public bool Cleanup { get; set; }
        public bool Backup { get; set; }
    }
}
