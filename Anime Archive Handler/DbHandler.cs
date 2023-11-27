using FuzzySharp;
using FuzzySharp.Extractor;
using JikanDotNet;
using LiteDB;

namespace Anime_Archive_Handler;

using static AnimeArchiveHandler;

public static class DbHandler
{
    private static readonly LiteDatabase Db = new(HelperClass.GetFileInProgramFolder("DataBase.db"));
    private static readonly LiteDatabase Al = new(HelperClass.GetFileInProgramFolder("AnimeList.db"));
    public static readonly ILiteCollection<Anime> AnimeList = Db.GetCollection<Anime>("Anime"); //loads anime database
    private static readonly ILiteCollection<TitleEntryDb> TitleEntryList = Db.GetCollection<TitleEntryDb>("TitleEntry");
    internal static readonly ILiteCollection<Anime> ToWatchList = Al.GetCollection<Anime>("ToWatch");
    internal static readonly ILiteCollection<TitleEntryDb> ToWatchListTitles = Al.GetCollection<TitleEntryDb>("ToWatchTitleEntry");
    internal static readonly ILiteCollection<SeasonNumberDb> ToWatchListSeasons = Al.GetCollection<SeasonNumberDb>("ToWatchSeasons");

    public static void EnsureIndexDb()
    {
        // Ensure index on MalId
        AnimeList.EnsureIndex(x => x.MalId);
        TitleEntryList.EnsureIndex(x => x.MalId);
        ToWatchList.EnsureIndex(x => x.MalId);
        ToWatchListTitles.EnsureIndex(x => x.MalId);
        
        // Ensure index on Title
        TitleEntryList.EnsureIndex(x => x.Title);
        ToWatchListTitles.EnsureIndex(x => x.Title);
    }

    public static void PopulateTitleEntryDb()
    {
        var animes = AnimeList.FindAll();
        foreach (var anime in animes)
        {
            foreach (var titleEntry in anime.Titles)
            {
                var titleEntryDb = new TitleEntryDb()
                {
                    MalId = anime.MalId,
                    Title = titleEntry.Title,
                    Type = titleEntry.Type
                };
                TitleEntryList.Upsert(titleEntryDb);
            }
        }
    }
    
    public static void SaveToAnimeList(Anime anime)
    {
        if (CheckAnimeExistence(anime.MalId))
        {
            ConsoleExt.WriteLineWithPretext("Anime already exists in the Anime List! Skipped Anime...", ConsoleExt.OutputType.Warning);
            return;
        }
        ToWatchList.Insert(anime);
        foreach (var titleEntry in anime.Titles)
        {
            var titleEntryDb = new TitleEntryDb()
            {
                MalId = anime.MalId,
                Title = titleEntry.Title,
                Type = titleEntry.Type
            };
            ToWatchListTitles.Insert(titleEntryDb);
        }
        var seasonNumberDb = new SeasonNumberDb()
        {
            MalId = anime.MalId,
            // need to figure out how to get the season number
        };
        ToWatchListSeasons.Insert(seasonNumberDb);
        ConsoleExt.WriteLineWithPretext("Anime Successfully added to the Anime List!", ConsoleExt.OutputType.Info);
    }
    
    public static void RemoveFromAnimeList(Anime anime)
    {
        ToWatchList.DeleteMany(x => x.MalId == anime.MalId);
        ToWatchListTitles.DeleteMany(x => x.MalId == anime.MalId);
        ToWatchListSeasons.DeleteMany(x => x.MalId == anime.MalId);
        ConsoleExt.WriteLineWithPretext("Anime Successfully removed to the Anime List!", ConsoleExt.OutputType.Info);
    }

    public static bool CheckAnimeExistence(long? malId)
    {
        var anime = ToWatchList.FindOne(x => x != null && x.MalId == malId);
        var animeTitle = ToWatchListTitles.FindOne(x => x != null && x.MalId == malId);
        return anime != null || animeTitle != null;
    }

    public static Anime? FindAnimeById(long malId)
    {
        return AnimeList.FindOne(x => x != null && x.MalId == malId);
    }
    
    public static Anime? GetAnimeWithTitle(string title)
    {
        var similarityPercentage = JsonFileUtility.GetValue<int>(UserSettingsFile, "SimilarityPercentage");
        var normalizedTitle = NormalizeTitle(title);

        // Fetch potential matches from the database 
        var potentialMatches = FetchPotentialMatchesFromDatabase(normalizedTitle);

        var enumerable = potentialMatches.ToList();

        // Use Process.ExtractTop() to get the best match
        var matches = Process.ExtractTop(normalizedTitle, enumerable);

        var extractedResults = matches as ExtractedResult<string>[] ?? matches.ToArray();
        if (!extractedResults.Any() || extractedResults.First().Score <= similarityPercentage) return null;
        
        // Use LiteDB's Query syntax to find the first matching record based on the title
        var titleEntryDb = TitleEntryList.Find(Query.EQ("Title", extractedResults.First().Value)).FirstOrDefault();

        if (titleEntryDb == null) return null;
        var malId = titleEntryDb.MalId;
        return AnimeList.FindOne(Query.EQ("MalId", malId));

    }

