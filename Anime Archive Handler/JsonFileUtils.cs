namespace Anime_Archive_Handler;

using System.IO;
using JikanDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JsonFileUtility
{
    public static List<Anime?> ReadFromJsonFile(string filePath)
    { 
        List<Anime?> animes = new List<Anime?>();
        // Open the JSON file
        using StreamReader sr = new StreamReader(filePath);
        // Create a JSON serializer
        JsonSerializer serializer = new JsonSerializer();

        // Read the file line by line
        while (sr.ReadLine() is { } line)
        {
            // Create a JsonReader object using the line
            using JsonReader reader = new JsonTextReader(new StringReader(line));
            // Deserialize the JSON data into an object of the specified type
            Anime? anime = serializer.Deserialize<Anime>(reader);
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
    
    public static int FindLastNonNullLine(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var reader = new StreamReader(stream);
    
        var lineCount = 1;
        var lastNonNullLine = -1;

        while (reader.ReadLine() is { } line)
        {
            if (!string.IsNullOrWhiteSpace(line) && line.ToLower() != "null")
            {
                lastNonNullLine = lineCount;
            }
            lineCount++;
        }
    
        return lastNonNullLine;
    }

    public static T GetValue<T>(string filePath, string variableName)
    {
        try
        {
            string jsonContent = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonContent);

            JToken? valueToken = jsonObject[variableName];
            if (valueToken != null)
            {
                return valueToken.Value<T>()!; //if this throws a null at one point then i need to revisit this to solve this
            }
            else
            {
                throw new ArgumentException($"Variable '{variableName}' not found in the JSON file.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading JSON file: {ex.Message}");
        }
    }
}