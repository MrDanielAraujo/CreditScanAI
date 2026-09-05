using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceAccountsAndValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "source_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    hierarchy_level = table.Column<int>(type: "integer", nullable: false),
                    parent_source_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    inferred_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    inferred_subtype = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_source_accounts_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_source_accounts_source_accounts_parent_source_account_id",
                        column: x => x.parent_source_account_id,
                        principalTable: "source_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_source_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    raw_column_label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    raw_value = table.Column<decimal>(type: "numeric(19,4)", nullable: true),
                    scale_factor = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    extraction_confidence = table.Column<decimal>(type: "numeric(3,2)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_values_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_values_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_values_source_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "source_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_account_values_period_source",
                table: "account_values",
                columns: new[] { "period_id", "source_account_id" });

            migrationBuilder.CreateIndex(
                name: "IX_account_values_document_id",
                table: "account_values",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_values_source_account_id",
                table: "account_values",
                column: "source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_values_tenant_id",
                table: "account_values",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_source_accounts_document",
                table: "source_accounts",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_accounts_parent_source_account_id",
                table: "source_accounts",
                column: "parent_source_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_source_accounts_tenant_id",
                table: "source_accounts",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_values");

            migrationBuilder.DropTable(
                name: "source_accounts");
        }
    }
}
