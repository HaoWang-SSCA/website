using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSCA.website.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFridayGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FridayGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BookName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BookEnglishName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FridayGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FridayGroups_DisplayOrder",
                table: "FridayGroups",
                column: "DisplayOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FridayGroups");
        }
    }
}
