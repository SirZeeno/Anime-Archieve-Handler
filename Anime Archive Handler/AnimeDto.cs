using MessagePack;

namespace Anime_Archive_Handler;

[MessagePackObject(true)]
public class AnimeDto
{
    /// <summary>ID associated with MyAnimeList.</summary>
    public long? MalId { get; set; }

    /// <summary>Anime's canonical link.</summary>
    public string? Url { get; set; }

    /// <summary>Title of the anime.</summary>
    [Obsolete("This will be removed in the future. Use titles property instead.")]
    public string? Title { get; set; }

    /// <summary>Title of the anime in English.</summary>
    [Obsolete("This will be removed in the future. Use titles property instead.")]
    public string? TitleEnglish { get; set; }

    /// <summary>Title of the anime in Japanese.</summary>
    [Obsolete("This will be removed in the future. Use titles property instead.")]
    public string? TitleJapanese { get; set; }

    /// <summary>
    ///     Anime's multiple titles (if any). Return null if there is none.
    /// </summary>
    [Obsolete("This will be removed in the future. Use titles property instead.")]
    public ICollection<string>? TitleSynonyms { get; set; }

    /// <summary>Anime's multiple titles (if any).</summary>
    public ICollection<TitleEntryDto>? Titles { get; set; }

    /// <summary>Anime type (e. g. "TV", "Movie").</summary>
    public string? Type { get; set; }

    /// <summary>Anime source (e .g. "Manga" or "Original").</summary>
    public string? Source { get; set; }

    /// <summary>Anime's episode count.</summary>
    public int? Episodes { get; set; }

    /// <summary>Anime's airing status (e. g. "Currently Airing").</summary>
    public string? Status { get; set; }

    /// <summary>Is anime currently airing.</summary>
    public bool Airing { get; set; }

    /// <summary>Anime's duration per episode.</summary>
    public string? Duration { get; set; }

    /// <summary>Anime's age rating.</summary>
    public string? Rating { get; set; }

    /// <summary>Anime's score on MyAnimeList up to 2 decimal places.</summary>
    public double? Score { get; set; }

    /// <summary>Number of people the anime has been scored by.</summary>
    public int? ScoredBy { get; set; }

    /// <summary>Anime rank on MyAnimeList (score).</summary>
    public int? Rank { get; set; }

    /// <summary>Anime popularity rank on MyAnimeList.</summary>
    public int? Popularity { get; set; }

    /// <summary>Anime members count on MyAnimeList.</summary>
    public int? Members { get; set; }

    /// <summary>Anime favourite count on MyAnimeList.</summary>
    public int? Favorites { get; set; }

    /// <summary>Anime's synopsis.</summary>
    public string? Synopsis { get; set; }

    /// <summary>Anime's background info.</summary>
    public string? Background { get; set; }

    /// <summary>Year the anime premiered.</summary>
    public int? Year { get; set; }

    /// <summary>
    ///     Anime's producers numerically indexed with array values.
    /// </summary>
    public ICollection<MalUrlDto>? Producers { get; set; }

    /// <summary>
    ///     Anime's licensors numerically indexed with array values.
    /// </summary>
    public ICollection<MalUrlDto>? Licensors { get; set; }

    /// <summary>
    ///     Anime's studio(s) numerically indexed with array values.
    /// </summary>
    public ICollection<MalUrlDto>? Studios { get; set; }

    /// <summary>Anime's genres numerically indexed with array values.</summary>
    public ICollection<MalUrlDto>? Genres { get; set; }

    /// <summary>Explicit genres</summary>
    public ICollection<MalUrlDto>? ExplicitGenres { get; set; }

    /// <summary>Anime's themes</summary>
    public ICollection<MalUrlDto>? Themes { get; set; }

    /// <summary>Anime's demographics</summary>
    public ICollection<MalUrlDto>? Demographics { get; set; }

    /// <summary>
    ///     If Approved is false then this means the entry is still pending review on MAL.
    /// </summary>
    public bool Approved { get; set; }
}

[MessagePackObject(true)]
public class TitleEntryDto
{
    /// <summary>Type of title (usually the language).</summary>
    public string? Type { get; set; }

    /// <summary>Value of the Title.</summary>
    public string? Title { get; set; }
}

[MessagePackObject(true)]
public class MalUrlDto
{
    /// <summary>ID associated with MyAnimeList.</summary>
    public long MalId { get; set; }

    /// <summary>Type of resource</summary>
    public string? Type { get; set; }

    /// <summary>Url to sub item main page.</summary>
    public string? Url { get; set; }

    /// <summary>Title/Name of the item</summary>
    public string? Name { get; set; }

    /// <summary>Overriden ToString method.</summary>
    /// <returns>Title if not null, base method elsewhere.</returns>
    public override string? ToString()
    {
        return Name ?? base.ToString();
    }
}