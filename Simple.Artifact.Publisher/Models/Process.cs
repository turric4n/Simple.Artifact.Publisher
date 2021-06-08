using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Simple.Artifact.Publisher.Models.Enums;

namespace Simple.Artifact.Publisher.Models
{
    public class Process
    {
        public string ProcessPath { get; set; }
        public ProcessType ProcessType { get; set; }
        public string Parameters { get; set; }
    }
}
