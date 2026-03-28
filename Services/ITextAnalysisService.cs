using System.Text.RegularExpressions;
using System.Windows.Documents;

namespace Notari.Services;

public interface ITextAnalysisService
{
    string CleanWord(string s);
    string StripBracketedContent(string text);
    Regex BuildBracketRegex(AppSettings settings);
    IEnumerable<Inline> FlattenInlines(InlineCollection inlines);
    IEnumerable<(TextPointer Start, TextPointer End, string Text)> GetLineSegments(Paragraph paragraph);
    string GetLastWordOf(string text);
}
