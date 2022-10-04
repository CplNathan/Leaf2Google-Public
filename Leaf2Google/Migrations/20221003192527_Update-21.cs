using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update21 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_t_leafs_securitykey_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_securitykey");

            migrationBuilder.DropIndex(
                name: "IX_t_leafs_securitykey_OwnerCarModelId",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "OwnerCarModelId",
                table: "t_leafs_securitykey");

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "t_leafs_securitykey",
                type: "varbinary(900)",
                nullable: false,
                defaultValue: new byte[0],
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey");

            migrationBuilder.AlterColumn<byte[]>(
                name: "UserId",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(900)");

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerCarModelId",
                table: "t_leafs_securitykey",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_leafs_securitykey_OwnerCarModelId",
                table: "t_leafs_securitykey",
                column: "OwnerCarModelId");

            migrationBuilder.AddForeignKey(
                name: "FK_t_leafs_securitykey_t_leafs_leaf_OwnerCarModelId",
                table: "t_leafs_securitykey",
                column: "OwnerCarModelId",
                principalTable: "t_leafs_leaf",
                principalColumn: "CarModelId");
        }
    }
}
