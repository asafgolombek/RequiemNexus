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
            // PostgreSQL-native boolean type.
            //
            // USING col::text NOT IN ('0','false','f') is used instead of col != 0 because
            // the latter fails when a column is already of type boolean (no boolean <> integer
            // operator exists in PostgreSQL). Casting via text handles both integer (0/1) and
            // boolean (false/true) source values safely.
            migrationBuilder.Sql(@"
                ALTER TABLE ""AspNetUsers""
                    ALTER COLUMN ""EmailConfirmed""         TYPE boolean USING ""EmailConfirmed""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""PhoneNumberConfirmed""   TYPE boolean USING ""PhoneNumberConfirmed""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""TwoFactorEnabled""       TYPE boolean USING ""TwoFactorEnabled""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""LockoutEnabled""         TYPE boolean USING ""LockoutEnabled""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""NotifyOnSecurityEvents"" TYPE boolean USING ""NotifyOnSecurityEvents""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""NotifyOnAccountChanges"" TYPE boolean USING ""NotifyOnAccountChanges""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""NotifyOnNewsletter""     TYPE boolean USING ""NotifyOnNewsletter""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Clans""
                    ALTER COLUMN ""IsHomebrew"" TYPE boolean USING ""IsHomebrew""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Disciplines""
                    ALTER COLUMN ""IsHomebrew"" TYPE boolean USING ""IsHomebrew""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Merits""
                    ALTER COLUMN ""RequiresSpecification""       TYPE boolean USING ""RequiresSpecification""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""CanBePurchasedMultipleTimes"" TYPE boolean USING ""CanBePurchasedMultipleTimes""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""IsHomebrew""                  TYPE boolean USING ""IsHomebrew""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Campaigns""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""Characters""
                    ALTER COLUMN ""IsRetired""  TYPE boolean USING ""IsRetired""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""IsArchived"" TYPE boolean USING ""IsArchived""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CombatEncounters""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""NpcStatBlocks""
                    ALTER COLUMN ""IsPrebuilt"" TYPE boolean USING ""IsPrebuilt""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterAspirations""
                    ALTER COLUMN ""IsLongTerm"" TYPE boolean USING ""IsLongTerm""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterConditions""
                    ALTER COLUMN ""IsResolved"" TYPE boolean USING ""IsResolved""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""AwardsBeat""  TYPE boolean USING ""AwardsBeat""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterNotes""
                    ALTER COLUMN ""IsStorytellerPrivate"" TYPE boolean USING ""IsStorytellerPrivate""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""CharacterTilts""
                    ALTER COLUMN ""IsActive"" TYPE boolean USING ""IsActive""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""InitiativeEntries""
                    ALTER COLUMN ""HasActed"" TYPE boolean USING ""HasActed""::text NOT IN ('0','false','f');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE ""ChronicleNpcs""
                    ALTER COLUMN ""IsAlive""   TYPE boolean USING ""IsAlive""::text NOT IN ('0','false','f'),
                    ALTER COLUMN ""IsVampire"" TYPE boolean USING ""IsVampire""::text NOT IN ('0','false','f');
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
