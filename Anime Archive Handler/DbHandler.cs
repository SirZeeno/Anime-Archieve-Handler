using FuzzySharp;
using JikanDotNet;
using LiteDB;

namespace Anime_Archive_Handler;

public static class DbHandler
{
    private static readonly LiteDatabase Db = new(HelperClass.GetFileInProgramFolder("DataBase.db"));
    public static ILiteCollection<Anime> AnimeList = Db.GetCollection<Anime>("Anime"); //loads anime database

    public static Anime? FindAnimeById(int malId)
    {
        return AnimeList.FindOne(x => x != null && x.MalId == malId);
    }

    public static Anime? GetAnimeWithTitle(string title)
    {
        string? englishTitle = null;
        string? defaultTitle = null;
        var similarityPercentage =
            JsonFileUtility.GetValue<int>(HelperClass.GetFileInProgramFolder("UserSettings.json"),
                "SimilarityPercentage");
        
        //it adds both languages to a list looks for the highest similarity on both languages and checks if they have the same malId

        foreach (var anime in AnimeList.FindAll())
        {
            var synonymTitles = new List<string>();
            foreach (var animeTitle in anime!.Titles)
                switch (animeTitle.Type.ToLower())
                {
                    case "english":
                        englishTitle = animeTitle.Title;
                        break;
                    //default is mostly japanese in english characters
                    case "default":
                        defaultTitle = animeTitle.Title;
                        break;
                    case "synonym":
                        synonymTitles.Add(animeTitle.Title);
                        break;
                }

            if (englishTitle == null && defaultTitle == null) continue;
            var normalizedEnglishTitle = englishTitle?.ToLower().Trim();
            var normalizedDefaultTitle = defaultTitle?.ToLower().Trim();
            var normalizedSynonymTitles = synonymTitles.Select(synonymTitle => synonymTitle.ToLower().Trim()).ToList();
            var normalizedTitle = title.ToLower().Trim();

            if (normalizedEnglishTitle != null)
            {
                // Perform fuzzy matching using FuzzySharp's token set ratio
                var similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedEnglishTitle);

                // Check if the similarity exceeds a certain threshold (e.g., 80%)
                if (similarity > similarityPercentage) return anime; // Found a matching anime
            }

            if (normalizedDefaultTitle != null)
            {
                // Perform fuzzy matching using FuzzySharp's token set ratio
                var similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedDefaultTitle);

                // Check if the similarity exceeds a certain threshold (e.g., 80%)
                if (similarity > similarityPercentage) return anime; // Found a matching anime
            }

            if (normalizedSynonymTitles
                .Select(normalizedSynonymTitle => Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedSynonymTitle))
                .Any(similarity => similarity > similarityPercentage)) return anime; // Found a matching anime
        }

        return null; //need to ask the user if it comes to this point
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
}