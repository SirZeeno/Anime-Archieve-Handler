using System.Security.Cryptography;
using FFMpegCore;

namespace Anime_Archive_Handler;

public static class FileHandler
{
    
    //File integrity check that returns false if the file is corrupt
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
    
    //Extracts the Audio Track Language by reading the Metadata
    internal static List<string?> TrackLanguageFromMetadata(string videoFilePath)
    {
        var mediaInfo = FFProbe.Analyse(videoFilePath);

        return mediaInfo.AudioStreams.Select(audioStream => audioStream.Language)
            .Where(audioStreamLanguage => audioStreamLanguage != null)
            .Where(audioStreamLanguage => audioStreamLanguage != null).ToList();
    }
    
    // Checks for if the currently being transferred anime already existing 
    internal static bool CheckForExistence(string source, string destination)
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
}