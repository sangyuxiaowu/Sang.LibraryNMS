namespace Sang.LibraryNMS
{
    /// <summary>
    /// 消息通知
    /// </summary>
    public interface IWebHock
    {
        /// <summary>
        /// 发送消息 Markdown 格式
        /// </summary>
        /// <param name="webhock">推送地址</param>
        /// <param name="message">用于微信短信等通知内容</param>
        Task<bool> SendMessageAsync(string webhock, string message);

        /// <summary>
        /// 发送消息 自定义 Json 推送
        /// </summary>
        /// <param name="webhock">推送地址</param>
        /// <param name="message">用于微信短信等通知内容</param>
        Task<bool> SendMessageAsync(string webhock, object message);
    }
}
