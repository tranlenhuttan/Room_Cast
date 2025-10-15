using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomCast.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MediaFiles",
                type: "TEXT",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                table: "MediaFiles",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Private");

            migrationBuilder.Sql("UPDATE MediaFiles SET UpdatedAt = CreatedAt WHERE UpdatedAt IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MediaFiles");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "MediaFiles");
        }
    }
}
