using AlAmalBusiness.DbContext.Infrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AlAmalBusiness.Api.Health
{
    // Readiness: can this instance reach SQL Server? The VPS deploy waits for
    // /health/ready before switching nginx to a new container, so a release
    // with a broken connection string never takes traffic, and the server
    // GUI's bear for this app turns red if the database goes away.
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        private readonly AppDbContext _db;

        public DatabaseHealthCheck(AppDbContext db) => _db = db;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _db.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Unhealthy("Cannot connect to the database.");
            }
            catch (Exception ex)
            {
                // The message only, never the exception: this endpoint is anonymous.
                return HealthCheckResult.Unhealthy("Database check failed: " + ex.GetType().Name);
            }
        }
    }
}
