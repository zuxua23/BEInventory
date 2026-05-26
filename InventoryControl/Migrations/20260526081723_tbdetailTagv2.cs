using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryControl.Migrations
{
    /// <inheritdoc />
    public partial class tbdetailTagv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tb_DO_Detail_Tag_tag_id",
                table: "tb_DO_Detail_Tag");

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_Tag_tag_id",
                table: "tb_DO_Detail_Tag",
                column: "tag_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tb_DO_Detail_Tag_tag_id",
                table: "tb_DO_Detail_Tag");

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_Tag_tag_id",
                table: "tb_DO_Detail_Tag",
                column: "tag_id");
        }
    }
}
