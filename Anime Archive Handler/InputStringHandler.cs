using System.Text.RegularExpressions;
using Humanizer;
using static System.Text.RegularExpressions.Regex;

namespace Anime_Archive_Handler;
using static AnimeArchiveHandler;
using static HelperClass;
using static FileHandler;

public static class InputStringHandler
{
    //removes all unnecessary pieces from the anime name
    internal static string RemoveUnnecessaryNamePieces(string fileName)
    {
        const string pattern1 = @"\[.*?\]|\(.*?\)";
        const string pattern2 = "_";
        const string pattern3 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        const string pattern4 = @"\d+(st|nd|rd|th)";
        const string pattern5 = @"(?<!['\w])[MCDLXVI]+(?![\w'])";
        const string pattern6 = @"\s{2,}.*$";

        var withoutBrackets = Replace(fileName, pattern1, string.Empty);
        var withoutUnderscore = Replace(withoutBrackets, pattern2, " ");
        var withoutSeason = Replace(withoutUnderscore, pattern3, " ");
        var withoutOrdinal = Replace(withoutSeason, pattern4, string.Empty);
        var withoutRomanNumerals = Replace(withoutOrdinal, pattern5, string.Empty);
        var withoutAfterSpaces =
            Replace(withoutRomanNumerals, pattern6, string.Empty); //removes everything after 2 spaces

        var output = withoutAfterSpaces.ApplyCase(LetterCasing.Title).TrimStart().TrimEnd();
        ConsoleExt.WriteLineWithPretext(output, ConsoleExt.OutputType.Info);
        return output;
    }
    
    public static string UrlNameExtractor(string? inputUrl)
    {
        if (inputUrl == null)
        {
            ConsoleExt.WriteLineWithPretext("Tried to extract Url but input was null!", ConsoleExt.OutputType.Error);
            return string.Empty;
        }
        
        const string pattern2 = @"\/ep-\d+";
        const string pattern3 = "-";
        const string pattern4 = @"\b\w+\s*$";

        if (inputUrl.Contains("gogoanime"))
        {
            const string pattern = @"https:\/\/gogoanime\.\w+\/watch\/";

            var removedWebsite = Replace(inputUrl, pattern, "");
            var removedEpisode = Replace(removedWebsite, pattern2, "");
            var removedDashes = Replace(removedEpisode, pattern3, " ");
            var removedLastWord = Replace(removedDashes, pattern4, "");
            ConsoleExt.WriteLineWithPretext(removedLastWord.Trim(), ConsoleExt.OutputType.Info);

            return removedLastWord.Trim();
        }

        if (inputUrl.Contains("anix"))
        {
            const string pattern = @"https:\/\/anix\.\w+\/anime\/";

            var removedWebsite = Replace(inputUrl, pattern, "");
            var removedEpisode = Replace(removedWebsite, pattern2, "");
            var removedDashes = Replace(removedEpisode, pattern3, " ");
            var removedLastWord = Replace(removedDashes, pattern4, "");
            ConsoleExt.WriteLineWithPretext(removedLastWord.Trim(), ConsoleExt.OutputType.Info);

            return removedLastWord.Trim();
        }
        return string.Empty;
    }

    //Checks if the Folder Name indicates if the anime has and OVA
    internal static bool HasOva(string fileName)
    {
        
        return false;
    }

    //Checks if the Folder Name indicates if the anime has multiple parts
    internal static bool HasMultipleParts(string fileName)
    {
        const string pattern = @"(?i)(Part|Parts|P)\s*(\d+)\s*[+\-]+\s*(\d+)";
        const string pattern2 = @"(?i)(Part|Parts|P)\s*(\d+)";

        var match1 = Match(fileName, pattern);
        var match2 = Match(fileName, pattern2);

        if (!match1.Success && !match2.Success)
        {
            ConsoleExt.WriteLineWithPretext("No Anime Season Part Found!", ConsoleExt.OutputType.Info);
            return false;
        }
        
        
        
        return true;
    }
    
