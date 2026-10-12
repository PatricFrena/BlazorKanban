using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanEstudantil.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite(connectionString));

        services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
        services.AddScoped<IHoraComplementarRepository, HoraComplementarRepository>();
        services.AddScoped<IProjetoTccRepository, ProjetoTccRepository>();
        services.AddScoped<IPerfilRepository, PerfilRepository>();
        return services;
    }

    /// <summary>Aplica as migrations pendentes. Chamado uma vez na inicialização do site.</summary>
    public static async Task InicializarBancoAsync(this IServiceProvider provider)
    {
        var fabrica = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using var ctx = await fabrica.CreateDbContextAsync();
        await ctx.Database.MigrateAsync();
        await ctx.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    }
}
