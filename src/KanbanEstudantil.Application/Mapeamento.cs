using KanbanEstudantil.Application.Dtos;
using KanbanEstudantil.Domain.Entities;

namespace KanbanEstudantil.Application;

/// <summary>Conversões entre entidades do Domain e DTOs.</summary>
internal static class Mapeamento
{
    public static DisciplinaDto ParaDto(this Disciplina d, DateOnly hoje) => new()
    {
        Id = d.Id,
        Nome = d.Nome,
        Professor = d.Professor,
        Cor = d.Cor.Valor,
        CargaHoraria = d.CargaHoraria,
        Faltas = d.Faltas,
        LimiteDeFaltasPercentual = d.LimiteDeFaltas.Percentual,
        LimiteDeFaltasEmAulas = d.LimiteDeFaltasEmAulas,
        PercentualFaltas = d.PercentualFaltas,
        EmRiscoDeFalta = d.EmRiscoDeFalta,
        ReprovadoPorFalta = d.ReprovadoPorFalta,
        Media = d.Media,
        Avaliacoes = d.Avaliacoes.Select(a => new AvaliacaoDto
        {
            Id = a.Id,
            Nome = a.Nome,
            Peso = a.Peso.Valor,
            NotaMaxima = a.NotaMaxima,
            Nota = a.Nota,
            DataEntrega = a.DataEntrega
        }).ToList(),
        Cards = d.Cards.Select(c => c.ParaDto(hoje)).ToList()
    };

    public static CardDto ParaDto(this Card c, DateOnly hoje) => new()
    {
        Id = c.Id,
        DisciplinaId = c.DisciplinaId,
        Titulo = c.Titulo,
        Descricao = c.Descricao,
        Coluna = c.Coluna,
        DataEntrega = c.DataEntrega,
        Prioridade = c.Prioridade,
        Tags = c.Tags.ToList(),
        PesoNota = c.PesoNota.Valor,
        NotaObtida = c.NotaObtida,
        Subtarefas = c.Subtarefas.Select(s => new SubtarefaDto { Id = s.Id, Titulo = s.Titulo, Concluida = s.Concluida }).ToList(),
        Membros = c.Membros.Select(m => new MembroDto { Id = m.Id, Nome = m.Nome, Email = m.Email ?? "", AvatarCor = m.AvatarCor.Valor }).ToList(),
        Anexos = c.Anexos.Select(a => new AnexoDto { Id = a.Id, Nome = a.Nome, Url = a.Url, Tipo = a.Tipo }).ToList(),
        PomodorosConcluidos = c.SessoesPomodoro.Count,
        Logs = c.Logs.OrderByDescending(l => l.DataHoraUtc)
            .Select(l => new LogDto { Usuario = l.Usuario, Descricao = l.Descricao, DataHoraUtc = l.DataHoraUtc }).ToList(),
        Criticidade = c.Criticidade(hoje)
    };

    public static CardDados ParaDados(this CardDto c) => new(
        c.Titulo,
        c.Descricao,
        c.Coluna,
        c.DataEntrega,
        c.Prioridade,
        c.PesoNota,
        c.NotaObtida,
        c.Tags,
        c.Subtarefas.Select(s => new SubtarefaDados(IdOuNulo(s.Id), s.Titulo, s.Concluida)).ToList(),
        c.Membros.Select(m => new MembroDados(IdOuNulo(m.Id), m.Nome, m.Email, m.AvatarCor)).ToList(),
        c.Anexos.Select(a => new AnexoDados(IdOuNulo(a.Id), a.Nome, a.Url, a.Tipo)).ToList());

    public static AvaliacaoDados ParaDados(this AvaliacaoDto a) =>
        new(IdOuNulo(a.Id), a.Nome, a.Peso, a.NotaMaxima, a.Nota, a.DataEntrega);

    public static HoraComplementarDto ParaDto(this HoraComplementar h) => new()
    {
        Id = h.Id,
        Titulo = h.Titulo,
        Descricao = h.Descricao,
        Tipo = h.Tipo,
        Horas = h.Horas,
        Data = h.Data,
        CertificadoUrl = h.CertificadoUrl,
        Validado = h.Validado
    };

    public static ProjetoTccDto ParaDto(this ProjetoTcc t) => new()
    {
        Id = t.Id,
        Titulo = t.Titulo,
        Orientador = t.Orientador,
        FaseAtual = t.FaseAtual,
        Progresso = t.Progresso,
        Fases = t.Fases.Select(f => new FaseTccDto
        {
            Id = f.Id,
            Nome = f.Nome,
            Concluida = f.Concluida,
            Prazo = f.Prazo,
            Notas = f.Notas
        }).ToList()
    };

    public static FaseTccDados ParaDados(this FaseTccDto f) => new(IdOuNulo(f.Id), f.Nome, f.Concluida, f.Prazo, f.Notas);

    public static PerfilDto ParaDto(this PerfilAcademico p) => new()
    {
        Nome = p.Nome,
        Email = p.Email ?? "",
        Curso = p.Curso,
        Semestre = p.Semestre,
        HorasComplementaresExigidas = p.HorasComplementaresExigidas,
        AvatarCor = p.AvatarCor.Valor
    };

    private static Guid? IdOuNulo(Guid id) => id == Guid.Empty ? null : id;
}
