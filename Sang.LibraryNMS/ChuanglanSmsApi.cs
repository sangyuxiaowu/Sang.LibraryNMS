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
                _logger.LogInformation($"����ɹ�: {res}");
                try
                {
                    var result = JsonSerializer.Deserialize<ChuanglanSmsResponse>(res);
                    if (result != null)
                        return result;
                    return new ChuanglanSmsResponse
                    {
                        code = "-1",
                        errorMsg = "JSON �����л�ʧ��"
                    };
                }
                catch (JsonException ex)
                {
                    _logger.LogError($"JSON �����л�ʧ��: {ex.Message}");
                    return new ChuanglanSmsResponse
                    {
                        code = "-1",
                        errorMsg = "JSON �����л�ʧ��"
                    };
                }
            }
            else
            {
                _logger.LogError($"����״̬ {response.StatusCode} {response.ReasonPhrase}");
                return new ChuanglanSmsResponse
                {
                    code = "-1",
                    errorMsg = response.ReasonPhrase
                };
            }
        }
    }


    /// <summary>
    /// �����ӿڷ������ݣ��ں�������ѯ�Ͷ��ŷ��͵ķ�������
    /// </summary>
    public record class ChuanglanSmsResponse
    {
        /// <summary>
        /// �ύ��Ӧ״̬�룬���ء�0����ʾ�ύ�ɹ�����ϸ�ο��ύ��Ӧ״̬�룩
        /// </summary>
        public string? code { get; set; }

        /// <summary>
        /// ��Ϣ id��32 λ�����֣�
        /// </summary>
        public string? msgId { get; set; }

        /// <summary>
        /// ��Ӧʱ��
        /// </summary>
        public string? time { get; set; }

        /// <summary>
        /// ״̬��˵�����ύ�ɹ����ؿգ�
        /// </summary>
        public string? errorMsg { get; set; }

        /// <summary>
        /// ʣ���������
        /// </summary>
        public string? balance { get; set; }

        /// <summary>
        /// ʧ������
        /// </summary>
        public string? failNum { get; set; }

        /// <summary>
        /// �ɹ�����
        /// </summary>
        public string? successNum { get; set; }
    }

}