using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Simple.Artifact.Publisher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Simple.Artifact.Publisher.Helpers;

namespace Simple.Artifact.Publisher.Controllers
{
    public class UploadController : Controller
    {
        private readonly ILogger<UploadController> _logger;
        private readonly long _fileSizeLimit = Int64.MaxValue;
        private readonly string _targetFilePath;
        private readonly string[] _permittedExtensions = { ".txt", ".zip", ".mp3" };

        public UploadController(ILogger<UploadController> logger, IConfiguration config)
        {
            _logger = logger;

            // To save physical files to a path provided by configuration:
            _targetFilePath = config.GetValue<string>("StoredFilesPath");
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadArtifact()
        {
            try
            {
                _logger.LogInformation($"Uploading artifact.");
                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                {
                    ModelState.AddModelError("File",
                        $"The request couldn't be processed (Error 1).");
                    // Log error

                    return BadRequest(ModelState);
                }

                var trustedFileNameForFileStorage = Path.GetRandomFileName();

                var boundary = MultipartRequestHelper.GetBoundary(
                    MediaTypeHeaderValue.Parse(Request.ContentType),
                    Int32.MaxValue);
                var reader = new MultipartReader(boundary, HttpContext.Request.Body);
                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    var hasContentDispositionHeader =
                        ContentDispositionHeaderValue.TryParse(
                            section.ContentDisposition, out var contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        // This check assumes that there's a file
                        // present without form data. If form data
                        // is present, this method immediately fails
                        // and returns the model error.
                        if (!MultipartRequestHelper
                            .HasFileContentDisposition(contentDisposition))
                        {
                            ModelState.AddModelError("File",
                                $"The request couldn't be processed (Error 2).");
                            // Log error

                            return BadRequest(ModelState);
                        }

                        // Don't trust the file name sent by the client. To display
                        // the file name, HTML-encode the value.
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                            contentDisposition.FileName.Value);

                        // **WARNING!**
                        // In the following example, the file is saved without
                        // scanning the file's contents. In most production
                        // scenarios, an anti-virus/anti-malware scanner API
                        // is used on the file before making the file available
                        // for download or for use by other systems. 
                        // For more information, see the topic that accompanies 
                        // this sample.

                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return BadRequest(ModelState);
                        }

                        await using var targetStream = System.IO.File.Create(
                            Path.Combine(_targetFilePath, trustedFileNameForFileStorage));

                        await targetStream.WriteAsync(streamedFileContent);

                        _logger.LogInformation(
                            "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                            "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                            trustedFileNameForDisplay, _targetFilePath,
                            trustedFileNameForFileStorage);

                        streamedFileContent = null;
                    }

                    // Drain any remaining section body that hasn't been consumed and
                    // read the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }

                return Created(nameof(UploadController), trustedFileNameForFileStorage);
            }
            catch(Exception ex)
            {
                _logger.LogError("Error while uploading artifact", ex.Message);
                throw;
            }
            finally
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();
            }
        }
    }
}
