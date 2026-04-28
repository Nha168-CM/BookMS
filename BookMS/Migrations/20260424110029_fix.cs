using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookMS.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 24, 18, 0, 26, 521, DateTimeKind.Local).AddTicks(1784), "Fiction Books", "Fiction" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 24, 18, 0, 26, 521, DateTimeKind.Local).AddTicks(1804), "Non-Fiction Books", "Non-Fiction" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 24, 18, 0, 26, 521, DateTimeKind.Local).AddTicks(1806), "Tech & Programming", "Technology" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 24, 18, 0, 26, 521, DateTimeKind.Local).AddTicks(1808), "Science Books", "Science" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 24, 18, 0, 26, 521, DateTimeKind.Local).AddTicks(1809), "History Books", "History" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 8, 11, 58, 26, 846, DateTimeKind.Local).AddTicks(3447), "", "Action" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 8, 11, 58, 26, 846, DateTimeKind.Local).AddTicks(3469), "", "Fantasy" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 8, 11, 58, 26, 846, DateTimeKind.Local).AddTicks(3470), "", "Drama" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 8, 11, 58, 26, 846, DateTimeKind.Local).AddTicks(3472), "", "Adventure" });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "Description", "Name" },
                values: new object[] { new DateTime(2026, 4, 8, 11, 58, 26, 846, DateTimeKind.Local).AddTicks(3474), "", "Animation" });
        }
    }
}
