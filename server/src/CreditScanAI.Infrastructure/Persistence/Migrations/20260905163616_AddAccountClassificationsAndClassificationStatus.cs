using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountClassificationsAndClassificationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "classification_completed_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_error",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "classification_started_at",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "classification_status",
                table: "documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "NotStarted");

            migrationBuilder.CreateTable(
                name: "account_classifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    standard_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    chart_of_accounts_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(4,3)", nullable: false),
                    classification_method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    evidence = table.Column<string>(type: "text", nullable: true),
                    review_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Pending"),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_classifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_account_classifications_chart_of_accounts_chart_of_accounts~",
                        column: x => x.chart_of_accounts_id,
                        principalTable: "chart_of_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_classifications_source_accounts_source_account_id",
                        column: x => x.source_account_id,
                        principalTable: "source_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_classifications_standard_accounts_standard_account_~",
                        column: x => x.standard_account_id,
                        principalTable: "standard_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_account_classifications_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_documents_classification_status",
                table: "documents",
                column: "classification_status");

            migrationBuilder.CreateIndex(
                name: "idx_account_classifications_review_status",
                table: "account_classifications",
                column: "review_status");

            migrationBuilder.CreateIndex(
                name: "IX_account_classifications_chart_of_accounts_id",
                table: "account_classifications",
                column: "chart_of_accounts_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_classifications_source_account_id",
                table: "account_classifications",
                column: "source_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_account_classifications_standard_account_id",
                table: "account_classifications",
                column: "standard_account_id");

            migrationBuilder.CreateIndex(
                name: "IX_account_classifications_tenant_id",
                table: "account_classifications",
                column: "tenant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_classifications");

            migrationBuilder.DropIndex(
                name: "idx_documents_classification_status",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "classification_completed_at",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "classification_error",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "classification_started_at",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "classification_status",
                table: "documents");
        }
    }
}
