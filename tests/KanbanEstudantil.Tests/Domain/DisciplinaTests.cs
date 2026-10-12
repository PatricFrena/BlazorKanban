using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Tests.Domain;

public class DisciplinaTests
{
    private static Disciplina Nova(int carga = 80) =>
        Disciplina.Criar("u1", "Cálculo I", "Prof. Rodrigo", CorHex.De("#6366f1"), carga);

    [Fact]
    public void Media_e_ponderada_pelos_pesos_das_avaliacoes_corrigidas()
    {
        var d = Nova();
        d.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "P1", 40, 10, 7.5, null),
            new AvaliacaoDados(null, "P2", 40, 10, null, null),
            new AvaliacaoDados(null, "Trabalho", 20, 10, 9.0, null)
        });

        // (7,5 * 40 + 9 * 20) / 60 = 8
        Assert.Equal(8.0, d.Media!.Value, 3);
    }

    [Fact]
    public void Media_e_normalizada_para_a_escala_de_dez()
    {
        var d = Nova();
        d.SincronizarAvaliacoes(new[] { new AvaliacaoDados(null, "Quiz", 100, 20, 15, null) });

        Assert.Equal(7.5, d.Media!.Value, 3);
    }

    [Fact]
    public void Media_e_nula_sem_notas_lancadas()
    {
        var d = Nova();
        d.SincronizarAvaliacoes(new[] { new AvaliacaoDados(null, "P1", 50, 10, null, null) });

        Assert.Null(d.Media);
    }

    [Fact]
    public void Soma_dos_pesos_nao_pode_passar_de_cem()
    {
        var d = Nova();
        var ex = Assert.Throws<DomainException>(() => d.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "P1", 70, 10, null, null),
            new AvaliacaoDados(null, "P2", 40, 10, null, null)
        }));
        Assert.Contains("100%", ex.Message);
    }

    [Fact]
    public void Nota_acima_do_maximo_e_rejeitada()
    {
        var d = Nova();
        Assert.Throws<DomainException>(() =>
            d.SincronizarAvaliacoes(new[] { new AvaliacaoDados(null, "P1", 50, 10, 11, null) }));
    }

    [Fact]
    public void Sincronizar_avaliacoes_mantem_o_id_de_quem_ja_existe_e_remove_o_que_sumiu()
    {
        var d = Nova();
        d.SincronizarAvaliacoes(new[]
        {
            new AvaliacaoDados(null, "P1", 50, 10, null, null),
            new AvaliacaoDados(null, "P2", 50, 10, null, null)
        });
        var p1 = d.Avaliacoes.First(a => a.Nome == "P1");

        d.SincronizarAvaliacoes(new[] { new AvaliacaoDados(p1.Id, "Prova 1", 100, 10, 6, null) });

        var unica = Assert.Single(d.Avaliacoes);
        Assert.Equal(p1.Id, unica.Id);
        Assert.Equal("Prova 1", unica.Nome);
    }

    [Theory]
    [InlineData(15, false, false)] // 18,75%
    [InlineData(16, true, false)]  // 20%: começa o alerta
    [InlineData(19, true, false)]  // 23,75%
    [InlineData(20, true, true)]   // 25%: reprovado
    public void Faltas_ativam_alerta_e_reprovacao_conforme_o_limite(int faltas, bool risco, bool reprovado)
    {
        var d = Nova(80);
        d.RegistrarFalta(faltas);

        Assert.Equal(risco, d.EmRiscoDeFalta);
        Assert.Equal(reprovado, d.ReprovadoPorFalta);
    }

    [Fact]
    public void Limite_de_faltas_pode_ser_configurado_por_instituicao()
    {
        var d = Disciplina.Criar("u1", "Física", null, CorHex.Padrao, 100, LimiteDeFaltas.De(30));
        d.RegistrarFalta(25);

        Assert.False(d.ReprovadoPorFalta);
        Assert.True(d.EmRiscoDeFalta); // alerta em 80% de 30% = 24%
        Assert.Equal(30, d.LimiteDeFaltasEmAulas);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(101)]
    public void Registrar_quantidade_invalida_de_faltas_e_rejeitado(int quantidade)
    {
        Assert.Throws<DomainException>(() => Nova().RegistrarFalta(quantidade));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_e_obrigatorio(string nome)
    {
        Assert.Throws<DomainException>(() => Disciplina.Criar("u1", nome, null, CorHex.Padrao, 60));
    }

    [Fact]
    public void Cor_invalida_e_rejeitada_para_evitar_injecao_em_estilos()
    {
        Assert.Throws<DomainException>(() => CorHex.De("red; background:url(x)"));
        Assert.Throws<DomainException>(() => CorHex.De("#12345"));
        Assert.Equal("#aabbcc", CorHex.De("#AABBCC").Valor);
    }
}
