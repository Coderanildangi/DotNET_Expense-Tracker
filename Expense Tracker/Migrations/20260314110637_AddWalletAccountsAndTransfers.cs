using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expense_Tracker.Migrations
{
    public partial class AddWalletAccountsAndTransfers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WalletAccountId",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WalletAccounts",
                columns: table => new
                {
                    WalletAccountId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", nullable: false),
                    OpeningBalance = table.Column<int>(type: "int", nullable: false),
                    CreditLimit = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAccounts", x => x.WalletAccountId);
                });

            migrationBuilder.CreateTable(
                name: "AccountTransfers",
                columns: table => new
                {
                    AccountTransferId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FromWalletAccountId = table.Column<int>(type: "int", nullable: false),
                    ToWalletAccountId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(120)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTransfers", x => x.AccountTransferId);
                    table.ForeignKey(
                        name: "FK_AccountTransfers_WalletAccounts_FromWalletAccountId",
                        column: x => x.FromWalletAccountId,
                        principalTable: "WalletAccounts",
                        principalColumn: "WalletAccountId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountTransfers_WalletAccounts_ToWalletAccountId",
                        column: x => x.ToWalletAccountId,
                        principalTable: "WalletAccounts",
                        principalColumn: "WalletAccountId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletAccountId",
                table: "Transactions",
                column: "WalletAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransfers_FromWalletAccountId",
                table: "AccountTransfers",
                column: "FromWalletAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransfers_ToWalletAccountId",
                table: "AccountTransfers",
                column: "ToWalletAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountTransfers_UserId",
                table: "AccountTransfers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletAccounts_UserId",
                table: "WalletAccounts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_WalletAccounts_WalletAccountId",
                table: "Transactions",
                column: "WalletAccountId",
                principalTable: "WalletAccounts",
                principalColumn: "WalletAccountId",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_WalletAccounts_WalletAccountId",
                table: "Transactions");

            migrationBuilder.DropTable(
                name: "AccountTransfers");

            migrationBuilder.DropTable(
                name: "WalletAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_WalletAccountId",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "WalletAccountId",
                table: "Transactions");
        }
    }
}
