namespace Notari.Models;

public record SynGroup(PartOfSpeech Pos, IReadOnlyList<string> Words)
{
    public string Label => Pos switch
    {
        PartOfSpeech.Noun               => "NOUNS",
        PartOfSpeech.Verb               => "VERBS",
        PartOfSpeech.Adjective          => "ADJECTIVES",
        PartOfSpeech.AdjectiveSatellite => "ADJ. SATELLITE",
        PartOfSpeech.Adverb             => "ADVERBS",
        _                               => "OTHER",
    };
}
