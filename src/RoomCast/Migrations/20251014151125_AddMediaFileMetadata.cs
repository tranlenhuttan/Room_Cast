using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomCast.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "MediaFiles",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "MediaFiles",
                newName: "OriginalFileName");

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "MediaFiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "MediaFiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StoredFileName",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPath",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("UPDATE MediaFiles SET StoredFileName = OriginalFileName WHERE StoredFileName = '' OR StoredFileName IS NULL;");
            migrationBuilder.Sql("UPDATE MediaFiles SET Title = OriginalFileName WHERE Title = '' OR Title IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentType",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "ThumbnailPath",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "MediaFiles");

            migrationBuilder.RenameColumn(
                name: "OriginalFileName",
                table: "MediaFiles",
                newName: "FileName");

            migrationBuilder.DropColumn(
                name: "StoredFileName",
                table: "MediaFiles");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "MediaFiles",
                newName: "Timestamp");
        }
    }
}
