using PhonKit;

namespace Notari.Models;

public record LookupResult(
    string Word,
    IReadOnlyList<PhoneticInfo> Phonetics,
    IReadOnlyList<WordItem> Rhymes,
    IReadOnlyList<WordItem> MultisyllabicRhymes,
    IReadOnlyList<WordItem> Assonance,
    IReadOnlyList<WordItem> Alliteration,
    IReadOnlyList<SynGroup> SynonymGroups,
    IReadOnlyList<WordItem> Antonyms,
    IReadOnlyList<WordItem> Hypernyms,
    IReadOnlyList<WordItem> Hyponyms)
{
    public IEnumerable<WordItem> AllWordItems =>
        Rhymes
        .Concat(MultisyllabicRhymes)
        .Concat(Assonance)
        .Concat(Alliteration)
        .Concat(SynonymGroups.SelectMany(g => g.Words))
        .Concat(Antonyms)
        .Concat(Hypernyms)
        .Concat(Hyponyms);
}
