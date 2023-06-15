namespace Anime_Archive_Handler;

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
}