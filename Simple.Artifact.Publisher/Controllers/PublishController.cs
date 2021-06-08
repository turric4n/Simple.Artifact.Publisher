using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Simple.Artifact.Publisher.Models;

namespace Simple.Artifact.Publisher.Controllers
{
    public class PublishController : Controller
    {
        private readonly string _srcFilePath;

        public PublishController(IConfiguration config)
        {
            _srcFilePath = config.GetValue<string>("StoredFilesPath");
        }

        [HttpPost]
        public IActionResult PublishArtifact([FromBody] PublishJob publishJob)
        {
            if (publishJob.Cleanup)
            {
                if (Directory.Exists(publishJob.Target))
                {
                    Directory.Delete(publishJob.Target, true);
                }

                Directory.CreateDirectory(publishJob.Target);

            }
            var ziparchive = ZipFile.OpenRead(Path.Combine(_srcFilePath, publishJob.File));
            ziparchive.ExtractToDirectory(publishJob.Target, true);

            return Ok();
        }

        [HttpDelete]
        public IActionResult RemoveArtifact([FromQuery] string artifactName)
        {
            System.IO.File.Delete(Path.Combine(_srcFilePath, artifactName));
            return Ok();
        }
    }
}
