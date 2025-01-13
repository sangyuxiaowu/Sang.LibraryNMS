using System.Text;
using System.Text.Json;

namespace Sang.LibraryNMS
{
    /// <summary>
    /// 消息通知
    /// </summary>
    public class WebHock : IWebHock
    {
        private readonly HttpClient _client;
        private readonly ILogger<WebHock> _logger;

        public WebHock(HttpClient client, ILogger<WebHock> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<bool> SendMessageAsync(string webhock, string message)
        {
            var post = new
            {
                msgtype = "markdown",
                markdown = new
                {
                    content = message
                }
            };
            return await SendMessageAsync(webhock, post);
        }

        public async Task<bool> SendMessageAsync(string webhock, object message)
        {
            if (string.IsNullOrWhiteSpace(webhock) || message == null)
            {
                _logger.LogWarning("SendMessageAsync webhock or message is null");
                return false;
            }
            try
            {

                var json = JsonSerializer.Serialize(message);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var resault = await _client.PostAsync(webhock, content);
                if (!resault.IsSuccessStatusCode)
                {
                    _logger.LogError($"SendMessageAsync {resault.StatusCode} {resault.ReasonPhrase}");
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "SendMessageAsync");
                return false;
            }
        }
    }
}
