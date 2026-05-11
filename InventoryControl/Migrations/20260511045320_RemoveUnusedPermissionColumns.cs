using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryControl.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedPermissionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tb_Permission_per_id",
                table: "tb_Permission");

            migrationBuilder.DropColumn(
                name: "per_desc",
                table: "tb_Permission");

            migrationBuilder.DropColumn(
                name: "per_id",
                table: "tb_Permission");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "per_desc",
                table: "tb_Permission",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "per_id",
                table: "tb_Permission",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Permission_per_id",
                table: "tb_Permission",
                column: "per_id",
                unique: true);
        }
    }
}
