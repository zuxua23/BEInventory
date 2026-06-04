using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryControl.Migrations
{
    /// <inheritdoc />
    public partial class tbReaderSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "duplicate_scan_interval",
                table: "tb_Reader",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "search_mode",
                table: "tb_Reader",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "session",
                table: "tb_Reader",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "critical_stock",
                table: "tb_Item",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "minimum_stock",
                table: "tb_Item",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "tb_Reader_Settings",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    reader_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    antenna_no = table.Column<int>(type: "int", nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    tx_power = table.Column<double>(type: "float", nullable: false),
                    sensitivity = table.Column<double>(type: "float", nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Reader_Settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Reader_Settings_tb_Reader_reader_id",
                        column: x => x.reader_id,
                        principalTable: "tb_Reader",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_Reader_Settings_reader_id",
                table: "tb_Reader_Settings",
                column: "reader_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_Reader_Settings");

            migrationBuilder.DropColumn(
                name: "duplicate_scan_interval",
                table: "tb_Reader");

            migrationBuilder.DropColumn(
                name: "search_mode",
                table: "tb_Reader");

            migrationBuilder.DropColumn(
                name: "session",
                table: "tb_Reader");

            migrationBuilder.DropColumn(
                name: "critical_stock",
                table: "tb_Item");

            migrationBuilder.DropColumn(
                name: "minimum_stock",
                table: "tb_Item");
        }
    }
}
