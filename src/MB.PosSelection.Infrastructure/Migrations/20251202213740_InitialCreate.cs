using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MB.PosSelection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PosRatios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PosName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CardType = table.Column<string>(type: "text", nullable: false),
                    CardBrand = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
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
                    table.PrimaryKey("PK_PosRatios", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PosRatios");
        }
    }
}
