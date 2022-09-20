using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audits_Leafs_OwnerLeafId",
                table: "Audits");

            migrationBuilder.DropForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Leafs",
                table: "Leafs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Auths",
                table: "Auths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Audits",
                table: "Audits");

            migrationBuilder.RenameTable(
                name: "Leafs",
                newName: "t_leafs_leaf");

            migrationBuilder.RenameTable(
                name: "Auths",
                newName: "t_auths_auth");

            migrationBuilder.RenameTable(
                name: "Audits",
                newName: "t_leafs_audit");

            migrationBuilder.RenameIndex(
                name: "IX_Auths_OwnerLeafId",
                table: "t_auths_auth",
                newName: "IX_t_auths_auth_OwnerLeafId");

            migrationBuilder.RenameIndex(
                name: "IX_Audits_OwnerLeafId",
                table: "t_leafs_audit",
                newName: "IX_t_leafs_audit_OwnerLeafId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_leaf",
                table: "t_leafs_leaf",
                column: "LeafId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_auths_auth",
                table: "t_auths_auth",
                column: "AuthId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_audit",
                table: "t_leafs_audit",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "t_auths_token",
                columns: table => new
                {
                    TokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerAuthId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_auths_token", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_t_auths_token_t_auths_auth_OwnerAuthId",
                        column: x => x.OwnerAuthId,
                        principalTable: "t_auths_auth",
                        principalColumn: "AuthId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_auths_token_OwnerAuthId",
                table: "t_auths_token",
                column: "OwnerAuthId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_auths_auth_t_leafs_leaf_OwnerLeafId",
                table: "t_auths_auth",
                column: "OwnerLeafId",
                principalTable: "t_leafs_leaf",
                principalColumn: "LeafId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerLeafId",
                table: "t_leafs_audit",
                column: "OwnerLeafId",
                principalTable: "t_leafs_leaf",
                principalColumn: "LeafId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_auths_auth_t_leafs_leaf_OwnerLeafId",
                table: "t_auths_auth");

            migrationBuilder.DropForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerLeafId",
                table: "t_leafs_audit");

            migrationBuilder.DropTable(
                name: "t_auths_token");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_leaf",
                table: "t_leafs_leaf");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_audit",
                table: "t_leafs_audit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_t_auths_auth",
                table: "t_auths_auth");

            migrationBuilder.RenameTable(
                name: "t_leafs_leaf",
                newName: "Leafs");

            migrationBuilder.RenameTable(
                name: "t_leafs_audit",
                newName: "Audits");

            migrationBuilder.RenameTable(
                name: "t_auths_auth",
                newName: "Auths");

            migrationBuilder.RenameIndex(
                name: "IX_t_leafs_audit_OwnerLeafId",
                table: "Audits",
                newName: "IX_Audits_OwnerLeafId");

            migrationBuilder.RenameIndex(
                name: "IX_t_auths_auth_OwnerLeafId",
                table: "Auths",
                newName: "IX_Auths_OwnerLeafId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Leafs",
                table: "Leafs",
                column: "LeafId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Audits",
                table: "Audits",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Auths",
                table: "Auths",
                column: "AuthId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audits_Leafs_OwnerLeafId",
                table: "Audits",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId");
        }
    }
}