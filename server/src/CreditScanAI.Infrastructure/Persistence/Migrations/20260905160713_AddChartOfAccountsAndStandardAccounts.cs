using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChartOfAccountsAndStandardAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "chart_of_accounts_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "chart_of_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chart_of_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_chart_of_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "standard_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chart_of_accounts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_subtype_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_standard_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_standard_accounts_account_subtypes_account_subtype_id",
                        column: x => x.account_subtype_id,
                        principalTable: "account_subtypes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_standard_accounts_account_types_account_type_id",
                        column: x => x.account_type_id,
                        principalTable: "account_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_standard_accounts_chart_of_accounts_chart_of_accounts_id",
                        column: x => x.chart_of_accounts_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_standard_accounts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_chart_of_accounts_id",
                table: "documents",
                column: "chart_of_accounts_id");

            migrationBuilder.CreateIndex(
                name: "idx_chart_of_accounts_one_default_per_tenant",
                table: "chart_of_accounts",
                column: "tenant_id",
                unique: true,
                filter: "is_default = true");

            migrationBuilder.CreateIndex(
                name: "IX_chart_of_accounts_tenant_id_name",
                table: "chart_of_accounts",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_standard_accounts_account_subtype_id",
                table: "standard_accounts",
                column: "account_subtype_id");

            migrationBuilder.CreateIndex(
                name: "IX_standard_accounts_account_type_id",
                table: "standard_accounts",
                column: "account_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_standard_accounts_chart_of_accounts_id_code",
                table: "standard_accounts",
                columns: new[] { "chart_of_accounts_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_standard_accounts_tenant_id",
                table: "standard_accounts",
                column: "tenant_id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_chart_of_accounts_chart_of_accounts_id",
                table: "documents",
                column: "chart_of_accounts_id",
                principalTable: "chart_of_accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_chart_of_accounts_chart_of_accounts_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "standard_accounts");

            migrationBuilder.DropTable(
                name: "chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "IX_documents_chart_of_accounts_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "chart_of_accounts_id",
                table: "documents");
        }
    }
}
