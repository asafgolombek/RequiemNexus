using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for character inventory (Phase 11 assets).
/// </summary>
public class CharacterAssetService(ApplicationDbContext dbContext, IAuthorizationHelper authHelper) : ICharacterAssetService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;

    /// <inheritdoc />
    public async Task<List<Asset>> GetListedCatalogAsync()
    {
        return await _dbContext.Assets
            .AsNoTracking()
            .Where(a => a.IsListedInCatalog)
            .OrderBy(a => a.Name)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterAsset> AddCharacterAssetAsync(int characterId, int assetId, int quantity, string userId)
    {
        await _authHelper.RequireCharacterAccessAsync(characterId, userId, "modify inventory");

        CharacterAsset row = new()
        {
            CharacterId = characterId,
            AssetId = assetId,
            Quantity = quantity,
        };
        _dbContext.CharacterAssets.Add(row);
        await _dbContext.SaveChangesAsync();
        return row;
    }

    /// <inheritdoc />
    public async Task RemoveCharacterAssetAsync(int characterAssetId, string userId)
    {
        CharacterAsset? row = await _dbContext.CharacterAssets
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId);

        if (row != null)
        {
            await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "remove inventory");
            _dbContext.CharacterAssets.Remove(row);
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task SetEquippedAsync(int characterAssetId, bool isEquipped, string userId)
    {
        CharacterAsset? row = await _dbContext.CharacterAssets
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "toggle equipped");
        row.IsEquipped = isEquipped;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetReadySlotAsync(int characterAssetId, int? slotIndex, string userId)
    {
        if (slotIndex is < 0 or > 2)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Ready slot must be 0–2 or null.");
        }

        CharacterAsset? row = await _dbContext.CharacterAssets
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "set ready slot");

        if (slotIndex.HasValue)
        {
            List<CharacterAsset> others = await _dbContext.CharacterAssets
                .Where(ca => ca.CharacterId == row.CharacterId && ca.ReadySlotIndex == slotIndex && ca.Id != row.Id)
                .ToListAsync();
            foreach (CharacterAsset o in others)
            {
                o.ReadySlotIndex = null;
            }
        }

        row.ReadySlotIndex = slotIndex;
        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetCurrentStructureAsync(int characterAssetId, int? currentStructure, string userId)
    {
        CharacterAsset? row = await _dbContext.CharacterAssets
            .Include(c => c.Character)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "update structure");
        row.CurrentStructure = currentStructure;
        await _dbContext.SaveChangesAsync();
    }
}
