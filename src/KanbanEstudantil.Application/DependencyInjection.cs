using KanbanEstudantil.Application.Common;
using KanbanEstudantil.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace KanbanEstudantil.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Alteracoes e os serviços são por sessão: cada usuário conectado tem o seu.
        services.AddScoped<Alteracoes>();
        services.AddScoped<DisciplinaService>();
        services.AddScoped<CardService>();
        services.AddScoped<HoraComplementarService>();
        services.AddScoped<TccService>();
        services.AddScoped<PerfilService>();
        return services;
    }
}
