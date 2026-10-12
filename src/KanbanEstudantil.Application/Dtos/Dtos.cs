using System.Text.Json;
using KanbanEstudantil.Domain.Enums;

namespace KanbanEstudantil.Application.Dtos;

// DTOs são classes mutáveis de propósito: os formulários do Blazor fazem @bind direto nelas.
// As regras continuam no Domain, que valida tudo quando o DTO volta para o serviço.

public class AvaliacaoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    /// <summary>Porcentagem de 0 a 100.</summary>
    public double Peso { get; set; }
    public double NotaMaxima { get; set; } = 10;
    public double? Nota { get; set; }
    public DateOnly? DataEntrega { get; set; }
}

public class DisciplinaDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Professor { get; set; } = "";
    public string Cor { get; set; } = "#6366f1";
    public int CargaHoraria { get; set; }
    public int Faltas { get; set; }
    public double LimiteDeFaltasPercentual { get; set; }
    public double LimiteDeFaltasEmAulas { get; set; }
    public double PercentualFaltas { get; set; }
    public bool EmRiscoDeFalta { get; set; }
    public bool ReprovadoPorFalta { get; set; }
    /// <summary>Média de 0 a 10; nula enquanto não há nota lançada.</summary>
    public double? Media { get; set; }
    public List<AvaliacaoDto> Avaliacoes { get; set; } = new();
    public List<CardDto> Cards { get; set; } = new();
}

public class NovaDisciplinaDto
{
    public string Nome { get; set; } = "";
    public string Professor { get; set; } = "";
    public string Cor { get; set; } = "#6366f1";
    public int CargaHoraria { get; set; } = 60;
}

public class SubtarefaDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public bool Concluida { get; set; }
}

public class MembroDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string AvatarCor { get; set; } = "#6366f1";
}

public class AnexoDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public string Url { get; set; } = "";
    public TipoAnexo Tipo { get; set; } = TipoAnexo.Link;
}

public class LogDto
{
    public string Usuario { get; set; } = "";
    public string Descricao { get; set; } = "";
    public DateTime DataHoraUtc { get; set; }
}

public class CardDto
{
    public Guid Id { get; set; }
    public Guid DisciplinaId { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public KanbanColuna Coluna { get; set; } = KanbanColuna.AFazer;
    public DateOnly? DataEntrega { get; set; }
    public Prioridade Prioridade { get; set; } = Prioridade.Media;
    public List<string> Tags { get; set; } = new();
    public double PesoNota { get; set; }
    public double? NotaObtida { get; set; }
    public List<SubtarefaDto> Subtarefas { get; set; } = new();
    public List<MembroDto> Membros { get; set; } = new();
    public List<AnexoDto> Anexos { get; set; } = new();
    public int PomodorosConcluidos { get; set; }
    public List<LogDto> Logs { get; set; } = new();
    public CriticidadeCard Criticidade { get; set; }

    public int SubtarefasConcluidas => Subtarefas.Count(s => s.Concluida);
    public double ProgressoSubtarefas => Subtarefas.Count == 0 ? 0 : SubtarefasConcluidas * 100.0 / Subtarefas.Count;

    /// <summary>Cópia profunda, usada pelos modais para editar sem alterar o original até salvar.</summary>
    public CardDto Clonar() =>
        JsonSerializer.Deserialize<CardDto>(JsonSerializer.Serialize(this)) ?? new CardDto();
}

public class HoraComplementarDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Descricao { get; set; } = "";
    public TipoHoraComplementar Tipo { get; set; } = TipoHoraComplementar.Curso;
    public double Horas { get; set; }
    public DateOnly Data { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? CertificadoUrl { get; set; }
    public bool Validado { get; set; }
}

public record ResumoHorasDto(double Total, double Exigidas, double Progresso, int Validadas, double Restantes);

public class FaseTccDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = "";
    public bool Concluida { get; set; }
    public DateOnly? Prazo { get; set; }
    public string Notas { get; set; } = "";
}

public class ProjetoTccDto
{
    public Guid Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Orientador { get; set; } = "";
    public List<FaseTccDto> Fases { get; set; } = new();
    public int FaseAtual { get; set; }
    public double Progresso { get; set; }
}

public class PerfilDto
{
    public string Nome { get; set; } = "";
    public string Email { get; set; } = "";
    public string Curso { get; set; } = "";
    public int Semestre { get; set; } = 1;
    public double HorasComplementaresExigidas { get; set; }
    public string AvatarCor { get; set; } = "#6366f1";
}
