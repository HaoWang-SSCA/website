using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSCA.website.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MessageMeetings",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "MessageMeetings",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MessageMeetings",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateTable(
                name: "HeroLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FileBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroLinks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeroLinks_DisplayOrder",
                table: "HeroLinks",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_HeroLinks_ExpiryDate",
                table: "HeroLinks",
                column: "ExpiryDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroLinks");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "MessageMeetings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Date",
                table: "MessageMeetings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "MessageMeetings",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");
        }
    }
}
