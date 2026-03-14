using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Expense_Tracker.Migrations
{
    public partial class BackfillLegacyCategoryIcons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        }
    }
}
