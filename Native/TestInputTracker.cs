namespace TypeMaster.Native;

// Incremental prefix alignment: an untyped reference suffix is not an error.
// Retain rows so Backspace and selection edits can roll back without re-scoring
// the whole passage on every keystroke. Work per new character is bounded.
internal sealed class TestInputTracker(string target)
{
    const int Radius = 128, Infinity = 100000000;
    sealed record Row(int Start, int[] Costs, int[] Matches, int Position, int Distance, int Correct)
    {
        public int Cost(int column) => column < Start || column >= Start + Costs.Length ? Infinity : Costs[column - Start];
        public int Match(int column) => column < Start || column >= Start + Matches.Length ? 0 : Matches[column - Start];
    }
    readonly List<Row> rows = [new(0, Enumerable.Range(0, Math.Min(target.Length, Radius) + 1).ToArray(), new int[Math.Min(target.Length, Radius) + 1], 0, 0, 0)];
    string previous = "";
    public int Correct => rows[^1].Correct;
    public int Position => rows[^1].Position;
    public int PositionAt(int length) => rows[Math.Clamp(length, 0, rows.Count - 1)].Position;
    public HashSet<int> MatchedPositions()
    {
        var matched = new HashSet<int>();
        for (int i = 0; i < previous.Length; i++)
        {
            int position = rows[i + 1].Position - 1;
            if (position >= 0 && position < target.Length && previous[i] == target[position] && rows[i + 1].Correct > rows[i].Correct) matched.Add(position);
        }
        return matched;
    }
    public int Update(string value)
    {
        int prefix = 0;
        while (prefix < previous.Length && prefix < value.Length && previous[prefix] == value[prefix]) prefix++;
        rows.RemoveRange(prefix + 1, rows.Count - prefix - 1);
        int addedErrors = 0;
        for (int i = prefix; i < value.Length; i++)
        {
            Row last = rows[^1];
            int start = Math.Max(0, last.Position - Radius), end = Math.Min(target.Length, last.Position + Radius);
            var costs = new int[end - start + 1]; var matches = new int[costs.Length];
            int best = Infinity, bestMatches = -1, position = start;
            for (int j = start; j <= end; j++)
            {
                int k = j - start;
                int cost = last.Cost(j) + 1, matched = last.Match(j);
                void Offer(int candidate, int count) { if (candidate < cost || candidate == cost && count > matched) { cost = candidate; matched = count; } }
                if (j > 0) Offer(last.Cost(j - 1) + (value[i] == target[j - 1] ? 0 : 1), last.Match(j - 1) + (value[i] == target[j - 1] ? 1 : 0));
                if (k > 0) Offer(costs[k - 1] + 1, matches[k - 1]);
                costs[k] = cost; matches[k] = matched;
                if (cost < best || cost == best && matched > bestMatches) { best = cost; bestMatches = matched; position = j; }
            }
            addedErrors += Math.Max(0, best - last.Distance);
            rows.Add(new Row(start, costs, matches, position, best, bestMatches));
        }
        previous = value;
        return addedErrors;
    }
}
