using System.Text.RegularExpressions;

namespace SubRenamer.Models
{
    /// <summary>
    /// 集号解析器，负责从文件名中智能识别和解析集号信息
    /// </summary>
    internal class NumberResolver
    {
        /// <summary>
        /// 文件分组类，用于将相似文件名的文件分组
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        private class VSFileGroup<T> where T : VSFile
        {
            /// <summary>
            /// 组内文件列表
            /// </summary>
            public List<T> FileList { get; }

            /// <summary>
            /// 可能的集号位置列表
            /// </summary>
            public List<int> LikelyEpNumPos { get; }

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="t">初始文件</param>
            public VSFileGroup(T t)
            {
                FileList = new List<T>();
                AddVSFile(t);
                LikelyEpNumPos = GetLikelyEpNumPos(t.Splited_filename);
            }

            /// <summary>
            /// 添加文件到组
            /// </summary>
            /// <param name="file">要添加的文件</param>
            public void AddVSFile(T file)
            {
                FileList.Add(file);
            }
        }

        /// <summary>
        /// 解析视频文件的集号（简单模式）
        /// 通过比较所有视频文件名，找到第一个不同的位置，提取数字作为集号
        /// </summary>
        /// <param name="names">文件名称管理器</param>
        /// <returns>是否解析成功</returns>
        public static bool Resolve(Names names)
        {
            try
            {
                string[] strs = Names.GetStrArray(names.videos);
                int len = strs[0].Length;
                int i = 0;
                // 找到第一个不同的字符位置
                for (; i < len; i++)
                {
                    char c = strs[0][i];
                    bool fl = false;
                    foreach (string s in strs)
                    {
                        if (c != s[i])
                        {
                            fl = true;
                            break;
                        }
                    }
                    if (fl)
                    {
                        break;
                    }
                }
                // 如果所有字符都相同，无法解析
                if (i >= len)
                {
                    return false;
                }

                // 从不同位置开始提取连续数字作为集号
                foreach (Video video in names.videos)
                {
                    string s = video.File.Name;
                    int j = i;
                    for (; j < s.Length; j++)
                    {
                        if (!IsNumber(s[j]))
                        {
                            break;
                        }
                    }
                    if (j == i)
                    {
                        continue;
                    }

                    string s2 = s.Substring(i, j - i);
                    video.Num = s2;
                }

                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 判断字符是否为数字
        /// </summary>
        /// <param name="c">字符</param>
        /// <returns>是否为数字</returns>
        private static bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// 获取文件名分割片段中可能是集号的位置
        /// </summary>
        /// <param name="a">文件名分割片段列表</param>
        /// <returns>可能的集号位置列表</returns>
        private static List<int> GetLikelyEpNumPos(List<string> a)
        {
            var result = new List<int>();
            for (int i = 0; i < a.Count; i++)
            {
                if (Renamer.IsLikelyEpisodeNumber(a[i])) result.Add(i);
            }
            return result;
        }

        private static bool HeadSequenceEqual(List<int> pos1, List<int> pos2)
        {
            int count = Math.Min(pos1.Count, pos2.Count);
            for (int i = 0; i < count; i++)
            {
                if (pos1[i] != pos2[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 分组解析文件列表的集号
        /// 先将相似文件名分组，再对每组分别解析集号
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        /// <param name="files">文件列表</param>
        /// <param name="min_match_rate">最小匹配度阈值</param>
        internal static void ResolveVSFileListBYGroup<T>(List<T> files, double min_match_rate) where T : VSFile
        {
            List<VSFileGroup<T>> group = GroupVSFiles(files, min_match_rate);
            foreach (var item in group)
            {
                ResolveVSFileList(item.FileList);
            }
        }

        /// <summary>
        /// 根据文件名相似度对文件进行分组
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        /// <param name="files">文件列表</param>
        /// <param name="min_match_rate">最小匹配度阈值</param>
        /// <returns>文件分组列表</returns>
        private static List<VSFileGroup<T>> GroupVSFiles<T>(List<T> files, double min_match_rate) where T : VSFile
        {
            List<VSFileGroup<T>> result = new List<VSFileGroup<T>>();

            if (files.Count <= 0)
            {
                return result;
            }
            // 创建第一个组
            var firstGroup = new VSFileGroup<T>(files[0]);
            result.Add(firstGroup);

            if (files.Count <= 1)
            {
                return result;
            }

            // 逐个文件进行分组匹配
            for (int i = 1; i < files.Count; i++)
            {
                var curr_file = files[i];
                var curr_splited_name = curr_file.Splited_filename;
                double curr_splited_name_length = 0;
                foreach (var item in curr_splited_name)
                {
                    curr_splited_name_length += item.Length;
                }

                double match_group_rate = 0;
                int match_group_num = -1;

                // 尝试匹配已有组
                for (int g_num = 0; g_num < result.Count; g_num++)
                {
                    var _group = result[g_num];
                    var group_head_file = _group.FileList[0];
                    var group_head_splited_name = group_head_file.Splited_filename;

                    // 先检查分割片段数量是否相近
                    int __a = group_head_splited_name.Count - curr_splited_name.Count;
                    if (__a < 4 && __a > -4)
                    {
                        double total_match_rate = 0;
                        for (int col = 0; col < curr_splited_name.Count; col++)
                        {
                            if (col < group_head_splited_name.Count)
                            {
                                double _rate;
                                if (curr_splited_name[col] == group_head_splited_name[col]) _rate = 1;
                                else _rate = CalculateWeightedSimilarity(curr_splited_name[col], group_head_splited_name[col]);
                                total_match_rate += _rate * curr_splited_name[col].Length / curr_splited_name_length;
                            }
                        }

                        // 如果匹配度达标且集号位置相同，则加入该组
                        if (total_match_rate >= min_match_rate)
                        {
                            // var _pos1 = GetLikelyEpNumPos(curr_splited_name);
                            // var _pos2 = _group.LikelyEpNumPos;
                            // if (HeadSequenceEqual(_pos1, _pos2) && total_match_rate > match_group_rate)
                            // {
                            match_group_rate = total_match_rate;
                            match_group_num = g_num;
                            // }
                        }
                    }
                }

                if (match_group_num >= 0)
                {
                    result[match_group_num].AddVSFile(files[i]);
                }
                else
                {
                    // 创建新组
                    var _item = new VSFileGroup<T>(files[i]);
                    result.Add(_item);
                }
            }

            return result;
        }

        /// <summary>
        /// 解析文件列表的集号
        /// 对于单个文件直接提取，对于多个文件找到差异最大的列作为集号列
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        /// <param name="files">文件列表</param>
        /// <returns>是否解析成功</returns>
        internal static bool ResolveVSFileList<T>(List<T> files) where T : VSFile
        {
            if (files == null || files.Count == 0) return false;

            // 单个文件直接提取集号
            if (files.Count <= 3)
            {
                foreach (var item in files)
                {
                    item.Num = Renamer.GetEpisodeNumber(item.File);
                }
                return true;
            }

            int colCount = files[0].Splited_filename.Count;
            if (colCount == 0) return false;

            // 找到差异最大的列（即集号所在列）
            int maxUniqueCount = -1;
            int maxUniqueColumn = -1;

            for (int col = 0; col < colCount; col++)
            {
                int current_num = 0;
                HashSet<string> uniqueValues = new HashSet<string>();
                foreach (var _v in files)
                {
                    if (col < _v.Splited_filename.Count)
                    {
                        uniqueValues.Add(_v.Splited_filename[col]);
                        current_num++;
                    }
                }

                if (current_num == files.Count)
                {
                    int currentUniqueCount = uniqueValues.Count;

                    if (currentUniqueCount > maxUniqueCount)
                    {
                        maxUniqueCount = currentUniqueCount;
                        maxUniqueColumn = col;
                    }
                }
            }

            // 从集号列提取集号
            foreach (var _v in files)
            {
                string str = Renamer.ResolveEpisodeNumber(_v.Splited_filename[maxUniqueColumn]);
                _v.Num = str;
            }

            return true;
        }

        /// <summary>
        /// 计算两个字符串的加权相似度
        /// 综合考虑前缀匹配和最长公共子串
        /// </summary>
        /// <param name="s1">第一个字符串</param>
        /// <param name="s2">第二个字符串</param>
        /// <returns>相似度（0-1）</returns>
        private static double CalculateWeightedSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            // 计算前缀匹配长度
            int prefixLen = 0;
            for (int i = 0; i < Math.Min(s1.Length, s2.Length); i++)
            {
                if (s1[i] == s2[i])
                    prefixLen++;
                else
                    break;
            }

            // 计算前两个最长公共子串长度
            int lcs1Len;
            int lcs2Len;
            if (prefixLen > 0)
            {
                string suffix1 = s1.Substring(prefixLen);
                string suffix2 = s2.Substring(prefixLen);
                (lcs1Len, lcs2Len) = FindTopTwoLCS_Optimized(suffix1, suffix2);
            }
            else
            {
                (lcs1Len, lcs2Len) = FindTopTwoLCS_Optimized(s1, s2);
            }

            // 加权计算总分
            double totalWeight = 0;
            double maxPossibleWeight = Math.Max(s1.Length, s2.Length);

            if (prefixLen > 0)
            {
                totalWeight += 1.0 * prefixLen;
                totalWeight += 0.7 * lcs1Len;
                totalWeight += 0.4 * lcs2Len;
            }
            else
            {
                totalWeight += 1.0 * lcs1Len;
                totalWeight += 0.7 * lcs2Len;
            }

            return totalWeight / maxPossibleWeight;
        }

        /// <summary>
        /// 优化的最长公共子串查找算法（对角线扫描）
        /// 查找前两个不重叠的最长公共子串
        /// </summary>
        /// <param name="s1">第一个字符串</param>
        /// <param name="s2">第二个字符串</param>
        /// <returns>(最长公共子串长度, 次长公共子串长度)</returns>
        private static (int, int) FindTopTwoLCS_Optimized(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return (0, 0);

            int m = s1.Length;
            int n = s2.Length;
            const int MIN_LEN = 3;

            if (m < MIN_LEN || n < MIN_LEN)
                return (0, 0);

            var maximalSubstrings = new List<(int start1, int start2, int len)>();

            (int start1, int start2, int len) top1 = (-1, -1, 0);
            (int start1, int start2, int len) top2 = (-1, -1, 0);

            // 扫描对角线上的公共子串
            void ScanDiagonal(int startI, int startJ, int diagLength)
            {
                if (diagLength < MIN_LEN) return;
                if (top2.len > 0 && diagLength <= top2.len) return;

                int i = startI, j = startJ;
                int currentLen = 0;
                int currentStartI = -1, currentStartJ = -1;

                while (i < m && j < n)
                {
                    if (s1[i] == s2[j])
                    {
                        if (currentLen == 0)
                        {
                            currentStartI = i;
                            currentStartJ = j;
                        }
                        currentLen++;
                    }
                    else if (currentLen > 0)
                    {
                        if (currentLen >= MIN_LEN)
                        {
                            TryUpdateTopTwo(currentStartI, currentStartJ, currentLen);
                            maximalSubstrings.Add((currentStartI, currentStartJ, currentLen));
                        }
                        currentLen = 0;

                        int remainingLength = Math.Min(m - i, n - j);
                        if (remainingLength < MIN_LEN || (top2.len > 0 && remainingLength <= top2.len)) break;
                    }
                    i++; j++;
                }

                if (currentLen >= MIN_LEN)
                {
                    TryUpdateTopTwo(currentStartI, currentStartJ, currentLen);
                    maximalSubstrings.Add((currentStartI, currentStartJ, currentLen));
                }
            }

            // 更新前两个最长公共子串
            void TryUpdateTopTwo(int start1, int start2, int len)
            {
                var candidate = (start1, start2, len);

                if (len > top1.len)
                {
                    var old1 = top1;
                    var old2 = top2;

                    top1 = candidate;

                    if (old1.len > 0 && !IsOverlappingGlobal(top1, old1))
                    {
                        top2 = old1;
                    }
                    else
                    {
                        top2 = old2;
                    }

                    if (top2.len > 0 && IsOverlappingGlobal(top1, top2))
                    {
                        top2 = (-1, -1, 0);
                    }
                }
                else if (len > top2.len)
                {
                    if (!IsOverlappingGlobal(top1, candidate))
                    {
                        top2 = candidate;
                    }
                }
            }

            // 扫描所有对角线
            ScanDiagonal(0, 0, Math.Min(m, n));

            int maxOffset = Math.Max(m, n) - 1;
            for (int offset = 1; offset <= maxOffset; offset++)
            {
                int currentMaxDiagLength = Math.Min(m, n) - offset;
                if (currentMaxDiagLength < MIN_LEN) break;
                if (top2.len > 0 && currentMaxDiagLength <= top2.len) break;

                if (offset < m)
                    ScanDiagonal(offset, 0, Math.Min(m - offset, n));

                if (offset < n)
                    ScanDiagonal(0, offset, Math.Min(m, n - offset));
            }

            if (maximalSubstrings.Count == 0) return (0, 0);

            // 按长度排序，确保找到真正最长的两个不重叠子串
            maximalSubstrings.Sort((a, b) => b.len.CompareTo(a.len));

            int finalLen1 = maximalSubstrings[0].len;
            var finalTop1 = maximalSubstrings[0];
            int finalLen2 = 0;

            for (int k = 1; k < maximalSubstrings.Count; k++)
            {
                var sub = maximalSubstrings[k];
                if (!IsOverlappingGlobal(finalTop1, sub))
                {
                    finalLen2 = sub.len;
                    break;
                }
            }

            return (finalLen1, finalLen2);
        }

        /// <summary>
        /// 判断两个子串是否在全局范围内重叠
        /// </summary>
        /// <param name="a">第一个子串</param>
        /// <param name="b">第二个子串</param>
        /// <returns>是否重叠</returns>
        private static bool IsOverlappingGlobal((int s1, int s2, int len) a, (int s1, int s2, int len) b)
        {
            bool overlapInS1 = !(a.s1 + a.len <= b.s1 || b.s1 + b.len <= a.s1);
            bool overlapInS2 = !(a.s2 + a.len <= b.s2 || b.s2 + b.len <= a.s2);

            return overlapInS1 || overlapInS2;
        }

        /// <summary>
        /// 使用正则表达式解析文件列表的集号
        /// </summary>
        /// <typeparam name="T">VSFile 派生类型</typeparam>
        /// <param name="files">文件列表</param>
        /// <param name="regex">正则表达式</param>
        internal static void ResolveVSFileListBYRegex<T>(List<T> files, string regex) where T : VSFile
        {
            foreach (var file in files)
            {
                string num = Regex.Replace(file.File.Name, regex, "");
                file.Num = num;
            }
        }
    }
}
