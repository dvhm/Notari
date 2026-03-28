using Notari.Models;
using PhonKit;

namespace Notari.Services;

public interface ILookupService : IAsyncDisposable
{
    Task<LookupResult> LookupWordAsync(string word, bool sortByZipf, int limit, CancellationToken ct);
    Task<IReadOnlyList<PhoneticInfo>> GetPhoneticsAsync(string word, CancellationToken ct);
    int? GetSyllableCount(string word);
    RhymeScore GetRhymeScore(string word1, string word2);
    Task EnrichWithSyllablesAsync(IReadOnlyList<WordItem> items, CancellationToken ct);
}
