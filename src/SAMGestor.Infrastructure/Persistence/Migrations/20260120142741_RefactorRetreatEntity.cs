using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SAMGestor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRetreatEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "region_configs",
                schema: "core");

            migrationBuilder.DropColumn(
                name: "region",
                schema: "core",
                table: "service_registrations");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyBody",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyTitle",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "PrivacyPolicyVersion",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "other_regions_pct",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "west_region_pct",
                schema: "core",
                table: "retreats");

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                schema: "core",
                table: "retreats",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_phone",
                schema: "core",
                table: "retreats",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                schema: "core",
                table: "retreats",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by_user_id",
                schema: "core",
                table: "retreats",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_publicly_visible",
                schema: "core",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified_at",
                schema: "core",
                table: "retreats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_modified_by_user_id",
                schema: "core",
                table: "retreats",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "location",
                schema: "core",
                table: "retreats",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "long_description",
                schema: "core",
                table: "retreats",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_policy_body",
                schema: "core",
                table: "retreats",
                type: "character varying(50000)",
                maxLength: 50000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "privacy_policy_published_at",
                schema: "core",
                table: "retreats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_policy_title",
                schema: "core",
                table: "retreats",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "privacy_policy_version",
                schema: "core",
                table: "retreats",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "published_at",
                schema: "core",
                table: "retreats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "requires_privacy_policy_acceptance",
                schema: "core",
                table: "retreats",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "short_description",
                schema: "core",
                table: "retreats",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                schema: "core",
                table: "retreats",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "retreat_emergency_codes",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by_user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    max_uses = table.Column<int>(type: "integer", nullable: true),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retreat_emergency_codes", x => x.id);
                    table.ForeignKey(
                        name: "FK_retreat_emergency_codes_retreats_retreat_id",
                        column: x => x.retreat_id,
                        principalSchema: "core",
                        principalTable: "retreats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "retreat_images",
                schema: "core",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    storage_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_retreat_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_retreat_images_retreats_retreat_id",
                        column: x => x.retreat_id,
                        principalSchema: "core",
                        principalTable: "retreats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_retreats_created_at",
                schema: "core",
                table: "retreats",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_retreats_edition",
                schema: "core",
                table: "retreats",
                column: "edition");

            migrationBuilder.CreateIndex(
                name: "ix_retreats_public_listing",
                schema: "core",
                table: "retreats",
                columns: new[] { "is_publicly_visible", "status", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_retreat_emergency_codes_active",
                schema: "core",
                table: "retreat_emergency_codes",
                columns: new[] { "retreat_id", "is_active", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_retreat_emergency_codes_code",
                schema: "core",
                table: "retreat_emergency_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retreat_images_storage_id",
                schema: "core",
                table: "retreat_images",
                column: "storage_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_retreat_images_type_order",
                schema: "core",
                table: "retreat_images",
                columns: new[] { "retreat_id", "type", "order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "retreat_emergency_codes",
                schema: "core");

            migrationBuilder.DropTable(
                name: "retreat_images",
                schema: "core");

            migrationBuilder.DropIndex(
                name: "ix_retreats_created_at",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropIndex(
                name: "ix_retreats_edition",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropIndex(
                name: "ix_retreats_public_listing",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "contact_email",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "contact_phone",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "created_by_user_id",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "is_publicly_visible",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "last_modified_at",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "last_modified_by_user_id",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "location",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "long_description",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "privacy_policy_body",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "privacy_policy_published_at",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "privacy_policy_title",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "privacy_policy_version",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "published_at",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "requires_privacy_policy_acceptance",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "short_description",
                schema: "core",
                table: "retreats");

            migrationBuilder.DropColumn(
                name: "status",
                schema: "core",
                table: "retreats");

            migrationBuilder.AddColumn<string>(
                name: "region",
                schema: "core",
                table: "service_registrations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyBody",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyTitle",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrivacyPolicyVersion",
                schema: "core",
                table: "retreats",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "other_regions_pct",
                schema: "core",
                table: "retreats",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "west_region_pct",
                schema: "core",
                table: "retreats",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "region_configs",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    observation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    retreat_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_region_configs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_region_configs_retreat_id_name",
                schema: "core",
                table: "region_configs",
                columns: new[] { "retreat_id", "name" },
                unique: true);
        }
    }
}
