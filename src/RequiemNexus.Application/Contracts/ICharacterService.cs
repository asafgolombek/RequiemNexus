namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Full character application API — <see cref="ICharacterReader"/>, <see cref="ICharacterWriter"/>, and <see cref="ICharacterProgressionService"/>.
/// </summary>
public interface ICharacterService : ICharacterReader, ICharacterWriter, ICharacterProgressionService
{
}
