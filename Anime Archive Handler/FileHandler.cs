using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using FFMpegCore;
using IniParser;

namespace Anime_Archive_Handler;

public static class FileHandler
{

    private static string? _errorLogFile;
    
    //File integrity checks if all the files in a anime folder arent corrupted and returns false if the file is corrupt and is used to check if the downloaded anime is fully working
    // and if one of the filed in the anime stored structure is corrupted
    internal static bool FileIntegrityCheck(IEnumerable<string> videoFilePaths)
    {
        var episodeNumber = 1;
        var nothingCorrupt = true;

        try
        {
            foreach (var videoFilePath in videoFilePaths)
            {
                if (!File.Exists(videoFilePath))
                {
                    ConsoleExt.WriteLineWithPretext($"File not found: {videoFilePath}", ConsoleExt.OutputType.Error);
                    nothingCorrupt = false;
                    continue;
                }
                
                FFProbe.Analyse(videoFilePath);
                episodeNumber++;
            }
        }
        catch (FileNotFoundException fnfEx)
        {
            ConsoleExt.WriteLineWithPretext($"File not found: {fnfEx.FileName}", ConsoleExt.OutputType.Error);
            nothingCorrupt = false;
        }
        catch (Exception e)
        {
            ConsoleExt.WriteLineWithPretext($"Anime Episode {episodeNumber} encountered an error!", ConsoleExt.OutputType.Error);
            ConsoleExt.WriteLineWithPretext(e, ConsoleExt.OutputType.Error);
            nothingCorrupt = false;
        }

        return nothingCorrupt;
    }
    
    //Extracts the Audio Track Language by reading the Metadata and is used for language detection of a downloaded anime
    internal static List<string?> TrackLanguageFromMetadata(string videoFilePath)
    {
        var mediaInfo = FFProbe.Analyse(videoFilePath);

        return mediaInfo.AudioStreams.Select(audioStream => audioStream.Language)
            .Where(audioStreamLanguage => audioStreamLanguage != null)
            .Where(audioStreamLanguage => audioStreamLanguage != null).ToList();
    }
    
    // Checks if the currently transferring anime already existing in the output folder
    internal static bool CheckForExistence(string source, string destination)
    {
        var sourceHash = GetMd5Checksum(source);
        var destinationHash = GetMd5Checksum(destination);

        return sourceHash == destinationHash;
    }
    
    // Calculate MD5 checksum of a file and is used for checking if two of the same files are actually the same
    private static string GetMd5Checksum(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
    
    // Checks the existence of a file, creates it if it doesnt exist
    internal static void CheckFileExistence(string fileToCheck)
    {
        if (!File.Exists(fileToCheck)) File.Create(fileToCheck);
    }
    
    // need to start caching all those so it only has to read them from the cache and check if the exist and if they dont, search for them again
    // returns the file path in the program folder and is used to find a file in the program directory when it isn't always gonna be in the same place
    internal static string GetFileInProgramFolder(string fileNameWithExtension)
    {
        foreach (var file in Directory.GetFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, fileNameWithExtension, SearchOption.AllDirectories))
        {
            return file;
        }

        var message = $"Couldn't find {fileNameWithExtension} file in program directory!";
        ConsoleExt.WriteLineWithPretext(message, ConsoleExt.OutputType.Error, new InvalidOperationException());
        throw new InvalidOperationException();
    }

    // returns the directory in the program folder and is used when the folder directory that i'm looking for isn't always in the same spot
    internal static string GetDirectoryInProgramFolder(string directoryName)
    {
        foreach (var directory in Directory.GetDirectories(
                     Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, directoryName,
                     SearchOption.AllDirectories))
        {
            return directory;
        }

        var message = $"Couldn't find {directoryName} directory in program directory!";
        ConsoleExt.WriteLineWithPretext(message, ConsoleExt.OutputType.Error, new InvalidOperationException());
        throw new InvalidOperationException();
    }
    
