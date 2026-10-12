using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Domain.Entities;

public record CardDados(
    string Titulo,
    string? Descricao,
    KanbanColuna Coluna,
    DateOnly? DataEntrega,
    Prioridade Prioridade,
    double PesoNota,
    double? NotaObtida,
    IReadOnlyList<string> Tags,
    IReadOnlyList<SubtarefaDados> Subtarefas,
    IReadOnlyList<MembroDados> Membros,
    IReadOnlyList<AnexoDados> Anexos);

public class Card
{
    private const int MaxTags = 10;

    private readonly List<string> _tags = new();
    private readonly List<Subtarefa> _subtarefas = new();
    private readonly List<MembroGrupo> _membros = new();
    private readonly List<Anexo> _anexos = new();
    private readonly List<SessaoPomodoro> _sessoesPomodoro = new();
    private readonly List<LogAtividade> _logs = new();

    private Card() { }

    internal Card(Guid disciplinaId, CardDados dados, DateTime agoraUtc)
    {
        DisciplinaId = disciplinaId;
        CriadoEmUtc = agoraUtc;
        Aplicar(dados, agoraUtc);
        _logs.Add(new LogAtividade($"Card criado na coluna '{dados.Coluna.ToLabel()}'", agoraUtc));
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid DisciplinaId { get; private set; }
    public string Titulo { get; private set; } = "";
    public string Descricao { get; private set; } = "";
    public KanbanColuna Coluna { get; private set; } = KanbanColuna.AFazer;
    public DateOnly? DataEntrega { get; private set; }
    public Prioridade Prioridade { get; private set; } = Prioridade.Media;
    public Peso PesoNota { get; private set; } = Peso.Zero;
    public double? NotaObtida { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public IReadOnlyList<string> Tags => _tags;
    public IReadOnlyList<Subtarefa> Subtarefas => _subtarefas.OrderBy(s => s.Ordem).ToList();
    public IReadOnlyList<MembroGrupo> Membros => _membros.OrderBy(m => m.Ordem).ToList();
    public IReadOnlyList<Anexo> Anexos => _anexos.OrderBy(a => a.Ordem).ToList();
    public IReadOnlyCollection<SessaoPomodoro> SessoesPomodoro => _sessoesPomodoro;
    public IReadOnlyCollection<LogAtividade> Logs => _logs;

    public int SubtarefasConcluidas => _subtarefas.Count(s => s.Concluida);

    public double ProgressoSubtarefas =>
        _subtarefas.Count == 0 ? 0 : SubtarefasConcluidas * 100.0 / _subtarefas.Count;

    public CriticidadeCard Criticidade(DateOnly hoje)
    {
        if (!DataEntrega.HasValue || Coluna == KanbanColuna.Concluido) return CriticidadeCard.Normal;
        var dias = DataEntrega.Value.DayNumber - hoje.DayNumber;
        if (dias < 0) return CriticidadeCard.Atrasado;
        if (dias == 0) return CriticidadeCard.Hoje;
        if (dias <= 2) return CriticidadeCard.Urgente;
        if (dias <= 5) return CriticidadeCard.Atencao;
        return CriticidadeCard.Normal;
    }

    public void Atualizar(CardDados dados, DateTime agoraUtc)
    {
        var colunaAnterior = Coluna;
        Aplicar(dados, agoraUtc);
        _logs.Add(new LogAtividade("Card atualizado", agoraUtc));
        if (colunaAnterior != Coluna)
            _logs.Add(new LogAtividade($"Movido de '{colunaAnterior.ToLabel()}' para '{Coluna.ToLabel()}'", agoraUtc));
    }

    public void MoverPara(KanbanColuna novaColuna, DateTime agoraUtc)
    {
        if (!Enum.IsDefined(novaColuna))
            throw new DomainException("Coluna inválida.");
        if (novaColuna == Coluna) return;

        var anterior = Coluna;
        Coluna = novaColuna;
        _logs.Add(new LogAtividade($"Movido de '{anterior.ToLabel()}' para '{novaColuna.ToLabel()}'", agoraUtc));
    }

    public void RegistrarPomodoro(DateTime inicioUtc, DateTime fimUtc) =>
        _sessoesPomodoro.Add(new SessaoPomodoro(inicioUtc, fimUtc));

    private void Aplicar(CardDados d, DateTime agoraUtc)
    {
        if (!Enum.IsDefined(d.Coluna)) throw new DomainException("Coluna inválida.");
        if (!Enum.IsDefined(d.Prioridade)) throw new DomainException("Prioridade inválida.");
        if (d.NotaObtida.HasValue) Guarda.Intervalo(d.NotaObtida.Value, 0, 10, "A nota");

        var tags = d.Tags
            .Select(t => Guarda.Texto(t, "A tag", 40, obrigatorio: false))
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count > MaxTags) throw new DomainException($"Use no máximo {MaxTags} tags por tarefa.");

        // Valida o que é simples antes de alterar o estado, para uma falha não deixar o card pela metade.
        var titulo = Guarda.Texto(d.Titulo, "O título", 150);
        var descricao = Guarda.Texto(d.Descricao, "A descrição", 2000, obrigatorio: false);
        var peso = Peso.De(d.PesoNota);

        Titulo = titulo;
        Descricao = descricao;
        Coluna = d.Coluna;
        DataEntrega = d.DataEntrega;
        Prioridade = d.Prioridade;
        PesoNota = peso;
        NotaObtida = d.NotaObtida;

        _tags.Clear();
        _tags.AddRange(tags);

        var subtarefas = Sincronizacao.Sincronizar(_subtarefas, d.Subtarefas, x => x.Id, e => e.Id,
            x => new Subtarefa(x), (e, x) => e.Aplicar(x));
        for (var i = 0; i < subtarefas.Count; i++) subtarefas[i].Ordem = i + 1;

        var membros = Sincronizacao.Sincronizar(_membros, d.Membros, x => x.Id, e => e.Id,
            x => new MembroGrupo(x), (e, x) => e.Aplicar(x));
        for (var i = 0; i < membros.Count; i++) membros[i].Ordem = i + 1;

        var anexos = Sincronizacao.Sincronizar(_anexos, d.Anexos, x => x.Id, e => e.Id,
            x => new Anexo(x, agoraUtc), (e, x) => e.Aplicar(x));
        for (var i = 0; i < anexos.Count; i++) anexos[i].Ordem = i + 1;
    }
}
