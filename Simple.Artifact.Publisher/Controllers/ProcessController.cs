using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Simple.Artifact.Publisher.Models.Enums;
using Process = Simple.Artifact.Publisher.Models.Process;

namespace Simple.Artifact.Publisher.Controllers
{
    public class ProcessController : Controller
    {
        public IActionResult ProcessStart([FromBody] Process process)
        {
            switch (process.ProcessType)
            {
                case ProcessType.Process:
                    var processinfo = new ProcessStartInfo(process.ProcessPath);
                    processinfo.Arguments = process.Parameters;
                    System.Diagnostics.Process.Start(processinfo);
                    break;
                case ProcessType.Service:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Ok();
        }

        public IActionResult ProcessStop([FromBody] Process process)
        {
            var processname = Path.GetFileName(process.ProcessPath);
            var proc = System.Diagnostics.Process.GetProcesses()
                .FirstOrDefault(x => String.Equals(x.ProcessName, processname, 
                    StringComparison.InvariantCultureIgnoreCase));
            proc?.Kill();
            return Ok();
        }
    }
}
