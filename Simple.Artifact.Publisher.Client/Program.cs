using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;

namespace Simple.Artifact.Publisher.Client
{
    class Program
    {
        static private string BaseEndpoint;
        static private string UploadEndpoint = "/upload/UploadArtifact";
        static private string ProcessEndpoint = "/process/ProcessStart";
        static private string PublishEndpoint = "/publish/PublishArtifact";
        public static async Task<string> UploadFile(string filePath)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File [{filePath}] not found.");
            }
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await httpClient.PostAsync($"{UploadEndpoint}", form);
            response.EnsureSuccessStatusCode();
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }

        public static async Task PublishFile(string filename, string targetpath)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);

            var publish = new PublishJob()
            {
                Target = targetpath,
                Cleanup = true,
                File = filename
            };
            var publishjob = JsonConvert.SerializeObject(publish);

            var jsoncontent = new StringContent(publishjob, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{PublishEndpoint}", jsoncontent);

            response.EnsureSuccessStatusCode();
        }



        public static async Task StartProcess(string processPath, string parameters)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);

            var process = new Process()
            {
                ProcessPath = processPath,
                Parameters = parameters
            };
            var processjob = JsonConvert.SerializeObject(process);

            var jsoncontent = new StringContent(processjob, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{ProcessEndpoint}", jsoncontent);

            response.EnsureSuccessStatusCode();
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed<Parameters>(o =>
                {
                    BaseEndpoint = o.Api;
                    switch (o.Action)
                    {
                        case Action.Publish:
                            var uploadResult = UploadFile(o.FileName).Result;
                            PublishFile(uploadResult, o.TargetFolder).Wait();
                            break;
                        case Action.Process:
                            StartProcess(o.ProcessPath, o.ProcessParameters).Wait();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
        }
    }
}

