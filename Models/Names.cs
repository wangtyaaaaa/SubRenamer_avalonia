using System.Text.RegularExpressions;

namespace SubRenamer.Models
{
    /// <summary>
    /// 文件扩展名管理类
    /// </summary>
    internal class Extentions
    {
        /// <summary>
        /// 视频类型标识
        /// </summary>
        public const int VIDEO = 1;
        /// <summary>
        /// 字幕类型标识
        /// </summary>
        public const int SUB = 2;
        /// <summary>
        /// 默认视频扩展名列表
        /// </summary>
        public static string[] video_ext = { "mp4", "mkv" };
        /// <summary>
        /// 默认字幕扩展名列表
        /// </summary>
        public static string[] sub_ext = { "ass", "ssa", "sub", "srt" };

        /// <summary>
        /// 获取指定类型的扩展名字符串（逗号分隔）
        /// </summary>
        /// <param name="type">类型标识（VIDEO或SUB）</param>
        /// <returns>扩展名字符串</returns>
        public static string GetExts(int type)
        {
            string[] strs;
            switch (type)
            {
                case VIDEO:
                    strs = video_ext;
                    break;
                case SUB:
                    strs = sub_ext;
                    break;
                default:
                    return "";
            }
            string result = "";
            foreach (string ext in strs)
            {
                result = result == "" ? ext : result + "," + ext;
            }
            return result;
        }

        /// <summary>
        /// 设置指定类型的扩展名列表
        /// </summary>
        /// <param name="exts">扩展名字符串（逗号分隔）</param>
        /// <param name="type">类型标识（VIDEO或SUB）</param>
        public static void SetExts(string exts, int type)
        {
            string[] strs = exts.Split(',');
            switch (type)
            {
                case VIDEO:
                    video_ext = strs;
                    break;
                case SUB:
                    sub_ext = strs;
                    break;
                default:
                    return;
            }
        }
    }

    /// <summary>
    /// 视频/字幕文件基类，包含文件信息和文件名分割结果
    /// </summary>
    internal class VSFile(FileInfo file)
    {
        /// <summary>
        /// 文件信息
        /// </summary>
        public FileInfo File { get; } = file;

        /// <summary>
        /// 文件名分割后的片段列表（用于匹配和分组）
        /// </summary>
        public List<string> Splited_filename { get; } = Renamer.SplitFileNameForGrouping(file);

        /// <summary>
        /// 解析出的集号
        /// </summary>
        public string? Num { get; set; }

        /// <summary>
        /// 将 VSFile 列表转换为 FileInfo 列表
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        /// <param name="files">VSFile 列表</param>
        /// <returns>FileInfo 列表</returns>
        public static List<FileInfo> FileListTOFileInfoList<T>(IEnumerable<T> files) where T : VSFile
        {
            var result = new List<FileInfo>();
            foreach (var item in files)
            {
                result.Add(item.File);
            }
            return result;
        }
    }

    /// <summary>
    /// 字幕文件类
    /// </summary>
    internal class Sub : VSFile
    {
        public Sub(FileInfo file) : base(file)
        {
        }
    }

    /// <summary>
    /// 视频文件类
    /// </summary>
    internal class Video : VSFile
    {
        public Video(FileInfo file) : base(file)
        {
        }
    }

    /// <summary>
    /// 文件名称管理器，负责收集和管理视频与字幕文件
    /// </summary>
    internal class Names
    {
        /// <summary>
        /// 是否为正则模式
        /// </summary>
        public bool IsRegex { get; }
        /// <summary>
        /// 是否已解析集号
        /// </summary>
        public bool Resolved { get; set; }

        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string path;

        /// <summary>
        /// 正则模式下视频文件名左边固定部分
        /// </summary>
        public string? Video_Left { get; }
        /// <summary>
        /// 正则模式下视频文件名右边固定部分
        /// </summary>
        public string? Video_Right { get; }
        /// <summary>
        /// 正则模式下字幕文件名左边固定部分
        /// </summary>
        public string? Sub_Left { get; }
        /// <summary>
        /// 正则模式下字幕文件名右边固定部分
        /// </summary>
        public string? Sub_Right { get; }

        /// <summary>
        /// 视频文件列表
        /// </summary>
        public List<Video> videos = new List<Video>();

        /// <summary>
        /// 字幕文件列表
        /// </summary>
        public List<Sub> subs = new List<Sub>();
        /// <summary>
        /// 子目录的文件名称管理器列表（递归搜索时使用）
        /// </summary>
        public List<Names> names = new List<Names>();

        /// <summary>
        /// 构造函数（普通模式，非递归）
        /// </summary>
        /// <param name="dInfo">目录信息</param>
        public Names(DirectoryInfo dInfo)
        {
            IsRegex = false;
            path = dInfo.Name;
            SetNames(dInfo, false);
        }

