using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SSCA.website.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMeetingScriptureAndPowerPointFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PowerPointBlobName",
                table: "MessageMeetings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scripture",
                table: "MessageMeetings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PowerPointBlobName",
                table: "MessageMeetings");

            migrationBuilder.DropColumn(
                name: "Scripture",
                table: "MessageMeetings");
        }
    }
}