    //extracts the season number from the folder name
    internal static void ExtractingSeasonNumber(string fileName)
    {
        const string pattern = @"(?i)(Season|Seasons|S)\s*(\d+)\s*[+\-]+\s*(\d+)";
        const string pattern4 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        const string pattern2 = @"(?i)\d+\s*(st|nd|rd|th)\s*(Season|Seasons|S)";
        const string pattern5 = @"(?<!['\w])[MCDLXVI]+(?![\w'])";

        var match1 = Match(fileName, pattern);
        var match2 = Match(fileName, pattern2);
        var match4 = Match(fileName, pattern4);
        var match5 = Match(fileName, pattern5);

        if (!match1.Success && !match2.Success && !match4.Success && !match5.Success)
        {
            ConsoleExt.WriteLineWithPretext("No Anime Season number Found!", ConsoleExt.OutputType.Warning);
            if (!HeadlessOperations)
            {
                ConsoleExt.WriteLineWithPretext("What Anime Season is it?", ConsoleExt.OutputType.Question);
                ConsoleExt.WriteLineWithPretext("Warning: numbers only in forms of 1,2,3 or 1+2+3 or 1-3", ConsoleExt.OutputType.Warning);
                string question = Escape("Season Number(s): ");
                Console.Write(question);
                var answer = Console.ReadLine();
                if (answer != null)
                {
                    string cutInputString = Replace(answer, question, "Season ");
                    ExtractingSeasonNumber(cutInputString);
                }
                return;
            }
            SeasonNumbers = new[] { 1 };
            ConsoleExt.WriteLineWithPretext($"Season Number: {SeasonNumbers[0]}", ConsoleExt.OutputType.Info);

            //ask the user what season the anime is.
            return;
        }

        const string pattern3 = @"[+-]";
        var match3 = Match(match1.Value, pattern3);

        if (!match3.Success)
        {
            SeasonNumbers = new int[1];
            if (int.TryParse(new string(match1.Value.Where(char.IsDigit).ToArray()), out SeasonNumbers[0]))
            {
            }
            else
            {
                if (int.TryParse(new string(match4.Value.Where(char.IsDigit).ToArray()), out SeasonNumbers[0]))
                {
                }
                else
                {
                    if (int.TryParse(new string(match2.Value.Where(char.IsDigit).ToArray()), out SeasonNumbers[0]))
                    {
                    }
                    else
                    {
                        if (int.TryParse(ConvertRomanToNumber(match5.Value).ToString(), out SeasonNumbers[0]))
                        {
                        }
                    }
                }
            }

            ConsoleExt.WriteLineWithPretext($"Season Number: {SeasonNumbers[0]}", ConsoleExt.OutputType.Info);
            return;
        }

        var seasonNumbers = new List<int>();
        foreach (Match match in Matches(match1.ToString(), @"\d+"))
            seasonNumbers.Add(Convert.ToInt32(match.Value));
        if (match3.ToString() == "+")
        {
            SeasonNumbers = new int[seasonNumbers.Count];
            SeasonNumbers = seasonNumbers.ToArray();
            ConsoleExt.WriteWithPretext($"Season Numbers: {SeasonNumbers[0]}", ConsoleExt.OutputType.Info);
            foreach (var number in SeasonNumbers)
                if (number != SeasonNumbers[0])
                    Console.Write(", " + number);
            Console.WriteLine();
        }

        if (match3.ToString() == "-")
        {
            var lowestNumber = seasonNumbers.Min();
            var highestNumber = seasonNumbers.Max();
            SeasonNumbers = new int[highestNumber];
            var index = 0;
            ConsoleExt.WriteWithPretext($"Season Numbers: {lowestNumber}", ConsoleExt.OutputType.Info);
            for (var i = lowestNumber; i <= highestNumber; i++)
            {
                SeasonNumbers[index] = i;
                if (index != 0) Console.Write($", {SeasonNumbers[index]}");
                index++;
            }

            Console.WriteLine();
        }
        else if (SeasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Invalid symbol", ConsoleExt.OutputType.Error);
        }
    }

    //extracts the language from the folder name
    internal static void ExtractingLanguage(string inputFolderPath)
    {
        var fileName = new DirectoryInfo(inputFolderPath).Name;
        const string pattern = "Dual[- ]Audio";

        var match = Match(fileName, pattern);
        if (match.Success)
        {
            SubOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        var files = Directory.GetFiles(inputFolderPath);
        var languages = TrackLanguageFromMetadata(files[1]);

        if (languages.Contains("eng", StringComparer.OrdinalIgnoreCase) ||
            languages.Contains("ger", StringComparer.OrdinalIgnoreCase))
        {
            SubOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        if (languages.Contains("jpn", StringComparer.OrdinalIgnoreCase))
        {
            SubOrDub = Languages.Sub;
            ConsoleExt.WriteLineWithPretext("Language is Sub", ConsoleExt.OutputType.Info);
            return;
        }

        if (languages.Contains("N/a", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleExt.WriteLineWithPretext("File(s) is/are Corrupt!", ConsoleExt.OutputType.Error);
            return;
        }

        var arguments = fileName.Split(" ");

        foreach (var argument in arguments)
            switch (argument.ToLower())
            {
                case "sub":
                    SubOrDub = Languages.Sub;
                    ConsoleExt.WriteLineWithPretext("Language is Sub", ConsoleExt.OutputType.Info);
                    break;
                case "dub":
                    SubOrDub = Languages.Dub;
                    ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
                    break;
                default:
                    ConsoleExt.WriteLineWithPretext("No Language defining argument found.",
                        ConsoleExt.OutputType.Warning);
                    ConsoleExt.WriteLineWithPretext("Please enter the language of the anime (Sub or Dub)",
                        ConsoleExt.OutputType.Question);
                    Console.WriteLine("1. Sub");
                    Console.WriteLine("2. Dub");
                    var index = int.Parse(Console.ReadLine() ?? string.Empty);
                    switch (index)
                    {
                        case 1:
                            SubOrDub = Languages.Sub;
                            break;
                        case 2:
                            SubOrDub = Languages.Dub;
                            break;
                        default:
                            Console.WriteLine("Invalid input! Anime is defaulted to Dubbed!");
                            SubOrDub = Languages.Dub;
                            break;
                    }

                    break;
            }
    }
}