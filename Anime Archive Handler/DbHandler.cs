using System.Globalization;
using FuzzySharp;
using FuzzySharp.Extractor;
using JikanDotNet;
using CsvHelper;
using CsvHelper.Configuration;
using LiteDB;
using Riok.Mapperly.Abstractions;

namespace Anime_Archive_Handler;

using static AnimeArchiveHandler;
using static FileHandler;

public static class DbHandler
{
    private static readonly LiteDatabase Db = new(GetFileInProgramFolder("DataBase.db")); // need to cache those in the program settings and only search for them again if it cant be found in the same location
    private static readonly LiteDatabase Al = new(GetFileInProgramFolder("AnimeList.db"));
    private static readonly LiteDatabase Ats = new(GetFileInProgramFolder("Animetosho.db"));
    private static readonly LiteDatabase Ts = new(GetFileInProgramFolder("TestDatabase.db")); // for testing purposes only
    internal static readonly ILiteCollection<AnimeDto> AnimeDb = Db.GetCollection<AnimeDto>("Anime"); //loads anime database
    private static readonly ILiteCollection<TitleEntryDb> TitleEntryList = Db.GetCollection<TitleEntryDb>("TitleEntry");
    private static readonly ILiteCollection<AnimeDto> ToWatchList = Al.GetCollection<AnimeDto>("ToWatch");
    private static readonly ILiteCollection<TitleEntryDb> ToWatchListTitles = Al.GetCollection<TitleEntryDb>("ToWatchTitleEntry");
    private static readonly ILiteCollection<Animetosho> Animetosho = Ats.GetCollection<Animetosho>("Animetosho");
    private static readonly ILiteCollection<AnimeDto> AnimeDtoTest = Ts.GetCollection<AnimeDto>("AnimeDto"); // for testing purposes only
    
    private static readonly string CsvFile = GetFileInProgramFolder("torrents-latest.csv");

    public static void EnsureIndexDb()
    {
        // Ensure index on MalId
        AnimeDb.EnsureIndex(x => x.MalId);
        TitleEntryList.EnsureIndex(x => x.MalId);
        ToWatchList.EnsureIndex(x => x.MalId);
        ToWatchListTitles.EnsureIndex(x => x.MalId);
        
        // Ensure index on Title
        TitleEntryList.EnsureIndex(x => x.Title);
        ToWatchListTitles.EnsureIndex(x => x.Title);
    }

    public static void PopulateTitleEntryDb()
    {
        var animes = AnimeDb.FindAll();
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
    
    public static void SaveToAnimeList(AnimeDto anime, int[]? seasonNumber)
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
        ConsoleExt.WriteLineWithPretext("Anime Successfully added to the Anime List!", ConsoleExt.OutputType.Info);
    }
    
    public static void RemoveFromAnimeList(long? animeId)
    {
        ToWatchList.DeleteMany(x => x.MalId == animeId);
        ToWatchListTitles.DeleteMany(x => x.MalId == animeId);
        ConsoleExt.WriteLineWithPretext("Anime Successfully removed to the Anime List!", ConsoleExt.OutputType.Info);
    }

    public static bool CheckAnimeExistence(long? malId)
    {
        var anime = ToWatchList.FindOne(x => x != null && x.MalId == malId);
        var animeTitle = ToWatchListTitles.FindOne(x => x != null && x.MalId == malId);
        return anime != null || animeTitle != null;
    }

    public static object? FindById<T>(long? malId, ILiteCollection<T> database)
    {
        if (database == AnimeDb)
        {
            ILiteCollection<AnimeDto?>? d = database as ILiteCollection<AnimeDto?>;
            return d?.FindOne(x => x != null && x.MalId == malId);
        }

        if (database != ToWatchListTitles) return null;
        {
            ILiteCollection<TitleEntryDb?>? d = database as ILiteCollection<TitleEntryDb?>;
            return d?.FindOne(x => x != null && x.MalId == malId);
        }
    }

    public static long? FindLastAnimeIdInDb()
    {
        return AnimeDb.FindOne(Query.All("MalId", Query.Descending)).MalId;
    }
    
    public static AnimeDto? GetAnimeWithTitle(string title)
    {
        var similarityPercentage = JsonFileUtility.GetValue<int>(UserSettingsFile, "SimilarityPercentage");
        var normalizedTitle = NormalizeTitle(title);

        // Fetch potential matches from the database 
        var potentialMatches = FetchPotentialMatchesFromDatabase(normalizedTitle);

        var enumerable = potentialMatches.ToList();

        // Use Process.ExtractTop() to get the best match
        var matches = Process.ExtractTop(normalizedTitle, enumerable);

        var extractedResults = matches as ExtractedResult<string>[] ?? matches.ToArray();
        if (extractedResults.Length == 0 || extractedResults.First().Score <= similarityPercentage) return null;
        
        // Use LiteDB's Query syntax to find the first matching record based on the title
        var titleEntryDb = TitleEntryList.Find(Query.EQ("Title", extractedResults.First().Value)).FirstOrDefault();

        if (titleEntryDb == null) return null;
        var malId = titleEntryDb.MalId;
        return AnimeDb.FindOne(Query.EQ("MalId", malId));

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

        // Filter titles based on the first number of characters
        foreach (var titleEntry in from titleEntry in allTitleEntries where titleEntry.Title != null let firstNCharacters = titleEntry.Title[..Math.Min(characterSearchRange, titleEntry.Title.Length)] where firstNCharacters.ToCharArray().Any(normalizedTitle.Contains) select titleEntry)
        {
            potentialTitles.Add(titleEntry.Title);
        }

        return potentialTitles;
    }
        
    public static string GetAnimeTitleWithAnime(AnimeDto? anime)
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

    internal static void CsvToDb(string csvFilePath)
    {
        // Create or open a LiteDB database
        using var reader = new StreamReader(csvFilePath);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true, // Assuming your CSV file has a header row
        };

        using var csv = new CsvReader(reader, config);

        while (csv.Read())
        {
            try
            {
                var record = csv.GetRecord<Animetosho>();
                Animetosho.Upsert(record!);
            }
            catch (Exception ex)
            {
                ErrorLogger("Processing Record Failed! ", ex);
            }
        }

        ConsoleExt.WriteLineWithPretext("Finished importing CSV into database", ConsoleExt.OutputType.Info);
    }

    internal static AnimeDto RemapToAnimeDto(Anime anime)
    {
        var mapper = new MapperlyMaps();
        return mapper.AnimeDto(anime);
    }
}

public class TitleEntryDb : TitleEntry
{
    public long? MalId { get; init; }
}

[Mapper(UseDeepCloning = true, IgnoreObsoleteMembersStrategy = IgnoreObsoleteMembersStrategy.Both)]
public partial class MapperlyMaps
{
    public partial AnimeDto AnimeDto(Anime anime);
}