using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_15Refactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_auths_auth_t_leafs_leaf_OwnerLeafId",
                table: "t_auths_auth");

            migrationBuilder.DropForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerLeafId",
                table: "t_leafs_audit");

            migrationBuilder.DropColumn(
                name: "PrimaryVin",
                table: "t_leafs_leaf");

            migrationBuilder.RenameColumn(
                name: "LeafId",
                table: "t_leafs_leaf",
                newName: "CarModelId");

            migrationBuilder.RenameColumn(
                name: "OwnerLeafId",
                table: "t_leafs_audit",
                newName: "OwnerCarModelId");

            migrationBuilder.RenameIndex(
                name: "IX_t_leafs_audit_OwnerLeafId",
                table: "t_leafs_audit",
                newName: "IX_t_leafs_audit_OwnerCarModelId");

            migrationBuilder.RenameColumn(
                name: "OwnerLeafId",
                table: "t_auths_auth",
                newName: "OwnerCarModelId");

            migrationBuilder.RenameIndex(
                name: "IX_t_auths_auth_OwnerLeafId",
                table: "t_auths_auth",
                newName: "IX_t_auths_auth_OwnerCarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_auths_auth_t_leafs_leaf_OwnerCarModelId",
                table: "t_auths_auth",
                column: "OwnerCarModelId",
                principalTable: "t_leafs_leaf",
                principalColumn: "CarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_audit",
                column: "OwnerCarModelId",
                principalTable: "t_leafs_leaf",
                principalColumn: "CarModelId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_auths_auth_t_leafs_leaf_OwnerCarModelId",
                table: "t_auths_auth");

            migrationBuilder.DropForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_audit");

            migrationBuilder.RenameColumn(
                name: "CarModelId",
                table: "t_leafs_leaf",
                newName: "LeafId");

            migrationBuilder.RenameColumn(
                name: "OwnerCarModelId",
                table: "t_leafs_audit",
                newName: "OwnerLeafId");

            migrationBuilder.RenameIndex(
                name: "IX_t_leafs_audit_OwnerCarModelId",
                table: "t_leafs_audit",
                newName: "IX_t_leafs_audit_OwnerLeafId");

            migrationBuilder.RenameColumn(
                name: "OwnerCarModelId",
                table: "t_auths_auth",
                newName: "OwnerLeafId");

            migrationBuilder.RenameIndex(
                name: "IX_t_auths_auth_OwnerCarModelId",
                table: "t_auths_auth",
                newName: "IX_t_auths_auth_OwnerLeafId");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryVin",
                table: "t_leafs_leaf",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

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
    }
}
