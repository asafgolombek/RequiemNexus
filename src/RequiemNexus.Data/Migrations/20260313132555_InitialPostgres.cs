using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    MemberSince = table.Column<DateOnly>(type: "date", nullable: true),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    DeletionScheduledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotifyOnSecurityEvents = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnAccountChanges = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnNewsletter = table.Column<bool>(type: "boolean", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    IsHomebrew = table.Column<bool>(type: "boolean", nullable: false),
                    HombrewAuthorUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Disciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsHomebrew = table.Column<bool>(type: "boolean", nullable: false),
                    HombrewAuthorUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Disciplines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    ArmorRating = table.Column<int>(type: "integer", nullable: false),
                    Penalty = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Merits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ValidRatings = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiresSpecification = table.Column<bool>(type: "boolean", nullable: false),
                    CanBePurchasedMultipleTimes = table.Column<bool>(type: "boolean", nullable: false),
                    IsHomebrew = table.Column<bool>(type: "boolean", nullable: false),
                    HombrewAuthorUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Merits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StoryTellerId = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Campaigns_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Campaigns_AspNetUsers_StoryTellerId",
                        column: x => x.StoryTellerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConsentedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DocumentVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    ConsentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FidoStoredCredentials",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserHandle = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    CredId = table.Column<string>(type: "text", nullable: false),
                    RegDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    CredType = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FidoStoredCredentials", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_FidoStoredCredentials_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<byte[]>(type: "bytea", nullable: false),
                    LastActive = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClanDisciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClanId = table.Column<int>(type: "integer", nullable: false),
                    DisciplineId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClanDisciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClanDisciplines_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClanDisciplines_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisciplinePowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisciplineId = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DicePool = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Cost = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisciplinePowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisciplinePowers_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignLore",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignLore", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignLore_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Concept = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Mask = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Dirge = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Touchstone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Height = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EyeColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    HairColor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Backstory = table.Column<string>(type: "text", nullable: false),
                    ClanId = table.Column<int>(type: "integer", nullable: true),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    Humanity = table.Column<int>(type: "integer", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    ExperiencePoints = table.Column<int>(type: "integer", nullable: false),
                    TotalExperiencePoints = table.Column<int>(type: "integer", nullable: false),
                    Beats = table.Column<int>(type: "integer", nullable: false),
                    MaxHealth = table.Column<int>(type: "integer", nullable: false),
                    CurrentHealth = table.Column<int>(type: "integer", nullable: false),
                    HealthDamage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaxWillpower = table.Column<int>(type: "integer", nullable: false),
                    CurrentWillpower = table.Column<int>(type: "integer", nullable: false),
                    BloodPotency = table.Column<int>(type: "integer", nullable: false),
                    MaxVitae = table.Column<int>(type: "integer", nullable: false),
                    CurrentVitae = table.Column<int>(type: "integer", nullable: false),
                    IsRetired = table.Column<bool>(type: "boolean", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Characters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Characters_Clans_ClanId",
                        column: x => x.ClanId,
                        principalTable: "Clans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CombatEncounters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CombatEncounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CombatEncounters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpcStatBlocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Concept = table.Column<string>(type: "text", nullable: false),
                    Size = table.Column<int>(type: "integer", nullable: false),
                    Health = table.Column<int>(type: "integer", nullable: false),
                    Willpower = table.Column<int>(type: "integer", nullable: false),
                    BludgeoningArmor = table.Column<int>(type: "integer", nullable: false),
                    LethalArmor = table.Column<int>(type: "integer", nullable: false),
                    AttributesJson = table.Column<string>(type: "text", nullable: false),
                    SkillsJson = table.Column<string>(type: "text", nullable: false),
                    DisciplinesJson = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                    IsPrebuilt = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcStatBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcStatBlocks_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SessionPrepNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionPrepNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SessionPrepNotes_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BeatLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AwardedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BeatLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BeatLedger_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_BeatLedger_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAspirations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    IsLongTerm = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAspirations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAspirations_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterAttributes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterAttributes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterBanes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterBanes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterBanes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    CustomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsResolved = table.Column<bool>(type: "boolean", nullable: false),
                    AwardsBeat = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterConditions_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterDisciplines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    DisciplineId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterDisciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterDisciplines_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterDisciplines_Disciplines_DisciplineId",
                        column: x => x.DisciplineId,
                        principalTable: "Disciplines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterEquipments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    EquipmentId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterEquipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterEquipments_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterEquipments_Equipment_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterMerits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    MeritId = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Specification = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMerits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterMerits_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterMerits_Merits_MeritId",
                        column: x => x.MeritId,
                        principalTable: "Merits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    AuthorUserId = table.Column<string>(type: "text", nullable: false),
                    IsStorytellerPrivate = table.Column<bool>(type: "boolean", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterNotes_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Specialty = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSkills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterTilts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    EncounterId = table.Column<int>(type: "integer", nullable: true),
                    TiltType = table.Column<int>(type: "integer", nullable: false),
                    CustomName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTilts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTilts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiceMacros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DicePool = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiceMacros", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiceMacros_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "XpLedger",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<int>(type: "integer", nullable: false),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: true),
                    Expense = table.Column<int>(type: "integer", nullable: true),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActingUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XpLedger", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XpLedger_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XpLedger_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EncounterId = table.Column<int>(type: "integer", nullable: false),
                    CharacterId = table.Column<int>(type: "integer", nullable: true),
                    NpcName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InitiativeMod = table.Column<int>(type: "integer", nullable: false),
                    RollResult = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    HasActed = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeEntries_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_InitiativeEntries_CombatEncounters_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "CombatEncounters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChronicleNpcs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PrimaryFactionId = table.Column<int>(type: "integer", nullable: true),
                    RoleInFaction = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublicDescription = table.Column<string>(type: "text", nullable: false),
                    StorytellerNotes = table.Column<string>(type: "text", nullable: false),
                    IsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    LinkedStatBlockId = table.Column<int>(type: "integer", nullable: true),
                    IsVampire = table.Column<bool>(type: "boolean", nullable: false),
                    AttributesJson = table.Column<string>(type: "text", nullable: false),
                    SkillsJson = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChronicleNpcs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChronicleNpcs_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CityFactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FactionType = table.Column<int>(type: "integer", nullable: false),
                    InfluenceRating = table.Column<int>(type: "integer", nullable: false),
                    PublicDescription = table.Column<string>(type: "text", nullable: false),
                    StorytellerNotes = table.Column<string>(type: "text", nullable: false),
                    Agenda = table.Column<string>(type: "text", nullable: false),
                    LeaderNpcId = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CityFactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CityFactions_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CityFactions_ChronicleNpcs_LeaderNpcId",
                        column: x => x.LeaderNpcId,
                        principalTable: "ChronicleNpcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FactionRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    FactionAId = table.Column<int>(type: "integer", nullable: false),
                    FactionBId = table.Column<int>(type: "integer", nullable: false),
                    StanceFromA = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_CityFactions_FactionAId",
                        column: x => x.FactionAId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_CityFactions_FactionBId",
                        column: x => x.FactionBId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeedingTerritories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    ControlledByFactionId = table.Column<int>(type: "integer", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedingTerritories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedingTerritories_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedingTerritories_CityFactions_ControlledByFactionId",
                        column: x => x.ControlledByFactionId,
                        principalTable: "CityFactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_OccurredAt",
                table: "AuditLogs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_CampaignId",
                table: "BeatLedger",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_CharacterId",
                table: "BeatLedger",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_BeatLedger_OccurredAt",
                table: "BeatLedger",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignLore_CampaignId",
                table: "CampaignLore",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_ApplicationUserId",
                table: "Campaigns",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Campaigns_StoryTellerId",
                table: "Campaigns",
                column: "StoryTellerId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAspirations_CharacterId",
                table: "CharacterAspirations",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterAttributes_CharacterId_Name",
                table: "CharacterAttributes",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterBanes_CharacterId",
                table: "CharacterBanes",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterConditions_CharacterId",
                table: "CharacterConditions",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDisciplines_CharacterId",
                table: "CharacterDisciplines",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterDisciplines_DisciplineId",
                table: "CharacterDisciplines",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEquipments_CharacterId",
                table: "CharacterEquipments",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterEquipments_EquipmentId",
                table: "CharacterEquipments",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMerits_CharacterId",
                table: "CharacterMerits",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMerits_MeritId",
                table: "CharacterMerits",
                column: "MeritId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNotes_CampaignId",
                table: "CharacterNotes",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterNotes_CharacterId",
                table: "CharacterNotes",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ApplicationUserId",
                table: "Characters",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_CampaignId",
                table: "Characters",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ClanId",
                table: "Characters",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSkills_CharacterId_Name",
                table: "CharacterSkills",
                columns: new[] { "CharacterId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTilts_CharacterId",
                table: "CharacterTilts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleNpcs_CampaignId",
                table: "ChronicleNpcs",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ChronicleNpcs_PrimaryFactionId",
                table: "ChronicleNpcs",
                column: "PrimaryFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_CityFactions_CampaignId",
                table: "CityFactions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CityFactions_LeaderNpcId",
                table: "CityFactions",
                column: "LeaderNpcId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanDisciplines_ClanId",
                table: "ClanDisciplines",
                column: "ClanId");

            migrationBuilder.CreateIndex(
                name: "IX_ClanDisciplines_DisciplineId",
                table: "ClanDisciplines",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_CombatEncounters_CampaignId",
                table: "CombatEncounters",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsentLogs_UserId",
                table: "ConsentLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DiceMacros_CharacterId",
                table: "DiceMacros",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinePowers_DisciplineId",
                table: "DisciplinePowers",
                column: "DisciplineId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_CampaignId",
                table: "FactionRelationships",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_FactionAId",
                table: "FactionRelationships",
                column: "FactionAId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_FactionBId",
                table: "FactionRelationships",
                column: "FactionBId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingTerritories_CampaignId",
                table: "FeedingTerritories",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedingTerritories_ControlledByFactionId",
                table: "FeedingTerritories",
                column: "ControlledByFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_CharacterId",
                table: "InitiativeEntries",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_EncounterId",
                table: "InitiativeEntries",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcStatBlocks_CampaignId",
                table: "NpcStatBlocks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcStatBlocks_IsPrebuilt",
                table: "NpcStatBlocks",
                column: "IsPrebuilt");

            migrationBuilder.CreateIndex(
                name: "IX_SessionPrepNotes_CampaignId",
                table: "SessionPrepNotes",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_ApplicationUserId",
                table: "UserSessions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_CampaignId",
                table: "XpLedger",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_CharacterId",
                table: "XpLedger",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_XpLedger_OccurredAt",
                table: "XpLedger",
                column: "OccurredAt");

            migrationBuilder.AddForeignKey(
                name: "FK_ChronicleNpcs_CityFactions_PrimaryFactionId",
                table: "ChronicleNpcs",
                column: "PrimaryFactionId",
                principalTable: "CityFactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_AspNetUsers_ApplicationUserId",
                table: "Campaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_Campaigns_AspNetUsers_StoryTellerId",
                table: "Campaigns");

            migrationBuilder.DropForeignKey(
                name: "FK_ChronicleNpcs_Campaigns_CampaignId",
                table: "ChronicleNpcs");

            migrationBuilder.DropForeignKey(
                name: "FK_CityFactions_Campaigns_CampaignId",
                table: "CityFactions");

            migrationBuilder.DropForeignKey(
                name: "FK_ChronicleNpcs_CityFactions_PrimaryFactionId",
                table: "ChronicleNpcs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BeatLedger");

            migrationBuilder.DropTable(
                name: "CampaignLore");

            migrationBuilder.DropTable(
                name: "CharacterAspirations");

            migrationBuilder.DropTable(
                name: "CharacterAttributes");

            migrationBuilder.DropTable(
                name: "CharacterBanes");

            migrationBuilder.DropTable(
                name: "CharacterConditions");

            migrationBuilder.DropTable(
                name: "CharacterDisciplines");

            migrationBuilder.DropTable(
                name: "CharacterEquipments");

            migrationBuilder.DropTable(
                name: "CharacterMerits");

            migrationBuilder.DropTable(
                name: "CharacterNotes");

            migrationBuilder.DropTable(
                name: "CharacterSkills");

            migrationBuilder.DropTable(
                name: "CharacterTilts");

            migrationBuilder.DropTable(
                name: "ClanDisciplines");

            migrationBuilder.DropTable(
                name: "ConsentLogs");

            migrationBuilder.DropTable(
                name: "DiceMacros");

            migrationBuilder.DropTable(
                name: "DisciplinePowers");

            migrationBuilder.DropTable(
                name: "FactionRelationships");

            migrationBuilder.DropTable(
                name: "FeedingTerritories");

            migrationBuilder.DropTable(
                name: "FidoStoredCredentials");

            migrationBuilder.DropTable(
                name: "InitiativeEntries");

            migrationBuilder.DropTable(
                name: "NpcStatBlocks");

            migrationBuilder.DropTable(
                name: "SessionPrepNotes");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "XpLedger");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Equipment");

            migrationBuilder.DropTable(
                name: "Merits");

            migrationBuilder.DropTable(
                name: "Disciplines");

            migrationBuilder.DropTable(
                name: "CombatEncounters");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Clans");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "CityFactions");

            migrationBuilder.DropTable(
                name: "ChronicleNpcs");
        }
    }
}
