using System.Text;
using MessagePack;


namespace JikanDotNet
{
    [MessagePackObject]
    public class AnimeDto
    {
        /// <summary>ID associated with MyAnimeList.</summary>
        [Key("mal_id")]
        public long? MalId { get; set; }

        /// <summary>Anime's canonical link.</summary>
        [Key("url")]
        public string Url { get; set; }

        /// <summary>Title of the anime.</summary>
        [Obsolete("This will be removed in the future. Use titles property instead.")]
        [Key("title")]
        public string Title { get; set; }

        /// <summary>Title of the anime in English.</summary>
        [Obsolete("This will be removed in the future. Use titles property instead.")]
        [Key("title_english")]
        public string TitleEnglish { get; set; }

        /// <summary>Title of the anime in Japanese.</summary>
        [Obsolete("This will be removed in the future. Use titles property instead.")]
        [Key("title_japanese")]
        public string TitleJapanese { get; set; }

        /// <summary>
        /// Anime's multiple titles (if any). Return null if there is none.
        /// </summary>
        [Obsolete("This will be removed in the future. Use titles property instead.")]
        [Key("title_synonyms")]
        public ICollection<string> TitleSynonyms { get; set; }

        /// <summary>Anime's multiple titles (if any).</summary>
        [Key("titles")]
        public ICollection<TitleEntryDto> Titles { get; set; }

        /// <summary>Anime type (e. g. "TV", "Movie").</summary>
        [Key("type")]
        public string Type { get; set; }

        /// <summary>Anime source (e .g. "Manga" or "Original").</summary>
        [Key("source")]
        public string Source { get; set; }

        /// <summary>Anime's episode count.</summary>
        [Key("episodes")]
        public int? Episodes { get; set; }

        /// <summary>Anime's airing status (e. g. "Currently Airing").</summary>
        [Key("status")]
        public string Status { get; set; }

        /// <summary>Is anime currently airing.</summary>
        [Key("airing")]
        public bool Airing { get; set; }

        /// <summary>Anime's duration per episode.</summary>
        [Key("duration")]
        public string Duration { get; set; }

        /// <summary>Anime's age rating.</summary>
        [Key("rating")]
        public string Rating { get; set; }

        /// <summary>Anime's score on MyAnimeList up to 2 decimal places.</summary>
        [Key("score")]
        public double? Score { get; set; }

        /// <summary>Number of people the anime has been scored by.</summary>
        [Key("scored_by")]
        public int? ScoredBy { get; set; }

        /// <summary>Anime rank on MyAnimeList (score).</summary>
        [Key("rank")]
        public int? Rank { get; set; }

        /// <summary>Anime popularity rank on MyAnimeList.</summary>
        [Key("popularity")]
        public int? Popularity { get; set; }

        /// <summary>Anime members count on MyAnimeList.</summary>
        [Key("members")]
        public int? Members { get; set; }

        /// <summary>Anime favourite count on MyAnimeList.</summary>
        [Key("favorites")]
        public int? Favorites { get; set; }

        /// <summary>Anime's synopsis.</summary>
        [IgnoreMember] // Exclude the original Synopsis property from serialization
        public string? Synopsis { get; set; }

        [Key("Synopsis")] // Use a custom name for the byte array property
        public byte[] SynopsisBytes
        {
            get
            {
                if (Synopsis != null) return Encoding.UTF8.GetBytes(Synopsis);
                return new byte[] { };
            }
            set => Synopsis = Encoding.UTF8.GetString(value);
        }

        /// <summary>Anime's background info.</summary>
        [Key("background")]
        public string Background { get; set; }

        /// <summary>Year the anime premiered.</summary>
        [Key("year")]
        public int? Year { get; set; }

        /// <summary>
        /// Anime's producers numerically indexed with array values.
        /// </summary>
        [Key("producers")]
        public ICollection<MalUrlDto> Producers { get; set; }

        /// <summary>
        /// Anime's licensors numerically indexed with array values.
        /// </summary>
        [Key("licensors")]
        public ICollection<MalUrlDto> Licensors { get; set; }

        /// <summary>
        /// Anime's studio(s) numerically indexed with array values.
        /// </summary>
        [Key("studios")]
        public ICollection<MalUrlDto> Studios { get; set; }

        /// <summary>Anime's genres numerically indexed with array values.</summary>
        [Key("genres")]
        public ICollection<MalUrlDto> Genres { get; set; }

        /// <summary>Explicit genres</summary>
        [Key("explicit_genres")]
        public ICollection<MalUrlDto> ExplicitGenres { get; set; }

        /// <summary>Anime's themes</summary>
        [Key("themes")]
        public ICollection<MalUrlDto> Themes { get; set; }

        /// <summary>Anime's demographics</summary>
        [Key("demographics")]
        public ICollection<MalUrlDto> Demographics { get; set; }

        /// <summary>
        /// If Approved is false then this means the entry is still pending review on MAL.
        /// </summary>
        [Key("approved")]
        public bool Approved { get; set; }
    }
    
    [MessagePackObject]
    public class TitleEntryDto
    {
        /// <summary>Type of title (usually the language).</summary>
        [Key("type")]
        public string Type { get; set; }

        /// <summary>Value of the Title.</summary>
        [Key("title")]
        public string Title { get; set; }
    }
    
    [MessagePackObject]
    public class MalUrlDto
    {
        /// <summary>ID associated with MyAnimeList.</summary>
        [Key("mal_id")]
        public long MalId { get; set; }

        /// <summary>Type of resource</summary>
        [Key("type")]
        public string Type { get; set; }

        /// <summary>Url to sub item main page.</summary>
        [Key("url")]
        public string Url { get; set; }

        /// <summary>Title/Name of the item</summary>
        [Key("name")]
        public string Name { get; set; }

        /// <summary>Overriden ToString method.</summary>
        /// <returns>Title if not null, base method elsewhere.</returns>
        public override string ToString() => this.Name ?? base.ToString();
    }
}
