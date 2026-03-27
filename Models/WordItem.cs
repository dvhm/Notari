using System.ComponentModel;

namespace Notari.Models;

public class WordItem : INotifyPropertyChanged
{
    private string _syllableLabel = "";

    public string Word { get; }

    public string SyllableLabel
    {
        get => _syllableLabel;
        set { _syllableLabel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SyllableLabel))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public WordItem(string word) { Word = word; }
}
