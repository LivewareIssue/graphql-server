using Server.Entities;

namespace Server.Services;

public interface IEntPersonService
{
    ValueTask<EntPerson?> GetAsync(string id);
}