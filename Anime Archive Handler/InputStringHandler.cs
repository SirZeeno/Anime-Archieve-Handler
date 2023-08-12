using System.Text.RegularExpressions;
using Humanizer;

namespace Anime_Archive_Handler;
using static AnimeArchiveHandler;
using static HelperClass;
using static FileHandler;

public static class InputStringHandler
{
    //removes all unnecessary pieces from the anime name
    internal static string RemoveUnnecessaryNamePieces(string fileName)
    {
        var pattern1 = @"\[.*?\]|\(.*?\)";
        var pattern2 = @"_";
        var pattern3 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        var pattern4 = @"\d+(st|nd|rd|th)";
        var pattern5 = @"(?<!['\w])[MCDLXVI]+(?![\w'])";
        var pattern6 = @"\s{2,}.*$";

        var withoutBrackets = Regex.Replace(fileName, pattern1, string.Empty);
        var withoutUnderscore = Regex.Replace(withoutBrackets, pattern2, " ");
        var withoutSeason = Regex.Replace(withoutUnderscore, pattern3, " ");
        var withoutOrdinal = Regex.Replace(withoutSeason, pattern4, string.Empty);
        var withoutRomanNumerals = Regex.Replace(withoutOrdinal, pattern5, string.Empty);
        var withoutAfterSpaces =
            Regex.Replace(withoutRomanNumerals, pattern6, string.Empty); //removes everything after 2 spaces

        var output = withoutAfterSpaces.ApplyCase(LetterCasing.Title).TrimStart().TrimEnd();
        ConsoleExt.WriteLineWithPretext(output, ConsoleExt.OutputType.Info);
        return output;
    }
    
    //extracts the season number from the folder name
    internal static void ExtractingSeasonNumber(string fileName)
    {
        var pattern = @"(?i)(Season|Seasons|S)\s*(\d+)\s*[+\-]+\s*(\d+)";
        var pattern4 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        var pattern2 = @"(?i)\d+\s*(st|nd|rd|th)\s*(Season|Seasons|S)";
        var pattern5 = @"(?<!['\w])[MCDLXVI]+(?![\w'])";

        var match1 = Regex.Match(fileName, pattern);
        var match2 = Regex.Match(fileName, pattern2);
        var match4 = Regex.Match(fileName, pattern4);
        var match5 = Regex.Match(fileName, pattern5);

        if (!match1.Success && !match2.Success && !match4.Success && !match5.Success)
        {
            ConsoleExt.WriteLineWithPretext("No Anime Season number Found!", ConsoleExt.OutputType.Warning);
            if (!HeadlessOperations) return;
            _seasonNumbers = new[] { 1 };
            ConsoleExt.WriteLineWithPretext($"Season Number: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);

            //ask the user what season the anime is.
            return;
        }

        var pattern3 = @"[+-]";
        var match3 = Regex.Match(match1.Value, pattern3);

        if (!match3.Success)
        {
            _seasonNumbers = new int[1];
            if (int.TryParse(new string(match1.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
            {
            }
            else
            {
                if (int.TryParse(new string(match4.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
                {
                }
                else
                {
                    if (int.TryParse(new string(match2.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
                    {
                    }
                    else
                    {
                        if (int.TryParse(ConvertRomanToNumber(match5.Value).ToString(), out _seasonNumbers[0]))
                        {
                        }
                    }
                }
            }

            ConsoleExt.WriteLineWithPretext($"Season Number: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);
            return;
        }

        var seasonNumbers = new List<int>();
        foreach (Match match in Regex.Matches(match1.ToString(), @"\d+"))
            seasonNumbers.Add(Convert.ToInt32(match.Value));
        if (match3.ToString() == "+")
        {
            _seasonNumbers = new int[seasonNumbers.Count];
            _seasonNumbers = seasonNumbers.ToArray();
            ConsoleExt.WriteWithPretext($"Season Numbers: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);
            foreach (var number in _seasonNumbers)
                if (number != _seasonNumbers[0])
                    Console.Write(", " + number);
            Console.WriteLine();
        }

        if (match3.ToString() == "-")
        {
            var lowestNumber = seasonNumbers.Min();
            var highestNumber = seasonNumbers.Max();
            _seasonNumbers = new int[highestNumber];
            var index = 0;
            ConsoleExt.WriteWithPretext($"Season Numbers: {lowestNumber}", ConsoleExt.OutputType.Info);
            for (var i = lowestNumber; i <= highestNumber; i++)
            {
                _seasonNumbers[index] = i;
                if (index != 0) Console.Write($", {_seasonNumbers[index]}");
                index++;
            }

            Console.WriteLine();
        }
        else if (_seasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Invalid symbol", ConsoleExt.OutputType.Error);
        }
    }

    //extracts the language from the folder name
    internal static void ExtractingLanguage(string inputFolderPath)
    {
        var fileName = new DirectoryInfo(inputFolderPath).Name;
        var pattern = "Dual[- ]Audio";

        var match = Regex.Match(fileName, pattern);
        if (match.Success)
        {
            _subOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        var files = Directory.GetFiles(inputFolderPath);
        var languages = TrackLanguageFromMetadata(files[1]);

        if (languages.Contains("eng", StringComparer.OrdinalIgnoreCase) ||
            languages.Contains("ger", StringComparer.OrdinalIgnoreCase))
        {
            _subOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        if (languages.Contains("jpn", StringComparer.OrdinalIgnoreCase))
        {
            _subOrDub = Languages.Sub;
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
                    _subOrDub = Languages.Sub;
                    ConsoleExt.WriteLineWithPretext("Language is Sub", ConsoleExt.OutputType.Info);
                    break;
                case "dub":
                    _subOrDub = Languages.Dub;
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
                            _subOrDub = Languages.Sub;
                            break;
                        case 2:
                            _subOrDub = Languages.Dub;
                            break;
                        default:
                            Console.WriteLine("Invalid input! Anime is defaulted to Dubbed!");
                            _subOrDub = Languages.Dub;
                            break;
                    }

                    break;
            }
    }
}