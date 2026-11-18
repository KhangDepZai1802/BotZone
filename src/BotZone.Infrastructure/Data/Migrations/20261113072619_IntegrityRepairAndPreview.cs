using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotZone.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IntegrityRepairAndPreview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(MigrationSql.Read("integrity-repair.sql"));
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "landing_preview_grants",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");

            migrationBuilder.AddColumn<string>(
                name: "sections_snapshot",
                table: "landing_preview_grants",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "session_hash",
                table: "landing_preview_grants",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_landing_preview_grants_session_hash",
                table: "landing_preview_grants",
                column: "session_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_landing_preview_grants_landing_drafts_page_id",
                table: "landing_preview_grants",
                column: "page_id",
                principalTable: "landing_drafts",
                principalColumn: "page_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Integrity repairs require an explicit reviewed forward migration; restore backup only to an isolated database.");
    }
}
