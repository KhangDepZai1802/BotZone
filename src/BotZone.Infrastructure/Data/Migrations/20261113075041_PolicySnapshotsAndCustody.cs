using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotZone.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PolicySnapshotsAndCustody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "trade_in_identifier_active_idx",
                table: "trade_in_items");

            migrationBuilder.DropIndex(
                name: "IX_inventory_quarantine_order_item_unit_amount_id",
                table: "inventory_quarantine");

            migrationBuilder.AddColumn<long>(
                name: "point_value_vnd",
                table: "order_item_unit_amounts",
                type: "bigint",
                nullable: false,
                defaultValue: 1000L);

            migrationBuilder.CreateIndex(
                name: "trade_in_identifier_active_idx",
                table: "trade_in_items",
                column: "identifier",
                unique: true,
                filter: "((identifier IS NOT NULL) AND (state <> ALL (ARRAY['completed'::text, 'returned'::text])))");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_quarantine_order_item_unit_amount_id_lot_id",
                table: "inventory_quarantine",
                columns: new[] { "order_item_unit_amount_id", "lot_id" },
                unique: true);
            migrationBuilder.Sql(MigrationSql.Read("policy-snapshot.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Use a reviewed forward migration; do not erase historical point-value snapshots.");
    }
}
