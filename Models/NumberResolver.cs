namespace SubRenamer.Models
{
    internal class NumberResolver
    {
        private class VSFileGroup<T> where T : VSFile
        {
            public List<T> FileList { get; }

            public List<int> LikelyEpNumPos { get; }

            public VSFileGroup(T t)
            {
                FileList = new List<T>();
                AddVSFile(t);
                LikelyEpNumPos = GetLikelyEpNumPos(t.Splited_filename);
            }

            public void AddVSFile(T file)
            {
                FileList.Add(file);
            }
        }

        public static bool Resolve(Names names)
        {
            try
            {
                string[] strs = Names.GetStrArray(names.videos);
                int len = strs[0].Length;
                int i = 0;
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
                if (i >= len)
                {
                    return false;
                }

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

        private static bool IsNumber(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static List<int> GetLikelyEpNumPos(List<string> a)
        {
            var result = new List<int>();
            for (int i = 0; i < a.Count; i++)
            {
                if (Renamer.IsLikelyEpisodeNumber(a[i])) result.Add(i);
            }

            return result;
        }

        internal static void ResolveGroupFileList<T>(List<T> files, double min_match_rate) where T : VSFile
        {
            List<VSFileGroup<T>> group = GroupVSFiles(files, min_match_rate);
            foreach (var item in group)
            {
                ResolveFileList(item.FileList);
            }
        }

        private static List<VSFileGroup<T>> GroupVSFiles<T>(List<T> files, double min_match_rate) where T : VSFile
        {
            List<VSFileGroup<T>> result = new List<VSFileGroup<T>>();

            if (files.Count <= 0)
            {
                return result;
            }
            var firstGroup = new VSFileGroup<T>(files[0]);
            result.Add(firstGroup);

            if (files.Count <= 1)
            {
                return result;
            }

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

                for (int g_num = 0; g_num < result.Count; g_num++)
                {
                    var _group = result[g_num];
                    var group_head_file = _group.FileList[0];
                    var group_head_splited_name = group_head_file.Splited_filename;

                    int __a = group_head_splited_name.Count - curr_splited_name.Count;
                    if (__a < 2 && __a > -2)
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

                        if (total_match_rate >= min_match_rate)
                        {
                            var _pos1 = GetLikelyEpNumPos(curr_splited_name);
                            var _pos2 = _group.LikelyEpNumPos;
                            if (_pos1.SequenceEqual(_pos2) && total_match_rate > match_group_rate)
                            {
                                match_group_rate = total_match_rate;
                                match_group_num = g_num;
                            }
                        }
                    }
                }

                if (match_group_num >= 0)
                {
                    result[match_group_num].AddVSFile(files[i]);
                }
                else
                {
                    var _item = new VSFileGroup<T>(files[i]);
                    result.Add(_item);
                }
            }

            return result;
        }

        internal static bool ResolveFileList<T>(List<T> files) where T : VSFile
        {
            if (files == null || files.Count == 0) return false;

            if (files.Count <= 1)
            {
                foreach (var item in files)
                {
                    item.Num = Renamer.GetEpisodeNumber(item.File);
                }
                return true;
            }

            int colCount = files[0].Splited_filename.Count;
            if (colCount == 0) return false;

            int maxUniqueCount = -1;
            int maxUniqueColumn = -1;

            for (int col = 0; col < colCount; col++)
            {
                HashSet<string> uniqueValues = new HashSet<string>();
                foreach (var _v in files)
                {
                    if (col < _v.Splited_filename.Count)
                        uniqueValues.Add(_v.Splited_filename[col]);
                }

                int currentUniqueCount = uniqueValues.Count;

                if (currentUniqueCount > maxUniqueCount)
                {
                    maxUniqueCount = currentUniqueCount;
                    maxUniqueColumn = col;
                }
            }

            foreach (var _v in files)
            {
                string str = Renamer.ResolveEpisodeNumber(_v.Splited_filename[maxUniqueColumn]);
                _v.Num = str;
            }

            return true;
        }

        private static double CalculateWeightedSimilarity(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return 0.0;

            int prefixLen = 0;
            for (int i = 0; i < Math.Min(s1.Length, s2.Length); i++)
            {
                if (s1[i] == s2[i])
                    prefixLen++;
                else
                    break;
            }

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

        private static bool IsOverlappingGlobal((int s1, int s2, int len) a, (int s1, int s2, int len) b)
        {
            bool overlapInS1 = !(a.s1 + a.len <= b.s1 || b.s1 + b.len <= a.s1);
            bool overlapInS2 = !(a.s2 + a.len <= b.s2 || b.s2 + b.len <= a.s2);

            return overlapInS1 || overlapInS2;
        }
    }
}
