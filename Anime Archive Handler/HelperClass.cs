using System.Reflection;
using System.Text.RegularExpressions;

namespace Anime_Archive_Handler;

public static class HelperClass
{
    //converts input number into ordinal number
    public static string ToOrdinal(int number)
    {
        if (number <= 0)
            throw new ArgumentOutOfRangeException(nameof(number), "The number must be a positive integer.");

        switch (number % 100)
        {
            case 11:
            case 12:
            case 13:
                return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    public static int ConvertRomanToNumber(string roman)
    {
        var romanValues = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 }
        };

        var result = 0;
        var previousValue = 0;

        for (var i = roman.Length - 1; i >= 0; i--)
        {
            var currentValue = romanValues[roman[i]];

            if (currentValue < previousValue)
                result -= currentValue;
            else
                result += currentValue;

            previousValue = currentValue;
        }

        return result;
    }

    public static int ConvertMixedStringToNumber(string input)
    {
        var romanValues = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 }
        };

        var result = 0;
        var currentRoman = string.Empty;

        foreach (var c in input)
            if (romanValues.ContainsKey(c))
            {
                currentRoman += c;
            }
            else if (currentRoman != string.Empty)
            {
                result += ConvertRomanToNumber(currentRoman);
                currentRoman = string.Empty;
            }

        // Convert the last extracted Roman numeral if any
        if (currentRoman != string.Empty) result += ConvertRomanToNumber(currentRoman);

        return result;
    }

    public static string GetFileInProgramFolder(string fileNameWithExtension)
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            fileNameWithExtension);
    }

    public static bool ManualInformationChecking()
    {
        ConsoleExt.WriteLineWithPretext("Is this Information Correct? (y/n)", ConsoleExt.OutputType.Question);
        var answer = Console.ReadLine()?.ToLower();
        switch (answer?.ToLower())
        {
            case "y":
            case "yes":
                return true;
            case "n":
            case "no":
                return false;
            default:
                ConsoleExt.WriteLineWithPretext("Answer Provided is either null or Indeterminable!",
                    ConsoleExt.OutputType.Error);

                throw new InvalidOperationException();
        }
    }
    
    public static bool ManualInformationChecking(string message)
    {
        ConsoleExt.WriteLineWithPretext($"{message} (y/n)", ConsoleExt.OutputType.Question);
        var answer = Console.ReadLine()?.ToLower();
        switch (answer?.ToLower())
        {
            case "y":
            case "yes":
                return true;
            case "n":
            case "no":
                return false;
            default:
                ConsoleExt.WriteLineWithPretext("Answer Provided is either null or Indeterminable!",
                    ConsoleExt.OutputType.Error);

                throw new InvalidOperationException();
        }
    }

    public static string UrlNameExtractor(string? inputUrl)
    {
        if (inputUrl == null || !inputUrl.Contains("gogoanime")) return string.Empty;
        var pattern = @"https:\/\/gogoanime\.\w+\/watch\/";
        var pattern2 = @"\/ep-\d+";
        var pattern3 = "-";
        var pattern4 = @"\b\w+\s*$";

        var removedWebsite = Regex.Replace(inputUrl, pattern, "");
        var removedEpisode = Regex.Replace(removedWebsite, pattern2, "");
        var removedDashes = Regex.Replace(removedEpisode, pattern3, " ");
        var removedLastWord = Regex.Replace(removedDashes, pattern4, "");
        ConsoleExt.WriteLineWithPretext(removedLastWord.Trim(), ConsoleExt.OutputType.Info);

        return removedLastWord.Trim();
    }

    public static string ManualStringRemoval(string? userInputString, string inputString)
    {
        var pattern =
            @""; //this pattern needs to consist of the userInputString and any empty spaces that come before or after

        var removedWord = Regex.Replace(inputString, pattern, "");

        ConsoleExt.WriteLineWithPretext(removedWord.Trim(), ConsoleExt.OutputType.Info);

        return removedWord.Trim();
    }
}