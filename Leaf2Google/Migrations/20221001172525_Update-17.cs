using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update17 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_audit");

            migrationBuilder.DropIndex(
                name: "IX_t_leafs_audit_OwnerCarModelId",
                table: "t_leafs_audit");

            migrationBuilder.RenameColumn(
                name: "OwnerCarModelId",
                table: "t_leafs_audit",
                newName: "Owner");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Owner",
                table: "t_leafs_audit",
                newName: "OwnerCarModelId");

            migrationBuilder.CreateIndex(
                name: "IX_t_leafs_audit_OwnerCarModelId",
                table: "t_leafs_audit",
                column: "OwnerCarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_leafs_audit_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_audit",
                column: "OwnerCarModelId",
                principalTable: "t_leafs_leaf",
                principalColumn: "CarModelId");
        }
    }
}
