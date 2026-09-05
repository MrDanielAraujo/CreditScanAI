using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sequence_order = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_types", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_types_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    industry = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fiscal_year_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reporting_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "BRL"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.id);
                    table.ForeignKey(
                        name: "FK_companies_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    quarter = table.Column<int>(type: "integer", nullable: true),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_periods_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_subtypes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    sequence_order = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_subtypes", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_subtypes_account_types_account_type_id",
                        column: x => x.account_type_id,
                        principalTable: "account_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_subtypes_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    upload_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    file_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    extraction_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    extraction_started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    extraction_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    extraction_error = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_documents_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_documents_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "type_subtype_compatibility",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_subtype_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sequence_order = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_type_subtype_compatibility", x => x.id);
                    table.ForeignKey(
                        name: "FK_type_subtype_compatibility_account_subtypes_account_subtype~",
                        column: x => x.account_subtype_id,
                        principalTable: "account_subtypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_type_subtype_compatibility_account_types_account_type_id",
                        column: x => x.account_type_id,
                        principalTable: "account_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_type_subtype_compatibility_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_subtypes_account_type_id",
                table: "account_subtypes",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_subtypes_tenant_id_code",
                table: "account_subtypes",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_types_tenant_id_code",
                table: "account_types",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_tenant_id_code",
                table: "companies",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_documents_status",
                table: "documents",
                column: "extraction_status");

            migrationBuilder.CreateIndex(
                name: "idx_documents_tenant_company",
                table: "documents",
                columns: new[] { "tenant_id", "company_id" });

            migrationBuilder.CreateIndex(
                name: "idx_documents_type",
                table: "documents",
                column: "document_type");

            migrationBuilder.CreateIndex(
                name: "IX_documents_company_id",
                table: "documents",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_periods_tenant_id_period_type_year_quarter",
                table: "periods",
                columns: new[] { "tenant_id", "period_type", "year", "quarter" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tenants_name",
                table: "tenants",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_type_subtype_compatibility_account_subtype_id",
                table: "type_subtype_compatibility",
                column: "account_subtype_id");

            migrationBuilder.CreateIndex(
                name: "IX_type_subtype_compatibility_account_type_id",
                table: "type_subtype_compatibility",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_type_subtype_compatibility_tenant_id_account_type_id_accoun~",
                table: "type_subtype_compatibility",
                columns: new[] { "tenant_id", "account_type_id", "account_subtype_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_tenant_id_email",
                table: "users",
                columns: new[] { "tenant_id", "email" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "periods");

            migrationBuilder.DropTable(
                name: "type_subtype_compatibility");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "account_subtypes");

            migrationBuilder.DropTable(
                name: "account_types");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
