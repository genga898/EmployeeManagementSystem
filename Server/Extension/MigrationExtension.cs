using Microsoft.EntityFrameworkCore;
using ServerLibrary.Data;

namespace Server.Extension;

public static class MigrationExtension
{
    public static void ApplyMigrations(this IApplicationBuilder applicationBuilder)
    {
        using IServiceScope scope = applicationBuilder.ApplicationServices.CreateScope();
        
        using AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
}