using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "custom_notifications",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sent_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    target_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    target_filter_json = table.Column<string>(type: "jsonb", nullable: false),
                    template_subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    template_body = table.Column<string>(type: "character varying(50000)", maxLength: 50000, nullable: false),
                    template_preheader_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    template_cta_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    template_cta_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    template_secondary_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    template_secondary_text = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    template_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    total_recipients = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custom_notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_custom_notifications_retreat_id",
                schema: "core",
                table: "custom_notifications",
                column: "retreat_id");

            migrationBuilder.CreateIndex(
                name: "IX_custom_notifications_sent_at",
                schema: "core",
                table: "custom_notifications",
                column: "sent_at");

            migrationBuilder.CreateIndex(
                name: "IX_custom_notifications_sent_by_user_id",
                schema: "core",
                table: "custom_notifications",
                column: "sent_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_custom_notifications_status",
                schema: "core",
                table: "custom_notifications",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "custom_notifications",
                schema: "core");
        }
    }
}
