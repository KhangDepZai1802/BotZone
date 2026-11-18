using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotZone.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IdentityNamesAndFinancialHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_identity_role_claims_identity_roles_RoleId",
                table: "identity_role_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_identity_user_claims_user_UserId",
                table: "identity_user_claims");

            migrationBuilder.DropForeignKey(
                name: "FK_identity_user_logins_user_UserId",
                table: "identity_user_logins");

            migrationBuilder.DropForeignKey(
                name: "FK_identity_user_roles_identity_roles_RoleId",
                table: "identity_user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_identity_user_roles_user_UserId",
                table: "identity_user_roles");

            migrationBuilder.DropForeignKey(
                name: "FK_identity_user_tokens_user_UserId",
                table: "identity_user_tokens");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "identity_user_tokens",
                newName: "value");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "identity_user_tokens",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "identity_user_tokens",
                newName: "login_provider");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "identity_user_tokens",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "identity_user_roles",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "identity_user_roles",
                newName: "user_id");

            migrationBuilder.RenameIndex(
                name: "IX_identity_user_roles_RoleId",
                table: "identity_user_roles",
                newName: "IX_identity_user_roles_role_id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "identity_user_logins",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "identity_user_logins",
                newName: "provider_display_name");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "identity_user_logins",
                newName: "provider_key");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "identity_user_logins",
                newName: "login_provider");

            migrationBuilder.RenameIndex(
                name: "IX_identity_user_logins_UserId",
                table: "identity_user_logins",
                newName: "IX_identity_user_logins_user_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "identity_user_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "identity_user_claims",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "identity_user_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "identity_user_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_identity_user_claims_UserId",
                table: "identity_user_claims",
                newName: "IX_identity_user_claims_user_id");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "identity_roles",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "identity_roles",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "identity_roles",
                newName: "normalized_name");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "identity_roles",
                newName: "concurrency_stamp");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "identity_role_claims",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "identity_role_claims",
                newName: "role_id");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "identity_role_claims",
                newName: "claim_value");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "identity_role_claims",
                newName: "claim_type");

            migrationBuilder.RenameIndex(
                name: "IX_identity_role_claims_RoleId",
                table: "identity_role_claims",
                newName: "IX_identity_role_claims_role_id");

            migrationBuilder.AddForeignKey(
                name: "FK_identity_role_claims_identity_roles_role_id",
                table: "identity_role_claims",
                column: "role_id",
                principalTable: "identity_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_identity_user_claims_user_user_id",
                table: "identity_user_claims",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_identity_user_logins_user_user_id",
                table: "identity_user_logins",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_identity_user_roles_identity_roles_role_id",
                table: "identity_user_roles",
                column: "role_id",
                principalTable: "identity_roles",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_identity_user_roles_user_user_id",
                table: "identity_user_roles",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_identity_user_tokens_user_user_id",
                table: "identity_user_tokens",
                column: "user_id",
                principalTable: "user",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            migrationBuilder.Sql(MigrationSql.Read("financial-history.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Use a reviewed forward migration to preserve financial history.");
    }
}
