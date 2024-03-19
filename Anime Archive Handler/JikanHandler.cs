using JikanDotNet;
using JikanDotNet.Config;
using static Anime_Archive_Handler.DbHandler;

namespace Anime_Archive_Handler;

public static class JikanHandler
{
    private static int _id = 1;
    private static Anime? _anime;
    private static int _consecutiveNulls;

    //need to look into making the json database update every so often
    //need to look into hosting my own jikan API server
    //need to work on getting getAnimeRelations to work so i can link all relational animes together

    public static async Task Start()
    {
        var rateLimiter = new RateLimiter(40, 60000);

        while (true)
        {
            if (rateLimiter.Check())
            {
                //need to check if the specific malId is not contained in the database, if it is, it will skip it, if it isn't but there is a null on the line,
                //it will overwrite it, if there is no null and it actually doesnt exist it will add it, if it exists but some of the information changed it will overwrite it

                _anime = await GetAnime(_id);
                if (_anime != null)
                {
                    AnimeDb.Upsert(RemapToAnimeDto(_anime));
                }
                _id++;
                if (_anime == null)
                    _consecutiveNulls++;
                else
                    _consecutiveNulls = 0;
                if (_consecutiveNulls <= 500) break;
            }
            else
            {
                ConsoleExt.WriteLineWithPretext("Function skipped", ConsoleExt.OutputType.Warning);
            }

            await Task.Delay(1000);
        }
    }

    private static async Task<Anime?> GetAnime(int id)
    {
        ConsoleExt.WriteWithPretext("Getting Anime with ID: " + _id, ConsoleExt.OutputType.Info);
        IJikan jikan = new Jikan(new JikanClientConfiguration { SuppressException = true });
        BaseJikanResponse<Anime> responseString = await jikan.GetAnimeAsync(id);
        PaginatedJikanResponse<ICollection<RelatedEntry>> responseString2 = await jikan.GetAnimeRelationsAsync(id);
        
        if (responseString != null)
        {
            var anime = responseString.Data;
            string? englishName = null;
            string? defaultName = null;
            if (anime is { Titles: not null })
            {
                foreach (var title in anime.Titles)
                    switch (title.Type.ToLower())
                    {
                        case "english":
                            englishName = title.Title;
                            break;
                        //default is mostly japanese in english characters
                        case "default":
                            defaultName = title.Title;
                            break;
                    }

                switch (englishName)
                {
                    case null when defaultName == null:
                        Console.Write(", English and Default Title Not Found!");
                        break;
                    case null:
                        Console.Write(", Name: " + defaultName);
                        break;
                    default:
                        Console.Write(", Name: " + englishName);
                        break;
                }
            }

            Console.WriteLine();
            //await Task.Delay(1000);
            return anime;
        }

        Console.Write(", Anime not found!");
        Console.WriteLine();
        return null;
    }
}

internal class RateLimiter
{
    private readonly int _limit;
    private int _count;

    public RateLimiter(int limit, int interval)
    {
        _limit = limit;
        new Timer(ResetCounter!, null, interval, interval);
    }

    private void ResetCounter(object state)
    {
        _count = 0;
    }

    public bool Check()
    {
        if (_count >= _limit) return false;

        _count++;
        return true;
    }
}