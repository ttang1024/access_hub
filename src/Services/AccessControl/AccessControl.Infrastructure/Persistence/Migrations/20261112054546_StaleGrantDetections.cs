using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessHub.AccessControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class StaleGrantDetections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stale_grant_detections",
                schema: "access_control",
                columns: table => new
                {
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    detected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stale_grant_detections", x => x.source_event_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stale_grant_detections",
                schema: "access_control");
        }
    }
}
