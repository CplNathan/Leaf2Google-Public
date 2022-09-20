using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update_1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audits_Leafs_AuditOwnerLeafId",
                table: "Audits");

            migrationBuilder.DropIndex(
                name: "IX_Audits_AuditOwnerLeafId",
                table: "Audits");

            migrationBuilder.DropColumn(
                name: "AuditOwnerLeafId",
                table: "Audits");

            migrationBuilder.AddColumn<string>(
                name: "test",
                table: "Leafs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Audits",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerLeafId",
                table: "Audits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Audits_OwnerLeafId",
                table: "Audits",
                column: "OwnerLeafId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audits_Leafs_OwnerLeafId",
                table: "Audits",
                column: "OwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audits_Leafs_OwnerLeafId",
                table: "Audits");

            migrationBuilder.DropIndex(
                name: "IX_Audits_OwnerLeafId",
                table: "Audits");

            migrationBuilder.DropColumn(
                name: "test",
                table: "Leafs");

            migrationBuilder.DropColumn(
                name: "OwnerLeafId",
                table: "Audits");

            migrationBuilder.AlterColumn<string>(
                name: "Data",
                table: "Audits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AuditOwnerLeafId",
                table: "Audits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Audits_AuditOwnerLeafId",
                table: "Audits",
                column: "AuditOwnerLeafId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audits_Leafs_AuditOwnerLeafId",
                table: "Audits",
                column: "AuditOwnerLeafId",
                principalTable: "Leafs",
                principalColumn: "LeafId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}