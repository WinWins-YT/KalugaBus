namespace KalugaBus.Extensions;

public static class StringExtensions
{
    public static bool ContainsMultiple(this string s, string query)
    {
        var words = s.Trim().Split();
        var queryWords = query.Trim().Split().ToList();

        foreach (var word in words)
        {
            for (var i = 0; i < queryWords.Count; i++)
            {
                if (word.Contains(queryWords[i], StringComparison.InvariantCultureIgnoreCase))
                {
                    queryWords.RemoveAt(i);
                    break;
                }
            }

            if (queryWords.Count == 0)
                return true;
        }

        return false;
    }
}