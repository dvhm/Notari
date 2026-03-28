using Notari.Models;
using PhonKit;

namespace Notari.Services;

public sealed class LookupService : ILookupService
{
    private static readonly (string Code, Models.PartOfSpeech Pos)[] _posCodes =
    [
        ("n", Models.PartOfSpeech.Noun),
        ("v", Models.PartOfSpeech.Verb),
        ("a", Models.PartOfSpeech.Adjective),
        ("s", Models.PartOfSpeech.AdjectiveSatellite),
        ("r", Models.PartOfSpeech.Adverb),
    ];

    private readonly PhoneticDatabase _db;

    public LookupService(PhoneticDatabase db) => _db = db;

    public Task<LookupResult> LookupWordAsync(string word, bool sortByZipf, int limit, CancellationToken ct) =>
        Task.Run(() =>
        {
            static List<Models.WordItem> ToItems(IEnumerable<string> words) =>
                words.Select(w => new Models.WordItem(w)).ToList();

            var phonetics    = _db.GetPhonetics(word);
            var rhymes       = ToItems(_db.GetRhymes(word, limit, sortByZipf).Select(r => r.Text));
            var multi        = ToItems(_db.GetMultisyllabicRhymes(word, limit, sortByZipf).Select(r => r.Text));
            var assonance    = ToItems(_db.GetAssonance(word, limit, sortByZipf).Select(r => r.Text));
            var alliteration = ToItems(_db.GetAlliteration(word, limit, sortByZipf).Select(r => r.Text));
            var synGroups    = _posCodes
                .Select(p => new Models.SynGroup(p.Pos, ToItems(_db.GetSynonyms(word, pos: p.Code, limit: limit, sortByZipf: sortByZipf).Select(r => r.Text))))
                .Where(g => g.Words.Count > 0)
                .ToList();
            var antonyms  = ToItems(_db.GetAntonyms(word, limit, sortByZipf).Select(r => r.Text));
            var hypernyms = ToItems(_db.GetHypernyms(word, null, limit, sortByZipf).Select(r => r.Text));
            var hyponyms  = ToItems(_db.GetHyponyms(word, null, limit, sortByZipf).Select(r => r.Text));

            return new LookupResult(word, phonetics, rhymes, multi, assonance, alliteration,
                synGroups, antonyms, hypernyms, hyponyms);
        }, ct);

    public Task<IReadOnlyList<PhoneticInfo>> GetPhoneticsAsync(string word, CancellationToken ct) =>
        Task.Run(() => _db.GetPhonetics(word), ct);

    public int? GetSyllableCount(string word) => _db.GetSyllableCount(word);

    public RhymeScore GetRhymeScore(string word1, string word2) => _db.GetRhymeScore(word1, word2);

    public async Task EnrichWithSyllablesAsync(IReadOnlyList<Models.WordItem> items, CancellationToken ct)
    {
        try
        {
            const int batchSize = 20;
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                var results = await Task.Run(
                    () => batch.Select(item => (item, Syl: _db.GetSyllableCount(item.Word))).ToList(),
                    ct);

                ct.ThrowIfCancellationRequested();

                foreach (var (item, syl) in results)
                    if (syl.HasValue) item.SyllableLabel = $"({syl.Value})";

                if (i + batchSize < items.Count)
                    await Task.Delay(50, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}
