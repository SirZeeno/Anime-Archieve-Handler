using System.Text.RegularExpressions;

namespace Anime_Archive_Handler;

using static JsonFileUtility;
using static InputStringHandler;
using static DbHandler;
using static HelperClass;
using static FileHandler;
using static SettingsManager;

//before doing the integrity check on each episode in the folder i need to make sure the files that i am checking are actual video files and not checksum files or any other files like in the Akashic Records of Bastard Magic Instructor folder
//if the selected anime from the database is not the correct anime, create a selection of all the high similarity matches and let the user choose the correct one. But if they are still not correct, the user can choose to use a custom Name or anime
// surrealdb might me a good replacement for litedb

/// <summary>
/// The following animes are having issues in the database search process and are returning an incorrect anime or version of the anime
///
/// Yamada's First Time returns Time: Toki no Shiori
/// 
/// </summary>

/// <summary>
///     need to check the destination folder for an existing version of the anime in both languages, and if the anime is
///     dub but it finds a match in sub, it will remove the sub version, place the dub version into the dub folder. if it
///     finds the same anime in the same language it will skip it. if it finds the same anime
///     in dub but i am trying to add a sub then it will also just skip it, unless i have a setting on that will still
///     allow it to add the anime.
///     all of these checks need to be for each season of an anime and not the anime itself
///     and need the check to actually check either via a checksum or by just checking the names of the files within the
///     destination folder
/// </summary>
internal abstract class AnimeArchiveHandler
{
    internal static readonly string UserSettingsFile = GetFileInProgramFolder("UserSettings.json");
    private static readonly string AnimeOutputFolder = GetSetting("Output Paths", "AnimeOutputFolder");
    internal static readonly bool HeadlessOperations = bool.Parse(GetSetting("Execution Settings", "HeadlessOperations"));
    private static readonly bool MultiplePartsInOneFolder = bool.Parse(GetSetting("Execution Settings", "MultiplePartsInOneFolder"));
    internal static readonly string SettingsPath = GoGetter();

    internal static Language? SubOrDub;
    private static string? _animeName;
    internal static int[]? SeasonNumbers;
    internal static int[]? PartNumbers;
    private static string? _sourceFolder;

    private static bool _hasSubFolder;
    private static bool _hasMultipleParts;

    // need to refactor this entire starting function and check that all the features are there and de-mess things for cleaner and less boilerplate code
    private static void Main(string[] args)
    {
        EnsureIndexDb();
        switch (args.Length)
        {
            case >= 1:
            {
                foreach (var arg in args)
                {
                    //Task.Run(Start).Wait();

                    _sourceFolder = arg;
                    _hasSubFolder = HasSubFolders(arg);
                    
                    ConsoleExt.WriteLineWithPretext($"Has sub-folders: {_hasSubFolder}", ConsoleExt.OutputType.Info);
                    
                    _animeName = RemoveUnnecessaryNamePieces(new DirectoryInfo(arg).Name);
                    var animeTitleInDb = GetAnimeWithTitle(_animeName);
                    
                    if (animeTitleInDb != null)
                    {
                        ConsoleExt.WriteLineWithPretext(animeTitleInDb.MalId, ConsoleExt.OutputType.Info);
                        ConsoleExt.WriteLineWithPretext($"{GetAnimeTitleWithAnime(animeTitleInDb)}, {animeTitleInDb.MalId}", ConsoleExt.OutputType.Info);
                    }
                    
                    else
                    {
                        ConsoleExt.WriteLineWithPretext("No Anime found in DataBase!", ConsoleExt.OutputType.Error);
                    }
                    
                    _hasMultipleParts = HasMultipleParts(new DirectoryInfo(arg).Name);
                    ExtractingSeasonNumber(new DirectoryInfo(arg).Name);
                    
                    var folders = _hasSubFolder ? GetSeasonDirectories() : [arg];
                    
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

                    //ConsoleExt.WriteLineWithPretext($"Database Last Entre was on Line: {FindLastNonNullLine(JsonPath)}", ConsoleExt.OutputType.Info);
                }

                break;
            }
            case 0:
                if (ManualInformationChecking("Do you want to edit the AnimeList?"))
                {
                    AnimeListHandler.StartAnimeListEditing();
                }
                
                else
                {
                    //Task.Run(async () => await WebScraper.ScrapeAnimetoshoExports()).GetAwaiter().GetResult();
                    //ManualBbEditing.StartEditing();
                }
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

    // Creates all the folders if they dont already exist and is used for the folder structure that is the output and store folder for the entire program
    private static void DirectoryCreator()
    {
        if (SubOrDub == null || _animeName == null || SeasonNumbers == null)
        {
            ConsoleExt.WriteLineWithPretext("Anime Name, Sub or Dub, or Season Number is null",
                ConsoleExt.OutputType.Error);
            return;
        }

        if (!Directory.Exists(AnimeOutputFolder)) Directory.CreateDirectory(AnimeOutputFolder);
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!)))
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!));
        if (!Directory.Exists(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!, _animeName)))
            Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!, _animeName));

        if (Directory.Exists(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!, _animeName, @"\Season ",
                SeasonNumbers[0].ToString()))) //this is only applicable for one season
            return;
        Directory.CreateDirectory(Path.Combine(AnimeOutputFolder, SubOrDub.ToString()!, _animeName, @"\Season ",
            SeasonNumbers[0].ToString()));
    }


    // Moves all the episodes to the destination folder
    private static void MoveEpisodes(string[] files)
    {
        if (SeasonNumbers == null) return;
        foreach (var season in SeasonNumbers)
        {
            var episodeNumber = 1;
            foreach (var file in files)
            {
                var fileExtension = new FileInfo(file).Extension;
                var destinationFile = AnimeOutputFolder + @"\" + SubOrDub + @"\" + _animeName + @"\Season " +
                                      season + @"\" + _animeName + " #" + episodeNumber + fileExtension;
                if (!CheckForExistence(file,
                        File.Exists(destinationFile) ? destinationFile : GetFileInProgramFolder("UserSettings.json")) &&
                    !FileIntegrityCheck(File.Exists(destinationFile) ? [destinationFile] : new[] { "" }))
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

    internal enum Language
    {
        Sub,
        Dub
    }
}