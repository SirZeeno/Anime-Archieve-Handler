namespace Anime_Archive_Handler;

using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FFMpegCore;
using Humanizer;
using JikanDotNet;

using static JsonFileUtility;
using static JikanHandler;
using static HelperClass;

//i also need this to be able to handle movie folders inside the anime folder
//i also need this to be able to handle multiple seasons (working on rn, still need to test)
//need to handle if a anime has multiple parts of a season
//need to rework the execution
//need to add a par2 backup system that creates a 25% or lower par2 backup file for the entire anime folder
//need to add a want to watch list to this program, and a (already watched/in anime folder) list that i can easily search and that is more user readable then the json database
//nothing gets removed if nothing has [] like in Tsukimichi S01 1080p Dual Audio BDRip 10 bits DD x265-EMBER


/// <summary>
/// need to check the destination folder for an existing version of the anime in both languages, and if the anime is dub but it finds a match in sub, it will
/// remove the sub version, place the dub version into the dub folder. if it finds the same anime in the same language it will skip it. if it finds the same anime
/// in dub but i am trying to add a sub then it will also just skip it, unless i have a setting on that will still allow it to add the anime.
/// all of these checks need to be for each season of an anime and not the anime itself
/// and need the check to actually check either via a checksum or by just checking the names of the files within the destination folder
/// </summary>

abstract class AnimeArchiveHandler
{
    private static readonly string AnimeOutputFolder = GetValue<string>(GetFileInProgramFolder("UserSettings.json"), "AnimeOutputFolder");

    private enum Languages
    {
        Sub,
        Dub
    }

    private static Languages? _subOrDub;
    private static string? _animeName;
    private static int[]? _seasonNumbers;
    private static string? _sourceFolder;

    private static bool _hasSubFolder;
    private static readonly bool HeadlessOperations = GetValue<bool>(GetFileInProgramFolder("UserSettings.json"), "HeadlessOperations");

    private static void Main(string[] args)
    {
        foreach (var arg in args)
        {
            //Task.Run(Start).Wait();
            LoadAnimeDb(); //loads the DB into the _animes list

            _sourceFolder = arg;
            _hasSubFolder = HasSubFolders(arg);
            ConsoleExt.WriteLineWithPretext("Has sub-folders: " + _hasSubFolder, ConsoleExt.OutputType.Info);
            _animeName = RemoveUnnecessaryNamePieces(new DirectoryInfo(arg).Name);
            Anime? animeTitleInDb = GetAnimeWithTitle(_animeName);
            if (animeTitleInDb != null)
            {
                ConsoleExt.WriteLineWithPretext(GetAnimeTitleWithAnime(animeTitleInDb) + ", " + animeTitleInDb.MalId,
                    ConsoleExt.OutputType.Info);
            }
            ExtractingSeasonNumber(new DirectoryInfo(arg).Name);
            string[] folders = _hasSubFolder ? GetSeasonDirectories() : new [] {arg};
            foreach (var folder in folders)
            {
                string[] directoryFiles = Directory.GetFiles(folder); //for further use when moving the episodes
                if (FileIntegrityCheck(directoryFiles))
                {
                    ExtractingLanguage(folder);
                    if (HeadlessOperations)
                    {
                        //ConsoleExt.WriteLineWithPretext("Moving All the Season Episodes!", ConsoleExt.OutputType.Info);
                        //ConsoleExt.WriteLineWithPretext("Copied all Episodes from that Season to the Anime Folder.", ConsoleExt.OutputType.Info);
                    }
                    else
                    {
                        if (ManualInformationChecking())
                        {
                            //ConsoleExt.WriteLineWithPretext("Moving All the Season Episodes!", ConsoleExt.OutputType.Info);
                            //ConsoleExt.WriteLineWithPretext("Copied all Episodes from that Season to the Anime Folder.", ConsoleExt.OutputType.Info);
                        }
                        //need to see whats wrong and correct it
                    }
                }
                else
                {
                    ConsoleExt.WriteLineWithPretext("Moving on to next...", ConsoleExt.OutputType.Warning);
                }
            }
            ConsoleExt.WriteLineWithPretext("Database Last Entre was on Line: " + FindLastNonNullLine(JsonPath), ConsoleExt.OutputType.Info);
        }
        
        ConsoleExt.WriteLineWithPretext("Program has finished running!", ConsoleExt.OutputType.Info);
        Thread.Sleep(1000000);
    }

