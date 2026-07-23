using System.Text;
using System.Text.RegularExpressions;

namespace SubRenamer.Models
{
    /// <summary>
    /// 文件重命名器，负责字幕文件的匹配和重命名操作
    /// </summary>
    internal class Renamer
    {
        /// <summary>
        /// 过滤垃圾信息的正则表达式（如编码格式、分辨率、hash值等）
        /// </summary>
        private static readonly string regex = "(10[Bb][Ii][Tt])|([xXhH]26[45])|(\\d+([\\*Xx])\\d+)|([0-9]{2,5}([pP]))|(\\[[0-9a-fA-F]{8}\\])|(YYDM-11FANS)|([a-zA-Z]{2,5}([Rr][Ii][Pp]))|([0-9a-zA-Z_]{6,200})";
        /// <summary>
        /// 集号前缀的正则表达式（如"第"、"話"、"话"、"集"）
        /// </summary>
        private static readonly string regex_headAndTail = "第|話|话|集";
        private static readonly string regex_episode = @"(?i)episode";
        private static readonly string regex_ep = @"(?i)ep";
        /// <summary>
        /// 撤销操作字典，键为旧文件名，值为新文件名
        /// </summary>
        private static readonly Dictionary<string, string> Redo_Log = new Dictionary<string, string>();

        /// <summary>
        /// 重命名字幕文件
        /// 将字幕文件重命名为与视频文件同名（保留字幕扩展名）
        /// </summary>
        /// <param name="video">视频文件</param>
        /// <param name="subs">字幕文件列表</param>
        /// <param name="delimiter">分隔符（用于提取复杂扩展名）</param>
        internal static void RenameSubs(FileInfo video, List<FileInfo> subs, string? delimiter)
        {
            string vname = GetFullNameWithOutExtension(video);
            foreach (FileInfo sub in subs)
            {
                string ext = GetFullExtension(sub, delimiter);
                try
                {
                    string new_name = vname + ext;
                    SetRedoDic(sub.FullName, new_name);
                    sub.MoveTo(new_name);
                }
                catch
                {
                    // 如果重命名失败，使用视频名+原字幕名作为新文件名
                    string new_name = vname + "." + sub.Name;
                    SetRedoDic(sub.FullName, new_name);
                    sub.MoveTo(new_name);
                }
            }
        }

        /// <summary>
        /// 设置撤销字典条目
        /// </summary>
        /// <param name="oldname">旧文件名</param>
        /// <param name="newname">新文件名</param>
        private static void SetRedoDic(string oldname, string newname)
        {
            if (Redo_Log.ContainsKey(oldname))
            {
                _ = Redo_Log.Remove(oldname);
            }
            Redo_Log.Add(oldname, newname);
        }

        /// <summary>
        /// 清空撤销字典
        /// </summary>
        public static void ClearRedoDic()
        {
            Redo_Log.Clear();
        }

        /// <summary>
        /// 执行撤销操作
        /// 将所有重命名的文件恢复到原来的名称
        /// </summary>
        /// <returns>是否撤销成功</returns>
        public static bool Revoke()
        {
            Dictionary<string, string>.Enumerator e = Redo_Log.GetEnumerator();
            while (e.MoveNext())
            {
                string old = e.Current.Key;
                FileInfo newfile = new FileInfo(e.Current.Value);
                if (newfile.Exists)
                {
                    try
                    {
                        newfile.MoveTo(old);
                    }
                    catch
                    {
                        ClearRedoDic();
                        return false;
                    }
                }
            }
            ClearRedoDic();
            return true;
        }

        /// <summary>
        /// 检查是否有可撤销的操作
        /// </summary>
        /// <returns>是否有可撤销操作</returns>
        public static bool IsRedoAvailabel()
        {
            return Redo_Log.Count != 0;
        }

