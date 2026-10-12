using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace KanbanEstudantil.Infrastructure.Persistence;

/// <summary>
/// Dados de demonstração para desenvolvimento (os mesmos do protótipo original).
/// Só devem ser usados quando o banco do usuário está vazio e a configuração permite.
/// </summary>
public static class DadosDeExemplo
{
    public static async Task AplicarAsync(IDbContextFactory<AppDbContext> fabrica, string usuarioId, DateOnly hoje)
    {
        await using var ctx = await fabrica.CreateDbContextAsync();
        if (await ctx.Disciplinas.AnyAsync(d => d.UsuarioId == usuarioId)) return;

        var agora = DateTime.UtcNow;
        DateOnly Dias(int n) => hoje.AddDays(n);

        // ─── Cálculo I ───────────────────────────────────────────────
        var calculo = Disciplina.Criar(usuarioId, "Cálculo I", "Prof. Rodrigo Mendes", CorHex.De("#6366f1"), 80);
        calculo.RegistrarFalta(4);
        calculo.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "P1", 40, 10, 7.5, Dias(-30)),
            new AvaliacaoDados(null, "P2", 40, 10, null, Dias(15)),
            new AvaliacaoDados(null, "Trabalho", 20, 10, 9.0, Dias(-10))
        });
        calculo.AdicionarCard(Card("Estudar Derivadas", "Revisão completa para P2", KanbanColuna.EmAndamento, Dias(10),
            Prioridade.Alta, 0, null, new[] { "#Prova" },
            new[] { ("Regra da cadeia", true), ("Derivadas implícitas", false), ("Exercícios lista 3", false) }), agora);
        calculo.AdicionarCard(Card("Lista de Exercícios 4", "", KanbanColuna.AFazer, Dias(2),
            Prioridade.Media, 0, null, new[] { "#LeituraObrigatória" }), agora);
        calculo.AdicionarCard(Card("Prova 1 - Limites", "", KanbanColuna.Concluido, null,
            Prioridade.Alta, 40, 7.5, new[] { "#Prova" }), agora);

        // ─── Programação II ──────────────────────────────────────────
        var prog = Disciplina.Criar(usuarioId, "Programação II", "Profa. Ana Lima", CorHex.De("#10b981"), 60);
        prog.RegistrarFalta(2);
        prog.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "Projeto Final", 60, 10, null, Dias(20)),
            new AvaliacaoDados(null, "Prova Teórica", 40, 10, 8.5, Dias(-5))
        });
        prog.AdicionarCard(Card("Projeto Kanban Blazor", "TCC prático da disciplina", KanbanColuna.EmAndamento, Dias(20),
            Prioridade.Alta, 60, null, new[] { "#TrabalhoEmGrupo", "#AtividadePrática" },
            new[] { ("Modelagem de dados", true), ("Interface Kanban", true), ("Módulo de notas", false), ("Testes finais", false) },
            new[] { new MembroDados(null, "Carlos", "carlos@uni.br", "#f59e0b"), new MembroDados(null, "Juliana", "ju@uni.br", "#ec4899") }), agora);
        prog.AdicionarCard(Card("Prova Teórica - POO", "", KanbanColuna.Concluido, null,
            Prioridade.Alta, 40, 8.5, new[] { "#Prova" }), agora);

        // ─── Engenharia de Software ──────────────────────────────────
        var eng = Disciplina.Criar(usuarioId, "Engenharia de Software", "Prof. Marcos Farias", CorHex.De("#f59e0b"), 60);
        eng.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "Seminário", 30, 10, null, Dias(7)),
            new AvaliacaoDados(null, "Prova", 70, 10, null, Dias(25))
        });
        eng.AdicionarCard(Card("Seminário: Metodologias Ágeis", "", KanbanColuna.AFazer, Dias(7),
            Prioridade.Alta, 30, null, new[] { "#TrabalhoEmGrupo" }), agora);
        eng.AdicionarCard(Card("Leitura: Livro Scrum", "", KanbanColuna.EmAndamento, Dias(5),
            Prioridade.Media, 0, null, new[] { "#LeituraObrigatória" }), agora);

        ctx.Disciplinas.AddRange(calculo, prog, eng);

        // ─── TCC, horas e perfil ─────────────────────────────────────
        var tcc = ProjetoTcc.Criar(usuarioId, "Kanban Inteligente para Gestão Acadêmica", "Prof. Marcelo Costa", new[]
        {
            new FaseTccDados(null, "Escolha do Tema", true, null, null),
            new FaseTccDados(null, "Reuniões de Orientação", false, hoje.AddDays(30), null),
            new FaseTccDados(null, "Escrita do TCC", false, hoje.AddMonths(4), null),
            new FaseTccDados(null, "Revisão e Correções", false, hoje.AddMonths(5), null),
            new FaseTccDados(null, "Entrega Final", false, hoje.AddMonths(6), null)
        });
        if (!await ctx.Tccs.AnyAsync(t => t.UsuarioId == usuarioId)) ctx.Tccs.Add(tcc);

        if (!await ctx.HorasComplementares.AnyAsync(h => h.UsuarioId == usuarioId))
        {
            ctx.HorasComplementares.AddRange(
                HoraComplementar.Criar(usuarioId, "Curso React.js", null, TipoHoraComplementar.Curso, 40, hoje.AddMonths(-3), null, true),
                HoraComplementar.Criar(usuarioId, "Semana Acadêmica 2024", null, TipoHoraComplementar.SemanaAcademica, 20, hoje.AddMonths(-2), null, true),
                HoraComplementar.Criar(usuarioId, "Palestra: IA na Saúde", null, TipoHoraComplementar.Palestra, 4, hoje.AddDays(-15), null, false),
                HoraComplementar.Criar(usuarioId, "Projeto de Extensão", null, TipoHoraComplementar.Extensao, 60, hoje.AddMonths(-1), null, true));
        }

        var perfil = await ctx.Perfis.FirstOrDefaultAsync(p => p.UsuarioId == usuarioId);
        if (perfil is null)
        {
            perfil = PerfilAcademico.CriarPadrao(usuarioId);
            ctx.Perfis.Add(perfil);
        }
        perfil.Atualizar("Lucas Oliveira", "lucas@universidade.br", "Ciência da Computação", 5, 200, CorHex.Padrao);

        await ctx.SaveChangesAsync();
    }

    private static CardDados Card(string titulo, string descricao, KanbanColuna coluna, DateOnly? entrega,
        Prioridade prioridade, double pesoNota, double? nota, string[] tags,
        (string Titulo, bool Concluida)[]? subtarefas = null, MembroDados[]? membros = null) =>
        new(titulo, descricao, coluna, entrega, prioridade, pesoNota, nota, tags,
            (subtarefas ?? Array.Empty<(string, bool)>()).Select(s => new SubtarefaDados(null, s.Titulo, s.Concluida)).ToList(),
            membros ?? Array.Empty<MembroDados>(),
            Array.Empty<AnexoDados>());
}
