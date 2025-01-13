namespace Sang.LibraryNMS
{
    /// <summary>
    /// 消息通知
    /// </summary>
    public interface ICallPhone
    {
        /// <summary>
        /// 打电话
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <param name="code">故障码，验证码等</param>
        /// <param name="tts">已通过审核的语音通知文本转语音模板</param>
        Task MakeCallAsync(string phone, int code, string tts);
    }
}
