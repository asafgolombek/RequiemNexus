using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character equipment inventory.
/// </summary>
public class CharacterEquipmentService(ApplicationDbContext dbContext) : ICharacterEquipmentService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public async Task<List<Equipment>> GetAvailableEquipmentAsync()
    {
        return await _dbContext.Equipment.OrderBy(e => e.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterEquipment> AddEquipmentAsync(int characterId, int equipmentId, int quantity)
    {
        CharacterEquipment ce = new()
        {
            CharacterId = characterId,
            EquipmentId = equipmentId,
            Quantity = quantity,
        };
        _dbContext.CharacterEquipments.Add(ce);
        await _dbContext.SaveChangesAsync();
        return ce;
    }

    /// <inheritdoc />
    public async Task RemoveEquipmentAsync(int characterEquipmentId)
    {
        CharacterEquipment? ce = await _dbContext.CharacterEquipments.FindAsync(characterEquipmentId);
        if (ce != null)
        {
            _dbContext.CharacterEquipments.Remove(ce);
            await _dbContext.SaveChangesAsync();
        }
    }
}
