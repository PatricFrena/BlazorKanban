using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Tests.Domain;

public class ProjetoTccTests
{
    private static FaseTccDados Fase(string nome, bool concluida = false, Guid? id = null) =>
        new(id, nome, concluida, null, null);

    [Fact]
    public void Fases_mantem_a_ordem_recebida()
    {
        var tcc = ProjetoTcc.Criar("u1", "Meu TCC", "", new[] { Fase("Tema"), Fase("Escrita"), Fase("Entrega") });

        Assert.Equal(new[] { "Tema", "Escrita", "Entrega" }, tcc.Fases.Select(f => f.Nome));
    }

    [Fact]
    public void Reordenar_e_adicionar_fases_respeita_a_nova_ordem()
    {
        var tcc = ProjetoTcc.Criar("u1", "Meu TCC", "", new[] { Fase("Tema"), Fase("Escrita") });
        var tema = tcc.Fases[0];
        var escrita = tcc.Fases[1];

        tcc.Atualizar("Meu TCC", "", new[] { Fase("Escrita", id: escrita.Id), Fase("Nova"), Fase("Tema", id: tema.Id) });

        Assert.Equal(new[] { "Escrita", "Nova", "Tema" }, tcc.Fases.Select(f => f.Nome));
    }

    [Fact]
    public void Fase_atual_e_a_primeira_nao_concluida_e_o_progresso_acompanha()
    {
        var tcc = ProjetoTcc.Criar("u1", "TCC", "", new[] { Fase("A", true), Fase("B"), Fase("C") });

        Assert.Equal(1, tcc.FaseAtual);
        Assert.Equal(100.0 / 3, tcc.Progresso, 3);
    }

    [Fact]
    public void Com_tudo_concluido_a_fase_atual_e_a_ultima()
    {
        var tcc = ProjetoTcc.Criar("u1", "TCC", "", new[] { Fase("A", true), Fase("B", true) });

        Assert.Equal(1, tcc.FaseAtual);
        Assert.Equal(100, tcc.Progresso);
    }

    [Fact]
    public void Titulo_vazio_e_rejeitado()
    {
        Assert.Throws<DomainException>(() => ProjetoTcc.Criar("u1", " ", "", Array.Empty<FaseTccDados>()));
    }
}

public class HoraComplementarTests
{
    [Fact]
    public void Horas_devem_ser_positivas()
    {
        Assert.Throws<DomainException>(() => HoraComplementar.Criar("u1", "Curso", null,
            TipoHoraComplementar.Curso, 0, new DateOnly(2026, 1, 1), null, false));
    }

    [Fact]
    public void Link_do_certificado_precisa_ser_http()
    {
        Assert.Throws<DomainException>(() => HoraComplementar.Criar("u1", "Curso", null,
            TipoHoraComplementar.Curso, 10, new DateOnly(2026, 1, 1), "javascript:alert(1)", false));
    }

    [Fact]
    public void Certificado_em_branco_vira_nulo()
    {
        var h = HoraComplementar.Criar("u1", "Curso", null, TipoHoraComplementar.Curso, 10,
            new DateOnly(2026, 1, 1), "  ", false);

        Assert.Null(h.CertificadoUrl);
    }
}

public class PerfilAcademicoTests
{
    [Fact]
    public void Email_invalido_e_rejeitado_mas_vazio_e_aceito()
    {
        var p = PerfilAcademico.CriarPadrao("u1");

        Assert.Throws<DomainException>(() => p.Atualizar("Ana", "isso-nao-e-email", "TI", 3, 200, CorHex.Padrao));
        p.Atualizar("Ana", "", "TI", 3, 200, CorHex.Padrao);
        Assert.Null(p.Email);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Semestre_fora_do_intervalo_e_rejeitado(int semestre)
    {
        var p = PerfilAcademico.CriarPadrao("u1");

        Assert.Throws<DomainException>(() => p.Atualizar("Ana", null, "TI", semestre, 200, CorHex.Padrao));
    }
}
