namespace Anime_Archive_Handler;

using System.Reflection;

public static class HelperClass
{
    //converts input number into ordinal number
    public static string ToOrdinal(int number)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "The number must be a positive integer.");
        }

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
        Dictionary<char, int> romanValues = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 }
        };

        int result = 0;
        int previousValue = 0;

        for (int i = roman.Length - 1; i >= 0; i--)
        {
            int currentValue = romanValues[roman[i]];

            if (currentValue < previousValue)
            {
                result -= currentValue;
            }
            else
            {
                result += currentValue;
            }

            previousValue = currentValue;
        }

        return result;
    }
    
    public static int ConvertMixedStringToNumber(string input)
    {
        Dictionary<char, int> romanValues = new Dictionary<char, int>
        {
            { 'I', 1 },
            { 'V', 5 },
            { 'X', 10 },
            { 'L', 50 },
            { 'C', 100 },
            { 'D', 500 },
            { 'M', 1000 }
        };

        int result = 0;
        string currentRoman = string.Empty;

        foreach (char c in input)
        {
            if (romanValues.ContainsKey(c))
            {
                currentRoman += c;
            }
            else if (currentRoman != string.Empty)
            {
                result += ConvertRomanToNumber(currentRoman);
                currentRoman = string.Empty;
            }
        }

        // Convert the last extracted Roman numeral if any
        if (currentRoman != string.Empty)
        {
            result += ConvertRomanToNumber(currentRoman);
        }

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
        string? answer = Console.ReadLine()?.ToLower();
        if (answer?.ToLower() == "y")
        {
            return true;
        }
        if (answer?.ToLower() == "n")
        {
            return false;
        }

        ConsoleExt.WriteLineWithPretext("Answer Provided is either null or Indeterminable!", ConsoleExt.OutputType.Error);

        throw new InvalidOperationException();
    }

    public static void UrlNameExtractor(string inputUrl)
    {
        if (inputUrl.Contains("gogoanime"))
        {
            
        }
    }
}