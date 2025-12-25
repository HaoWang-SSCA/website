using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSCA.website.API.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessageMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Speaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Topic = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AudioBlobName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VideoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsGospel = table.Column<bool>(type: "boolean", nullable: false),
                    IsSpecialMeeting = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageMeetings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageMeetings_Date",
                table: "MessageMeetings",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_MessageMeetings_IsGospel",
                table: "MessageMeetings",
                column: "IsGospel");

            migrationBuilder.CreateIndex(
                name: "IX_MessageMeetings_IsSpecialMeeting",
                table: "MessageMeetings",
                column: "IsSpecialMeeting");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MessageMeetings");
        }
    }
}
