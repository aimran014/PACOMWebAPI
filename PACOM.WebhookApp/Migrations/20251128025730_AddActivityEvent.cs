using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PACOM.WebhookApp.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Scope = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScopeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Organization = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CredentialId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CredentialNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MykadNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaFromId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaToId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomDataUDF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomDataString = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UtcTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReaderName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomDataEventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomDataUDFType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Version);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityEvents");
        }
    }
}
