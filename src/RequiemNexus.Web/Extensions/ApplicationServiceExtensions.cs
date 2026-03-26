namespace RequiemNexus.Web.Extensions;

/// <summary>
/// Application and domain service registrations.
/// </summary>
internal static class ApplicationServiceExtensions
{
    internal static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<RequiemNexus.Application.Contracts.IAuthorizationHelper, RequiemNexus.Application.Services.AuthorizationHelper>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICampaignService, RequiemNexus.Application.Services.CampaignService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IBeatLedgerService, RequiemNexus.Application.Services.BeatLedgerService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IConditionService, RequiemNexus.Application.Services.ConditionService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IStorytellerGlimpseService, RequiemNexus.Application.Services.StorytellerGlimpseService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterService, RequiemNexus.Application.Services.CharacterManagementService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IClanService, RequiemNexus.Application.Services.ClanService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IMeritService, RequiemNexus.Application.Services.MeritService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IDisciplineService, RequiemNexus.Application.Services.DisciplineService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IAdvancementService, RequiemNexus.Application.Services.AdvancementService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IAuditLogService, RequiemNexus.Application.Services.AuditLogService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IUserDataExportService, RequiemNexus.Application.Services.UserDataExportService>();
        services.AddHostedService<RequiemNexus.Web.Services.AccountDeletionCleanupService>();
        services.AddHostedService<RequiemNexus.Web.BackgroundServices.SessionTerminationService>();
        services.AddSingleton<RequiemNexus.Domain.Contracts.IExperienceCostRules, RequiemNexus.Domain.ExperienceCostRules>();
        services.AddSingleton<RequiemNexus.Domain.Contracts.ICharacterCreationRules, RequiemNexus.Domain.CharacterCreationRules>();
        services.AddSingleton<RequiemNexus.Domain.Contracts.IConditionRules, RequiemNexus.Domain.ConditionRules>();
        services.AddSingleton<RequiemNexus.Domain.Contracts.IDiceService, RequiemNexus.Domain.Services.DiceService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterExportService, RequiemNexus.Application.Services.CharacterExportService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterService, RequiemNexus.Application.Services.EncounterService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterQueryService, RequiemNexus.Application.Services.EncounterQueryService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterParticipantService, RequiemNexus.Application.Services.EncounterParticipantService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterPrepService, RequiemNexus.Application.Services.EncounterPrepService>();
        services.AddScoped<RequiemNexus.Application.Contracts.INpcCombatService, RequiemNexus.Application.Services.NpcCombatService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterTemplateService, RequiemNexus.Application.Services.EncounterTemplateService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IKindredLineageService, RequiemNexus.Application.Services.KindredLineageService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IBloodSympathyRollService, RequiemNexus.Application.Services.BloodSympathyRollService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IBloodBondService, RequiemNexus.Application.Services.BloodBondService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IBloodBondQueryService, RequiemNexus.Application.Services.BloodBondQueryService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IPredatoryAuraService, RequiemNexus.Application.Services.PredatoryAuraService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IGhoulManagementService, RequiemNexus.Application.Services.GhoulManagementService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IPerceptionRollService, RequiemNexus.Application.Services.PerceptionRollService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICityFactionService, RequiemNexus.Application.Services.CityFactionService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IChronicleNpcService, RequiemNexus.Application.Services.ChronicleNpcService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISocialManeuverLifecycleCoordinator, RequiemNexus.Application.Services.SocialManeuverLifecycleCoordinator>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISocialManeuveringService, RequiemNexus.Application.Services.SocialManeuveringService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISocialManeuverQueryService, RequiemNexus.Application.Services.SocialManeuverQueryService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISocialManeuverRollService, RequiemNexus.Application.Services.SocialManeuverRollService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IFeedingTerritoryService, RequiemNexus.Application.Services.FeedingTerritoryService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IFactionRelationshipService, RequiemNexus.Application.Services.FactionRelationshipService>();
        services.AddScoped<RequiemNexus.Application.Contracts.INpcStatBlockService, RequiemNexus.Application.Services.NpcStatBlockService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterNoteService, RequiemNexus.Application.Services.CharacterNoteService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterAssetService, RequiemNexus.Application.Services.CharacterAssetService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IAssetProcurementService, RequiemNexus.Application.Services.AssetProcurementService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IBloodlineService, RequiemNexus.Application.Services.BloodlineService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICovenantService, RequiemNexus.Application.Services.CovenantService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISorceryService, RequiemNexus.Application.Services.SorceryService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ISorceryActivationService, RequiemNexus.Application.Services.SorceryActivationService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICoilService, RequiemNexus.Application.Services.CoilService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterMeritService, RequiemNexus.Application.Services.CharacterMeritService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterDisciplineService, RequiemNexus.Application.Services.CharacterDisciplineService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IDevotionService, RequiemNexus.Application.Services.DevotionService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IModifierService, RequiemNexus.Application.Services.ModifierService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IDerivedStatService, RequiemNexus.Application.Services.DerivedStatService>();
        services.AddScoped<RequiemNexus.Application.Contracts.ITraitResolver, RequiemNexus.Application.Services.TraitResolver>();
        services.AddScoped<RequiemNexus.Application.Contracts.ICharacterHealthService, RequiemNexus.Application.Services.CharacterHealthService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IAttackService, RequiemNexus.Application.Services.AttackService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IEncounterWeaponDamageRollService, RequiemNexus.Application.Services.EncounterWeaponDamageRollService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IDiceMacroService, RequiemNexus.Application.Services.DiceMacroService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IPublicRollService, RequiemNexus.Application.Services.PublicRollService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewDisciplineService, RequiemNexus.Application.Services.HomebrewDisciplineService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewMeritService, RequiemNexus.Application.Services.HomebrewMeritService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewClanService, RequiemNexus.Application.Services.HomebrewClanService>();
        services.AddScoped<RequiemNexus.Application.Contracts.IHomebrewPackService, RequiemNexus.Application.Services.HomebrewPackService>();

        services.AddSingleton<RequiemNexus.Web.Services.ToastService>();
        services.AddScoped<RequiemNexus.Web.Services.ScreenReaderAnnouncer>();
        services.AddSingleton<RequiemNexus.Web.Services.CommandPaletteService>();
        services.AddScoped<RequiemNexus.Web.Services.PlatformShortcutHintService>();
        services.AddSingleton<Microsoft.AspNetCore.Authentication.Cookies.ITicketStore, RequiemNexus.Web.Services.DatabaseTicketStore>();
    }
}
