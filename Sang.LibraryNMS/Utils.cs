namespace Sang.LibraryNM
{
    public class Utils
    {

        /// <summary>
        /// 文本通知模版卡片
        /// </summary>
        /// <param name="app_title">来源名称</param>
        /// <param name="title">一级标题</param>
        /// <param name="desc">一级标题辅助信息</param>
        /// <param name="emphasis_title">关键数据</param>
        /// <param name="emphasis_desc">关键数据介绍</param>
        /// <param name="info_title">引用文本标题</param>
        /// <param name="info">引用文本介绍内容</param>
        /// <param name="sub_title_text">二级标题</param>
        /// <param name="list">列表键值对</param>
        /// <param name="card_action">卡片点击动作，1 跳转网页，2 跳转小程序</param>
        /// <param name="url">跳转地址</param>
        /// <param name="appid">小程序appid</param>
        /// <param name="desc_color">来源文字的颜色，0(默认) 灰色，1 黑色，2 红色，3 绿色</param>
        /// <param name="icon_url">来源名称图标</param>
        /// <returns>json object</returns>
        public static object makeMessage(
            string app_title, string title, string desc,
            string emphasis_title, string emphasis_desc,
            string info_title, string info,
            string sub_title_text, Dictionary<string, string> list,
            int card_action, string url, string appid,
            int desc_color = 0, string icon_url = "https://wx.qlogo.cn/mmhead/Q3auHgzwzM7PcBw1Xkibm1Bb1lHJ8GqFFpCbuyuUhQ5V7uA51houlYA/96")
        {
            var horizontal_content_list = new List<object>();
            foreach (var item in list)
            {
                horizontal_content_list.Add(new
                {
                    keyname = item.Key,
                    value = item.Value
                });
            }
            return new
            {
                msgtype = "template_card",
                template_card = new
                {
                    card_type = "text_notice",
                    source = new
                    {
                        icon_url,
                        desc = app_title,
                        desc_color
                    },
                    main_title = new
                    {
                        title,
                        desc
                    },
                    emphasis_content = new
                    {
                        title = emphasis_title,
                        desc = emphasis_desc
                    },
                    quote_area = new
                    {
                        title = info_title,
                        quote_text = info
                    },
                    sub_title_text,
                    horizontal_content_list,
                    card_action = new
                    {
                        type = card_action,
                        url,
                        appid
                    }
                }
            };
        }
    }
}
