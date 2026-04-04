using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Default <see cref="IReferenceDataCache"/> — loads seeded catalog rows once and serves them without further EF queries.
/// </summary>
public sealed class ReferenceDataCache : IReferenceDataCache
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);

    private IReadOnlyList<Clan> _referenceClans = [];
    private IReadOnlyList<Discipline> _referenceDisciplines = [];
    private IReadOnlyList<Merit> _referenceMerits = [];
    private IReadOnlyList<CovenantDefinition> _covenantDefinitions = [];
    private IReadOnlyList<SorceryRiteDefinition> _sorceryRiteDefinitions = [];
    private IReadOnlyList<ScaleDefinition> _scaleDefinitions = [];
    private IReadOnlyList<CoilDefinition> _coilDefinitions = [];
    private IReadOnlyList<BloodlineDefinition> _bloodlineDefinitions = [];
    private IReadOnlyList<CovenantDefinitionMerit> _covenantDefinitionMerits = [];
    private IReadOnlyList<DevotionDefinition> _devotionDefinitions = [];

    private volatile bool _isInitialized;

    /// <inheritdoc />
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public IReadOnlyList<Clan> ReferenceClans
    {
        get
        {
            EnsureInitialized();
            return _referenceClans;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Discipline> ReferenceDisciplines
    {
        get
        {
            EnsureInitialized();
            return _referenceDisciplines;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<Merit> ReferenceMerits
    {
        get
        {
            EnsureInitialized();
            return _referenceMerits;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CovenantDefinition> CovenantDefinitions
    {
        get
        {
            EnsureInitialized();
            return _covenantDefinitions;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<SorceryRiteDefinition> SorceryRiteDefinitions
    {
        get
        {
            EnsureInitialized();
            return _sorceryRiteDefinitions;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<ScaleDefinition> ScaleDefinitions
    {
        get
        {
            EnsureInitialized();
            return _scaleDefinitions;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CoilDefinition> CoilDefinitions
    {
        get
        {
            EnsureInitialized();
            return _coilDefinitions;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<BloodlineDefinition> BloodlineDefinitions
    {
        get
        {
            EnsureInitialized();
            return _bloodlineDefinitions;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<CovenantDefinitionMerit> CovenantDefinitionMerits
    {
        get
        {
            EnsureInitialized();
            return _covenantDefinitionMerits;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DevotionDefinition> DevotionDefinitions
    {
        get
        {
            EnsureInitialized();
            return _devotionDefinitions;
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(ApplicationDbContext context, CancellationToken cancellationToken = default) =>
        LoadFromDatabaseAsync(context, cancellationToken, forceReload: true);

    /// <inheritdoc />
    public async Task LoadFromDatabaseAsync(ApplicationDbContext context, CancellationToken cancellationToken = default, bool forceReload = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isInitialized && !forceReload)
            {
                return;
            }

            List<Clan> clans = await context.Clans.AsNoTracking()
                .Where(c => !c.IsHomebrew)
                .Include(c => c.ClanDisciplines)
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<Discipline> disciplines = await context.Disciplines.AsNoTracking()
                .Where(d => !d.IsHomebrew)
                .Include(d => d.Covenant)
                .Include(d => d.Bloodline)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<Merit> merits = await context.Merits.AsNoTracking()
                .Where(m => !m.IsHomebrew)
                .Include(m => m.Prerequisites)
                .OrderBy(m => m.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<CovenantDefinition> covenants = await context.CovenantDefinitions.AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<SorceryRiteDefinition> rites = await context.SorceryRiteDefinitions.AsNoTracking()
                .Include(r => r.RequiredCovenant)
                .Include(r => r.RequiredClan)
                .OrderBy(r => r.SorceryType)
                .ThenBy(r => r.Level)
                .ThenBy(r => r.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<ScaleDefinition> scales = await context.ScaleDefinitions.AsNoTracking()
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<CoilDefinition> coils = await context.CoilDefinitions.AsNoTracking()
                .Include(c => c.Scale)
                .OrderBy(c => c.ScaleId)
                .ThenBy(c => c.Level)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<BloodlineDefinition> bloodlines = await context.BloodlineDefinitions.AsNoTracking()
                .Include(b => b.AllowedParentClans)
                .OrderBy(b => b.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<CovenantDefinitionMerit> covenantMerits = await context.CovenantDefinitionMerits.AsNoTracking()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            List<DevotionDefinition> devotions = await context.DevotionDefinitions.AsNoTracking()
                .Include(d => d.Prerequisites)
                .ThenInclude(p => p.Discipline)
                .OrderBy(d => d.Name)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            _referenceClans = clans;
            _referenceDisciplines = disciplines;
            _referenceMerits = merits;
            _covenantDefinitions = covenants;
            _sorceryRiteDefinitions = rites;
            _scaleDefinitions = scales;
            _coilDefinitions = coils;
            _bloodlineDefinitions = bloodlines;
            _covenantDefinitionMerits = covenantMerits;
            _devotionDefinitions = devotions;
            _isInitialized = true;
        }
        finally
        {
            _loadGate.Release();
        }
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException(
                "Reference data cache is not initialized. Ensure host startup ran ReferenceDataCacheWarmupHostedService or call LoadFromDatabaseAsync in tests.");
        }
    }
}
