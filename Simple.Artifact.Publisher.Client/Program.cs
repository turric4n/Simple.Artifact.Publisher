using CommandLine;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Simple.Artifact.Publisher.Client
{
    class Program
    {
        private static string BaseEndpoint;
        private static string UploadEndpoint = "/upload/UploadArtifact";
        private static string ProcessEndpoint = "/process/ProcessStart";
        private static string PublishEndpoint = "/publish/PublishArtifact";
        private static string RemoveEndpoint = "/publish/RemoveArtifact";
        private static string KillProcessByUsername = "/process/ProcessStopByUser";

        public static async Task<string> UploadFile(string filePath)
        {
            using var httpClient = new HttpClient();
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

        public static async Task PublishFile(string filename, string targetpath, bool cleanup)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);

            var publish = new PublishJob()
            {
                Target = targetpath,
                Cleanup = cleanup,
                File = filename
            };
            var publishjob = JsonConvert.SerializeObject(publish);

            var jsoncontent = new StringContent(publishjob, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{PublishEndpoint}", jsoncontent);

            response.EnsureSuccessStatusCode();
        }


        public static async Task RemoveArtifact(string filename)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);

            var response = await httpClient.DeleteAsync($"{RemoveEndpoint}?artifactname={filename}");

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

        public static async Task StopProcessByUsername(string processName, string processUsername)
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(10);
            httpClient.BaseAddress = new Uri(BaseEndpoint);

            var process = new ProcessUsername()
            {
                ProcessName = processName,
                ProcessUser = processUsername
            };

            var processjob = JsonConvert.SerializeObject(process);

            var jsoncontent = new StringContent(processjob, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync($"{KillProcessByUsername}", jsoncontent);

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
                            PublishFile(uploadResult, o.TargetFolder, o.Cleanup).Wait();
                            RemoveArtifact(uploadResult).Wait();
                            break;
                        case Action.Process:
                            StartProcess(o.ProcessPath, o.ProcessParameters).Wait();
                            break;
                        case Action.KillProcess:
                            StopProcessByUsername(o.ProcessNameToKill, o.ProcessUsernameToKill).Wait();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                });
        }
    }
}

