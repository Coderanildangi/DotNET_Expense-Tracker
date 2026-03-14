using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expense_Tracker.Migrations
{
    public partial class ExpandCategoryIconLength : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "nvarchar(100)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(5)");

            migrationBuilder.Sql(@"
UPDATE [Categories]
SET [Icon] = CASE UPPER(LTRIM(RTRIM([Icon])))
    WHEN 'S'  THEN 'fa-solid fa-wallet'
    WHEN 'R'  THEN 'fa-solid fa-hand-holding-dollar'
    WHEN 'ST' THEN 'fa-solid fa-chart-line'
    WHEN 'RE' THEN 'fa-solid fa-house'
    WHEN 'TR' THEN 'fa-solid fa-plane'
    WHEN 'FO' THEN 'fa-solid fa-utensils'
    WHEN 'SH' THEN 'fa-solid fa-basket-shopping'
    WHEN 'UT' THEN 'fa-solid fa-bolt'
    ELSE [Icon]
END;
");

            migrationBuilder.Sql(@"
UPDATE [Categories]
SET [Icon] = 'fa-solid fa-tag'
WHERE [Icon] IS NULL
   OR LTRIM(RTRIM([Icon])) = ''
   OR LTRIM(RTRIM([Icon])) NOT LIKE 'fa-%';
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Icon",
                table: "Categories",
                type: "nvarchar(5)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)");
        }
    }
}
