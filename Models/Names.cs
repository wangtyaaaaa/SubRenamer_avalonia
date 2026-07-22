using System.Text.RegularExpressions;

namespace SubRenamer.Models
{
    internal class Extentions
    {
        public const int VIDEO = 1;
        public const int SUB = 2;
        public static string[] video_ext = { "mp4", "mkv" };
        public static string[] sub_ext = { "ass", "ssa", "sub", "srt" };

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

    internal class VSFile(FileInfo file)
    {
        public FileInfo File { get; } = file;

        public List<string> Splited_filename { get; } = Renamer.SplitFileNameForGrouping(file);

        public string? Num { get; set; }

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

    internal class Sub : VSFile
    {
        public Sub(FileInfo file) : base(file)
        {
        }
    }

    internal class Video : VSFile
    {
        public Video(FileInfo file) : base(file)
        {
        }
    }

    internal class Names
    {
        public bool IsRegex { get; }
        public bool Resolved { get; set; }

        public string path;

        public string? Video_Left { get; }
        public string? Video_Right { get; }
        public string? Sub_Left { get; }
        public string? Sub_Right { get; }

        public List<Video> videos = new List<Video>();

        public List<Sub> subs = new List<Sub>();
        public List<Names> names = new List<Names>();

        public Names(DirectoryInfo dInfo)
        {
            IsRegex = false;
            path = dInfo.Name;
            SetNames(dInfo, false);
        }

        public Names(DirectoryInfo dInfo, bool recursion)
        {
            IsRegex = false;
            path = dInfo.Name;
            SetNames(dInfo, recursion);
        }

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

        internal string GetSubReplasePattern()
        {
            return "(" + Sub_Left + ")|(" + Sub_Right + ")";
        }

        internal string GetVideoReplasePattern()
        {
            return "(" + Video_Left + ")|(" + Video_Right + ")";
        }

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

        private bool IsSub(FileInfo item)
        {
            return MatchExtebsion(item, Extentions.sub_ext);
        }

        private bool IsVideo(FileInfo item)
        {
            return MatchExtebsion(item, Extentions.video_ext);
        }

        private bool MatchExtebsion(FileInfo item, string[] extebsion)
        {
            foreach (string ext in extebsion)
            {
                if (item.Extension.ToLower() == "." + ext.ToLower()) { return true; }
            }
            return false;
        }

        internal int GetVideoCount()
        {
            int count = videos.Count;
            foreach (Names name in names)
            {
                count += name.GetVideoCount();
            }
            return count;
        }

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
