using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;

namespace Anime_Archive_Handler.Interfaces;

public interface IFileReading
{
    static abstract string ReadFile(string filePath);
    
    static abstract string[] ReadFiles(string[] filePaths);
}

public class ReadMetaData : IFileReading
{
    public static string ReadFile(string filePath)
    {
        NHentaiMetaData? metaData = JsonConvert.DeserializeObject<NHentaiMetaData>(filePath);
        
        return null;
    }

    public static string[] ReadFiles(string[] filePaths)
    {
        List<string> fileOutputPaths = [];
        fileOutputPaths.AddRange(filePaths.Select(ReadFile));
        return fileOutputPaths.ToArray();
    }
}

public class ReadAnimetoshoCsv : IFileReading
{
    public static string ReadFile(string filePath)
    {
        return null;
    }

    public static string[] ReadFiles(string[] filePaths)
    {
        List<string> fileOutputPaths = [];
        fileOutputPaths.AddRange(filePaths.Select(ReadFile));
        return fileOutputPaths.ToArray();
    }
}

// Takes a input of a text file to convert into a csv file while is needed to create updated torrent database
public class ReadAnimetoshoTxt : IFileReading
{
    public static string ReadFile(string filePath)
    {
        try
        {
            var outputFilePath = Path.ChangeExtension(filePath, ".csv"); // Path for the new CSV file

            // Reading from the text file
            using (var reader = new StreamReader(filePath))
            using (var csvReader = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                   {
                       Delimiter = "\t", // Set the delimiter used in your text file to tabs
                       HasHeaderRecord = true, // If your file has header row
                   }))
            {
                // Writing to the CSV file
                using (var writer = new StreamWriter(outputFilePath))
                using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    var records = csvReader.GetRecords<Animetosho>();
                    csvWriter.WriteRecords(records);
                }
            }

            ConsoleExt.WriteLineWithPretext("File converted successfully.", ConsoleExt.OutputType.Info);
            return outputFilePath;
        }
        catch (Exception e)
        {
            // Log or print exception details
            ConsoleExt.WriteLineWithPretext("Error converting file: ", ConsoleExt.OutputType.Error, e);
            throw;
        }
    }

    public static string[] ReadFiles(string[] filePaths)
    {
        List<string> fileOutputPaths = [];
        fileOutputPaths.AddRange(filePaths.Select(ReadFile));
        return fileOutputPaths.ToArray();
    }
}