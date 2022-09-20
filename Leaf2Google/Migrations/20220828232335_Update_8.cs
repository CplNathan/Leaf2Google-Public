using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerLeafId",
                table: "Auths",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Auths_OwnerLeafId",
                table: "Auths",
                column: "OwnerLeafId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths");

            migrationBuilder.DropIndex(
                name: "IX_Auths_OwnerLeafId",
                table: "Auths");

            migrationBuilder.DropColumn(
                name: "OwnerLeafId",
                table: "Auths");
        }
    }
}