namespace Anime_Archive_Handler;

public static class ConsoleExt
{
    public enum OutputType
    {
        Error,
        Info,
        Warning
    }
    
    public static int WriteLineWithPretext<T>(T output, OutputType outputType)
    {
        int length1 = CurrentTime();
        int length2 = DetermineOutputType(outputType);
        Console.WriteLine(output);
        return length1 + length2;
    }
    
    public static int WriteWithPretext<T>(T output, OutputType outputType)
    {
        int length1 = CurrentTime();
        int length2 = DetermineOutputType(outputType);
        Console.Write(output);
        return length1 + length2;
    }

    private static int DetermineOutputType(OutputType outputType)
    {
        if (outputType == OutputType.Error)
        {
            return ErrorType();
        }
        if (outputType == OutputType.Info)
        {
            return InfoType();
        }
        if (outputType == OutputType.Warning)
        {
            return WarningType();
        }
        return 0;
    }

    private static int CurrentTime()
    {
        var dateTime = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("[" + dateTime + "] ");
        Console.ForegroundColor = oldColor;
        return dateTime.Length + 3;
    }
    
    private static int InfoType()
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[Info] ");
        Console.ForegroundColor = oldColor;
        return 7;
    }
    
    private static int ErrorType()
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Write("[Error] ");
        Console.ForegroundColor = oldColor;
        return 8;
    }
    
    private static int WarningType()
    {
        var oldColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write("[Warning] ");
        Console.ForegroundColor = oldColor;
        return 10;
    }
}