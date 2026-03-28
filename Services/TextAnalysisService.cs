using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace Notari.Services;

public sealed class TextAnalysisService : ITextAnalysisService
{
    private static readonly Regex _bracketedContent =
        new(@"\[[^\]]*\]|\([^\)]*\)", RegexOptions.Compiled);

    private static readonly Dictionary<(bool, bool, bool), Regex> _bracketRegexCache = [];

    public string CleanWord(string s)
    {
        if (s.Length == 0) return s;

        bool needsCleaning = false;
        for (int i = 0; i < s.Length; i++)
            if (!char.IsLetterOrDigit(s[i]) && s[i] != '\'') { needsCleaning = true; break; }
        if (!needsCleaning) return s;

        Span<char> buf = s.Length <= 256 ? stackalloc char[s.Length] : new char[s.Length];
        int len = 0;
        foreach (char c in s)
            if (char.IsLetterOrDigit(c) || c == '\'') buf[len++] = c;
        return new string(buf[..len]);
    }

    public string StripBracketedContent(string text) =>
        _bracketedContent.Replace(text, " ");

    public Regex BuildBracketRegex(AppSettings settings)
    {
        var key = (settings.DimSquare, settings.DimRound, settings.DimCurly);
        if (_bracketRegexCache.TryGetValue(key, out var cached))
            return cached;

        var parts = new List<string>();
        if (settings.DimSquare) parts.Add(@"\[[^\]]*\]");
        if (settings.DimRound)  parts.Add(@"\([^\)]*\)");
        if (settings.DimCurly)  parts.Add(@"\{[^\}]*\}");
        var regex = parts.Count > 0
            ? new Regex(string.Join("|", parts), RegexOptions.Compiled)
            : new Regex(@"(?!x)x", RegexOptions.Compiled);
        _bracketRegexCache[key] = regex;
        return regex;
    }

    public IEnumerable<Inline> FlattenInlines(InlineCollection inlines)
    {
        foreach (var inline in inlines)
        {
            if (inline is Span span)
                foreach (var child in FlattenInlines(span.Inlines))
                    yield return child;
            else
                yield return inline;
        }
    }

    public IEnumerable<(TextPointer Start, TextPointer End, string Text)> GetLineSegments(Paragraph para)
    {
        var segStart = para.ContentStart;
        var segEnd   = para.ContentStart;
        var sb = new System.Text.StringBuilder();
        bool nextRunSetsStart = false;

        foreach (var inline in FlattenInlines(para.Inlines))
        {
            if (inline is LineBreak)
            {
                yield return (segStart, segEnd, sb.ToString());
                sb.Clear();
                nextRunSetsStart = true;
            }
            else if (inline is Run run)
            {
                if (nextRunSetsStart)
                {
                    segStart = run.ContentStart;
                    nextRunSetsStart = false;
                }
                segEnd = run.ContentEnd;
                sb.Append(run.Text);
            }
        }

        yield return (segStart, segEnd, sb.ToString());
    }

    public string GetLastWordOf(string text)
    {
        var trimmed = text.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] is '[' or '(' or '{')
            return string.Empty;

        var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            string cleaned = CleanWord(parts[i]);
            if (cleaned.Length > 0) return cleaned.ToLowerInvariant();
        }
        return string.Empty;
    }
}