    //keeps a running log of all the errors that occured when the program was running and stores them in one file
    //doesnt reuse the same file when the program is restarted
    //has to keep a file directory record of the file when created when the first error occurs, and has to delete that record when the program is closed
    // could use a uid that gets generated new everytime the program gets started, but this uid needs to get associated with the log file
    internal static void ErrorLogger(string errorInfo, Exception ex)
    {
        if (_errorLogFile == null || File.Exists(_errorLogFile))
        {
            var stream = File.Create(Path.Combine(GetDirectoryInProgramFolder("Errors"), $"Error Log: {DateTime.Now:MM/dd/yyyy HH:mm:ss}.txt"));
            _errorLogFile = stream.Name;
            stream.Close();
        }
        // Log the error or handle it as needed
        var errorMessage = $"Error, {errorInfo}: {ex.Message}";
        ConsoleExt.WriteLineWithPretext(errorMessage, ConsoleExt.OutputType.Error);

        // Write the error message to the log file
        using var logWriter = new StreamWriter(Path.Combine(GetDirectoryInProgramFolder("Errors"), _errorLogFile), append: true);
        logWriter.WriteLine($"{DateTime.Now:MM/dd/yyyy HH:mm:ss}: {errorMessage}");
        // Optionally, write more details about the error or the problematic record
    }
    
    // Takes a input of a text file to convert into a csv file while is needed to create updated torrent database
    internal static string TextToCsv(string inputFilePath)
    {
        try
        {
            var outputFilePath = Path.ChangeExtension(inputFilePath, ".csv"); // Path for the new CSV file

            // Reading from the text file
            using (var reader = new StreamReader(inputFilePath))
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

            Console.WriteLine("File converted successfully.");
            return outputFilePath;
        }
        catch (Exception e)
        {
            // Log or print exception details
            ConsoleExt.WriteLineWithPretext("Error converting file: ", ConsoleExt.OutputType.Error, e);
            throw;
        }
    }
}

public static class SettingsManager 
{
    private static readonly FileIniDataParser Parser = new FileIniDataParser();

    // Returns a specified setting which can be used to get user settings or stored settings
    private static string GetValue(string filePath, string sectionName, string keyName)
    {
        string pattern = @"\./";
        
        var data = Parser.ReadFile(filePath);
        var match = Regex.Match(data[sectionName][keyName], pattern);

        // checks for ./ which means that its a directory and its inside the working directory
        return match.Success ? Regex.Replace(data[sectionName][keyName], pattern, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)! + @"\") : data[sectionName][keyName];
    }

    // Is used to write or update settings in the user settings or in the stored settings
    internal static void SetValue(string filePath, string sectionName, string keyName, string keyValue) 
    {
        var data = Parser.ReadFile(filePath);

        data[sectionName][keyName] = keyValue;
        Parser.WriteFile(filePath, data);
    }

    internal static string GetSetting(string sectionName, string keyName)
    {
        string settings = FileHandler.GetFileInProgramFolder("Settings.ini");
        string userSettings = FileHandler.GetFileInProgramFolder("UserSettings.ini");

        string setting = GetValue(userSettings, sectionName, keyName);

        if (setting != "null") return setting;
        setting = GetValue(settings, $"Cached {sectionName}", keyName);
        if (setting == "null")
        {
            setting = GetValue(settings, $"Default {sectionName}", keyName);
        }

        return setting;
    }
}

public interface IDirectoryCreation
{
    string location { get;}
    static abstract void CreateDirectory();
}

// need to figure out how to access a non-static variable in a static function and class
public class CreateHentaiDirectory : IDirectoryCreation
{
    public string location => SettingsManager.GetSetting("Output Paths", "HentaiOutputFolder"); //the user set location or default location

    public static void CreateDirectory()
    {
        
    }
}

public class CreateMangaDirectory : IDirectoryCreation
{
    public string location => SettingsManager.GetSetting("Output Paths", "MangaOutputFolder"); //the user set location or default location

    public static void CreateDirectory()
    {
        
    }
}

public class CreateAnimeDirectory : IDirectoryCreation
{
    public string location => SettingsManager.GetSetting("Output Paths", "AnimeOutputFolder"); //the user set location or default location

    public static void CreateDirectory()
    {
        
    }
}