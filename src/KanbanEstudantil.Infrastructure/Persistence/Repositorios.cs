using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KanbanEstudantil.Infrastructure.Persistence;

// Todos os repositórios filtram pelo usuário atual: um usuário nunca enxerga dados de outro.

public class DisciplinaRepository : IDisciplinaRepository
{
    private readonly IDbContextFactory<AppDbContext> _fabrica;
    private readonly IUsuarioAtual _usuario;

    public DisciplinaRepository(IDbContextFactory<AppDbContext> fabrica, IUsuarioAtual usuario)
    {
        _fabrica = fabrica;
        _usuario = usuario;
    }

    private IQueryable<Disciplina> Consulta(AppDbContext ctx)
    {
        var usuarioId = _usuario.UsuarioId;
        return ctx.Disciplinas
            .Where(d => d.UsuarioId == usuarioId)
            .Include(d => d.Avaliacoes)
            .Include(d => d.Cards).ThenInclude(c => c.Subtarefas)
            .Include(d => d.Cards).ThenInclude(c => c.Membros)
            .Include(d => d.Cards).ThenInclude(c => c.Anexos)
            .Include(d => d.Cards).ThenInclude(c => c.SessoesPomodoro)
            .Include(d => d.Cards).ThenInclude(c => c.Logs)
            .AsSplitQuery();
    }

    public async Task<IReadOnlyList<Disciplina>> ListarAsync(CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        return await Consulta(ctx).AsNoTracking().OrderBy(d => d.Nome).ToListAsync(ct);
    }

    public async Task<Disciplina?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        return await Consulta(ctx).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task AdicionarAsync(Disciplina disciplina, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        ctx.Disciplinas.Add(disciplina);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<bool> AtualizarAsync(Guid id, Action<Disciplina> alterar, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var disciplina = await Consulta(ctx).FirstOrDefaultAsync(d => d.Id == id, ct);
        if (disciplina is null) return false;

        alterar(disciplina);
        await ctx.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoverAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        var disciplina = await ctx.Disciplinas.FirstOrDefaultAsync(d => d.Id == id && d.UsuarioId == usuarioId, ct);
        if (disciplina is null) return false;

        ctx.Disciplinas.Remove(disciplina);
        await ctx.SaveChangesAsync(ct);
        return true;
    }
}

public class HoraComplementarRepository : IHoraComplementarRepository
{
    private readonly IDbContextFactory<AppDbContext> _fabrica;
    private readonly IUsuarioAtual _usuario;

    public HoraComplementarRepository(IDbContextFactory<AppDbContext> fabrica, IUsuarioAtual usuario)
    {
        _fabrica = fabrica;
        _usuario = usuario;
    }

    public async Task<IReadOnlyList<HoraComplementar>> ListarAsync(CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        return await ctx.HorasComplementares.AsNoTracking()
            .Where(h => h.UsuarioId == usuarioId)
            .OrderByDescending(h => h.Data)
            .ToListAsync(ct);
    }

    public async Task AdicionarAsync(HoraComplementar hora, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        ctx.HorasComplementares.Add(hora);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<bool> AtualizarAsync(Guid id, Action<HoraComplementar> alterar, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        var hora = await ctx.HorasComplementares.FirstOrDefaultAsync(h => h.Id == id && h.UsuarioId == usuarioId, ct);
        if (hora is null) return false;

        alterar(hora);
        await ctx.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoverAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        var hora = await ctx.HorasComplementares.FirstOrDefaultAsync(h => h.Id == id && h.UsuarioId == usuarioId, ct);
        if (hora is null) return false;

        ctx.HorasComplementares.Remove(hora);
        await ctx.SaveChangesAsync(ct);
        return true;
    }
}

public class ProjetoTccRepository : IProjetoTccRepository
{
    private readonly IDbContextFactory<AppDbContext> _fabrica;
    private readonly IUsuarioAtual _usuario;

    public ProjetoTccRepository(IDbContextFactory<AppDbContext> fabrica, IUsuarioAtual usuario)
    {
        _fabrica = fabrica;
        _usuario = usuario;
    }

    private IQueryable<ProjetoTcc> Consulta(AppDbContext ctx)
    {
        var usuarioId = _usuario.UsuarioId;
        return ctx.Tccs.Where(t => t.UsuarioId == usuarioId).Include(t => t.Fases);
    }

    public async Task<ProjetoTcc?> ObterAsync(CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        return await Consulta(ctx).AsNoTracking().FirstOrDefaultAsync(ct);
    }

    public async Task AdicionarAsync(ProjetoTcc tcc, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        ctx.Tccs.Add(tcc);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<bool> AtualizarAsync(Action<ProjetoTcc> alterar, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var tcc = await Consulta(ctx).FirstOrDefaultAsync(ct);
        if (tcc is null) return false;

        alterar(tcc);
        await ctx.SaveChangesAsync(ct);
        return true;
    }
}

public class PerfilRepository : IPerfilRepository
{
    private readonly IDbContextFactory<AppDbContext> _fabrica;
    private readonly IUsuarioAtual _usuario;

    public PerfilRepository(IDbContextFactory<AppDbContext> fabrica, IUsuarioAtual usuario)
    {
        _fabrica = fabrica;
        _usuario = usuario;
    }

    public async Task<PerfilAcademico> ObterOuCriarAsync(CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        var perfil = await ctx.Perfis.AsNoTracking().FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);
        if (perfil is not null) return perfil;

        perfil = PerfilAcademico.CriarPadrao(usuarioId);
        ctx.Perfis.Add(perfil);
        await ctx.SaveChangesAsync(ct);
        return perfil;
    }

    public async Task AtualizarAsync(Action<PerfilAcademico> alterar, CancellationToken ct = default)
    {
        await using var ctx = await _fabrica.CreateDbContextAsync(ct);
        var usuarioId = _usuario.UsuarioId;
        var perfil = await ctx.Perfis.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId, ct);
        if (perfil is null)
        {
            perfil = PerfilAcademico.CriarPadrao(usuarioId);
            ctx.Perfis.Add(perfil);
        }

        alterar(perfil);
        await ctx.SaveChangesAsync(ct);
    }
}
