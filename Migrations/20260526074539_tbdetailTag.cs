using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryControl.Migrations
{
    /// <inheritdoc />
    public partial class tbdetailTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_DO_Detail_Tag",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    do_detail_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_DO_Detail_Tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_DO_Detail_Tag_tb_DO_Detail_do_detail_id",
                        column: x => x.do_detail_id,
                        principalTable: "tb_DO_Detail",
                        principalColumn: "do_detail_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_DO_Detail_Tag_tb_Tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tb_Tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_Tag_do_detail_id",
                table: "tb_DO_Detail_Tag",
                column: "do_detail_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_Tag_tag_id",
                table: "tb_DO_Detail_Tag",
                column: "tag_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_DO_Detail_Tag");
        }
    }
}
