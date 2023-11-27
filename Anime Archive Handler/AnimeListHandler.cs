using System.Text.RegularExpressions;
using JikanDotNet;

namespace Anime_Archive_Handler;

using static AnimeArchiveHandler;
using static InputStringHandler;
using static DbHandler;
using static JsonFileUtility;
using static FileHandler;

public static class AnimeListHandler
{
    //need to also add a season feature to the list that allows for the anime list to also have the season information that tells what season the user wants

    private static string _animeList =
        GetValue<string>(UserSettingsFile, "AnimeListOutput");

    private static readonly string AnimeListBackup = HelperClass.GetFileInProgramFolder("AnimeList.db");
    private static List<Anime?>? _anime;

    public static void StartAnimeListEditing()
    {
        if (string.IsNullOrEmpty(_animeList)) _animeList = AnimeListBackup;

        ConsoleExt.WriteLineWithPretext($"Anime List is Stored at: {_animeList}", ConsoleExt.OutputType.Info);
        CheckFileExistence(_animeList);
        
        //being able to add different seasons of the same anime to the database

        while (true)
        {
            _anime = new List<Anime?>();
            ConsoleExt.WriteLineWithPretext("What Anime would you like to Add/Remove to the List?",
                ConsoleExt.OutputType.Question);
            Console.Write("Anime Name or URL: ");

            string? inputString = Console.ReadLine();
            string pattern = Regex.Escape("Anime Name or URL: ");
            if (inputString == null) return;
            string cutInputString = Regex.Replace(inputString, pattern, "");
            string? animeName = CheckIfUrl(cutInputString);

            // Adds the anime to the list
            if (cutInputString.StartsWith("+"))
            {
                AddAnime(animeName);
            }

            // Removes the anime from the list
            if (cutInputString.StartsWith("-"))
            {
                RemoveAnime(animeName);
            }

            //if nothing is there then its going to look them up in the database and let the user decide what to do depending on the result
            else
            {
                bool animeExistences = animeName != null && CheckAnimeExistence(GetAnimeWithTitle(animeName)?.MalId);
                if (animeExistences)
                {
                    if (!HelperClass.ManualInformationChecking(
                            "Anime already exists in the Anime List! Would you like to remove it?")) continue;
                    RemoveAnime(animeName);
                }
                else
                {
                    if (!HelperClass.ManualInformationChecking(
                            "Anime does not exist in the Anime List! Would you like to add it?")) continue;
                    AddAnime(animeName);
                }
            }
        }
    }

    private static string? CheckIfUrl(string cutInputString)
    {
        if (cutInputString.Contains("https://") || cutInputString.Contains("http://"))
        {
            string animeName = UrlNameExtractor(cutInputString);
            ConsoleExt.WriteLineWithPretext($"Anime Name: {animeName}", ConsoleExt.OutputType.Info);
            return animeName;
        }
        else
        {
            return cutInputString;
        }
    }

    private static void AddAnime(string? animeName)
    {
        if (animeName != null)
        {
            ExtractingSeasonNumber(animeName);
            _anime?.Add(GetAnimeWithTitle(RemoveUnnecessaryNamePieces(animeName)));
        }

        if (_anime == null) return;
        foreach (var anime in _anime.Where(anime => anime != null))
        {
            if (anime != null) SaveToAnimeList(anime);
        }
    }

    private static void RemoveAnime(string? animeName)
    {
        if (animeName != null)
        {
            ExtractingSeasonNumber(animeName);
            _anime?.Add(GetAnimeWithTitle(RemoveUnnecessaryNamePieces(animeName)));
        }

        if (_anime == null) return;
        foreach (var anime in _anime.Where(anime => anime != null))
        {
            if (anime != null) RemoveFromAnimeList(anime);
        }
    }

}