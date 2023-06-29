namespace Anime_Archive_Handler;

using static JikanHandler;
using static JsonFileUtility;
using JikanDotNet;


public static class AnimeListHandler
{
    //if the anime already exists in the list, dont add it, if it doesnt, add it
    //need to also add a season feature to the list that allows for the anime list to also have the season information that tells what season the user wants

    private static string _animeList = GetValue<string>(HelperClass.GetFileInProgramFolder("UserSettings.json"), "AnimeListOutput");
    private static readonly string AnimeListBackup = HelperClass.GetFileInProgramFolder("AnimeList.json");
    private static List<Anime?>? _anime;

    public static void StartAnimeListEditing()
    {
        if (string.IsNullOrEmpty(_animeList))
        {
            _animeList = AnimeListBackup;
        }
        
        ConsoleExt.WriteLineWithPretext($"Anime List is Stored at: {_animeList}", ConsoleExt.OutputType.Info);
        CheckFileExistence();
        UpdateList();

        string? animeName;

        string? inputString = Console.ReadLine();
        if (inputString != null && (inputString.Contains("https://") || inputString.Contains("http://")))
        {
            animeName = HelperClass.UrlNameExtractor(inputString);
        }
        else
        {
            animeName = inputString;
        }

        if (animeName == null) return;
        var animeToAdd = GetAnimeWithTitle(animeName);

        bool nonExistent = true;
        if (_anime != null)
        {
            foreach (var unused in _anime.Where(anime => GetAnimeTitleWithAnime(animeToAdd) == GetAnimeTitleWithAnime(anime)))
            {
                nonExistent = false;
            }
        }

        if (nonExistent)
        {
            WriteToJsonFile(_animeList, animeToAdd);
        }
    }

    private static void UpdateList()
    {
        _anime = ReadFromJsonFile(_animeList);
        ConsoleExt.WriteLineWithPretext("Loaded/Updated Anime List", ConsoleExt.OutputType.Info);
    }
    
    private static void CheckFileExistence()
    {
        if (!File.Exists(_animeList))
        {
            File.Create(_animeList);
        }
    }
    
    
}