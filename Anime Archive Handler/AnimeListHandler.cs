namespace Anime_Archive_Handler;

public static class AnimeListHandler
{
    //i need an input system that takes an input from the user and compares the anime name with the database via partial search, looking if the anime has an
    //english name, if it does, use that name for the entry.
    //if the anime already exists in the list, dont add it, if it doesnt, add it
    //need to also add a season feature to the list that allows for the anime list to also have the season information that tells what season the user wants

    private static string _animeList = JsonFileUtility.GetValue<string>(HelperClass.GetFileInProgramFolder("UserSettings.json"), "AnimeListOutput");
    private static readonly string AnimeListBackup = HelperClass.GetFileInProgramFolder("AnimeList.json");

    public static void StartAnimeListEditing()
    {
        if (string.IsNullOrEmpty(_animeList))
        {
            _animeList = AnimeListBackup;
        }

        ConsoleExt.WriteLineWithPretext($"Anime List is Stored at: {_animeList}", ConsoleExt.OutputType.Info);

        CheckFileExistence();
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
        var animeToAdd = JikanHandler.GetAnimeWithTitle(animeName);
        
        JsonFileUtility.WriteToJsonFile(_animeList, animeToAdd);
    }
    
    private static void CheckFileExistence()
    {
        if (!File.Exists(_animeList))
        {
            File.Create(_animeList);
        }
    }
    
    
}