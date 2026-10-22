namespace AppBackendBasic.Services.Captions;

/// <summary>
/// Faithful port of Python's difflib.SequenceMatcher(None, a, b).ratio() (no junk heuristic,
/// which only engages for len(b) >= 200 - words are far shorter). Used for fuzzy word alignment.
/// </summary>
public static class DifflibRatio
{
    public static double Ratio(string a, string b)
    {
        int total = a.Length + b.Length;
        if (total == 0) return 1.0;

        var b2j = new Dictionary<char, List<int>>();
        for (int j = 0; j < b.Length; j++)
        {
            if (!b2j.TryGetValue(b[j], out var list)) b2j[b[j]] = list = new List<int>();
            list.Add(j);
        }

        int matches = 0;
        var queue = new Stack<(int alo, int ahi, int blo, int bhi)>();
        queue.Push((0, a.Length, 0, b.Length));
        while (queue.Count > 0)
        {
            var (alo, ahi, blo, bhi) = queue.Pop();
            var (i, j, k) = LongestMatch(a, b, b2j, alo, ahi, blo, bhi);
            if (k == 0) continue;
            matches += k;
            if (alo < i && blo < j) queue.Push((alo, i, blo, j));
            if (i + k < ahi && j + k < bhi) queue.Push((i + k, ahi, j + k, bhi));
        }
        return 2.0 * matches / total;
    }

    private static (int i, int j, int k) LongestMatch(string a, string b, Dictionary<char, List<int>> b2j,
        int alo, int ahi, int blo, int bhi)
    {
        int besti = alo, bestj = blo, bestsize = 0;
        var j2len = new Dictionary<int, int>();
        for (int i = alo; i < ahi; i++)
        {
            var newj2len = new Dictionary<int, int>();
            if (b2j.TryGetValue(a[i], out var js))
            {
                foreach (int j in js)
                {
                    if (j < blo) continue;
                    if (j >= bhi) break;
                    int k = (j2len.TryGetValue(j - 1, out var prev) ? prev : 0) + 1;
                    newj2len[j] = k;
                    if (k > bestsize)
                    {
                        besti = i - k + 1;
                        bestj = j - k + 1;
                        bestsize = k;
                    }
                }
            }
            j2len = newj2len;
        }
        while (besti > alo && bestj > blo && a[besti - 1] == b[bestj - 1]) { besti--; bestj--; bestsize++; }
        while (besti + bestsize < ahi && bestj + bestsize < bhi && a[besti + bestsize] == b[bestj + bestsize]) bestsize++;
        return (besti, bestj, bestsize);
    }
}
