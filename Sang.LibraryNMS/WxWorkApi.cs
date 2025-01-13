using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sang.LibraryNM
{
    public class WxWorkApi
    {
        private readonly HttpClient _client;
        private readonly WxWorkApiOptions _options;
        private readonly ILogger<WxWorkApi> _logger;
        private AccessTokenResponse _acc;
        private DateTime _acc_expires = DateTime.MinValue;

        /// <summary>
        ///  Json 格式化配置，忽略null值
        /// </summary>
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public WxWorkApi(HttpClient client, IOptions<WxWorkApiOptions> options, ILogger<WxWorkApi> logger)
        {
            _client = client;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 获取 AccessToken
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception">WxWorkApiOptions 未配置</exception>
        public async Task<AccessTokenResponse?> GetAccessToken()
        {
            if (_options is null)
                throw new Exception("WxWorkApiOptions is null");

            // 判断是否过期
            if (_acc is not null && _acc_expires > DateTime.Now)
                return _acc;

            var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={_options.Corpid}&corpsecret={_options.Corpsecret}";
            var response = await _client.GetStringAsync(url);
            var access = JsonSerializer.Deserialize<AccessTokenResponse>(response);
            if (access is null)
            {
                _logger.LogError("获取 AccessToken 失败");
                return null;
            }
            _acc = access;
            _acc_expires = DateTime.Now.AddSeconds(access.expires_in - 10);
            return access;
        }


        #region 发送应用消息

        /// <summary>
        /// 发送应用文本消息
        /// </summary>
        /// <param name="agentid">企业应用的id</param>
        /// <param name="message">文本消息内容</param>
        /// <param name="touser">指定接收消息的成员，成员ID列表（多个接收者用‘|’分隔，最多支持1000个）。特殊情况：指定为"@all"，则向该企业应用的全部成员发送</param>
        /// <param name="toparty">指定接收消息的部门，部门ID列表，多个接收者用‘|’分隔，最多支持100个。当touser为"@all"时忽略本参数</param>
        /// <param name="totag">指定接收消息的标签，标签ID列表，多个接收者用‘|’分隔，最多支持100个。当touser为"@all"时忽略本参数</param>
        /// <returns></returns>
        public async Task<BackJson> SendAppTextMessage(string message, string? touser = null, string? toparty = null, string? totag = null)
        {
            // 均为null时发送给全部
            if (touser is null && toparty is null && totag is null)
                touser = "@all";

            var accessToken = await GetAccessToken();
            if (accessToken is null)
            {
                _logger.LogError("获取 AccessToken 失败");
                return new BackJson
                {
                    errcode = -1,
                    errmsg = "获取 AccessToken 失败"
                };
            }

            var url = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={accessToken.access_token}";
            var content = new
            {
                touser = touser,
                toparty = toparty,
                totag = totag,
                msgtype = "text",
                agentid = _options.AgentId,
                text = new
                {
                    content = message
                }
            };
            var json = JsonSerializer.Serialize(content);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, data);
            var result = await response.Content.ReadAsStringAsync();
            _logger.LogInformation(result);
            var back = JsonSerializer.Deserialize<BackJson>(result);
            if (back is null)
            {
                return new BackJson
                {
                    errcode = -1,
                    errmsg = "JSON 反序列化失败"
                };
            }
            return back;
        }




        #endregion

    }


    public record class AccessTokenResponse
    {
        public int? errcode { get; set; }
        public string? errmsg { get; set; }
        public string? access_token { get; set; }
        public int expires_in { get; set; }
    }

    public record class BackJson
    {
        public int errcode { get; set; }
        public string errmsg { get; set; }
    }


    public sealed class WxWorkApiOptions
    {
        public int AgentId { get; set; }
        public string Corpid { get; set; }
        public string Corpsecret { get; set; }
    }
}