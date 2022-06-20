using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Simple.Artifact.Publisher.Models;
using System;
using System.IO;
using System.IO.Compression;

namespace Simple.Artifact.Publisher.Controllers
{
    public class PublishController : Controller
    {
        private readonly ILogger<PublishController> _logger;
        private readonly string _srcFilePath;
        private int _maxTries = 5;

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
                _logger.LogInformation($"Publishing artifact {publishJob.File} - {publishJob.Target}");

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

                _logger.LogInformation($"Loading artifact file - {publishJob.File}");
                using var ziparchive = ZipFile.OpenRead(Path.Combine(_srcFilePath, publishJob.File));
                _logger.LogInformation($"Loaded artifact file - {publishJob.File}");

                var extracted = false;
                var tries = 0;

                while (tries < _maxTries && !extracted)
                {
                    try
                    {
                        tries += 1;
                        _logger.LogInformation($"Unziping artifact, try nº {tries} - {publishJob.File} - {publishJob.Target}");
                        ziparchive.ExtractToDirectory(publishJob.Target, true);
                        extracted = true;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error unzipping artifact, try nº {tries}", e.Message);

                        if (tries == _maxTries)
                        {
                            throw new Exception($"Error unzipping artifact, try nº {tries} - {e.Message}");
                        }
                    }
                }
                _logger.LogInformation($"Publishing successful {publishJob.File} - {publishJob.Target}");

                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error while publishing artifact - {e.Message}") ;
                return StatusCode(500, e.Message);
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
