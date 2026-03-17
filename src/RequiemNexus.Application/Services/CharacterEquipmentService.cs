using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character equipment inventory.
/// </summary>
public class CharacterEquipmentService(ApplicationDbContext dbContext, IAuthorizationHelper authHelper) : ICharacterEquipmentService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<Equipment>> GetAvailableEquipmentAsync()
    {
        return await _dbContext.Equipment.OrderBy(e => e.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterEquipment> AddEquipmentAsync(int characterId, int equipmentId, int quantity, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "modify equipment");

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
    public async Task RemoveEquipmentAsync(int characterEquipmentId, string userId)
    {
        CharacterEquipment? ce = await _dbContext.CharacterEquipments
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == characterEquipmentId);

        if (ce != null)
        {
            await _authHelper.RequireCharacterAccessAsync(ce.CharacterId, userId, "remove equipment");
            _dbContext.CharacterEquipments.Remove(ce);
            await _dbContext.SaveChangesAsync();
        }
    }
}
