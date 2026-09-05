using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CreditScanAI.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountClassificationReviewNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "review_notes",
                table: "account_classifications",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "review_notes",
                table: "account_classifications");
        }
    }
}
