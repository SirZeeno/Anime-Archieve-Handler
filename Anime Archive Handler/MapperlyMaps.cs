using JikanDotNet;
using Riok.Mapperly.Abstractions;

namespace Anime_Archive_Handler;

[Mapper(UseDeepCloning = true, IgnoreObsoleteMembersStrategy = IgnoreObsoleteMembersStrategy.Both)]
public partial class MapperlyMaps
{
    public partial AnimeDto animeDto(Anime anime);
}