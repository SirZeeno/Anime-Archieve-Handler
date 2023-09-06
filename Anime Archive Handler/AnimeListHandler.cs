using System.Text.RegularExpressions;
using JikanDotNet;

namespace Anime_Archive_Handler;

using static AnimeArchiveHandler;
using static InputStringHandler;
using static DbHandler;
using static JsonFileUtility;

public static class AnimeListHandler
{
    //if the anime already exists in the list, dont add it, if it doesnt, add it
    //need to also add a season feature to the list that allows for the anime list to also have the season information that tells what season the user wants

    private static string _animeList =
        GetValue<string>(UserSettingsFile, "AnimeListOutput");

    private static readonly string AnimeListBackup = HelperClass.GetFileInProgramFolder("AnimeList.db");
    private static List<Anime?>? _anime;

    public static void StartAnimeListEditing()
    {
        if (string.IsNullOrEmpty(_animeList)) _animeList = AnimeListBackup;

        ConsoleExt.WriteLineWithPretext($"Anime List is Stored at: {_animeList}", ConsoleExt.OutputType.Info);
        CheckFileExistence();

        string? animeName;
        ConsoleExt.WriteLineWithPretext("What Anime would you like to add to the List?", ConsoleExt.OutputType.Question);
        Console.Write("Anime Name or URL: ");

        string? inputString = Console.ReadLine();
        string pattern = Regex.Escape("Anime Name or URL: ");
        if (inputString == null) return;
        string cutInputString = Regex.Replace(inputString, pattern, "");
        if (cutInputString.Contains("https://") || cutInputString.Contains("http://"))
        {
            animeName = UrlNameExtractor(cutInputString);
            ConsoleExt.WriteLineWithPretext($"Anime Name: {animeName}", ConsoleExt.OutputType.Info);
        }
        else
        {
            animeName = cutInputString;
        }

        ExtractingSeasonNumber(animeName);
        var animeToAdd = GetAnimeWithTitle(RemoveUnnecessaryNamePieces(animeName));

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
            //WriteToJsonFile(_animeList, animeToAdd);
        }
    }

    private static bool ExistsWithinList(string animeTitle)
    {
        var anime = GetAnimeWithTitle(animeTitle);
        
        return false;
    }

    private static void UpdateAnimeList()
    {
        _anime = ReadFromJsonFile(_animeList);
        ConsoleExt.WriteLineWithPretext("Updated Anime List", ConsoleExt.OutputType.Info);
    }

    private static void CheckFileExistence()
    {
        if (!File.Exists(_animeList)) File.Create(_animeList);
    }
}