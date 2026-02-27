using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

public interface ICharacterService
{
    Task<List<Character>> GetCharactersByUserIdAsync(string userId);
    Task<Character?> GetCharacterByIdAsync(int id);
    Task DeleteCharacterAsync(int id);
    Task<Character> EmbraceCharacterAsync(Character newCharacter);
}
