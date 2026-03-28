using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Notari.Services;

/// <summary>
/// Provides text analysis operations bound to a RichTextBox.
/// All methods must be called on the UI thread.
/// </summary>
public interface IDocumentService
{
    string GetActiveWord();
    string GetWordAtPointer(TextPointer pointer);
    List<Rect> FindWordRects(string word);
    List<Rect> FindBracketRects(Regex pattern);
    (int WordCount, int LineCount) GetDocumentStats();
    List<(double Y, string Text)> GetSyllableSegments();
    List<(double Y, double X, string LastWord)> GetRhymeSchemeSegments();
    void ScrollToTypewriterPosition(ScrollViewer canvas);
}
