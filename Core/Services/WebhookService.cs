using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    public interface IWebhookService
    {
        Task SendWebhookAsync(string url, string token, VersioningResult result);
    }

    public class WebhookService : IWebhookService
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public WebhookService(ILogger logger, HttpClient? httpClient = null)
        {
            _logger = logger;
            _httpClient = httpClient ?? new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        public async Task SendWebhookAsync(string url, string token, VersioningResult result)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                _logger.Debug("Webhook URL not provided, skipping webhook notification");
                return;
            }

            try
            {
                var payload = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                var content = new StringContent(payload, Encoding.UTF8, "application/json");

                // Add HMAC signature if token is provided
                // Note: Custom headers should be added to request headers, not content headers
                // But for webhook signatures, we'll add it to content headers for now
                // In production, consider using request headers if the webhook receiver expects it there
                if (!string.IsNullOrWhiteSpace(token))
                {
                    var signature = GenerateHmacSignature(payload, token);
                    content.Headers.Add("X-Versioner-Signature", signature);
                }

                // Send webhook asynchronously (non-blocking)
                // Use Task.Run to avoid blocking, but don't await to keep it non-blocking
                var webhookTask = Task.Run(async () =>
                {
                    try
                    {
                        var response = await _httpClient.PostAsync(url, content);
                        if (response.IsSuccessStatusCode)
                        {
                            _logger.Debug("Webhook notification sent successfully to {url}", url);
                        }
                        else
                        {
                            _logger.Warning("Webhook notification failed with status {status} for {url}", response.StatusCode, url);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Failed to send webhook notification to {url}", url);
                        // Don't throw - webhook failures should not affect tool execution
                    }
                });
                
                // Fire and forget - don't await to keep it non-blocking
                _ = webhookTask;

                _logger.Debug("Webhook notification queued for {url}", url);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error preparing webhook notification for {url}", url);
                // Don't throw - webhook failures should not affect tool execution
            }
        }

        private string GenerateHmacSignature(string payload, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }

    public class VersioningResult
    {
        public string BuildLabel { get; set; } = string.Empty;
        public string ArtifactVersion { get; set; } = string.Empty;
        public string GitHash { get; set; } = string.Empty;
        public string WorkingFolder { get; set; } = string.Empty;
        public bool IsMonoRepo { get; set; }
        public int ArtifactsVersioned { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

