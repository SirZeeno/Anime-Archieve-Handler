using FuzzySharp;
using FuzzySharp.Extractor;
using JikanDotNet;
using LiteDB;

namespace Anime_Archive_Handler;

public static class DbHandler
{
    private static readonly LiteDatabase Db = new(HelperClass.GetFileInProgramFolder("DataBase.db"));
    public static ILiteCollection<Anime> AnimeList = Db.GetCollection<Anime>("Anime"); //loads anime database

    public static void EnsureIndexDb()
    {
        // Ensure index on Titles.Title
        AnimeList.EnsureIndex("Titles.$.Title");
        
        // Ensure index on MalId
        AnimeList.EnsureIndex(x => x.MalId);
    }

    public static Anime? FindAnimeById(int malId)
    {
        return AnimeList.FindOne(x => x != null && x.MalId == malId);
    }
    
    public static Anime? GetAnimeWithTitle(string title)
    {
        var similarityPercentage = JsonFileUtility.GetValue<int>(HelperClass.GetFileInProgramFolder("UserSettings.json"), "SimilarityPercentage");
        var normalizedTitle = NormalizeTitle(title);
    
        // Fetch potential matches from the database 
        var potentialMatches = FetchPotentialMatchesFromDatabase(normalizedTitle);

        // Use Process.ExtractTop() to get the best match
        var matches = Process.ExtractTop(normalizedTitle, potentialMatches.Keys.ToList());

        var extractedResults = matches as ExtractedResult<string>[] ?? matches.ToArray();
        if (extractedResults.Any() && extractedResults.First().Score > similarityPercentage)
        {
            return potentialMatches[extractedResults.First().Value];
        }

        return null;
    }

    private static string NormalizeTitle(string title)
    {
        return title.ToLower().Trim();
    }

    private static Dictionary<string, Anime> FetchPotentialMatchesFromDatabase(string normalizedTitle)
    {
        var titlesWithAnime = new Dictionary<string, Anime>();
        
        // Fetch a broader subset of animes. Adjust this based on your dataset's size and distribution.
        var potentialAnimes = AnimeList.FindAll().ToList();

        foreach (var anime in potentialAnimes)
        {
            if (anime.Titles != null)
            {
                foreach (var titleEntry in anime.Titles)
                {
                    if (!string.IsNullOrEmpty(titleEntry.Title) && 
                        NormalizeTitle(titleEntry.Title).StartsWith(normalizedTitle.Substring(0, 1)))
                    {
                        titlesWithAnime[NormalizeTitle(titleEntry.Title)] = anime;
                    }
                }
            }
        }

        return titlesWithAnime;
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
                    //default is mostly japanese in english characters
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