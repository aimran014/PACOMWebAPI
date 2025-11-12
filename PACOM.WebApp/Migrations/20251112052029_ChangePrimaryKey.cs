using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PACOM.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class ChangePrimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityEvents",
                table: "ActivityEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "ActivityEvents",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ActivityEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityEvents",
                table: "ActivityEvents",
                column: "Version");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ActivityEvents",
                table: "ActivityEvents");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "ActivityEvents",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "ActivityEvents",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ActivityEvents",
                table: "ActivityEvents",
                column: "Id");
        }
    }
}
