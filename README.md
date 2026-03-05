# Sang.LibraryNMS

一个用于消息通知与告警的 .NET 类库，支持 Webhook 推送、语音外呼、企业微信（WeCom）应用消息、JS-SDK 签名等功能。

**目标框架**：`net8.0` / `net9.0`

---

# 功能概览

## Webhook（`WebHock` / `IWebHock`）

通用 HTTP JSON POST 推送工具，可向任意 Webhook 地址发送消息。

- `SendMessageAsync(string webhock, string message)`：将文本包装为 `{ msgtype="markdown", markdown={content=...} }` 后发送。
- `SendMessageAsync(string webhock, object message)`：将任意对象序列化为 JSON 后 POST 发送。

## 语音外呼（`CallPhone` / `ICallPhone` + `AccessOption`）

通过阿里云等语音 API 发起语音外呼通知，需配置 `AccessOption`（`AK` / `SK`）。

## 企业微信/企微 API（`WxWorkApi`、`WxWorkUtils`）

封装企业微信常用接口：

- `WxWorkApi`：AccessToken、JsApiTicket、网页授权、应用消息发送等。
- `WxWorkUtils`：模板卡片 payload 构建、JS-SDK 签名生成。

---

# 安装

若已发布至 NuGet，可通过以下命令安装：

```bash
dotnet add package Sang.LibraryNMS
```

若未发布，可直接引用项目：

```xml
<ProjectReference Include="..\Sang.LibraryNMS\Sang.LibraryNMS.csproj" />
```

---

# 快速开始

## 注入服务（Program.cs / Startup.cs）

```csharp
using Sang.LibraryNMS;

// 注册 WebHock
builder.Services.AddHttpClient<IWebHock, WebHock>();

// 注册企业微信 API
builder.Services.Configure<WxWorkApiOptions>(builder.Configuration.GetSection("WxWork"));
builder.Services.AddHttpClient<WxWorkApi>();
```

## Webhook 推送 Markdown 消息

```csharp
public class NotifyService
{
    private readonly IWebHock _webHock;

    public NotifyService(IWebHock webHock)
    {
        _webHock = webHock;
    }

    public async Task SendAlert(string webhookUrl, string text)
    {
        // 发送 Markdown 格式消息（自动封装为 msgtype=markdown）
        bool ok = await _webHock.SendMessageAsync(webhookUrl, text);
    }
}
```

## 企微签名 GetConfigSignature

用于前端 `wx.config` / `wx.agentConfig` 初始化时获取签名参数：

```csharp
public class JsSignController : ControllerBase
{
    private readonly WxWorkApi _wxWorkApi;

    public JsSignController(WxWorkApi wxWorkApi)
    {
        _wxWorkApi = wxWorkApi;
    }

    [HttpGet("signature")]
    public async Task<IActionResult> GetSignature([FromQuery] string url)
    {
        ConfigSignature? sig = await _wxWorkApi.GetConfigSignature(url);
        // sig.corpId, sig.timestamp, sig.nonceStr, sig.signature
        return Ok(sig);
    }
}
```

返回字段说明：

| 字段 | 说明 |
|------|------|
| `corpId` | 企业 ID |
| `timestamp` | 时间戳（Unix 秒） |
| `nonceStr` | 随机字符串 |
| `signature` | SHA1 签名，用于 `wx.config` / `wx.agentConfig` |

## 发送应用文本消息 SendAppTextMessage

```csharp
// 向全部成员发送文本消息
BackJson result = await _wxWorkApi.SendAppTextMessage("服务器告警：CPU 超过 90%");

// 向指定用户发送
BackJson result2 = await _wxWorkApi.SendAppTextMessage(
    message: "部署完成",
    touser: "zhangsan|lisi"
);
```

## 模板卡片 WxWorkUtils.makeMessage

`WxWorkUtils.makeMessage(...)` 返回符合企业微信"文本通知型模板卡片"格式的匿名对象（`object`），配合 `WebHock.SendMessageAsync(string webhock, object message)` 即可发送。

> 注意：`WebHock` 只负责 POST JSON，**不**负责获取 token 或拼接 URL。

### 完整示例

```csharp
public class TemplateCardService
{
    private readonly WxWorkApi _wxWorkApi;
    private readonly IWebHock _webHock;

    public TemplateCardService(WxWorkApi wxWorkApi, IWebHock webHock)
    {
        _wxWorkApi = wxWorkApi;
        _webHock = webHock;
    }

    public async Task SendTemplateCardAsync()
    {
        // 1. 获取 AccessToken
        var tokenResp = await _wxWorkApi.GetAccessToken();
        if (tokenResp == null) return;

        // 2. 拼接发送 URL
        string sendUrl = $"https://qyapi.weixin.qq.com/cgi-bin/message/send?access_token={tokenResp.access_token}";

        // 3. 构建模板卡片 payload
        var payload = WxWorkUtils.makeMessage(
            app_title: "监控系统",
            title: "服务器告警",
            desc: "高优先级",
            emphasis_title: "99%",
            emphasis_desc: "CPU 使用率",
            info_title: "告警说明",
            info: "服务器 CPU 持续超过阈值，请尽快处理。",
            sub_title_text: "详细信息",
            list: new Dictionary<string, string>
            {
                { "主机", "web-server-01" },
                { "时间", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            },
            card_action: 1,
            url: "https://example.com/monitor",
            appid: ""
        );

        // 还需要在 payload 中指定 touser / toparty / agentid，
        // 此处直接构造完整 message 对象发送：
        var message = new
        {
            touser = "@all",
            agentid = 1000001, // 替换为实际 AgentId
            msgtype = "template_card",
            template_card = ((dynamic)payload).template_card
        };

        // 4. 使用 WebHock 发送（POST JSON）
        bool ok = await _webHock.SendMessageAsync(sendUrl, message);
    }
}
```