        /// <summary>
        /// 获取不含扩展名的完整文件名
        /// </summary>
        /// <param name="video">文件信息</param>
        /// <returns>不含扩展名的完整路径</returns>
        private static string GetFullNameWithOutExtension(FileInfo video)
        {
            for (int i = video.FullName.Length - 1; i >= 0; i--)
            {
                if (video.FullName[i] == '.')
                {
                    return video.FullName.Substring(0, i);
                }
            }
            return video.FullName;
        }

        /// <summary>
        /// 获取字幕文件的完整扩展名（考虑复杂扩展名情况）
        /// </summary>
        /// <param name="sub">字幕文件</param>
        /// <param name="delimiter">分隔符</param>
        /// <returns>完整扩展名</returns>
        private static string GetFullExtension(FileInfo sub, string? delimiter)
        {
            if (delimiter == null || delimiter.Length == 0)
                return GetFullExtension(sub);
            string name = sub.Name.Trim();
            int index = name.LastIndexOf(delimiter[0]);
            if (index == -1)
                return GetFullExtension(sub);
            return name.Substring(index);
        }

        /// <summary>
        /// 获取字幕文件的完整扩展名
        /// 处理复杂扩展名情况，如 .chs.ass、.eng.srt 等
        /// </summary>
        /// <param name="sub">字幕文件</param>
        /// <returns>完整扩展名</returns>
        private static string GetFullExtension(FileInfo sub)
        {
            string name = sub.Name.Trim();
            char[] cs = name.ToArray();
            List<int> index = new List<int>();
            for (int i = 0; i < cs.Length; i++)
            {
                if (cs[i] == '.')
                {
                    index.Add(i);
                }
            }

            for (int i = 0; i < index.Count; i++)
            {
                if (i == index.Count - 1)
                {
                    return sub.Extension;
                }
                if (index[i + 1] - index[i] <= 10)
                {
                    return name.Substring(index[i]);
                }
                string ext = name.Substring(index[i]);
                string ext2 = Regex.Replace(ext, regex, "");
                if (ext == ext2)
                {
                    return ext;
                }
            }
            return sub.Extension;
        }

