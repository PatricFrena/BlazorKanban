using BlazorKanban;
using BlazorKanban.Data;
using BlazorKanban.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components; // Necessário para NavigationManager
using System.Net.Http; // Adicionado para HttpClient

var builder = WebApplication.CreateBuilder(args);

// --- Serviços de Autenticação e Dados ---

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();


// --- Serviços Blazor e API ---

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// NOVO E ESSENCIAL: Adiciona os serviços necessários para a API Controller (KanbanListController)
builder.Services.AddControllers();


// --- Seus Serviços Customizados e HTTP ---

builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// ??? CORREÇÃO ESSENCIAL: Configura o HttpClient com o BaseAddress da sua API (porta 7068).
// Isso resolve o erro "Invalid request URI was provided" quebra a URI absoluta.
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7068/")
});


// --- Pipeline de Requisições (HTTP) ---

var app = builder.Build();

// Configuração do ambiente
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();

// NOVO E ESSENCIAL: Mapeia as rotas para os Controllers de API
app.MapControllers();

app.UseAuthentication();
app.UseAuthorization();

// Mapeia os componentes e o modo de renderização interativo
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();