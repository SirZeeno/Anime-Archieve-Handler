namespace Anime_Archive_Handler;

public class AnimeListHandler
{
    //i need an input system that takes an input from the user and compares the anime name with the database via partial search, looking if the anime has an
    //english name, if it does, use that name for the entry.
    //if the anime already exists in the list, dont add it, if it doesnt, add it
    //need to also add a season feature to the list that allows for the anime list to also have the season information that tells what season the user wants
    
    private static readonly string AnimeList = @"Z:\Anime\Test\TestAnimeList.json";

    public static void StartAnimeListEditing()
    {
        CheckFileExistence();
        
        
    }
    
    private static void CheckFileExistence()
    {
        if (!File.Exists(AnimeList))
        {
            File.Create(AnimeList);
        }
    }
    
    
}