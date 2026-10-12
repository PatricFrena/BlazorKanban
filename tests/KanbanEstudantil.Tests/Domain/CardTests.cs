using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Tests.Domain;

public class CardTests
{
    private static readonly DateTime Agora = new(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Hoje = new(2026, 10, 12);

    private static CardDados Dados(
        string titulo = "Estudar derivadas",
        KanbanColuna coluna = KanbanColuna.AFazer,
        DateOnly? entrega = null,
        string[]? tags = null,
        SubtarefaDados[]? subtarefas = null,
        AnexoDados[]? anexos = null,
        double peso = 0,
        double? nota = null) =>
        new(titulo, "", coluna, entrega, Prioridade.Media, peso, nota,
            tags ?? Array.Empty<string>(),
            subtarefas ?? Array.Empty<SubtarefaDados>(),
            Array.Empty<MembroDados>(),
            anexos ?? Array.Empty<AnexoDados>());

    private static (Disciplina, Card) NovoCard(CardDados? dados = null)
    {
        var d = Disciplina.Criar("u1", "Cálculo", null, CorHex.Padrao, 60);
        var card = d.AdicionarCard(dados ?? Dados(), Agora);
        return (d, card);
    }

    [Theory]
    [InlineData(-1, CriticidadeCard.Atrasado)]
    [InlineData(0, CriticidadeCard.Hoje)]
    [InlineData(1, CriticidadeCard.Urgente)]
    [InlineData(2, CriticidadeCard.Urgente)]
    [InlineData(3, CriticidadeCard.Atencao)]
    [InlineData(5, CriticidadeCard.Atencao)]
    [InlineData(6, CriticidadeCard.Normal)]
    public void Criticidade_depende_dos_dias_ate_a_entrega(int dias, CriticidadeCard esperado)
    {
        var (_, card) = NovoCard(Dados(entrega: Hoje.AddDays(dias)));

        Assert.Equal(esperado, card.Criticidade(Hoje));
    }

    [Fact]
    public void Card_concluido_nunca_aparece_como_atrasado()
    {
        var (_, card) = NovoCard(Dados(coluna: KanbanColuna.Concluido, entrega: Hoje.AddDays(-10)));

        Assert.Equal(CriticidadeCard.Normal, card.Criticidade(Hoje));
    }

    [Fact]
    public void Criar_card_registra_log()
    {
        var (_, card) = NovoCard();

        var log = Assert.Single(card.Logs);
        Assert.Contains("A Fazer", log.Descricao);
    }

    [Fact]
    public void Mover_card_muda_a_coluna_e_registra_log()
    {
        var (d, card) = NovoCard();

        d.MoverCard(card.Id, KanbanColuna.Concluido, Agora);

        Assert.Equal(KanbanColuna.Concluido, card.Coluna);
        Assert.Contains(card.Logs, l => l.Descricao.Contains("Concluído"));
    }

    [Fact]
    public void Mover_para_a_mesma_coluna_nao_gera_log()
    {
        var (d, card) = NovoCard();

        d.MoverCard(card.Id, KanbanColuna.AFazer, Agora);

        Assert.Single(card.Logs);
    }

    [Fact]
    public void Coluna_invalida_e_rejeitada()
    {
        var (d, card) = NovoCard();

        Assert.Throws<DomainException>(() => d.MoverCard(card.Id, (KanbanColuna)99, Agora));
    }

    [Fact]
    public void Card_inexistente_lanca_nao_encontrado()
    {
        var (d, _) = NovoCard();

        Assert.Throws<NaoEncontradoException>(() => d.MoverCard(Guid.NewGuid(), KanbanColuna.Concluido, Agora));
    }

    [Fact]
    public void Atualizar_sincroniza_subtarefas_preservando_ids()
    {
        var (_, card) = NovoCard(Dados(subtarefas: new[]
        {
            new SubtarefaDados(null, "A", false),
            new SubtarefaDados(null, "B", false)
        }));
        var a = card.Subtarefas.First(s => s.Titulo == "A");

        card.Atualizar(Dados(subtarefas: new[]
        {
            new SubtarefaDados(a.Id, "A", true),   // existente, agora concluída
            new SubtarefaDados(null, "C", false)   // nova; "B" foi removida
        }), Agora);

        Assert.Equal(2, card.Subtarefas.Count);
        Assert.Same(a, card.Subtarefas.First(s => s.Titulo == "A"));
        Assert.True(a.Concluida);
        Assert.DoesNotContain(card.Subtarefas, s => s.Titulo == "B");
        Assert.Equal(50, card.ProgressoSubtarefas);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://servidor/arquivo")]
    [InlineData("nao-e-uma-url")]
    public void Anexo_com_url_que_nao_e_http_e_rejeitado(string url)
    {
        var d = Disciplina.Criar("u1", "Cálculo", null, CorHex.Padrao, 60);

        Assert.Throws<DomainException>(() =>
            d.AdicionarCard(Dados(anexos: new[] { new AnexoDados(null, "x", url, TipoAnexo.Link) }), Agora));
    }

    [Fact]
    public void Anexo_https_e_aceito_e_usa_a_url_como_nome_quando_nao_ha_nome()
    {
        var (_, card) = NovoCard(Dados(anexos: new[] { new AnexoDados(null, "", "https://exemplo.com/a.pdf", TipoAnexo.PDF) }));

        Assert.Equal("https://exemplo.com/a.pdf", Assert.Single(card.Anexos).Nome);
    }

    [Fact]
    public void Tags_repetidas_ou_vazias_sao_descartadas()
    {
        var (_, card) = NovoCard(Dados(tags: new[] { "#Prova", "#prova", "  ", "#Seminário" }));

        Assert.Equal(new[] { "#Prova", "#Seminário" }, card.Tags);
    }

    [Fact]
    public void Nota_e_peso_fora_dos_limites_sao_rejeitados()
    {
        var d = Disciplina.Criar("u1", "Cálculo", null, CorHex.Padrao, 60);

        Assert.Throws<DomainException>(() => d.AdicionarCard(Dados(nota: 10.5), Agora));
        Assert.Throws<DomainException>(() => d.AdicionarCard(Dados(peso: 120), Agora));
        Assert.Throws<DomainException>(() => d.AdicionarCard(Dados(titulo: " "), Agora));
    }

    [Fact]
    public void Remover_card_tira_o_card_da_disciplina()
    {
        var (d, card) = NovoCard();

        d.RemoverCard(card.Id);

        Assert.Empty(d.Cards);
    }

    [Fact]
    public void Pomodoro_com_fim_antes_do_inicio_e_rejeitado()
    {
        var (_, card) = NovoCard();

        Assert.Throws<DomainException>(() => card.RegistrarPomodoro(Agora, Agora.AddMinutes(-1)));
        card.RegistrarPomodoro(Agora, Agora.AddMinutes(25));
        Assert.Single(card.SessoesPomodoro);
    }
}
