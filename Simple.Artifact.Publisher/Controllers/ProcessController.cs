using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using Microsoft.Extensions.Logging;
using Simple.Artifact.Publisher.Models;
using Simple.Artifact.Publisher.Models.Enums;
using Process = Simple.Artifact.Publisher.Models.Process;

namespace Simple.Artifact.Publisher.Controllers
{
    public class ProcessController : Controller
    {
        private readonly ILogger<ProcessController> _logger;

        public ProcessController(ILogger<ProcessController> logger)
        {
            _logger = logger;
        }

        public IActionResult ProcessStart([FromBody] Process process)
        {
            _logger.LogInformation($"Process start called {process.ProcessPath} - {process.Parameters}");

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
                .FirstOrDefault(x => string.Equals(x.ProcessName, processname, 
                    StringComparison.InvariantCultureIgnoreCase));
            proc?.Kill();
            return Ok();
        }

        public IActionResult ProcessStopByUser([FromBody] ProcessUsername processUsername)
        {
            _logger.LogInformation($"Process kill called -> {processUsername.ProcessName} - {processUsername.ProcessUser}");

            var processes = System.Diagnostics.Process.GetProcessesByName(processUsername.ProcessName)
                .Where(process => GetProcessOwner(process.Id) == processUsername.ProcessUser);

            foreach (var process in processes)
            {
                _logger.LogInformation($"Killing process -> {process.ProcessName}");

                process.Kill();
            }

            return Ok();
        }

        static string GetProcessOwner(int processId)
        {
            string query = "Select * From Win32_Process Where ProcessID = " + processId;
            using ManagementObjectSearcher moSearcher = new ManagementObjectSearcher(query);
            using ManagementObjectCollection moCollection = moSearcher.Get();

            foreach (ManagementObject mo in moCollection)
            {
                string[] args = new string[] { string.Empty };
                int returnVal = Convert.ToInt32(mo.InvokeMethod("GetOwner", args));
                if (returnVal == 0)
                    return args[0];
            }

            return "N/A";
        }
    }
}