    private static string NormalizeTitle(string title)
    {
        return title.ToLower().Trim();
    }
    
    private static IEnumerable<string> FetchPotentialMatchesFromDatabase(string normalizedTitle)
    {
        var potentialTitles = new HashSet<string>();
        var characterSearchRange = JsonFileUtility.GetValue<int>(UserSettingsFile, "CharacterSearchRange");
    
        // Fetch all potential TitleEntryDb from the database
        var allTitleEntries = TitleEntryList.FindAll().ToHashSet();

        // Filter titles based on the first N characters
        foreach (var titleEntry in from titleEntry in allTitleEntries where titleEntry.Title != null let firstNCharacters = titleEntry.Title[..Math.Min(characterSearchRange, titleEntry.Title.Length)] where firstNCharacters.ToCharArray().Any(normalizedTitle.Contains) select titleEntry)
        {
            potentialTitles.Add(titleEntry.Title);
        }

        return potentialTitles;
    }
        
    public static string GetAnimeTitleWithAnime(Anime? anime)
    {
        string? englishTitle = null;
        string? defaultTitle = null;

        if (anime != null)
            foreach (var title in anime.Titles)
                switch (title.Type.ToLower())
                {
                    case "english":
                        englishTitle = title.Title;
                        break;
                    //default is mostly japanese in English characters
                    case "default":
                        defaultTitle = title.Title;
                        break;
                }

        if (englishTitle != null) return englishTitle;

        return defaultTitle ?? "";
    }

    
    [Obsolete("UpdateNullDbPlaces is not being used anymore and will be replaced soon")]
    private static void UpdateNullDbPlaces()
    {
        //need to rework this to find each null place in order, request with jikan to see if there is new information and then input that information to that line where the null was
        int id = 1;
        using var stream = File.OpenRead("JsonPath");
        using var reader = new StreamReader(stream);

        var lineCount = 1;

        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && line.ToLower() != "null") id = lineCount;
            lineCount++;
        }
    }
    
    [Obsolete("ConvertAnimeDb is not being used anymore and will be removed soon")]
    public static void ConvertAnimeDb()
    {
        var animeData = JsonFileUtility.ReadFromJsonFile("JsonPath");
        using (var db = new LiteDatabase(HelperClass.GetFileInProgramFolder("DataBase.db")))
        {
            var col = db.GetCollection<Anime>("Anime");

            foreach (var anime in animeData.Where(anime => anime != null))
            {
                if (anime != null) col.Insert(anime);
            }
        }

        ConsoleExt.WriteLineWithPretext("Done Converting Database", ConsoleExt.OutputType.Info);
    }
    
    /*
    public static void TestNestedPropertyLimitation()
    {
        var db = new LiteDatabase(@HelperClass.GetFileInProgramFolder("TestDatabase.db"));
        var col = db.GetCollection<TestData>("testData");

        // Insert some sample data
        col.Insert(new TestData { Titles = new List<TitleEntry> { new TitleEntry { Type = "english", Title = "SampleTitle1" } } });
        col.Insert(new TestData { Titles = new List<TitleEntry> { new TitleEntry { Type = "japanese", Title = "SampleTitle2" } } });

        // Direct query
        var directResults = col.Find(Query.EQ("Titles.Title", "SampleTitle1")).ToList();
        Console.WriteLine($"Direct Query Results Count: {directResults.Count}");

        // Wildcard query
        var wildcardResults = col.Find(Query.EQ("Titles.$.Title", "SampleTitle1")).ToList();
        Console.WriteLine($"Wildcard Query Results Count: {wildcardResults.Count}");

        // List item query (e.g., query the first item in the Titles list)
        var listItemResults = col.Find(Query.EQ("Titles[0].Title", "SampleTitle1")).ToList();
        Console.WriteLine($"List Item Query Results Count: {listItemResults.Count}");
    }

    private class TestData
    {
        public int Id { get; set; }
        public List<TitleEntry> Titles { get; set; }
    }
    */
}

public class TitleEntryDb : TitleEntry
{
    public long? MalId { get; init; }
}

public class SeasonNumberDb
{
    public long? MalId { get; init; }
    public int[]? SeasonNumbers { get; init; }
}