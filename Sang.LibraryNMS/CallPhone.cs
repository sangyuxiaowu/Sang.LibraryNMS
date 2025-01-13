using Microsoft.Extensions.Options;
using Sang.AliyunUrlHMAC;
using System.Text.Json;

namespace Sang.LibraryNMS
{

    //文档信息 https://help.aliyun.com/document_detail/393519.htm?spm=a2c4g.11186623.0.0.34476cf0ztBSSE#api-detail-35
    // 

    public class CallPhone : ICallPhone
    {

        private readonly AccessOption _acc;
        private readonly HttpClient _client;
        private readonly ILogger<CallPhone> _logger;
        public CallPhone(IOptions<AccessOption> acc, ILogger<CallPhone> logger, HttpClient client)
        {
            _acc = acc.Value;
            _client = client;
            _logger = logger;
        }

        public async Task MakeCallAsync(string phone, int code, string tts)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                _logger.LogWarning("MakeCallAsync phone is null");
                return;
            }
            var al = new AliyunUrl(_acc.AK, _acc.SK);
            var parameters = new Dictionary<string, string>
            {
                {"Action", "SingleCallByTts"},
                {"CalledNumber", phone},
                {"TtsCode", tts},
                {"TtsParam", JsonSerializer.Serialize(new {code = code})}
            };
            try
            {
                await _client.GetAsync(al.SignUrl("https://dyvmsapi.aliyuncs.com/", parameters, HttpMethod.Get));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "MakeCallAsync");
            }
        }
    }
}
