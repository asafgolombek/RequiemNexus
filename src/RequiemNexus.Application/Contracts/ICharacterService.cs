namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Full character application API — composition of <see cref="ICharacterReader"/> and <see cref="ICharacterWriter"/>.
/// </summary>
public interface ICharacterService : ICharacterReader, ICharacterWriter
{
}
