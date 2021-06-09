using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Simple.Artifact.Publisher.Models;

namespace Simple.Artifact.Publisher.Controllers
{
    public class PublishController : Controller
    {
        private readonly ILogger _logger;
        private readonly string _srcFilePath;

        public PublishController(IConfiguration config, ILogger<PublishController> logger)
        {
            _logger = logger;
            _srcFilePath = config.GetValue<string>("StoredFilesPath");
        }

        [HttpPost]
        public IActionResult PublishArtifact([FromBody] PublishJob publishJob)
        {
            try
            {
                if (publishJob.Cleanup)
                {
                    try
                    {
                        if (Directory.Exists(publishJob.Target))
                        {
                            Directory.Delete(publishJob.Target, true);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }

                    try
                    {
                        Directory.CreateDirectory(publishJob.Target);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message);
                    }

                }
                using var ziparchive = ZipFile.OpenRead(Path.Combine(_srcFilePath, publishJob.File));

                ziparchive.ExtractToDirectory(publishJob.Target, true);

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError("Error while publishing artifact", e.Message);
                throw;
            }

        }

        [HttpDelete]
        public IActionResult RemoveArtifact([FromQuery] string artifactName)
        {
            System.IO.File.Delete(Path.Combine(_srcFilePath, artifactName));
            return Ok();
        }
    }
}
