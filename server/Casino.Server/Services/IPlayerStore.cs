using Casino.Server.Models;

namespace Casino.Server.Services;

public interface IPlayerStore
{
    Player Create(string name);
    Player? GetById(Guid id);
    Player? GetByToken(Guid token);
}
