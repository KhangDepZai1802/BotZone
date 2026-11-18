using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotZone.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WarrantyQuantityAndLedgerSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "old_unit_id",
                table: "warranty_replacements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "new_unit_id",
                table: "warranty_replacements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "new_lot_id",
                table: "warranty_replacements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "old_lot_id",
                table: "warranty_replacements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "order_item_unit_amount_id",
                table: "warranty_replacements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "sections_snapshot",
                table: "landing_preview_grants",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb",
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.CreateTable(
                name: "inventory_quarantine",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    order_item_unit_amount_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lot_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inventory_quarantine", x => x.id);
                    table.CheckConstraint("quarantine_quantity_check", "quantity=1");
                    table.CheckConstraint("quarantine_state_check", "state IN ('quarantine','service','released','disposed')");
                    table.ForeignKey(
                        name: "FK_inventory_quarantine_inventory_lots_lot_id",
                        column: x => x.lot_id,
                        principalTable: "inventory_lots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_quarantine_order_item_unit_amounts_order_item_uni~",
                        column: x => x.order_item_unit_amount_id,
                        principalTable: "order_item_unit_amounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warranty_replacements_new_lot_id",
                table: "warranty_replacements",
                column: "new_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_warranty_replacements_old_lot_id",
                table: "warranty_replacements",
                column: "old_lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_warranty_replacements_order_item_unit_amount_id",
                table: "warranty_replacements",
                column: "order_item_unit_amount_id");

            migrationBuilder.AddCheckConstraint(
                name: "replacement_tracking_source_check",
                table: "warranty_replacements",
                sql: "(old_unit_id IS NOT NULL AND new_unit_id IS NOT NULL AND old_lot_id IS NULL AND new_lot_id IS NULL) OR (old_unit_id IS NULL AND new_unit_id IS NULL AND old_lot_id IS NOT NULL AND new_lot_id IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_quarantine_lot_id",
                table: "inventory_quarantine",
                column: "lot_id");

            migrationBuilder.CreateIndex(
                name: "IX_inventory_quarantine_order_item_unit_amount_id",
                table: "inventory_quarantine",
                column: "order_item_unit_amount_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_warranty_replacements_inventory_lots_new_lot_id",
                table: "warranty_replacements",
                column: "new_lot_id",
                principalTable: "inventory_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warranty_replacements_inventory_lots_old_lot_id",
                table: "warranty_replacements",
                column: "old_lot_id",
                principalTable: "inventory_lots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_warranty_replacements_order_item_unit_amounts_order_item_un~",
                table: "warranty_replacements",
                column: "order_item_unit_amount_id",
                principalTable: "order_item_unit_amounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
            migrationBuilder.Sql(MigrationSql.Read("aftersales-sources.sql"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => throw new NotSupportedException("Use a reviewed forward migration or restore backup to an isolated database.");
    }
}
