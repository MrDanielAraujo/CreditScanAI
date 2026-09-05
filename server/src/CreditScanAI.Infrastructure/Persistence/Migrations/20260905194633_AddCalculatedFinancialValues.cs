using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculatedFinancialValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "calculated_financial_values",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    value = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calculated_financial_values", x => x.id);
                    table.ForeignKey(
                        name: "FK_calculated_financial_values_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calculated_financial_values_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_calculated_financial_values_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_calculated_financial_values_company_period_key",
                table: "calculated_financial_values",
                columns: new[] { "company_id", "period_id", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_calculated_financial_values_period_id",
                table: "calculated_financial_values",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "IX_calculated_financial_values_tenant_id",
                table: "calculated_financial_values",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calculated_financial_values");
        }
    }
}
