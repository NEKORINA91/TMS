using Microsoft.Extensions.Hosting;
using TMS.DAL;

namespace TMS.Helpers
{
    public class AttendanceAutoAbsentService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        public AttendanceAutoAbsentService(IServiceScopeFactory scopes) { _scopes = scopes; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<TuitionProContext>();
                    await AttendanceHelper.EnsureAbsent(db, DateTime.Now);
                }
                catch
                {
                    // ignore and try again next minute
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}