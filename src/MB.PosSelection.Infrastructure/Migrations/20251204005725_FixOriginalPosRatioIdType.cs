using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MB.PosSelection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOriginalPosRatioIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Önce tabloyu temizle (Çünkü veri tipi değişiyor, eski veri uyumsuz)
            migrationBuilder.Sql("TRUNCATE TABLE \"PosRatioHistories\";");

            // 2. Hatalı olan Integer kolonu sil
            migrationBuilder.DropColumn(
                name: "OriginalPosRatioId",
                table: "PosRatioHistories");

            // 3. UUID tipinde, doğru kolonu yeniden ekle
            migrationBuilder.AddColumn<Guid>(
                name: "OriginalPosRatioId",
                table: "PosRatioHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OriginalPosRatioId",
                table: "PosRatioHistories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
