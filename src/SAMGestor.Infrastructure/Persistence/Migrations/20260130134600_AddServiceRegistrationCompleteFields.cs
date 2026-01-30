using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceRegistrationCompleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_registrations_retreat_id_status",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.AddColumn<string>(
                name: "church_life_description",
                schema: "core",
                table: "service_registrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "client_ip",
                schema: "core",
                table: "service_registrations",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "education_level",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "family_relationship_description",
                schema: "core",
                table: "service_registrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                schema: "core",
                table: "service_registrations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "marital_status",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "marketing_opt_in",
                schema: "core",
                table: "service_registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "marketing_opt_in_at",
                schema: "core",
                table: "service_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "neighborhood",
                schema: "core",
                table: "service_registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_content_type",
                schema: "core",
                table: "service_registrations",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "photo_size_bytes",
                schema: "core",
                table: "service_registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "photo_storage_key",
                schema: "core",
                table: "service_registrations",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "photo_uploaded_at",
                schema: "core",
                table: "service_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "post_retreat_life_summary",
                schema: "core",
                table: "service_registrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "postal_code",
                schema: "core",
                table: "service_registrations",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "prayer_life_description",
                schema: "core",
                table: "service_registrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pregnancy",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "prev_uncalled_applications",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "profession",
                schema: "core",
                table: "service_registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rahamin_vida_completed",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "self_relationship_description",
                schema: "core",
                table: "service_registrations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "shirt_size",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "state",
                schema: "core",
                table: "service_registrations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "street_and_number",
                schema: "core",
                table: "service_registrations",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "terms_accepted",
                schema: "core",
                table: "service_registrations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "terms_accepted_at",
                schema: "core",
                table: "service_registrations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                schema: "core",
                table: "service_registrations",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                schema: "core",
                table: "service_registrations",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                schema: "core",
                table: "service_registrations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "whatsapp",
                schema: "core",
                table: "service_registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_retreat_id_status_gender",
                schema: "core",
                table: "service_registrations",
                columns: new[] { "retreat_id", "status", "gender" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_service_registrations_retreat_id_status_gender",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "church_life_description",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "client_ip",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "education_level",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "family_relationship_description",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "height_cm",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "marital_status",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "marketing_opt_in",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "marketing_opt_in_at",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "neighborhood",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "photo_content_type",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "photo_size_bytes",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "photo_storage_key",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "photo_uploaded_at",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "post_retreat_life_summary",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "postal_code",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "prayer_life_description",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "pregnancy",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "prev_uncalled_applications",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "profession",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "rahamin_vida_completed",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "self_relationship_description",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "shirt_size",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "state",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "street_and_number",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "terms_accepted",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "terms_accepted_at",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "terms_version",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "user_agent",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "weight_kg",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "whatsapp",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.CreateIndex(
                name: "IX_service_registrations_retreat_id_status",
                schema: "core",
                table: "service_registrations",
                columns: new[] { "retreat_id", "status" });
        }
    }
}
