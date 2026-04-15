using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuotationAPI.V2.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAccessRequestWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessRequestNotes",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessRequestedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessReviewNotes",
                table: "Users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AccessReviewedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessReviewedBy",
                table: "Users",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccessStatus",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "RequestedRoleName",
                table: "Users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessRequestNotes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessRequestedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessReviewNotes",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessReviewedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessReviewedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "AccessStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RequestedRoleName",
                table: "Users");
        }
    }
}