---

# 配置说明

## WxWorkApiOptions

在 `appsettings.json` 中添加：

```json
{
  "WxWork": {
    "CorpId": "ww企业ID",
    "CorpSecret": "应用凭证密钥",
    "AgentId": 1000001
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `CorpId` | `string` | 企业 ID，在企业微信管理后台获取 |
| `CorpSecret` | `string` | 应用的凭证密钥 |
| `AgentId` | `int` | 企业应用的 ID |

## AccessOption

语音外呼所需密钥配置：

```json
{
  "Access": {
    "AK": "访问密钥",
    "SK": "安全密钥"
  }
}
```

---

# 企业微信（WeCom）详细说明

## WxWorkApi 主要方法

### `GetAccessToken()`

获取企业微信 AccessToken，内部会缓存结果，在过期前自动复用（提前 10 秒刷新）。

```csharp
var token = await _wxWorkApi.GetAccessToken();
// token.access_token — 凭证字符串
// token.expires_in   — 有效期（秒）
```

### `GetJsApiTicket()`

获取 `jsapi_ticket`，内部依赖 `GetAccessToken()`，同样带缓存。

```csharp
var ticket = await _wxWorkApi.GetJsApiTicket();
// ticket.ticket — JS-SDK 票据
```

### `GetConfigSignature(string url)`

一键生成前端 JS-SDK 所需的签名参数，内部流程：

1. 调用 `GetJsApiTicket()` 获取 ticket
2. 通过 `Utils.GenerateNonceStr()` 生成随机字符串
3. 获取当前 Unix 时间戳
4. 调用 `WxWorkUtils.makeJsSignature(ticket, timestamp, nonce, url)` 计算签名

```csharp
var sig = await _wxWorkApi.GetConfigSignature("https://example.com/page");
// 返回 ConfigSignature { corpId, timestamp, nonceStr, signature }
```

### `BuildWebAuthUrl(string redirectUri, string state, string scope)`

构造网页授权链接（用于获取企业成员身份），scope 默认 `snsapi_base`。

### `BuildWebLoginUrl(string redirectUri, string state)`

构造企业微信 Web 登录链接（适用于 PC 端 SSO 登录场景）。

### `GetWebLoginUserInfo(string code)`

用 OAuth code 换取登录用户信息（`userid` / `openid`）。

### `GetUser(string userid)`

根据 `userid` 获取企业成员详细信息（姓名、部门、手机、邮箱等）。

### `SendAppTextMessage(string message, string? touser, string? toparty, string? totag)`

发送应用文本消息，若 `touser`/`toparty`/`totag` 均为 null，则默认发送给 `@all`。

---

## JS-SDK 签名说明

`WxWorkUtils.makeJsSignature(ticket, timestamp, nonce, url)` 按以下格式拼接字符串后进行 SHA1 哈希：

```
jsapi_ticket=TICKET&noncestr=NONCE&timestamp=TIMESTAMP&url=URL
```

> 参数必须按字母序（jsapi_ticket → noncestr → timestamp → url）排列，且 `url` 必须与当前页面完整 URL 一致（含参数，不含 `#` 后内容）。

前端 `wx.config` 初始化示例：

```javascript
wx.config({
  beta: true,
  debug: false,
  appId: data.corpId,
  timestamp: data.timestamp,
  nonceStr: data.nonceStr,
  signature: data.signature,
  jsApiList: ['onMenuShareAppMessage']
});
```

---

# 注意事项

- `SendMessageAsync` 在 `webhock` 为空或 `message` 为 null 时返回 `false`，不抛出异常。
- 企业微信 AccessToken 和 jsapi_ticket 均有 7200 秒有效期，`WxWorkApi` 内部已做内存缓存（提前 10 秒过期），多实例部署时请注意缓存一致性（建议注册为 Scoped 或 Singleton，并搭配分布式缓存）。
- `WxWorkUtils.makeMessage(...)` 返回的是包含完整 `msgtype=template_card` 结构的匿名对象，需结合正确的 `touser`/`agentid` 字段才能成功调用企业微信消息发送接口。

---

# License

本项目遵循 [LICENSE.txt](LICENSE.txt) 中的许可协议。

