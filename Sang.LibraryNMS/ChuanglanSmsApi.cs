using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Sang.LibraryNMS
{
    public class ChuanglanSmsApi
    {
        private readonly ChuanglanSmsApiOptions _options;
        private readonly HttpClient _client;
        private readonly ILogger<ChuanglanSmsApi> _logger;

        public ChuanglanSmsApi(IOptions<ChuanglanSmsApiOptions> options, HttpClient client, ILogger<ChuanglanSmsApi> logger)
        {
            _options = options.Value;
            _client = client;
            _logger = logger;
        }

        public async Task<ChuanglanSmsResponse> SendSms(string mobile, string msg, bool needstatus = true)
        {
            var postArr = new
            {
                account = _options.ApiAccount,
                password = _options.ApiPassword,
                msg,
                phone = mobile,
                report = needstatus.ToString().ToLower()
            };

            var result = await PostAsync(_options.ApiSendUrl, postArr);
            return result;
        }

        public async Task<ChuanglanSmsResponse> SendVariableSms(string msg, string[] parameters)
        {
            var postArr = new
            {
                account = _options.ApiAccount,
                password = _options.ApiPassword,
                msg,
                @params = string.Join(",", parameters),
                report = "true"
            };

            var result = await PostAsync(_options.ApiVariableUrl, postArr);
            return result;
        }

        public async Task<ChuanglanSmsResponse> QueryBalance()
        {
            var postArr = new
            {
                account = _options.ApiAccount,
                password = _options.ApiPassword
            };

            var result = await PostAsync(_options.ApiBalanceQueryUrl, postArr);
            return result;
        }

        private async Task<ChuanglanSmsResponse> PostAsync(string url, object data)
        {
            var content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var res = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"请求成功: {res}");
                try
                {
                    var result = JsonSerializer.Deserialize<ChuanglanSmsResponse>(res);
                    if (result != null)
                        return result;
                    return new ChuanglanSmsResponse
                    {
                        code = "-1",
                        errorMsg = "JSON 反序列化失败"
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"JSON 反序列化失败: {ex.Message}");
                    return new ChuanglanSmsResponse
                    {
                        code = "-1",
                        errorMsg = "JSON 反序列化失败"
                    };
                }
            }
            else
            {
                _logger.LogError($"请求状态 {response.StatusCode} {response.ReasonPhrase}");
                return new ChuanglanSmsResponse
                {
                    code = "-1",
                    errorMsg = response.ReasonPhrase
                };
            }
        }
    }


    /// <summary>
    /// 创蓝接口返回内容，融合了余额查询和短信发送的返回内容
    /// </summary>
    public record class ChuanglanSmsResponse
    {
        /// <summary>
        /// 提交响应状态码，返回“0”表示提交成功（详细参考提交响应状态码）
        /// </summary>
        public string? code { get; set; }

        /// <summary>
        /// 消息 id（32 位纯数字）
        /// </summary>
        public string? msgId { get; set; }

        /// <summary>
        /// 响应时间
        /// </summary>
        public string? time { get; set; }

        /// <summary>
        /// 状态码说明（提交成功返回空）
        /// </summary>
        public string? errorMsg { get; set; }

        /// <summary>
        /// 剩余可用条数
        /// </summary>
        public string? balance { get; set; }

        /// <summary>
        /// 失败条数
        /// </summary>
        public string? failNum { get; set; }

        /// <summary>
        /// 成功条数
        /// </summary>
        public string? successNum { get; set; }
    }

}