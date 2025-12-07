using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MB.PosSelection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosRatioHistoryAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                table: "PosRatios",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PosRatioHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalPosRatioId = table.Column<int>(type: "integer", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BatchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PosName = table.Column<string>(type: "text", nullable: false),
                    CardType = table.Column<string>(type: "text", nullable: false),
                    CardBrand = table.Column<string>(type: "text", nullable: false),
                    Installment = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    MinFee = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PosRatioHistories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PosRatios_BatchId",
                table: "PosRatios",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosRatioHistories_BatchId",
                table: "PosRatioHistories",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PosRatioHistories_OriginalPosRatioId",
                table: "PosRatioHistories",
                column: "OriginalPosRatioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosRatioHistories");

            migrationBuilder.DropIndex(
                name: "IX_PosRatios_BatchId",
                table: "PosRatios");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "PosRatios");
        }
    }
}
