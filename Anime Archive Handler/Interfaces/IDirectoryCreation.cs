// do namespace names or values matter???
namespace Anime_Archive_Handler.Interfaces;

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