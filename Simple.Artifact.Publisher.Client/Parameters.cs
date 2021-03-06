using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace Simple.Artifact.Publisher.Client
{
    public class Parameters
    {
        [Option('a', "action", Required = true, HelpText = "Action to send")]
        public Action Action { get; set; }
        [Option('e', "endpoint", Required = true, HelpText = "Publish Endpoint")]
        public string Api { get; set; }
        [Option('n', "process", Required = false, HelpText = "Process to call")]
        public string ProcessPath { get; set; }
        [Option('p', "parameters", Required = false, HelpText = "Parameters")]
        public string ProcessParameters { get; set; }

        [Option('k', "kill", Required = false, HelpText = "Process to kill")]
        public string ProcessNameToKill { get; set; }

        [Option('u', "username", Required = false, HelpText = "Username of the process to kill")]
        public string ProcessUsernameToKill { get; set; }

        [Option('f', "filename", Required = false, HelpText = "Filename to publish")]
        public string FileName { get; set; }
        [Option('t', "target", Required = false, HelpText = "Target to publish")]
        public string TargetFolder { get; set; }
        [Option('c', "cleanup", Required = false, HelpText = "Cleanup target location")]
        public bool Cleanup { get; set; }
    }
}