    //Checks if the input folder has sub-folders
    private static bool HasSubFolders(string inputDirectory)
    {
        string[] folderNames = Directory.GetDirectories(inputDirectory);
        if (folderNames.Length > 0)
        {
            return true;
        }
        return false;
    }

    //File integrity check that returns false if the file is corrupt
    private static bool FileIntegrityCheck(string[] videoFilePaths)
    {
        int episodeNumber = 0;
        bool nothingCorrupt = true;
        
        try
        {
            foreach (var videoFilePath in videoFilePaths)
            {
                FFProbe.Analyse(videoFilePath);
                episodeNumber++;
            }
        }
        catch (Exception)
        {
            ConsoleExt.WriteLineWithPretext("Anime Episode " + episodeNumber + " is Corrupted!", ConsoleExt.OutputType.Error);
            nothingCorrupt = false;
        }

        return nothingCorrupt;
    }

    // Checks for if the currently being transferred anime already existing 
    private static bool CheckForExistence(string source, string destination)
    {
        string sourceHash = GetMd5Checksum(source);
        string destinationHash = GetMd5Checksum(destination);

        return sourceHash == destinationHash;
    }
    
    // Calculate MD5 checksum of a file
    private static string GetMd5Checksum(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    //Extracts the Audio Track Language by reading the Metadata
    private static List<string> TrackLanguageFromMetadata(string videoFilePath)
    {
        var audioTrackLanguages = new List<string>();
        var mediaInfo = FFProbe.Analyse(videoFilePath);

        foreach (var audioStreamLanguage in mediaInfo.AudioStreams.Select(audioStream => audioStream.Language).Where(audioStreamLanguage => audioStreamLanguage != null))
        {
            if (audioStreamLanguage == null) continue;
            audioTrackLanguages.Add(audioStreamLanguage);
        }

        return audioTrackLanguages;
    }

    //extracts the language from the folder name
    private static void ExtractingLanguage(string inputFolderPath)
    {
        string fileName = new DirectoryInfo(inputFolderPath).Name;
        string pattern = "Dual[- ]Audio";
        
        var match = Regex.Match(fileName, pattern);
        if (match.Success)
        {
            _subOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        string[] files = Directory.GetFiles(inputFolderPath);
        List<string> languages = TrackLanguageFromMetadata(files[1]);

        if (languages.Contains("eng", StringComparer.OrdinalIgnoreCase) || languages.Contains("ger", StringComparer.OrdinalIgnoreCase))
        {
            _subOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }
        if (languages.Contains("jpn", StringComparer.OrdinalIgnoreCase))
        {
            _subOrDub = Languages.Sub;
            ConsoleExt.WriteLineWithPretext("Language is Sub", ConsoleExt.OutputType.Info);
            return;
        }
        if (languages.Contains("N/a", StringComparer.OrdinalIgnoreCase))
        {
            ConsoleExt.WriteLineWithPretext("File(s) is/are Corrupt!", ConsoleExt.OutputType.Error);
            return;
        }

        string[] arguments = fileName.Split(" ");

        foreach (var argument in arguments)
        {
            switch (argument.ToLower())
            {
                case "sub":
                    _subOrDub = Languages.Sub;
                    ConsoleExt.WriteLineWithPretext("Language is Sub", ConsoleExt.OutputType.Info);
                    break;
                case "dub":
                    _subOrDub = Languages.Dub;
                    ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
                    break;
                default:
                    ConsoleExt.WriteLineWithPretext("No Language defining argument found.", ConsoleExt.OutputType.Warning);
                    Console.WriteLine("Please enter the language of the anime (Sub or Dub)");
                    Console.WriteLine("1. Sub");
                    Console.WriteLine("2. Dub");
                    int index = int.Parse(Console.ReadLine() ?? string.Empty);
                    switch (index)
                    {
                        case 1:
                            _subOrDub = Languages.Sub;
                            break;
                        case 2:
                            _subOrDub = Languages.Dub;
                            break;
                        default:
                            Console.WriteLine("Invalid input! Anime is defaulted to Dubbed!");
                            _subOrDub = Languages.Dub;
                            break;
                    }
                    break;
            }
        }
    }

    //extracts the season number from the folder name
    private static void ExtractingSeasonNumber(string fileName)
    {
        string pattern = @"(?i)(Season|Seasons|S)\s*(\d+)\s*[+\-]+\s*(\d+)";
        string pattern4 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        string pattern2 = @"(?i)\d+\s*(st|nd|rd|th)\s*(Season|Seasons|S)";
        string pattern5 = @"\b[MCDLXVI]+\b";

        var match1 = Regex.Match(fileName, pattern);
        var match2 = Regex.Match(fileName, pattern2);
        var match4 = Regex.Match(fileName, pattern4);
        var match5 = Regex.Match(fileName, pattern5);
        
        if (!match1.Success && !match2.Success && !match4.Success && !match5.Success)
        {
            ConsoleExt.WriteLineWithPretext("No Anime Season number Found!", ConsoleExt.OutputType.Warning);
            if (HeadlessOperations)
            {
                _seasonNumbers = new[] {1};
            }

            //ask the user what season the anime is.
            return;
        }

        string pattern3 = @"[+-]";
        var match3 = Regex.Match(match1.Value, pattern3);

        if (!match3.Success)
        {
            _seasonNumbers = new int[1];
            if (int.TryParse(new string(match1.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0])) {}
            else
            {
                if (int.TryParse(new string(match4.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0])) {}
                else
                {
                    if (int.TryParse(new string(match2.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0])) {}
                    else
                    {
                        if (int.TryParse(ConvertRomanToNumber(match5.Value).ToString(), out _seasonNumbers[0])) {}
                    }
                }
            }
            ConsoleExt.WriteLineWithPretext("Season Number: " + _seasonNumbers[0], ConsoleExt.OutputType.Info);
            return;
        }

        List<int> seasonNumbers = new List<int>();
        foreach (Match match in Regex.Matches(match1.ToString(), @"\d+"))
        {
            seasonNumbers.Add(Convert.ToInt32(match.Value));
        }
        if (match3.ToString() == "+")
        {
            _seasonNumbers = new int[seasonNumbers.Count];
            _seasonNumbers = seasonNumbers.ToArray();
            ConsoleExt.WriteWithPretext("Season Numbers: " + _seasonNumbers[0], ConsoleExt.OutputType.Info);
            foreach (var number in _seasonNumbers)
            {
                if (number != _seasonNumbers[0])
                {
                    Console.Write(", " + number);
                }
            }
            Console.WriteLine();
        }
        if (match3.ToString() == "-")
        {
            int lowestNumber = seasonNumbers.Min();
            int highestNumber = seasonNumbers.Max();
            _seasonNumbers = new int[highestNumber];
            int index = 0;
            ConsoleExt.WriteWithPretext("Season Numbers: " + lowestNumber, ConsoleExt.OutputType.Info);
            for (int i = lowestNumber; i <= highestNumber; i++)
            {
                _seasonNumbers[index] = i;
                if (index != 0)
                {
                    Console.Write(", " + _seasonNumbers[index]);
                }
                index++;
            }
            Console.WriteLine();
        }
        else if (_seasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Invalid symbol", ConsoleExt.OutputType.Error);
        }
    }

    //removes all unnecessary pieces from the anime name
    private static string RemoveUnnecessaryNamePieces(string fileName)
    {
        string pattern1 = @"\[.*?\]|\(.*?\)"; //need to add underscores into the pattern
        //string pattern2 = @"(?<=-\s).*";
        //string pattern3 = @"([a-z]+-[a-z]+)";
        string withoutBrackets = Regex.Replace(fileName, pattern1, string.Empty);
        //string withoutDashes = Regex.Replace(withoutBrackets, pattern2 , string.Empty);
        
        string output = withoutBrackets.ApplyCase(LetterCasing.Title).TrimStart().TrimEnd();
        ConsoleExt.WriteLineWithPretext(output, ConsoleExt.OutputType.Info);
        return output;
    }

    // Gets the season folders in the anime folder and nothing like movie folders, language folders
    private static string[] GetSeasonDirectories()
    {
        string[] allFolders = Directory.GetDirectories(_sourceFolder ?? throw new InvalidOperationException());
        string pattern = @"\d+";

        foreach (var folder in allFolders)
        {
            string[] splitFolderName = folder.Split(@"\");
            int lastSplit = splitFolderName.Length;
            
            var match = Regex.Match(splitFolderName[lastSplit - 1], pattern);
            if (match.Success)
            {
                ConsoleExt.WriteLineWithPretext(folder, ConsoleExt.OutputType.Info);
            }
        }

        return (from folder in allFolders let match = Regex.Match(folder.Split(@"\")[folder.Split(@"\").Length - 1], pattern) where match.Success select folder).ToArray();
    }

    //Creates all the folders if they dont already exist
    private static void DirectoryCreator()
    {
        if (_subOrDub == null || _animeName == null || _seasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Anime Name, Sub or Dub, or Season Number is null", ConsoleExt.OutputType.Error);
            return;
        }
        if (!Directory.Exists(AnimeOutputFolder))
        {
            Directory.CreateDirectory(AnimeOutputFolder);
        }
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!)))
        {
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!));
        }
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName)))
        {
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName));
        }

        if (Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName, @"\Season ", _seasonNumbers[0].ToString())))//this is only applicable for one season
        {
            
            return;
        }
        Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName, @"\Season ", _seasonNumbers[0].ToString()));
    }
    
    
    // Moves all the episodes to the destination folder
    private static void MoveEpisodes(string[] files)
    {
        if (_seasonNumbers != null)
            foreach (var season in _seasonNumbers)
            {
                int episodeNumber = 1;
                foreach (var file in files)
                {
                    string fileExtension = new FileInfo(file).Extension;
                    string destinationFile = AnimeOutputFolder + @"\" + _subOrDub + @"\" + _animeName + @"\Season " +
                                             season +
                                             @"\" + _animeName + " #" + episodeNumber + fileExtension;
                    string[] destinationFileTest = { destinationFile };
                    if (!CheckForExistence(file, destinationFile) && !FileIntegrityCheck(destinationFileTest))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        long fileSize = fileInfo.Length;

                        //smaller = more precision, but slower
                        int bufferSize = 4096 * 4096;
                        byte[] buffer = new byte[bufferSize];

                        long totalBytesRead = 0;

                        string preMessage = "Episode " + episodeNumber + ": ";

                        using (FileStream source = new FileStream(file, FileMode.Open, FileAccess.Read))
                        {
                            using (FileStream destination =
                                   new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
                            {
                                int bytesRead;
                                while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    totalBytesRead += bytesRead;
                                    var progress = (int)((double)totalBytesRead / fileSize * 100);

                                    destination.Write(buffer, 0, bytesRead);

                                    Console.CursorLeft = 0;
                                    Console.Write(ConsoleExt.WriteWithPretext(preMessage, ConsoleExt.OutputType.Info));
                                    Console.CursorLeft = 0 + preMessage.Length;
                                    Console.Write("[");
                                    Console.CursorLeft = 1 + preMessage.Length;
                                    Console.Write(new string('=', progress / 2));
                                    Console.CursorLeft = 51 + preMessage.Length;
                                    Console.Write("]");
                                    Console.CursorLeft = 53 + preMessage.Length;
                                    Console.Write($"{progress}%");
                                }
                            }
                        }

                        Console.WriteLine();
                    }

                    episodeNumber++;
                }
            }
    }
}