        /// <summary>
        /// 使用存储的集号来获取文件列表
        /// </summary>
        /// <param name="list">文件列表</param>
        /// <param name="num">集号</param>
        /// <returns>匹配的文件列表</returns>
        internal static List<T> GetSubListByNum<T>(List<T> list, string num) where T : VSFile
        {
            List<T> result = new List<T>();
            foreach (T file in list)
            {
                if (file.Num == num) result.Add(file);
                else if (file.Num != null && num != null && file.Num.Contains(".") && num.Contains("."))
                {
                    if (
                        double.TryParse(
                            file.Num,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double d1)
                        &&
                        double.TryParse(
                            num,
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double d2)
                        )
                    {
                        if (d1 == d2) result.Add(file);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 根据集号获取匹配的字幕文件列表（模糊匹配）
        /// </summary>
        /// <param name="names">文件名称管理器</param>
        /// <param name="num">集号</param>
        /// <returns>匹配的字幕文件列表</returns>
        internal static List<FileInfo> GetSubList(Names names, string num)
        {
            List<FileInfo> subs = new List<FileInfo>();
            foreach (Sub sub in names.subs)
            {
                if (IsFit(sub.File, num))
                {
                    subs.Add(sub.File);
                }
            }
            return subs;
        }

        /// <summary>
        /// 判断字幕文件是否匹配指定集号
        /// </summary>
        /// <param name="sub">字幕文件</param>
        /// <param name="num">集号</param>
        /// <returns>是否匹配</returns>
        private static bool IsFit(FileInfo sub, string num)
        {
            string? subNum = GetEpisodeNumber(sub);
            if (subNum != null)
            {
                if (subNum == num)
                {
                    return true;
                }
                // 尝试数值比较（处理如 "01" 和 "1" 的情况）
                else if (double.TryParse(subNum, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d1) &&
         double.TryParse(num, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double d2))
                {
                    if (d1 == d2) return true;
                }
            }
            else
            {
                // 如果无法提取集号，尝试在文件名中查找集号
                string name = sub.Name.Replace(sub.Extension, "");
                name = Regex.Replace(name, regex, "");
                if (IsFitNum(name, num))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 在文件名中查找集号（子串匹配）
        /// </summary>
        /// <param name="name">文件名（不含扩展名）</param>
        /// <param name="num">集号</param>
        /// <returns>是否匹配</returns>
        private static bool IsFitNum(string name, string num)
        {
            char[] na = name.ToCharArray();
            char[] nm = num.ToCharArray();
            for (int i = 0; i < na.Length - nm.Length + 1; i++)
            {
                bool ifcontinue = false;
                if (na[i] == nm[0])
                {
                    int j = 1;
                    for (; j < nm.Length; j++)
                    {
                        if (na[i + j] != nm[j])
                        {
                            ifcontinue = true;
                            break;
                        }
                    }
                    if (ifcontinue)
                    {
                        continue;
                    }

                    // 确保集号后面不是数字（避免 "12" 匹配到 "123"）
                    if (i + j < na.Length)
                    {
                        if (na[i + j] >= '0' && na[i + j] <= '9')
                        {
                            continue;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 判断字符串是否可能是集号
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>是否可能是集号</returns>
        internal static bool IsLikelyEpisodeNumber(string str)
        {
            string str2 = ResolveEpisodeNumber(str);

            if (!double.TryParse(str2, out double f))
            {
                return false;
            }

            if (f < 0 || f > 1900)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将文件名分割为片段（用于分组）
        /// 考虑了各种分隔符和数字/字母边界
        /// </summary>
        /// <param name="file">文件信息</param>
        /// <returns>分割后的片段列表</returns>
        internal static List<string> SplitFileNameForGrouping(FileInfo file)
        {
            var filename = file.Name.Replace(file.Extension, "");
            List<string> result = new List<string>();
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < filename.Length; i++)
            {
                char c = filename[i];

                // 处理数字间的点（如 1.2 表示 1集2话）
                if (c == '.' && current.Length > 0 && i + 1 < filename.Length)
                {
                    bool prevIsDigit = char.IsDigit(current[current.Length - 1]);
                    bool nextIsDigit = char.IsDigit(filename[i + 1]);

                    if (prevIsDigit && nextIsDigit)
                    {
                        current.Append(c);
                        current.Append(filename[i + 1]);
                        i++;
                        continue;
                    }
                }

                // 处理常见分隔符
                if (c == ' ' || c == '.' || c == '_' || c == '-' ||
                    c == '[' || c == ']' || c == '(' || c == ')' ||
                    c == '{' || c == '}')
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    // 处理数字/字母边界
                    if (current.Length > 0)
                    {
                        bool lastIsDigit = char.IsDigit(current[current.Length - 1]);
                        bool currentIsDigit = char.IsDigit(c);

                        if (lastIsDigit != currentIsDigit)
                        {
                            result.Add(current.ToString());
                            current.Clear();
                        }
                    }
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        /// <summary>
        /// 解析集号（去除前缀如 "EP"、"Episode"、"第"、"话" 等）
        /// </summary>
        /// <param name="str">输入字符串</param>
        /// <returns>解析后的集号</returns>
        internal static string ResolveEpisodeNumber(string str)
        {
            // 去除 Episode/ep 前缀（大小写不敏感）
            string result = Regex.Replace(str, regex_episode, "");
            result = Regex.Replace(result, regex_ep, "");
            // 去除集号前缀（如"第"、"話"、"话"、"集"）
            result = Regex.Replace(result, regex_headAndTail, "");
            return result;
        }

        /// <summary>
        /// 从文件名中提取集号
        /// </summary>
        /// <param name="video">文件信息</param>
        /// <returns>提取的集号，失败返回 null</returns>
        internal static string? GetEpisodeNumber(FileInfo video)
        {
            string name = (string)video.Name.Clone();
            name = name.Replace(video.Extension, "");
            List<string> strs = Split(name);
            foreach (string str in strs)
            {
                // 去除 ep 前缀（大小写不敏感）
                string str2 = Regex.Replace(str, regex_ep, "");
                // 去除集号前缀
                str2 = Regex.Replace(str2, regex_headAndTail, "");

                // 尝试解析为浮点数
                if (!double.TryParse(str2, out double f))
                {
                    continue;
                }

                // 检查范围（0-1900）
                if (f < 0 || f > 1900)
                {
                    continue;
                }

                return str2;
            }
            return null;
        }

        /// <summary>
        /// 将文件名按括号和空格分割
        /// </summary>
        /// <param name="name">文件名</param>
        /// <returns>分割后的片段列表</returns>
        public static List<string> Split(string name)
        {
            List<string> result = new List<string>();
            string name2 = Replace(name);
            char[] ca = name2.ToCharArray();
            for (int i = 0; i < ca.Length; i++)
            {
                if (ca[i] == ' ')
                {
                    try
                    {
                        int end = FindMatchingPos(ca, i, ' ');
                        result.Add(name2.Substring(i + 1, end - i - 1));
                    }
                    catch
                    {
                        result.Add(name2.Substring(i));
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 将括号替换为空格
        /// </summary>
        /// <param name="name">输入字符串</param>
        /// <returns>替换后的字符串</returns>
        private static string Replace(string name)
        {
            string s = name.Replace('[', ' ');
            s = s.Replace(']', ' ');
            s = s.Replace('(', ' ');
            s = s.Replace(')', ' ');
            s = s.Replace('{', ' ');
            s = s.Replace('}', ' ');
            s = Regex.Replace(s, "[\\s]+", " ");
            return s;
        }

        /// <summary>
        /// 查找匹配的括号位置
        /// </summary>
        /// <param name="ca">字符数组</param>
        /// <param name="begin">开始位置</param>
        /// <param name="left">左括号字符</param>
        /// <returns>匹配的右括号位置</returns>
        private static int FindMatchingPos(char[] ca, int begin, char left)
        {
            char right;
            switch (left)
            {
                case '[':
                    right = ']';
                    break;
                case '(':
                    right = ')';
                    break;
                case '{':
                    right = '}';
                    break;
                case ' ':
                    right = left;
                    break;
                default:
                    throw new Exception("cannot get matching char on RIGHT");
            }
            int count = 0;
            for (int i = begin + 1; i < ca.Length; i++)
            {
                if (ca[i] == right)
                {
                    if (count == 0)
                    {
                        return i;
                    }
                    else
                    {
                        count--;
                    }
                }
                else if (ca[i] == left)
                {
                    count++;
                }
            }
            throw new Exception("cannot find matching pos");
        }

        /// <summary>
        /// 给所有视频匹配字幕，返回列表，列表最后是匹配不到视频的字幕
        /// </summary>
        /// <param name="allVideos">视频文件列表</param>
        /// <param name="subs">字幕文件列表</param>
        /// <returns>配对好的视频字幕组列表</returns>
        internal static List<PairedVSFileGroup> GetPairedVSFileGroups(List<Video> allVideos, List<Sub> subs)
        {
            var result = new List<PairedVSFileGroup>();
            var allSubs = new List<Sub>(subs);
            foreach (var video in allVideos)
            {
                var group = new PairedVSFileGroup(video);
                result.Add(group);
                string? episodeNum = video.Num;
                if (!string.IsNullOrEmpty(episodeNum))
                {
                    var matchedSubs = GetSubListByNum(allSubs, episodeNum);
                    foreach (var sub in matchedSubs)
                    {
                        group.AddSub(sub);
                        allSubs.Remove(sub);
                    }
                }
            }
            var endGroup = new PairedVSFileGroup(null);
            result.Add(endGroup);
            foreach (var sub in allSubs)
            {
                endGroup.AddSub(sub);
            }
            return result;
        }
    }
}