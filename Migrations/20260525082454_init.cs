using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryControl.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tb_DO",
                columns: table => new
                {
                    do_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    do_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    scanner_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_DO", x => x.do_id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Item",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    itm_id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    itm_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    itm_desc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Location",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    loc_id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    loc_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    loc_desc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false),
                    isSystem = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Location", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Module",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    mod_key = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    mod_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Module", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Role",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    rol_code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    rol_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_Stock_Taking",
                columns: table => new
                {
                    stt_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    remark = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Stock_Taking", x => x.stt_id);
                });

            migrationBuilder.CreateTable(
                name: "tb_User",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    usr_id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    usr_fullname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    usr_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    usr_password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_User", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tb_DO_Detail",
                columns: table => new
                {
                    do_detail_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    do_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    itm_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    qty_required = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_DO_Detail", x => x.do_detail_id);
                    table.ForeignKey(
                        name: "FK_tb_DO_Detail_tb_DO_do_id",
                        column: x => x.do_id,
                        principalTable: "tb_DO",
                        principalColumn: "do_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_DO_Detail_tb_Item_itm_id",
                        column: x => x.itm_id,
                        principalTable: "tb_Item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_Reader",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    reader_id = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    location_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    rdr_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ip_address = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Reader", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Reader_tb_Location_location_id",
                        column: x => x.location_id,
                        principalTable: "tb_Location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_Tag",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_epc = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    location_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Tag", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Tag_tb_Item_item_id",
                        column: x => x.item_id,
                        principalTable: "tb_Item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_Tag_tb_Location_location_id",
                        column: x => x.location_id,
                        principalTable: "tb_Location",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_Permission",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    module_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    per_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    per_name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    isActive = table.Column<bool>(type: "bit", nullable: false),
                    isDelete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Permission", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Permission_tb_Module_module_id",
                        column: x => x.module_id,
                        principalTable: "tb_Module",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tb_User_Role",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    user_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_User_Role", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_User_Role_tb_Role_role_id",
                        column: x => x.role_id,
                        principalTable: "tb_Role",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tb_User_Role_tb_User_user_id",
                        column: x => x.user_id,
                        principalTable: "tb_User",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tb_Transaction",
                columns: table => new
                {
                    trs_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    trs_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    reference_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    reader_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Transaction", x => x.trs_id);
                    table.ForeignKey(
                        name: "FK_tb_Transaction_tb_Reader_reader_id",
                        column: x => x.reader_id,
                        principalTable: "tb_Reader",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tb_History_Print",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    trs_type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ref_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_History_Print", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_History_Print_tb_Item_item_id",
                        column: x => x.item_id,
                        principalTable: "tb_Item",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tb_History_Print_tb_Tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tb_Tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tb_Stock_Taking_Detail",
                columns: table => new
                {
                    st_detail_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    stt_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    remark = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    action = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Stock_Taking_Detail", x => x.st_detail_id);
                    table.ForeignKey(
                        name: "FK_tb_Stock_Taking_Detail_tb_Item_item_id",
                        column: x => x.item_id,
                        principalTable: "tb_Item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tb_Stock_Taking_Detail_tb_Stock_Taking_stt_id",
                        column: x => x.stt_id,
                        principalTable: "tb_Stock_Taking",
                        principalColumn: "stt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tb_Stock_Taking_Detail_tb_Tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tb_Tag",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tb_Role_Permission",
                columns: table => new
                {
                    id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    permission_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    role_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Role_Permission", x => x.id);
                    table.ForeignKey(
                        name: "FK_tb_Role_Permission_tb_Permission_permission_id",
                        column: x => x.permission_id,
                        principalTable: "tb_Permission",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tb_Role_Permission_tb_Role_role_id",
                        column: x => x.role_id,
                        principalTable: "tb_Role",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tb_Transaction_Detail",
                columns: table => new
                {
                    trs_detail_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    trs_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    tag_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false),
                    item_id = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tb_Transaction_Detail", x => x.trs_detail_id);
                    table.ForeignKey(
                        name: "FK_tb_Transaction_Detail_tb_Item_item_id",
                        column: x => x.item_id,
                        principalTable: "tb_Item",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tb_Transaction_Detail_tb_Tag_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tb_Tag",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_tb_Transaction_Detail_tb_Transaction_trs_id",
                        column: x => x.trs_id,
                        principalTable: "tb_Transaction",
                        principalColumn: "trs_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_do_id",
                table: "tb_DO_Detail",
                column: "do_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_DO_Detail_itm_id",
                table: "tb_DO_Detail",
                column: "itm_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_History_Print_item_id",
                table: "tb_History_Print",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_History_Print_tag_id",
                table: "tb_History_Print",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Item_itm_id",
                table: "tb_Item",
                column: "itm_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Permission_module_id",
                table: "tb_Permission",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Reader_location_id",
                table: "tb_Reader",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Reader_reader_id",
                table: "tb_Reader",
                column: "reader_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Role_Permission_permission_id",
                table: "tb_Role_Permission",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Role_Permission_role_id",
                table: "tb_Role_Permission",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Stock_Taking_Detail_item_id",
                table: "tb_Stock_Taking_Detail",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Stock_Taking_Detail_stt_id",
                table: "tb_Stock_Taking_Detail",
                column: "stt_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Stock_Taking_Detail_tag_id",
                table: "tb_Stock_Taking_Detail",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Tag_item_id",
                table: "tb_Tag",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Tag_location_id",
                table: "tb_Tag",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Tag_tag_id",
                table: "tb_Tag",
                column: "tag_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_Transaction_reader_id",
                table: "tb_Transaction",
                column: "reader_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Transaction_Detail_item_id",
                table: "tb_Transaction_Detail",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Transaction_Detail_tag_id",
                table: "tb_Transaction_Detail",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_Transaction_Detail_trs_id",
                table: "tb_Transaction_Detail",
                column: "trs_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_User_usr_id",
                table: "tb_User",
                column: "usr_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tb_User_Role_role_id",
                table: "tb_User_Role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_tb_User_Role_user_id",
                table: "tb_User_Role",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tb_DO_Detail");

            migrationBuilder.DropTable(
                name: "tb_History_Print");

            migrationBuilder.DropTable(
                name: "tb_Role_Permission");

            migrationBuilder.DropTable(
                name: "tb_Stock_Taking_Detail");

            migrationBuilder.DropTable(
                name: "tb_Transaction_Detail");

            migrationBuilder.DropTable(
                name: "tb_User_Role");

            migrationBuilder.DropTable(
                name: "tb_DO");

            migrationBuilder.DropTable(
                name: "tb_Permission");

            migrationBuilder.DropTable(
                name: "tb_Stock_Taking");

            migrationBuilder.DropTable(
                name: "tb_Tag");

            migrationBuilder.DropTable(
                name: "tb_Transaction");

            migrationBuilder.DropTable(
                name: "tb_Role");

            migrationBuilder.DropTable(
                name: "tb_User");

            migrationBuilder.DropTable(
                name: "tb_Module");

            migrationBuilder.DropTable(
                name: "tb_Item");

            migrationBuilder.DropTable(
                name: "tb_Reader");

            migrationBuilder.DropTable(
                name: "tb_Location");
        }
    }
}
