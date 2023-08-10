using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FFMpegCore;
using Humanizer;

namespace Anime_Archive_Handler;

using static JsonFileUtility;
using static JikanHandler;
using static DbHandler;
using static HelperClass;

//i also need this to be able to handle movie folders inside the anime folder
//need to handle if a anime has multiple parts of a season
//need to rework the execution
//need to add a par2 backup system that creates a 25% or lower par2 backup file for the entire anime folder
//need to add a want to watch list to this program, and a (already watched/in anime folder) list that i can easily search and that is more user readable then the json database
//apply a tag to each episode video file that is the genre tags of the anime so it can be found under those as well

/// <summary>
///     need to check the destination folder for an existing version of the anime in both languages, and if the anime is
///     dub but it finds a match in sub, it will
///     remove the sub version, place the dub version into the dub folder. if it finds the same anime in the same language
///     it will skip it. if it finds the same anime
///     in dub but i am trying to add a sub then it will also just skip it, unless i have a setting on that will still
///     allow it to add the anime.
///     all of these checks need to be for each season of an anime and not the anime itself
///     and need the check to actually check either via a checksum or by just checking the names of the files within the
///     destination folder
/// </summary>
internal abstract class AnimeArchiveHandler
{
    private static readonly string AnimeOutputFolder =
        GetValue<string>(GetFileInProgramFolder("UserSettings.json"), "AnimeOutputFolder");

    private static Languages? _subOrDub;
    private static string? _animeName;
    private static int[]? _seasonNumbers;
    private static string? _sourceFolder;

    private static bool _hasSubFolder;

    private static readonly bool HeadlessOperations =
        GetValue<bool>(GetFileInProgramFolder("UserSettings.json"), "HeadlessOperations");

    private static void Main(string[] args)
    {
        switch (args.Length)
        {
            case >= 1:
            {
                foreach (var arg in args)
                {
                    //Task.Run(Start).Wait();
                    AnimeListHandler.StartAnimeListEditing();

                    _sourceFolder = arg;
                    _hasSubFolder = HasSubFolders(arg);
                    ConsoleExt.WriteLineWithPretext($"Has sub-folders: {_hasSubFolder}", ConsoleExt.OutputType.Info);
                    _animeName = RemoveUnnecessaryNamePieces(new DirectoryInfo(arg).Name);
                    var animeTitleInDb = GetAnimeWithTitle(_animeName);
                    if (animeTitleInDb != null)
                        ConsoleExt.WriteLineWithPretext(
                            $"{GetAnimeTitleWithAnime(animeTitleInDb)}, {animeTitleInDb.MalId}",
                            ConsoleExt.OutputType.Info);
                    ExtractingSeasonNumber(new DirectoryInfo(arg).Name);
                    var folders = _hasSubFolder ? GetSeasonDirectories() : new[] { arg };
                    foreach (var folder in folders)
                    {
                        var directoryFiles = Directory.GetFiles(folder); //for further use when moving the episodes
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

                    ConsoleExt.WriteLineWithPretext($"Database Last Entre was on Line: {FindLastNonNullLine(JsonPath)}",
                        ConsoleExt.OutputType.Info);
                }

                break;
            }
            case 0:
                AnimeListHandler.StartAnimeListEditing();
                break;
        }

        ConsoleExt.WriteLineWithPretext("Program has finished running!", ConsoleExt.OutputType.Info);
        Thread.Sleep(1000000);
    }

    //Checks if the input folder has sub-folders
    private static bool HasSubFolders(string inputDirectory)
    {
        var folderNames = Directory.GetDirectories(inputDirectory);
        return folderNames.Length > 0;
    }

    //File integrity check that returns false if the file is corrupt
    private static bool FileIntegrityCheck(string[] videoFilePaths)
    {
        var episodeNumber = 0;
        var nothingCorrupt = true;

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
            ConsoleExt.WriteLineWithPretext($"Anime Episode {episodeNumber} is Corrupted!",
                ConsoleExt.OutputType.Error);
            nothingCorrupt = false;
        }

        return nothingCorrupt;
    }

