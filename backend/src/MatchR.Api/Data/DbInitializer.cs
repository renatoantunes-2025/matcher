using MatchR.Api.Models;
using MatchR.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace MatchR.Api.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies pending migrations and seeds the first Admin broker so someone
    /// can log in and approve every other access request from the Admin screen.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MatchRDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await db.Database.MigrateAsync();

        if (await db.Brokers.AnyAsync(b => b.Role == BrokerRole.Admin)) return;

        var adminEmail = config["Admin:Email"] ?? "admin@matchr.com.br";
        var adminPassword = config["Admin:Password"] ?? "TrocarSenha123!";

        db.Brokers.Add(new Broker
        {
            Name = config["Admin:Name"] ?? "Administrador MatchR",
            Email = adminEmail,
            PasswordHash = authService.HashPassword(adminPassword),
            Creci = "-",
            Role = BrokerRole.Admin,
            Status = BrokerStatus.Active
        });

        await db.SaveChangesAsync();
        logger.LogWarning(
            "Admin inicial criado: {Email} / senha temporária definida em Admin:Password (troque após o primeiro login).",
            adminEmail);
    }
}