        /// <summary>
        /// 构造函数（普通模式，可递归）
        /// </summary>
        /// <param name="dInfo">目录信息</param>
        /// <param name="recursion">是否递归搜索子目录</param>
        public Names(DirectoryInfo dInfo, bool recursion)
        {
            IsRegex = false;
            path = dInfo.Name;
            SetNames(dInfo, recursion);
        }

        /// <summary>
        /// 构造函数（正则模式）
        /// </summary>
        /// <param name="dInfo">目录信息</param>
        /// <param name="v_left">视频文件名左边固定部分</param>
        /// <param name="v_right">视频文件名右边固定部分</param>
        /// <param name="s_left">字幕文件名左边固定部分</param>
        /// <param name="s_right">字幕文件名右边固定部分</param>
        public Names(DirectoryInfo dInfo, string v_left, string v_right, string s_left, string s_right)
        {
            IsRegex = true;
            path = dInfo.Name;
            Video_Left = v_left;
            Video_Right = v_right;
            Sub_Left = s_left;
            Sub_Right = s_right;
            SetNames2(dInfo);
        }

        /// <summary>
        /// 正则模式下设置文件名
        /// </summary>
        /// <param name="dInfo">目录信息</param>
        private void SetNames2(DirectoryInfo dInfo)
        {
            if (dInfo.Exists)
            {
                string v_patt = "^" + Video_Left + "\\S{1,6}" + Video_Right + "$";
                string s_patt = "^" + Sub_Left + "\\S{1,6}" + Sub_Right + "$";
                try
                {
                    foreach (FileInfo item in dInfo.GetFiles())
                    {
                        string name = item.Name;
                        if (Regex.IsMatch(item.Name, v_patt))
                        {
                            videos.Add(new Video(item));
                        }
                        else if (Regex.IsMatch(item.Name, s_patt))
                        {
                            subs.Add(new Sub(item));
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("匹配错误，请检查表达式\n" + e.Message, e);
                }
            }
        }

        /// <summary>
        /// 获取字幕文件名的正则替换模式
        /// </summary>
        /// <returns>正则表达式</returns>
        internal string GetSubReplasePattern()
        {
            return "(" + Sub_Left + ")|(" + Sub_Right + ")";
        }

        /// <summary>
        /// 获取视频文件名的正则替换模式
        /// </summary>
        /// <returns>正则表达式</returns>
        internal string GetVideoReplasePattern()
        {
            return "(" + Video_Left + ")|(" + Video_Right + ")";
        }

        /// <summary>
        /// 普通模式下设置文件名
        /// </summary>
        /// <param name="dInfo">目录信息</param>
        /// <param name="recursion">是否递归搜索子目录</param>
        private void SetNames(DirectoryInfo dInfo, bool recursion)
        {
            if (dInfo.Exists)
            {
                foreach (FileInfo item in dInfo.GetFiles())
                {
                    if (IsVideo(item))
                    {
                        videos.Add(new Video(item));
                    }
                    else if (IsSub(item))
                    {
                        subs.Add(new Sub(item));
                    }
                }
                if (recursion)
                {
                    foreach (DirectoryInfo dir in dInfo.GetDirectories())
                    {
                        Names name = new Names(dir, true);
                        names.Add(name);
                    }
                }
            }
        }

        /// <summary>
        /// 判断文件是否为字幕文件
        /// </summary>
        /// <param name="item">文件信息</param>
        /// <returns>是否为字幕文件</returns>
        private bool IsSub(FileInfo item)
        {
            return MatchExtebsion(item, Extentions.sub_ext);
        }

        /// <summary>
        /// 判断文件是否为视频文件
        /// </summary>
        /// <param name="item">文件信息</param>
        /// <returns>是否为视频文件</returns>
        private bool IsVideo(FileInfo item)
        {
            return MatchExtebsion(item, Extentions.video_ext);
        }

        /// <summary>
        /// 判断文件扩展名是否匹配指定列表
        /// </summary>
        /// <param name="item">文件信息</param>
        /// <param name="extebsion">扩展名列表</param>
        /// <returns>是否匹配</returns>
        private bool MatchExtebsion(FileInfo item, string[] extebsion)
        {
            foreach (string ext in extebsion)
            {
                if (item.Extension.ToLower() == "." + ext.ToLower()) { return true; }
            }
            return false;
        }

        /// <summary>
        /// 获取视频文件总数（包含子目录）
        /// </summary>
        /// <returns>视频文件总数</returns>
        internal int GetVideoCount()
        {
            int count = videos.Count;
            foreach (Names name in names)
            {
                count += name.GetVideoCount();
            }
            return count;
        }

        /// <summary>
        /// 将视频列表转换为文件名数组
        /// </summary>
        /// <param name="list">视频列表</param>
        /// <returns>文件名数组</returns>
        public static string[] GetStrArray(List<Video> list)
        {
            string[] res = new string[list.Count];

            int i = 0;
            foreach (var item in list)
            {
                res[i++] = item.File.Name;
            }

            return res;
        }
    }
}
