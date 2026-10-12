using KanbanEstudantil.Application;
using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Infrastructure;
using KanbanEstudantil.Infrastructure.Persistence;
using KanbanEstudantil.Web.Servicos;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// O banco fica em App_Data/ por padrão. Para usar outro, defina ConnectionStrings:Padrao.
var conexao = builder.Configuration.GetConnectionString("Padrao");
if (string.IsNullOrWhiteSpace(conexao))
{
    var pasta = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
    Directory.CreateDirectory(pasta);
    conexao = $"Data Source={Path.Combine(pasta, "kanban.db")}";
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(conexao);

// Provisório: enquanto não há login, todo mundo é o mesmo usuário local.
// A etapa de autenticação troca esta linha por um IUsuarioAtual que lê o usuário logado.
builder.Services.AddSingleton<IUsuarioAtual, UsuarioLocal>();

var app = builder.Build();

await app.Services.InicializarBancoAsync();

if (app.Configuration.GetValue<bool>("Seed:DadosDeExemplo"))
{
    var fabrica = app.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await DadosDeExemplo.AplicarAsync(fabrica, UsuarioLocal.Id, DateOnly.FromDateTime(DateTime.Today));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
