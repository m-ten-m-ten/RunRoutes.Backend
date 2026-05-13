using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunRoutes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeCommentUpdatedAtNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            // ★ データ移行:既存の「未編集相当」レコード(updated_at = created_at + 1秒以内)を NULL に
            migrationBuilder.Sql(@"
                UPDATE comments
                SET updated_at = NULL
                WHERE updated_at <= created_at + INTERVAL '1 second';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ロールバック時: NULL を created_at で埋めてから NOT NULL に戻す
            migrationBuilder.Sql(@"
                UPDATE comments
                SET updated_at = created_at
                WHERE updated_at IS NULL;
            ");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "comments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
