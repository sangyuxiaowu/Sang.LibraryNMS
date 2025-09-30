namespace Sang.LibraryNM
{
    public class Utils
    {

        /// <summary>
        /// 生成随机字符
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string GenerateNonceStr(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
