namespace Anime_Archive_Handler;

using JikanDotNet.Config;
using JikanDotNet;
using FuzzySharp;

public static class JikanHandler
{
    public static readonly string JsonPath = HelperClass.GetFileInProgramFolder("DataBase.json");
    private static int _id = 1;
    private static Anime? _anime;
    private static int _consecutiveNulls;
    private static List<Anime?>? _animes;
    
    //need to look into making the json database update every so often
    //need to look into hosting my own jikan API server
    //when updating i only need to check the line number of the json file, and then check if it says null, and if it doesnt, then it doesnt need to update but just skip it
    //but if i want to update information i need to check the line number and then update the information

    public static async Task Start()
    {
        if (!File.Exists(JsonPath))
        {
            await using (FileStream unused = File.Create(JsonPath)) { }
        }
        _animes = JsonFileUtility.ReadFromJsonFile(JsonPath);
        var rateLimiter = new RateLimiter(40, 60000);

        while (true)
        {
            if (rateLimiter.Check())
            {
                //need to check if the specific malId is not contained in the json file, if it is, it will skip it, if it isn't but there is a null on the line,
                //it will overwrite it, if there is no null and it actually doesnt exist it will add it, if it exists but some of the information changed it will overwrite it
                _anime = await GetAnime(_id);
                JsonFileUtility.WriteToJsonFile(JsonPath, _anime);
                _id++;
                if (_anime == null)
                {
                    _consecutiveNulls++;
                }
                else
                {
                    _consecutiveNulls = 0;
                }
                if (_consecutiveNulls <= 500)
                {
                    break;
                }
            }
            else
            {
                ConsoleExt.WriteLineWithPretext("Function skipped", ConsoleExt.OutputType.Warning);
            }

            _animes = JsonFileUtility.ReadFromJsonFile(JsonPath);

            await Task.Delay(1000);
        }
    }

    private static async Task<Anime?> GetAnime(int id)
    {
        ConsoleExt.WriteWithPretext("Getting Anime with ID: " + _id, ConsoleExt.OutputType.Info);
        IJikan jikan = new Jikan(new JikanClientConfiguration { SuppressException = true });
        BaseJikanResponse<Anime> responseString = await jikan.GetAnimeAsync(id);
        if (responseString != null)
        {
            Anime anime = responseString.Data;
            string? englishName = null;
            string? defaultName = null;
            if (anime is { Titles: not null })
            {
                foreach (var title in anime.Titles)
                {
                    switch (title.Type.ToLower())
                    {
                        case "english":
                            englishName = title.Title;
                            break;
                        //default is mostly japanese in english characters
                        case "default":
                            defaultName = title.Title;
                            break;
                    }
                }

                switch (englishName)
                {
                    case null when defaultName == null:
                        Console.Write(", English and Default Title Not Found!");
                        break;
                    case null:
                        Console.Write(", Name: " + defaultName);
                        break;
                    default:
                        Console.Write(", Name: " + englishName);
                        break;
                }
            }
            Console.WriteLine();
            //await Task.Delay(1000);
            return anime;
        }

        Console.Write(", Anime not found!");
        Console.WriteLine();
        return null;
    }

    public static void LoadAnimeDb()
    {
        _animes = JsonFileUtility.ReadFromJsonFile(JsonPath);
        ConsoleExt.WriteLineWithPretext("Done Reading Json", ConsoleExt.OutputType.Info);
    }
    
    public static string GetAnimeTitleWithMalId(long? id)
    {
        string? englishTitle = null;
        string? defaultTitle = null;
        
        foreach (var title in _animes!.Where(anime => anime!.MalId == id).SelectMany(anime => anime!.Titles))
        {
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
        }

        if (englishTitle != null)
        {
            return englishTitle;
        }

        return defaultTitle ?? "";
    }
    
    public static string GetAnimeTitleWithAnime(Anime? anime)
    {
        string? englishTitle = null;
        string? defaultTitle = null;

        if (anime != null)
        {
            foreach (var title in anime.Titles)
            {
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
            }
        }

        if (englishTitle != null)
        {
            return englishTitle;
        }

        if (defaultTitle != null)
        {
            return defaultTitle;
        }

        return "";
    }

    public static Anime? GetAnimeWithTitle(string title)
    {
        string? englishTitle = null;
        string? defaultTitle = null;

        foreach (var anime in _animes!)
        {
            foreach (var animeTitle in anime!.Titles)
            {
                //need to figure out how to implement partial search that gets executed when it cant find anything under the full input title 
                switch (animeTitle.Type.ToLower())
                {
                    case "english":
                        englishTitle = animeTitle.Title;
                        break;
                    //default is mostly japanese in english characters
                    case "default":
                        defaultTitle = animeTitle.Title;
                        break;
                }
            }

            if (englishTitle != null || defaultTitle != null)
            {
                string? normalizedEnglishTitle = englishTitle?.ToLower().Trim();
                string? normalizedDefaultTitle = defaultTitle?.ToLower().Trim();
                string normalizedTitle = title.ToLower().Trim();
                if (normalizedEnglishTitle != null)
                {
                    // Perform fuzzy matching using FuzzySharp's token set ratio
                    int similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedEnglishTitle);
                    
                    // Check if the similarity exceeds a certain threshold (e.g., 70%)
                    if (similarity > 80)
                    {
                        return anime; // Found a matching movie
                    }
                }
                if (normalizedDefaultTitle != null)
                {
                    // Perform fuzzy matching using FuzzySharp's token set ratio
                    int similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedDefaultTitle);
                    
                    // Check if the similarity exceeds a certain threshold (e.g., 70%)
                    if (similarity > 80)
                    {
                        return anime; // Found a matching movie
                    }
                }
            }
        }
        
        return null; //need to ask the user if it comes to this point
    }
}

class RateLimiter
{
    private int _count;
    private readonly int _limit;
    private readonly int _interval;
    private readonly Timer _timer;

    public RateLimiter(int limit, int interval)
    {
        _limit = limit;
        _interval = interval;
        _timer = new Timer(ResetCounter!, null, interval, interval);
    }

    private void ResetCounter(object state)
    {
        _count = 0;
    }

    public bool Check()
    {
        if (_count >= _limit)
        {
            return false;
        }

        _count++;
        return true;
    }
}