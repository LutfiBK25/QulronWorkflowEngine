using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "applications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    version = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    version_build = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    last_compiled = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_activated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_applications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_type = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    locked_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modules", x => x.id);
                    table.ForeignKey(
                        name: "FK_modules_applications_application_id",
                        column: x => x.application_id,
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "database_action_modules",
                columns: table => new
                {
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    statement = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_action_modules", x => x.module_id);
                    table.ForeignKey(
                        name: "FK_database_action_modules_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "field_modules",
                columns: table => new
                {
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_type = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_field_modules", x => x.module_id);
                    table.ForeignKey(
                        name: "FK_field_modules_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_modules",
                columns: table => new
                {
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    subtype = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    remote = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dynamic_call = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_modules", x => x.module_id);
                    table.ForeignKey(
                        name: "FK_process_modules_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "process_module_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    process_module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    label_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    action_type = table.Column<int>(type: "integer", nullable: true),
                    action_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action_module_type = table.Column<int>(type: "integer", nullable: true),
                    pass_label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    fail_label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    commented_flag = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_process_module_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_process_module_details_process_modules_process_module_id",
                        column: x => x.process_module_id,
                        principalTable: "process_modules",
                        principalColumn: "module_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_application_name",
                table: "applications",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_modules_application",
                table: "modules",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "idx_modules_name",
                table: "modules",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "idx_modules_type",
                table: "modules",
                column: "module_type");

            migrationBuilder.CreateIndex(
                name: "idx_process_details_module",
                table: "process_module_details",
                column: "process_module_id");

            migrationBuilder.CreateIndex(
                name: "uq_process_label",
                table: "process_module_details",
                columns: new[] { "process_module_id", "label_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_process_sequence",
                table: "process_module_details",
                columns: new[] { "process_module_id", "sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "database_action_modules");

            migrationBuilder.DropTable(
                name: "field_modules");

            migrationBuilder.DropTable(
                name: "process_module_details");

            migrationBuilder.DropTable(
                name: "process_modules");

            migrationBuilder.DropTable(
                name: "modules");

            migrationBuilder.DropTable(
                name: "applications");
        }
    }
}
