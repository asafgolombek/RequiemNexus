using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RequiemNexus.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixBooleanColumnsForPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The database was originally created via EnsureCreated using SQLite conventions,
            // which maps bool to INTEGER. This migration corrects all boolean columns to the
            // PostgreSQL-native boolean type. USING expr != 0 safely casts 0→false, 1→true.
            migrationBuilder.Sql(@"
                ALTER TABLE ""AspNetUsers""
                    ALTER COLUMN ""EmailConfirmed""         TYPE boolean USING ""EmailConfirmed"" != 0,
                    ALTER COLUMN ""PhoneNumberConfirmed""   TYPE boolean USING ""PhoneNumberConfirmed"" != 0,
                    ALTER COLUMN ""TwoFactorEnabled""       TYPE boolean USING ""TwoFactorEnabled"" != 0,
                    ALTER COLUMN ""LockoutEnabled""         TYPE boolean USING ""LockoutEnabled"" != 0,
                    ALTER COLUMN ""NotifyOnSecurityEvents"" TYPE boolean USING ""NotifyOnSecurityEvents"" != 0,
                    ALTER COLUMN ""NotifyOnAccountChanges"" TYPE boolean USING ""NotifyOnAccountChanges"" != 0,
                    ALTER COLUMN ""NotifyOnNewsletter""     TYPE boolean USING ""NotifyOnNewsletter"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Clans""
                    ALTER COLUMN ""IsHomebrew"" TYPE boolean USING ""IsHomebrew"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Disciplines""
                    ALTER COLUMN ""IsHomebrew"" TYPE boolean USING ""IsHomebrew"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Merits""
                    ALTER COLUMN ""RequiresSpecification""       TYPE boolean USING ""RequiresSpecification"" != 0,
                    ALTER COLUMN ""CanBePurchasedMultipleTimes"" TYPE boolean USING ""CanBePurchasedMultipleTimes"" != 0,
                    ALTER COLUMN ""IsHomebrew""                  TYPE boolean USING ""IsHomebrew"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Campaigns""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters""
                    ALTER COLUMN ""IsRetired""  TYPE boolean USING ""IsRetired"" != 0,
                    ALTER COLUMN ""IsArchived"" TYPE boolean USING ""IsArchived"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CombatEncounters""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""NpcStatBlocks""
                    ALTER COLUMN ""IsPrebuilt"" TYPE boolean USING ""IsPrebuilt"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterAspirations""
                    ALTER COLUMN ""IsLongTerm"" TYPE boolean USING ""IsLongTerm"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterConditions""
                    ALTER COLUMN ""IsResolved""  TYPE boolean USING ""IsResolved"" != 0,
                    ALTER COLUMN ""AwardsBeat""  TYPE boolean USING ""AwardsBeat"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterNotes""
                    ALTER COLUMN ""IsStorytellerPrivate"" TYPE boolean USING ""IsStorytellerPrivate"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterTilts""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""InitiativeEntries""
                    ALTER COLUMN ""HasActed"" TYPE boolean USING ""HasActed"" != 0;
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ChronicleNpcs""
                    ALTER COLUMN ""IsAlive""   TYPE boolean USING ""IsAlive"" != 0,
                    ALTER COLUMN ""IsVampire"" TYPE boolean USING ""IsVampire"" != 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""AspNetUsers""
                    ALTER COLUMN ""EmailConfirmed""         TYPE integer USING ""EmailConfirmed""::integer,
                    ALTER COLUMN ""PhoneNumberConfirmed""   TYPE integer USING ""PhoneNumberConfirmed""::integer,
                    ALTER COLUMN ""TwoFactorEnabled""       TYPE integer USING ""TwoFactorEnabled""::integer,
                    ALTER COLUMN ""LockoutEnabled""         TYPE integer USING ""LockoutEnabled""::integer,
                    ALTER COLUMN ""NotifyOnSecurityEvents"" TYPE integer USING ""NotifyOnSecurityEvents""::integer,
                    ALTER COLUMN ""NotifyOnAccountChanges"" TYPE integer USING ""NotifyOnAccountChanges""::integer,
                    ALTER COLUMN ""NotifyOnNewsletter""     TYPE integer USING ""NotifyOnNewsletter""::integer;
            ");

            migrationBuilder.Sql(@"ALTER TABLE ""Clans"" ALTER COLUMN ""IsHomebrew"" TYPE integer USING ""IsHomebrew""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""Disciplines"" ALTER COLUMN ""IsHomebrew"" TYPE integer USING ""IsHomebrew""::integer;");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Merits""
                    ALTER COLUMN ""RequiresSpecification""       TYPE integer USING ""RequiresSpecification""::integer,
                    ALTER COLUMN ""CanBePurchasedMultipleTimes"" TYPE integer USING ""CanBePurchasedMultipleTimes""::integer,
                    ALTER COLUMN ""IsHomebrew""                  TYPE integer USING ""IsHomebrew""::integer;
            ");

            migrationBuilder.Sql(@"ALTER TABLE ""Campaigns"" ALTER COLUMN ""IsActive"" TYPE integer USING ""IsActive""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""Characters"" ALTER COLUMN ""IsRetired"" TYPE integer USING ""IsRetired""::integer, ALTER COLUMN ""IsArchived"" TYPE integer USING ""IsArchived""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""CombatEncounters"" ALTER COLUMN ""IsActive"" TYPE integer USING ""IsActive""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""NpcStatBlocks"" ALTER COLUMN ""IsPrebuilt"" TYPE integer USING ""IsPrebuilt""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""CharacterAspirations"" ALTER COLUMN ""IsLongTerm"" TYPE integer USING ""IsLongTerm""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""CharacterConditions"" ALTER COLUMN ""IsResolved"" TYPE integer USING ""IsResolved""::integer, ALTER COLUMN ""AwardsBeat"" TYPE integer USING ""AwardsBeat""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""CharacterNotes"" ALTER COLUMN ""IsStorytellerPrivate"" TYPE integer USING ""IsStorytellerPrivate""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""CharacterTilts"" ALTER COLUMN ""IsActive"" TYPE integer USING ""IsActive""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""InitiativeEntries"" ALTER COLUMN ""HasActed"" TYPE integer USING ""HasActed""::integer;");
            migrationBuilder.Sql(@"ALTER TABLE ""ChronicleNpcs"" ALTER COLUMN ""IsAlive"" TYPE integer USING ""IsAlive""::integer, ALTER COLUMN ""IsVampire"" TYPE integer USING ""IsVampire""::integer;");
        }
    }
}
