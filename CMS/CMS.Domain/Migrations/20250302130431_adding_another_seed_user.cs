using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMS.Domain.Migrations
{
    /// <inheritdoc />
    public partial class adding_another_seed_user : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1eecb40c-c701-4445-89d4-d1aa7d70460d",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "226cca69-f046-4d15-8b81-9b9ba34f2214",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3F476A40-97F4-42C6-A226-602AED74A4BC",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "91c3461a-7da3-4033-b907-b104b903d793",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b024cbbe-f64e-4d1b-9c6e-05ac0f0e3ebb",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { "e82b73d1-c5a1-44cf-b68f-e29e2f3cb803", null, "Viewer", "VIEWER" });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c6585ab9-8b5f-4332-a174-92429db8add2",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "8ef355c0-fce8-4b04-bcf9-48a2d3f17fb1", "AQAAAAIAAYagAAAAECbWV3PdYNzqVNxwpYuNqQtXPtSXBb05vxC0UEfxkvKpAKL4cbnpQ4KvnkCX7dIbkw==", "7f1c6cd7-1187-4539-921c-e3879e86a2f8" });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "Email", "EmailConfirmed", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "d852b320-72b4-4e94-94c3-0c643f960f64", 0, "d4503ff5-205a-45ad-b042-3b173fba4e08", "viewer@viewer.com", true, false, null, "VIEWER@VIEWER.COM", "VIEWER", "AQAAAAIAAYagAAAAEAW5zM4BMm8+5Zi7+HxbBF1u2BqTO+yG95G2xITsT3VePBrvjVOrWxfCQbDQ94scMg==", null, false, "6f46fa6d-1220-461b-9c3a-13b07be950a8", false, "viewer" });

            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { "e82b73d1-c5a1-44cf-b68f-e29e2f3cb803", "d852b320-72b4-4e94-94c3-0c643f960f64" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { "e82b73d1-c5a1-44cf-b68f-e29e2f3cb803", "d852b320-72b4-4e94-94c3-0c643f960f64" });

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e82b73d1-c5a1-44cf-b68f-e29e2f3cb803");

            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "d852b320-72b4-4e94-94c3-0c643f960f64");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1eecb40c-c701-4445-89d4-d1aa7d70460d",
                column: "ConcurrencyStamp",
                value: "2fd4cd70-41ce-4b06-a116-fe0032a80681");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "226cca69-f046-4d15-8b81-9b9ba34f2214",
                column: "ConcurrencyStamp",
                value: "73c4cd28-e3f3-4794-b58f-04b2bc8cab62");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3F476A40-97F4-42C6-A226-602AED74A4BC",
                column: "ConcurrencyStamp",
                value: "eb679db5-e7b0-4bac-a78f-9fabe360e202");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "91c3461a-7da3-4033-b907-b104b903d793",
                column: "ConcurrencyStamp",
                value: "3d8d3d27-3c9f-41c6-8d23-33d8c769e179");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "b024cbbe-f64e-4d1b-9c6e-05ac0f0e3ebb",
                column: "ConcurrencyStamp",
                value: "0de1676b-e75c-4753-b79b-b2f27ef64fae");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "c6585ab9-8b5f-4332-a174-92429db8add2",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "e1ac4f9c-0850-40b2-bec1-71c6f48a68e1", "AQAAAAEAACcQAAAAEMsi6Cc6AN+wTU2+946Tprp5ys1hrlGPeUZVcsRM/cFqeA5d8Bz3AZk7l0i/3dFXGQ==", "0f79735b-baed-4b4d-99a7-1c62ab0abb66" });
        }
    }
}
