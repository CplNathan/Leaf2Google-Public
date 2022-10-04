using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Leaf2Google.Migrations
{
    public partial class Update20 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "credentialId",
                table: "t_leafs_securitykey");

            migrationBuilder.RenameColumn(
                name: "KeyId",
                table: "t_leafs_securitykey",
                newName: "AaGuid");

            migrationBuilder.AddColumn<string>(
                name: "CredType",
                table: "t_leafs_securitykey",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "t_leafs_securitykey",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RegDate",
                table: "t_leafs_securitykey",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "SignatureCounter",
                table: "t_leafs_securitykey",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<byte[]>(
                name: "UserHandle",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "UserId",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredType",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "RegDate",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "SignatureCounter",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "UserHandle",
                table: "t_leafs_securitykey");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "t_leafs_securitykey");

            migrationBuilder.RenameColumn(
                name: "AaGuid",
                table: "t_leafs_securitykey",
                newName: "KeyId");

            migrationBuilder.AddColumn<byte[]>(
                name: "credentialId",
                table: "t_leafs_securitykey",
                type: "varbinary(max)",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddPrimaryKey(
                name: "PK_t_leafs_securitykey",
                table: "t_leafs_securitykey",
                column: "KeyId");
        }
    }
}
