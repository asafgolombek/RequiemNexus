using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for character inventory (Phase 11 assets).
/// </summary>
public class CharacterAssetService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ILogger<CharacterAssetService> logger) : ICharacterAssetService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ILogger<CharacterAssetService> _logger = logger;

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
            IsEquipped = false,
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
            .Include(c => c.Asset)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "toggle equipped");

        List<CharacterAsset> all = await LoadTrackedInventoryForCharacterAsync(row.CharacterId);
        CharacterAssetInventoryMutation.ApplyEquippedProposal(all, characterAssetId, isEquipped);

        Result<bool> check = CharacterAssetInventoryMutation.ValidateInventory(all);
        if (!check.IsSuccess)
        {
            _logger.LogWarning(
                "Equip validation failed for character asset {CharacterAssetId} on character {CharacterId}: {Reason}",
                characterAssetId,
                row.CharacterId,
                check.Error);
            throw new InvalidOperationException(check.Error);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task SetBackpackSlotAsync(int characterAssetId, int? slotIndex, string userId)
    {
        if (slotIndex is < 0 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(slotIndex), "Backpack slot must be 0–9 or null.");
        }

        CharacterAsset? row = await _dbContext.CharacterAssets
            .Include(c => c.Character)
            .Include(c => c.Asset)
            .FirstOrDefaultAsync(c => c.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        await _authHelper.RequireCharacterAccessAsync(row.CharacterId, userId, "set backpack slot");

        List<CharacterAsset> all = await LoadTrackedInventoryForCharacterAsync(row.CharacterId);
        CharacterAssetInventoryMutation.ApplyBackpackProposal(all, characterAssetId, slotIndex);

        Result<bool> check = CharacterAssetInventoryMutation.ValidateInventory(all);
        if (!check.IsSuccess)
        {
            _logger.LogWarning(
                "Backpack validation failed for character asset {CharacterAssetId} on character {CharacterId}: {Reason}",
                characterAssetId,
                row.CharacterId,
                check.Error);
            throw new InvalidOperationException(check.Error);
        }

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

    private async Task<List<CharacterAsset>> LoadTrackedInventoryForCharacterAsync(int characterId)
    {
        return await _dbContext.CharacterAssets
            .Include(ca => ca.Asset)
            .Where(ca => ca.CharacterId == characterId)
            .ToListAsync();
    }
}
