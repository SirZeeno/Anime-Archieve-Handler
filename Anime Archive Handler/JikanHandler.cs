using System.Text;

namespace Anime_Archive_Handler;

using JikanDotNet.Config;
using JikanDotNet;
using MessagePack;
using FuzzySharp;
using AutoMapper;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        CreateMap<Anime, AnimeDto>();
        CreateMap<TitleEntry, TitleEntryDto>();
        CreateMap<MalUrl, MalUrlDto>();
        // Add more mappings if needed
    }
}

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
        LoadAnimeDb();
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

            LoadAnimeDb();

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
        ConsoleExt.WriteLineWithPretext("Done Reading Database", ConsoleExt.OutputType.Info);
    }
    
    public static void ConvertAnimeDb()
    {
        // Configuration to map properties from Anime to AnimeDto
        MapperConfiguration config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new AutoMapperProfile());
        });

        // Create a mapper instance
        IMapper mapper = config.CreateMapper();

        List<Anime?> animeData = JsonFileUtility.ReadFromJsonFile(JsonPath);

        List<byte[]> serializedDataList = new List<byte[]>();

        foreach (var anime in animeData)
        {
            AnimeDto animeDto = mapper.Map<AnimeDto>(anime);
            byte[] binaryData = MessagePackSerializer.Serialize(animeDto);
            
            // Debug statements to check the 'synopsis' property and its serialization output
            Console.WriteLine($"Synopsis: {animeDto.Synopsis}");

            serializedDataList.Add(binaryData);
        }

        // Concatenate all serialized data into a single byte array
        byte[] concatenatedData = serializedDataList.SelectMany(data => data).ToArray();

        // Write the concatenated binary data to the binary file
        File.WriteAllBytes(HelperClass.GetFileInProgramFolder("DataBase.bin"), concatenatedData);

        ConsoleExt.WriteLineWithPretext("Done Converting Database", ConsoleExt.OutputType.Info);
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

        return defaultTitle ?? "";
    }

    public static Anime? GetAnimeWithTitle(string title)
    {
        string? englishTitle = null;
        string? defaultTitle = null;
        int similarityPercentage = JsonFileUtility.GetValue<int>(HelperClass.GetFileInProgramFolder("UserSettings.json"), "SimilarityPercentage");
        
        //it adds both languages to a list looks for the highest similarity on both languages and checks if they have the same malId

        foreach (var anime in _animes!)
        {
            List<string> synonymTitles = new List<string>();
            foreach (var animeTitle in anime!.Titles)
            {
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
            }

            if (englishTitle == null && defaultTitle == null) continue;
            string? normalizedEnglishTitle = englishTitle?.ToLower().Trim();
            string? normalizedDefaultTitle = defaultTitle?.ToLower().Trim();
            List<string> normalizedSynonymTitles = synonymTitles.Select(synonymTitle => synonymTitle.ToLower().Trim()).ToList();
            string normalizedTitle = title.ToLower().Trim();

            if (normalizedEnglishTitle != null)
            {
                // Perform fuzzy matching using FuzzySharp's token set ratio
                int similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedEnglishTitle);

                // Check if the similarity exceeds a certain threshold (e.g., 80%)
                if (similarity > similarityPercentage)
                {
                    return anime; // Found a matching anime
                }
            }
            if (normalizedDefaultTitle != null)
            {
                // Perform fuzzy matching using FuzzySharp's token set ratio
                int similarity = Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedDefaultTitle);
                    
                // Check if the similarity exceeds a certain threshold (e.g., 80%)
                if (similarity > similarityPercentage)
                {
                    return anime; // Found a matching anime
                }
            }

            if (normalizedSynonymTitles.Select(normalizedSynonymTitle => Fuzz.TokenDifferenceRatio(normalizedTitle, normalizedSynonymTitle)).Any(similarity => similarity > similarityPercentage))
            {
                return anime; // Found a matching anime
            }
        }
        
        return null; //need to ask the user if it comes to this point
    }

    private static void UpdateNullDbPlaces()
    {
        //need to rework this to find each null place in order, request with jikan to see if there is new information and then input that information to that line where the null was
        using var stream = File.OpenRead(JsonPath);
        using var reader = new StreamReader(stream);
    
        var lineCount = 1;

        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && line.ToLower() != "null")
            {
                _id = lineCount;
            }
            lineCount++;
        }
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