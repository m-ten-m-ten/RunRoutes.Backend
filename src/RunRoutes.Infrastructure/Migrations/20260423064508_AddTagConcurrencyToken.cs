using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunRoutes.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTagConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // xmin は PostgreSQL が全テーブルに持つシステム列のため DDL 発行は不要。
            // モデル側 (Tag.Version + TagConfiguration) でこの列を concurrency token として参照するだけに留める。
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // システム列なので破棄不可。no-op。
        }
    }
}
