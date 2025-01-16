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

            var url = $"https://qyapi.weixin.qq.com/cgi-bin/gettoken?corpid={_options.CorpId}&corpsecret={_options.CorpSecret}";
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


        /// <summary>
        /// 构造网页授权链接
        /// https://developer.work.weixin.qq.com/document/path/91022
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <param name="state"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public string BuildWebAuthUrl(string redirectUri, string state, string scope = "snsapi_base")
        {
            return $"https://open.weixin.qq.com/connect/oauth2/authorize?appid={_options.CorpId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}&agentid={_options.AgentId}#wechat_redirect";
        }

        /// <summary>
        /// 构造web登录链接
        /// https://developer.work.weixin.qq.com/document/path/98152
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public string BuildWebLoginUrl(string redirectUri, string state)
        {
            return $"https://login.work.weixin.qq.com/wwlogin/sso/login?login_type=CorpApp&appid={_options.CorpId}&agentid={_options.AgentId}&redirect_uri={redirectUri}&state={state}";
        }


        /// <summary>
        /// 获取web登录用户信息
        /// https://developer.work.weixin.qq.com/document/path/98176
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<WebLoginUserInfo?> GetWebLoginUserInfo(string code)
        {
            var accessToken = await GetAccessToken();
            if (accessToken is null)
            {
                _logger.LogError("获取 AccessToken 失败");
                return null;
            }
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/auth/getuserinfo?access_token={accessToken.access_token}&code={code}";
            var response = await _client.GetStringAsync(url);
            var user = JsonSerializer.Deserialize<WebLoginUserInfo>(response);
            if (user is null || user.errcode!=0)
            {
                _logger.LogError($"获取用户信息失败，{response}");
            }
            return user;
        }


        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public async Task<WxWorkUser?> GetUser(string userid)
        {
            var accessToken = await GetAccessToken();
            if (accessToken is null)
            {
                _logger.LogError("获取 AccessToken 失败");
                return null;
            }
            var url = $"https://qyapi.weixin.qq.com/cgi-bin/user/get?access_token={accessToken.access_token}&userid={userid}";
            var response = await _client.GetStringAsync(url);
            var user = JsonSerializer.Deserialize<WxWorkUser>(response);
            if (user is null || user.errcode != 0)
            {
                _logger.LogError($"获取用户信息失败，{response}");
                return null;
            }
            return user;
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

    /// <summary>
    /// 企微web登录用户信息
    /// </summary>
    public record class WebLoginUserInfo : BackJson
    {
        /// <summary>
        /// 成员UserID
        /// </summary>
        public string? userid { get; set; }

        /// <summary>
        /// 非企业成员的标识，对当前企业唯一。不超过64字节
        /// </summary>
        public string? openid { get; set; }

        /// <summary>
        /// 外部联系人id，当且仅当用户是企业的客户，且跟进人在应用的可见范围内时返回
        /// </summary>
        public string? external_userid { get; set; }
    }

    /// <summary>
    /// 企业用户信息
    /// </summary>
    public record class WxWorkUser : BackJson
    {
        /// <summary>
        /// 成员UserID
        /// </summary>
        public string? userid { get; set; }

        /// <summary>
        /// 成员名称
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// 成员所属部门id列表
        /// </summary>
        public int[]? department { get; set; }

        /// <summary>
        /// 职务信息
        /// </summary>
        public string? position { get; set; }

        /// <summary>
        /// 手机号码
        /// </summary>
        public string? mobile { get; set; }

        /// <summary>
        /// 性别。0表示未定义，1表示男性，2表示女性
        /// </summary>
        public string? gender { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? email { get; set; }

        /// <summary>
        /// 头像url
        /// </summary>
        public string? avatar { get; set; }

        /// <summary>
        /// 别名
        /// </summary>
        public string? alias { get; set; }

        /// <summary>
        /// 激活状态: 1=已激活，2=已禁用，4=未激活，5=退出企业。
        /// </summary>
        public int? status { get; set; }

        /// <summary>
        /// 员工个人二维码，扫描可添加为外部联系人
        /// </summary>
        public string? qr_code { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string? address { get; set; }
    }

    /// <summary>
    /// AccessToken 响应
    /// </summary>
    public record class AccessTokenResponse : BackJson
    {
        /// <summary>
        /// 获取到的凭证
        /// </summary>
        public string? access_token { get; set; }

        /// <summary>
        /// 凭证有效时间，单位：秒
        /// </summary>
        public int expires_in { get; set; }
    }

    /// <summary>
    /// 返回结果
    /// </summary>
    public record class BackJson
    {
        /// <summary>
        /// 返回码
        /// </summary>
        public int? errcode { get; set; }

        /// <summary>
        /// 对返回码的文本描述内容
        /// </summary>
        public string? errmsg { get; set; }
    }


    /// <summary>
    /// 微信企业号配置
    /// </summary>
    public sealed class WxWorkApiOptions
    {
        /// <summary>
        /// 企业应用的id
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// 企业 ID
        /// </summary>
        public required string CorpId { get; set; }

        /// <summary>
        /// 应用的凭证密钥
        /// </summary>
        public required string CorpSecret { get; set; }
    }
}