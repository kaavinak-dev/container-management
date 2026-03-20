using Engines.FileStorageEngines.Abstractions;
using Minio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Engines.FileStorageEngines.Implementations
{
    public class MinioProjectStorageEngine : ProjectStorageEngine
    {
        IMinioClient _client;
        //HttpClient _httpClient;

        string accessKey = "minioadmin";
        string secretKey = "minioadmin";

        public MinioProjectStorageEngine(string engineUrl, int enginePort) : base(engineUrl, enginePort)
        {


        }

        public override void CreateStorageEngineClient()
        {
            try
            {
                _client = new MinioClient().WithEndpoint(_engineUrl, _enginePort).WithCredentials("minioadmin", "minioadmin").
                           WithSSL(false).
                           Build();
            }
            catch (Exception err)
            {

                Console.WriteLine(err.Message);
            }
            //_httpClient = new HttpClient();
            //CreateExecutableBucket();
        }

        public override async Task DeleteProject(string bucketName, string objectName)
        {
            await _client.RemoveObjectAsync(
                new Minio.DataModel.Args.RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName)
            );
        }

        public override async Task<string> UploadProject(Stream projectData, string bucketName = "quarantine", string projectName = null)
        {
            try
            {


                string objectName = String.IsNullOrEmpty(projectName) ? GenerateRandomObjectName() : projectName;

                // Step 1: Ensure the bucket exists
                bool bucketExists = await _client.BucketExistsAsync(
                    new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucketName)
                );

                if (!bucketExists)
                {
                    await _client.MakeBucketAsync(
                        new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucketName)
                    );
                }

                // Step 2: Generate presigned PUT URL (valid for 24 hours)
                var presignedUrl = await _client.PresignedPutObjectAsync(
                    new Minio.DataModel.Args.PresignedPutObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(objectName)
                        .WithExpiry(60 * 60 * 24) // 24 hours in seconds
                );

                // Step 3: Upload the file using the presigned URL with HttpClient
                using (var httpClient = new HttpClient())
                {
                    // Read the stream into a byte array or use StreamContent
                    using (var content = new StreamContent(projectData))
                    {
                        // Set content type if needed (optional)
                        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                        // Upload using HTTP PUT to the presigned URL
                        var response = await httpClient.PutAsync(presignedUrl, content);

                        // Check if upload was successful
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Upload failed with status code: {response.StatusCode}");
                        }
                    }
                }
                return objectName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading file: {ex.Message}");
                throw;
            }
            return null;

        }

        private void CreateExecutableBucket(string bucketName)
        {
            bool bucketExists = _client.BucketExistsAsync(new Minio.DataModel.Args.BucketExistsArgs().WithBucket(bucketName)).GetAwaiter().GetResult();
            if (!bucketExists)
            {
                _client.MakeBucketAsync(new Minio.DataModel.Args.MakeBucketArgs().WithBucket(bucketName));

            }

        }

        public async Task<Stream> DownloadProject(string bucketName, string projectName)
        {
            try
            {
                var memoryStream = new MemoryStream();

                await _client.GetObjectAsync(
                    new Minio.DataModel.Args.GetObjectArgs()
                        .WithBucket(bucketName)
                        .WithObject(projectName)
                        .WithCallbackStream((stream) =>
                        {
                            stream.CopyTo(memoryStream);
                        })
                );

                // Reset stream position to beginning for reading
                memoryStream.Position = 0;

                Console.WriteLine($"Downloaded file: {projectName} from bucket: {bucketName}");
                Console.WriteLine($"File size: {memoryStream.Length} bytes");

                return memoryStream;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file {projectName} from bucket {bucketName}: {ex.Message}");
                throw;
            }
        }

        private string GenerateRandomObjectName(string fileExtension = null)
        {
            // Option 1: GUID-based (guaranteed unique)
            string uniqueId = Guid.NewGuid().ToString("N"); // "N" format removes hyphens

            // Option 2: Timestamp + Random (more readable, still very unlikely to collide)
            // string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            // string randomPart = Guid.NewGuid().ToString("N").Substring(0, 8);
            // string uniqueId = $"{timestamp}_{randomPart}";

            // Add file extension if provided
            if (!string.IsNullOrEmpty(fileExtension))
            {
                // Ensure extension starts with a dot
                if (!fileExtension.StartsWith("."))
                {
                    fileExtension = "." + fileExtension;
                }
                return $"{uniqueId}{fileExtension}";
            }

            return uniqueId;
        }

        public override void UploadExecutable(string bucketName, Stream fileData)
        {
            _client.PutObjectAsync(
                new Minio.DataModel.Args.PutObjectArgs().WithBucket(bucketName)
                .WithObject("new file")
                .WithStreamData(fileData)
                .WithObjectSize(fileData.Length)

                );

        }

        public override async Task<Dictionary<string, object>> StorageEngineStatusChecker()
        {
            try
            {
                string url = $"http://{_engineUrl}:{_enginePort}/minio/v2/metrics/cluster";
                //var query = await _httpClient.GetAsync(url);
                //var response = await query.Content.ReadAsStringAsync();
                var communicator = new MinioHttpCommunicator(url, accessKey, secretKey);
                var response = await communicator.SendSignedGetAsync();
                var responseStream = response.Content;
                var result = await responseStream.ReadAsStringAsync();
                var resultDic = ParseMinioMetrics(result);
                resultDic["server"] = this._engineUrl + ":" + this._enginePort;
                return resultDic;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception("error happened when engine status check");
            }
        }

        public static Dictionary<string, object> ParseMinioMetrics(string metricsLog)
        {
            var result = new Dictionary<string, object>();

            // Extract free bytes usable
            var freeBytes = ExtractMetricValue(metricsLog, @"minio_cluster_capacity_usable_free_bytes\{[^}]*\}\s+([\d.e+]+)");
            if (freeBytes.HasValue)
            {
                result["FreeBytesUsable"] = freeBytes.Value;
            }

            // Extract S3 requests pending (waiting)
            var requestsPending = ExtractMetricValue(metricsLog, @"minio_s3_requests_waiting_total\{[^}]*\}\s+([\d.e+]+)");
            if (requestsPending.HasValue)
            {
                result["S3RequestsPending"] = requestsPending.Value;
            }

            // Extract health status
            var healthStatus = ExtractMetricValue(metricsLog, @"minio_cluster_health_status\{[^}]*\}\s+([\d.e+]+)");
            if (healthStatus.HasValue)
            {
                result["HealthStatus"] = healthStatus.Value;
                result["HealthStatusDescription"] = healthStatus.Value == 1 ? "Healthy" : "Unhealthy";
            }

            // Extract S3 requests rejected (sum of all rejection types)
            var rejectedAuth = ExtractMetricValue(metricsLog, @"minio_s3_requests_rejected_auth_total\{[^}]*\}\s+([\d.e+]+)") ?? 0;
            var rejectedHeader = ExtractMetricValue(metricsLog, @"minio_s3_requests_rejected_header_total\{[^}]*\}\s+([\d.e+]+)") ?? 0;
            var rejectedInvalid = ExtractMetricValue(metricsLog, @"minio_s3_requests_rejected_invalid_total\{[^}]*\}\s+([\d.e+]+)") ?? 0;
            var rejectedTimestamp = ExtractMetricValue(metricsLog, @"minio_s3_requests_rejected_timestamp_total\{[^}]*\}\s+([\d.e+]+)") ?? 0;

            result["S3RequestsRejectedAuth"] = rejectedAuth;
            result["S3RequestsRejectedHeader"] = rejectedHeader;
            result["S3RequestsRejectedInvalid"] = rejectedInvalid;
            result["S3RequestsRejectedTimestamp"] = rejectedTimestamp;
            result["S3RequestsRejectedTotal"] = rejectedAuth + rejectedHeader + rejectedInvalid + rejectedTimestamp;

            return result;
        }

        private static double? ExtractMetricValue(string metricsLog, string pattern)
        {
            var match = Regex.Match(metricsLog, pattern, RegexOptions.Multiline);
            if (match.Success && match.Groups.Count > 1)
            {
                if (double.TryParse(match.Groups[1].Value, out double value))
                {
                    return value;
                }
            }
            return null;
        }


        private void processServerHealthCheck(string serverLogText)
        {
            var logLines = serverLogText.Split(new[] { '\r', '\n' });

        }


    }

    public class MinioServerHealth
    {

        public enum ServerHealthEnum
        {
            RUNNING,
            NOT_RUNNING
        }

        public ServerHealthEnum ServerHealth;
        public string IncomingS3Requests;
        //public string NoOfUnsendMessagesInQueue;
        public string MemoryAvailable;
        public string RejectedS3Requests;
        public string S3RequestsInQueue;

        public MinioServerHealth(string serverUrl)
        {

        }

        //private Dictionary<string,string>

        public void ParseServerLogText(string logText)
        {
            string[] logTextArray = logText.Split(new[] { '\r', '\n' }).ToArray();
            var extractedData = ParsePrometheusMetrics(logTextArray);

        }


        public class PrometheusMetric
        {
            public string MetricName { get; set; }
            public string Labels { get; set; }
            public string Value { get; set; }
        }

        public List<PrometheusMetric> ParsePrometheusMetrics(string[] lines)
        {
            var metrics = new List<PrometheusMetric>();

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    continue;
                }

                var metric = new PrometheusMetric();

                if (line.Contains("{"))
                {
                    int openBrace = line.IndexOf('{');
                    int closeBrace = line.IndexOf('}');

                    metric.MetricName = line.Substring(0, openBrace);
                    metric.Labels = line.Substring(openBrace + 1, closeBrace - openBrace - 1);
                    metric.Value = line.Substring(closeBrace + 1).Trim();
                }
                else
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    metric.MetricName = parts[0];
                    metric.Labels = null;
                    metric.Value = parts.Length > 1 ? parts[1] : null;
                }

                metrics.Add(metric);
            }

            return metrics;
        }
    }

}