    // Checks for if the currently being transferred anime already existing 
    private static bool CheckForExistence(string source, string destination)
    {
        var sourceHash = GetMd5Checksum(source);
        var destinationHash = GetMd5Checksum(destination);

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
    private static List<string?> TrackLanguageFromMetadata(string videoFilePath)
    {
        var mediaInfo = FFProbe.Analyse(videoFilePath);

        return mediaInfo.AudioStreams.Select(audioStream => audioStream.Language)
            .Where(audioStreamLanguage => audioStreamLanguage != null)
            .Where(audioStreamLanguage => audioStreamLanguage != null).ToList();
    }

    //extracts the language from the folder name
    private static void ExtractingLanguage(string inputFolderPath)
    {
        var fileName = new DirectoryInfo(inputFolderPath).Name;
        var pattern = "Dual[- ]Audio";

        var match = Regex.Match(fileName, pattern);
        if (match.Success)
        {
            _subOrDub = Languages.Dub;
            ConsoleExt.WriteLineWithPretext("Language is Dub", ConsoleExt.OutputType.Info);
            return;
        }

        var files = Directory.GetFiles(inputFolderPath);
        var languages = TrackLanguageFromMetadata(files[1]);

        if (languages.Contains("eng", StringComparer.OrdinalIgnoreCase) ||
            languages.Contains("ger", StringComparer.OrdinalIgnoreCase))
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

        var arguments = fileName.Split(" ");

        foreach (var argument in arguments)
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
                    ConsoleExt.WriteLineWithPretext("No Language defining argument found.",
                        ConsoleExt.OutputType.Warning);
                    ConsoleExt.WriteLineWithPretext("Please enter the language of the anime (Sub or Dub)",
                        ConsoleExt.OutputType.Question);
                    Console.WriteLine("1. Sub");
                    Console.WriteLine("2. Dub");
                    var index = int.Parse(Console.ReadLine() ?? string.Empty);
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

    //extracts the season number from the folder name
    public static void ExtractingSeasonNumber(string fileName)
    {
        var pattern = @"(?i)(Season|Seasons|S)\s*(\d+)\s*[+\-]+\s*(\d+)";
        var pattern4 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        var pattern2 = @"(?i)\d+\s*(st|nd|rd|th)\s*(Season|Seasons|S)";
        var pattern5 = @"\b[MCDLXVI]+\b";

        var match1 = Regex.Match(fileName, pattern);
        var match2 = Regex.Match(fileName, pattern2);
        var match4 = Regex.Match(fileName, pattern4);
        var match5 = Regex.Match(fileName, pattern5);

        if (!match1.Success && !match2.Success && !match4.Success && !match5.Success)
        {
            ConsoleExt.WriteLineWithPretext("No Anime Season number Found!", ConsoleExt.OutputType.Warning);
            if (!HeadlessOperations) return;
            _seasonNumbers = new[] { 1 };
            ConsoleExt.WriteLineWithPretext($"Season Number: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);

            //ask the user what season the anime is.
            return;
        }

        var pattern3 = @"[+-]";
        var match3 = Regex.Match(match1.Value, pattern3);

        if (!match3.Success)
        {
            _seasonNumbers = new int[1];
            if (int.TryParse(new string(match1.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
            {
            }
            else
            {
                if (int.TryParse(new string(match4.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
                {
                }
                else
                {
                    if (int.TryParse(new string(match2.Value.Where(char.IsDigit).ToArray()), out _seasonNumbers[0]))
                    {
                    }
                    else
                    {
                        if (int.TryParse(ConvertRomanToNumber(match5.Value).ToString(), out _seasonNumbers[0]))
                        {
                        }
                    }
                }
            }

            ConsoleExt.WriteLineWithPretext($"Season Number: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);
            return;
        }

        var seasonNumbers = new List<int>();
        foreach (Match match in Regex.Matches(match1.ToString(), @"\d+"))
            seasonNumbers.Add(Convert.ToInt32(match.Value));
        if (match3.ToString() == "+")
        {
            _seasonNumbers = new int[seasonNumbers.Count];
            _seasonNumbers = seasonNumbers.ToArray();
            ConsoleExt.WriteWithPretext($"Season Numbers: {_seasonNumbers[0]}", ConsoleExt.OutputType.Info);
            foreach (var number in _seasonNumbers)
                if (number != _seasonNumbers[0])
                    Console.Write(", " + number);
            Console.WriteLine();
        }

        if (match3.ToString() == "-")
        {
            var lowestNumber = seasonNumbers.Min();
            var highestNumber = seasonNumbers.Max();
            _seasonNumbers = new int[highestNumber];
            var index = 0;
            ConsoleExt.WriteWithPretext($"Season Numbers: {lowestNumber}", ConsoleExt.OutputType.Info);
            for (var i = lowestNumber; i <= highestNumber; i++)
            {
                _seasonNumbers[index] = i;
                if (index != 0) Console.Write($", {_seasonNumbers[index]}");
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
    public static string RemoveUnnecessaryNamePieces(string fileName)
    {
        var pattern1 = @"\[.*?\]|\(.*?\)";
        var pattern2 = @"_";
        var pattern3 = @"(?i)(Season|Seasons|S)\s*(\d+)";
        var pattern4 = @"\d+(st|nd|rd|th)";
        var pattern5 = @"\b[MCDLXVI]+\b";
        var pattern6 = @"\s{2,}.*$";

        var withoutBrackets = Regex.Replace(fileName, pattern1, string.Empty);
        var withoutUnderscore = Regex.Replace(withoutBrackets, pattern2, " ");
        var withoutSeason = Regex.Replace(withoutUnderscore, pattern3, " ");
        var withoutOrdinal = Regex.Replace(withoutSeason, pattern4, string.Empty);
        var withoutRomanNumerals = Regex.Replace(withoutOrdinal, pattern5, string.Empty);
        var withoutAfterSpaces =
            Regex.Replace(withoutRomanNumerals, pattern6, string.Empty); //removes everything after 2 spaces

        var output = withoutAfterSpaces.ApplyCase(LetterCasing.Title).TrimStart().TrimEnd();
        ConsoleExt.WriteLineWithPretext(output, ConsoleExt.OutputType.Info);
        return output;
    }

    // Gets the season folders in the anime folder and nothing like movie folders, language folders
    private static string[] GetSeasonDirectories()
    {
        var allFolders = Directory.GetDirectories(_sourceFolder ?? throw new InvalidOperationException());
        var pattern = @"\d+";

        foreach (var folder in allFolders)
        {
            var splitFolderName = folder.Split(@"\");
            var lastSplit = splitFolderName.Length;

            var match = Regex.Match(splitFolderName[lastSplit - 1], pattern);
            if (match.Success) ConsoleExt.WriteLineWithPretext(folder, ConsoleExt.OutputType.Info);
        }

        return (from folder in allFolders
            let match = Regex.Match(folder.Split(@"\")[folder.Split(@"\").Length - 1], pattern)
            where match.Success
            select folder).ToArray();
    }

    //Creates all the folders if they dont already exist
    private static void DirectoryCreator()
    {
        if (_subOrDub == null || _animeName == null || _seasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Anime Name, Sub or Dub, or Season Number is null",
                ConsoleExt.OutputType.Error);
            return;
        }

        if (!Directory.Exists(AnimeOutputFolder)) Directory.CreateDirectory(AnimeOutputFolder);
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!)))
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!));
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName)))
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName));

        if (Directory.Exists(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName, @"\Season ",
                _seasonNumbers[0].ToString()))) //this is only applicable for one season
            return;
        Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, _subOrDub.ToString()!, _animeName, @"\Season ",
            _seasonNumbers[0].ToString()));
    }


    // Moves all the episodes to the destination folder
    private static void MoveEpisodes(string[] files)
    {
        if (_seasonNumbers == null) return;
        foreach (var season in _seasonNumbers)
        {
            var episodeNumber = 1;
            foreach (var file in files)
            {
                var fileExtension = new FileInfo(file).Extension;
                var destinationFile = AnimeOutputFolder + @"\" + _subOrDub + @"\" + _animeName + @"\Season " +
                                      season + @"\" + _animeName + " #" + episodeNumber + fileExtension;
                if (!CheckForExistence(file,
                        File.Exists(destinationFile) ? destinationFile : GetFileInProgramFolder("UserSettings.json")) &&
                    !FileIntegrityCheck(File.Exists(destinationFile) ? new[] { destinationFile } : new[] { "" }))
                {
                    var fileInfo = new FileInfo(file);
                    var fileSize = fileInfo.Length;

                    //smaller = more precision, but slower
                    var bufferSize = 4096 * 4096;
                    var buffer = new byte[bufferSize];

                    long totalBytesRead = 0;

                    var preMessage = "Episode " + episodeNumber + ": ";

                    using (var source = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        using (var destination =
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

    private enum Languages
    {
        Sub,
        Dub
    }
}