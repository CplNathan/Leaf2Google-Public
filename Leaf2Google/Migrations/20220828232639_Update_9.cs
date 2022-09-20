using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerLeafId",
                table: "Auths",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths");

            migrationBuilder.AlterColumn<Guid>(
                name: "OwnerLeafId",
                table: "Auths",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Auths_Leafs_OwnerLeafId",
                table: "Auths",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}