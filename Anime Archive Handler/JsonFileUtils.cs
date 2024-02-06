using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Anime_Archive_Handler;

public static class JsonFileUtility
{
    public static List<Languages>? GetLanguages(string filePath)
    {
        // Read the file content into a string
        string json = File.ReadAllText(filePath);
        
        // Deserialize from JSON to Language structure
        return JsonConvert.DeserializeObject<List<Languages>>(json);
    }

    public static void WriteLanguages(string filePath, List<Languages> root)
    {
        // Serialize back to JSON
        var updatedJsonText = JsonConvert.SerializeObject(root, Formatting.Indented);
        File.WriteAllText(filePath, updatedJsonText);
    }
}