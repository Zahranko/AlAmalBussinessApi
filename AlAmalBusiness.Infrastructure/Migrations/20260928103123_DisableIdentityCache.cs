using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AlAmalBusiness.Infrastructure.Migrations
{
    /// <summary>
    /// SQL Server pre-allocates IDENTITY values in blocks of 1000 and throws
    /// the unused rest away whenever the server restarts or fails over — on
    /// the shared SQL box that happens without warning, which is how prod's
    /// ticket numbers went 99 -> 1000. Ticket numbers are quoted by people,
    /// so the gap is a visible fault, not a cosmetic one. Turning the cache
    /// off is database-wide (Leads, Feedbacks and the rest stop jumping
    /// too); the cost is one extra log write per insert, nothing at this
    /// volume.
    ///
    /// Outside a transaction because ALTER DATABASE SCOPED CONFIGURATION
    /// refuses to run inside one, and through EXEC with TRY/CATCH so a login
    /// without the permission leaves the setting alone instead of failing
    /// the whole migration.
    /// </summary>
    public partial class DisableIdentityCache : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "BEGIN TRY EXEC('ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF'); END TRY " +
                "BEGIN CATCH PRINT 'IDENTITY_CACHE not changed: ' + ERROR_MESSAGE(); END CATCH",
                suppressTransaction: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "BEGIN TRY EXEC('ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = ON'); END TRY " +
                "BEGIN CATCH PRINT 'IDENTITY_CACHE not changed: ' + ERROR_MESSAGE(); END CATCH",
                suppressTransaction: true);
        }
    }
}
