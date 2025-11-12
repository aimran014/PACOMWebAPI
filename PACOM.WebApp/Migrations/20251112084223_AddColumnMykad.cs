using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PACOM.WebApp.Migrations
{
    /// <inheritdoc />
    public partial class AddColumnMykad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MykadNumber",
                table: "ActivityEvents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MykadNumber",
                table: "ActivityEvents");
        }
    }
}
