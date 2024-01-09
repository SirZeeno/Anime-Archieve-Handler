using JikanDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anime_Archive_Handler;

public static class JsonFileUtility
{
    public static List<Language>? GetLanguages(string filePath)
    {
        // Deserialize from JSON to C# Object
        return JsonConvert.DeserializeObject<List<Language>>(filePath);
    }

    public static void WriteLanguages(string filePath, List<Language> root)
    {
        // Serialize back to JSON
        var updatedJsonText = JsonConvert.SerializeObject(root, Formatting.Indented);
        File.WriteAllText(filePath, updatedJsonText);
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

    [Obsolete("This will be replaced soon by an INI reading system")]
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

public class Language
{
    public string Name { get; set; }
    public string ShortForm { get; set; }
    public bool IsActive { get; set; }
}