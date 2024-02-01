using JikanDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anime_Archive_Handler;

public static class JsonFileUtility
{
    public static List<Languages>? GetLanguages(string filePath)
    {
        // Deserialize from JSON to C# Object
        return JsonConvert.DeserializeObject<List<Languages>>(filePath);
    }

    public static void WriteLanguages(string filePath, List<Languages> root)
    {
        // Serialize back to JSON
        var updatedJsonText = JsonConvert.SerializeObject(root, Formatting.Indented);
        File.WriteAllText(filePath, updatedJsonText);
    }

    // might be able to rework this to work with the new manifest.json reading system but retain the modular readability when adding a new format
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
                ConsoleExt.OutputType.Warning, new Exception($"Tried retrieving a setting that doesnt exist or is null from the {filePath} file."));
            return default!;
        }
        catch (Exception e)
        {
            var message = $"Variable '{variableName}' not found in the JSON file.";
            ConsoleExt.WriteLineWithPretext(message, ConsoleExt.OutputType.Error, new Exception(message + $" {e.Message}"));
            throw;
        }
    }
}