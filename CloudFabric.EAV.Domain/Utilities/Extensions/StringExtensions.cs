using System.Text.RegularExpressions;

namespace CloudFabric.EAV.Domain.Utilities.Extensions;

public static class StringExtensions
{
    public static string SanitizeForMachineName(this string str)
    {
        var specSymbolsRegex = new Regex("[^\\d\\w_]*", RegexOptions.None, TimeSpan.FromMilliseconds(100));
        return specSymbolsRegex.Replace(str.Replace(" ", "_"), "").ToLower();
    }
}
