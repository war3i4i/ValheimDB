using System.Collections.Generic;

namespace ValheimDB;

public static class Utils
{
    public static void AddRangeForced<T, U>(this IDictionary<T, U> dict, Dictionary<T, U> items)
    {
        foreach (KeyValuePair<T, U> kvp in items) dict[kvp.Key] = kvp.Value;
    }
}