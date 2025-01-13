namespace Sang.LibraryNMS
{
    /// <summary>
    /// 密钥配置
    /// </summary>
    public sealed class AccessOption
    {
        /// <summary>
        /// 访问密钥
        /// </summary>
        public string AK { get; set; }

        /// <summary>
        /// 安全密钥
        /// </summary>
        public string SK { get; set; }
    }

    /// <summary>
    /// 创蓝短信API配置
    /// </summary>
    public sealed class ChuanglanSmsApiOptions
    {
        /// <summary>
        /// API发送URL
        /// </summary>
        public string ApiSendUrl { get; set; }

        /// <summary>
        /// API变量URL
        /// </summary>
        public string ApiVariableUrl { get; set; }

        /// <summary>
        /// API余额查询URL
        /// </summary>
        public string ApiBalanceQueryUrl { get; set; }

        /// <summary>
        /// API账号
        /// </summary>
        public string ApiAccount { get; set; }

        /// <summary>
        /// API密码
        /// </summary>
        public string ApiPassword { get; set; }
    }
}
