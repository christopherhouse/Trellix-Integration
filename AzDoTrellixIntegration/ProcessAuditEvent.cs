using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzDoTrellixIntegration
{
    public class ProcessAuditEvent
    {
        private readonly ILogger<ProcessAuditEvent> _logger;
        private readonly HttpClient _httpClient;
        private readonly Uri _webHookUri;

        public ProcessAuditEvent(ILogger<ProcessAuditEvent> logger,
            HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;

            _webHookUri = new Uri(Environment.GetEnvironmentVariable("webhookEndpoint")!);
        }

        [Function(nameof(ProcessAuditEvent))]
        public async Task Run(
            [ServiceBusTrigger("audit", Connection = "serviceBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            var json = message.Body.ToString();

            try
            {
                var content = new StringContent(json, new MediaTypeHeaderValue("application/json", "utf8"));

                var response = await _httpClient.PostAsync(_webHookUri, content);
                response.EnsureSuccessStatusCode();

                // Complete the message
                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to send message to webhook");
                // Webhook call failed, send the message back to the queue for another try
                await messageActions.AbandonMessageAsync(message);
            }
        }
    }
}
