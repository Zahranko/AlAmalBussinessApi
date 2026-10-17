using AlAmalBusiness.Application.DTOs.WhatsApp;
using AlAmalBusiness.Application.Services.Interface;
using System.Net;
using System.Net.Http.Json;

namespace AlAmalBusiness.Api.WhatsApp
{
    // IWhatsAppSender over the hospital's own WhatsApp platform
    // (D:\whatsapp-platform, https://whatsappapi.alamalhospitaljo.com), not
    // Meta directly: that system already holds the Meta token, the approved
    // templates, the send queue and the webhooks, and its /api/v1 exists for
    // exactly this — other hospital systems sending templates with an
    // "X-Api-Key". The key is created by an admin there (WhatsApp -> API
    // keys) and lives in WhatsApp:ApiKey, injected by deploy.yml from
    // PROD_WHATSAPP_API_KEY; blank means disabled.
    public sealed class WhatsAppPlatformClient : IWhatsAppSender
    {
        private readonly HttpClient _http;
        private readonly ILogger<WhatsAppPlatformClient> _logger;

        public WhatsAppPlatformClient(HttpClient http, IConfiguration config, ILogger<WhatsAppPlatformClient> logger)
        {
            _http = http;
            _logger = logger;
            var key = config["WhatsApp:ApiKey"];
            IsEnabled = !string.IsNullOrWhiteSpace(key) && _http.BaseAddress != null;
            if (IsEnabled) _http.DefaultRequestHeaders.Add("X-Api-Key", key);
        }

        public bool IsEnabled { get; }

        public async Task<WhatsAppBulkResult> SendBulkAsync(WhatsAppBulkMessage message, CancellationToken ct = default)
        {
            if (!IsEnabled) return new() { Error = "WhatsApp is not configured.", Retryable = true };

            HttpResponseMessage response;
            try
            {
                response = await _http.PostAsJsonAsync("api/v1/campaigns", new
                {
                    name = message.Name,
                    template = message.Template,
                    language = message.Language,
                    recipients = message.Recipients.Select(r => new
                    {
                        @ref = r.Ref,
                        phone = r.Phone,
                        name = r.Name,
                        fileNumber = r.FileNumber,
                        body = r.Body
                    })
                }, ct);
            }
            catch (HttpRequestException ex)
            {
                // No connection: nothing reached the platform.
                return new() { Error = $"WhatsApp platform unreachable: {ex.Message}", Retryable = true };
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                // Timed out after sending — it may have been queued.
                return new() { Error = "No answer from the WhatsApp platform; check its Bulk send history before resending." };
            }

            using (response)
            {
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadFromJsonAsync<BulkBody>(ct);
                    return new()
                    {
                        Success = true,
                        CampaignId = body?.CampaignId,
                        Results = (body?.Results ?? new()).Select(l => new WhatsAppBulkLine
                        {
                            Ref = l.Ref,
                            MessageId = l.Id?.ToString(),
                            Error = l.Error
                        }).ToList()
                    };
                }

                var error = await ReadError(response, ct);
                // Throttled or down: try again later. A 4xx is the platform
                // refusing the request (no such template, a malformed list);
                // nothing was queued, so it is safe to send again once fixed.
                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    _logger.LogError("WhatsApp platform rejected the API key ({Status}).", (int)response.StatusCode);
                return new() { Error = error, Retryable = true };
            }
        }

        public async Task<WhatsAppMessageStatus?> GetStatusAsync(string messageId, CancellationToken ct = default)
        {
            if (!IsEnabled) return null;
            try
            {
                using var response = await _http.GetAsync($"api/v1/messages/{Uri.EscapeDataString(messageId)}", ct);
                if (!response.IsSuccessStatusCode) return null;
                var body = await response.Content.ReadFromJsonAsync<StatusBody>(ct);
                return body == null ? null : new WhatsAppMessageStatus { Status = body.Status ?? "", Error = body.Error };
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                return null;
            }
        }

        private static async Task<string> ReadError(HttpResponseMessage response, CancellationToken ct)
        {
            try
            {
                // The platform answers { error } for its own refusals and a
                // problem-details body for model validation.
                var body = await response.Content.ReadFromJsonAsync<ErrorBody>(ct);
                var text = body?.Error ?? body?.Title;
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
            catch
            {
                // Not JSON.
            }
            return $"WhatsApp platform answered {(int)response.StatusCode}.";
        }

        private sealed record BulkLine(string? Ref, string? Phone, long? Id, string? Error);
        private sealed record BulkBody(int? CampaignId, List<BulkLine>? Results);
        private sealed record StatusBody(long Id, string? Status, string? Error);
        private sealed record ErrorBody(string? Error, string? Title);
    }
}
