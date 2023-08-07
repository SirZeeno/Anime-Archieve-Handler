using JikanDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anime_Archive_Handler;

//find the specified anime and remove it in the database, or add it if its not there in the right position
//need to maybe convert using self hosted sql for future use and efficiency

public static class JsonFileUtility
{
    public static List<Anime?> ReadFromJsonFile(string filePath)
    {
        var animes = new List<Anime?>();
        // Open the JSON file
        using var sr = new StreamReader(filePath);
        // Create a JSON serializer
        var serializer = new JsonSerializer();

        // Read the file line by line
        while (sr.ReadLine() is { } line)
        {
            // Create a JsonReader object using the line
            using JsonReader reader = new JsonTextReader(new StringReader(line));
            // Deserialize the JSON data into an object of the specified type
            var anime = serializer.Deserialize<Anime>(reader);
            // Add the anime to the list
            if (anime != null) animes.Add(anime);
        }

        // Return the list of animes
        return animes;
    }

    public static void WriteToJsonFile(string filePath, Anime? jsonData)
    {
        // Serialize the object to a JSON string
        string json = JsonConvert.SerializeObject(jsonData);

        // Open the JSON file using a FileStream
        using FileStream fs = new FileStream(filePath, FileMode.Append);
        
        // Write the JSON string to the file
        using StreamWriter sw = new StreamWriter(fs);
        sw.WriteLine(json);
    }

    public static Anime? ReadFromJsonFileAt(string filePath, int lineNumber)
    {
        using var sr = new StreamReader(filePath);
        var currentLineNumber = 1;

        while (currentLineNumber < lineNumber && sr.ReadLine() is not null) currentLineNumber++;

        if (currentLineNumber != lineNumber) return null;
        var line = sr.ReadLine();
        if (line is null) return null;
        var serializer = new JsonSerializer();
        using JsonReader reader = new JsonTextReader(new StringReader(line));
        return serializer.Deserialize<Anime>(reader);
    }

    public static void WriteToJsonFileAt(string filePath, Anime? jsonData, int lineNumber)
    {
        // Read the existing content of the file
        var lines = File.ReadAllLines(filePath);

        // Check if the line number is within the valid range
        if (lineNumber < 0 || lineNumber >= lines.Length)
            throw new ArgumentOutOfRangeException(nameof(lineNumber), "Invalid line number.");

        // Update the specified line with the new JSON data
        lines[lineNumber] = JsonConvert.SerializeObject(jsonData);

        // Write the modified content back to the file
        File.WriteAllLines(filePath, lines);
    }


    public static int FindLastNonNullLine(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);

        var lineCount = 1;
        var lastNonNullLine = -1;

        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && line.ToLower() != "null") lastNonNullLine = lineCount;
            lineCount++;
        }

        return lastNonNullLine;
    }

    public static T GetValue<T>(string filePath, string variableName)
    {
        try
        {
            var jsonContent = File.ReadAllText(filePath);
            var jsonObject = JObject.Parse(jsonContent);

            var valueToken = jsonObject[variableName];
            if (valueToken != null) return valueToken.Value<T>()!;

            ConsoleExt.WriteLineWithPretext($"Variable '{variableName}' not found in the JSON file.",
                ConsoleExt.OutputType.Warning);
            return default!;
        }
        catch (Exception)
        {
            throw new Exception($"Variable '{variableName}' not found in the JSON file.");
        }
    }
}