using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Domain.Entities;

public record AvaliacaoDados(Guid? Id, string Nome, double Peso, double NotaMaxima, double? Nota, DateOnly? DataEntrega);

public class Avaliacao
{
    private Avaliacao() { }

    internal Avaliacao(AvaliacaoDados dados) => Aplicar(dados);

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Ordem { get; internal set; }
    public string Nome { get; private set; } = "";
    public Peso Peso { get; private set; } = Peso.Zero;
    public double NotaMaxima { get; private set; } = 10;
    public double? Nota { get; private set; }
    public DateOnly? DataEntrega { get; private set; }

    /// <summary>Nota convertida para a escala de 0 a 10.</summary>
    public double? NotaNaEscalaDez => Nota.HasValue ? Nota.Value / NotaMaxima * 10 : null;

    internal void Aplicar(AvaliacaoDados d)
    {
        Guarda.Intervalo(d.NotaMaxima, 0.1, 100, "A nota máxima");
        if (d.Nota.HasValue) Guarda.Intervalo(d.Nota.Value, 0, d.NotaMaxima, "A nota");

        Nome = Guarda.Texto(d.Nome, "O nome da avaliação", 80);
        Peso = Peso.De(d.Peso);
        NotaMaxima = d.NotaMaxima;
        Nota = d.Nota;
        DataEntrega = d.DataEntrega;
    }
}

/// <summary>
/// Raiz do agregado: a disciplina controla suas avaliações, suas faltas e seus cards.
/// Qualquer alteração passa pelos métodos abaixo, que protegem as regras.
/// </summary>
public class Disciplina
{
    private readonly List<Avaliacao> _avaliacoes = new();
    private readonly List<Card> _cards = new();

    private Disciplina() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UsuarioId { get; private set; } = "";
    public string Nome { get; private set; } = "";
    public string Professor { get; private set; } = "";
    public CorHex Cor { get; private set; } = CorHex.Padrao;
    public int CargaHoraria { get; private set; } = 60;
    public int Faltas { get; private set; }
    public LimiteDeFaltas LimiteDeFaltas { get; private set; } = LimiteDeFaltas.Padrao;

    public IReadOnlyList<Avaliacao> Avaliacoes => _avaliacoes.OrderBy(a => a.Ordem).ToList();
    public IReadOnlyCollection<Card> Cards => _cards;

    public static Disciplina Criar(string usuarioId, string nome, string? professor, CorHex cor,
        int cargaHoraria, LimiteDeFaltas? limite = null)
    {
        var d = new Disciplina { UsuarioId = Guarda.Texto(usuarioId, "O usuário", 450) };
        d.AtualizarDados(nome, professor, cor, cargaHoraria, limite ?? LimiteDeFaltas.Padrao);
        return d;
    }

    public void AtualizarDados(string nome, string? professor, CorHex cor, int cargaHoraria, LimiteDeFaltas limite)
    {
        if (cargaHoraria < 1 || cargaHoraria > 500)
            throw new DomainException("A carga horária deve estar entre 1 e 500 horas.");

        Nome = Guarda.Texto(nome, "O nome da disciplina", 100);
        Professor = Guarda.Texto(professor, "O nome do professor", 100, obrigatorio: false);
        Cor = cor;
        CargaHoraria = cargaHoraria;
        LimiteDeFaltas = limite;
    }

    // ─── Faltas ───────────────────────────────────────────────────────────
    public double PercentualFaltas => CargaHoraria > 0 ? Faltas * 100.0 / CargaHoraria : 0;
    public double LimiteDeFaltasEmAulas => CargaHoraria * LimiteDeFaltas.Percentual / 100;
    public bool EmRiscoDeFalta => PercentualFaltas >= LimiteDeFaltas.PercentualDeAlerta;
    public bool ReprovadoPorFalta => PercentualFaltas >= LimiteDeFaltas.Percentual;

    public void RegistrarFalta(int quantidade = 1)
    {
        if (quantidade < 1 || quantidade > 100)
            throw new DomainException("Informe entre 1 e 100 faltas.");
        Faltas += quantidade;
    }

    // ─── Notas ────────────────────────────────────────────────────────────
    /// <summary>Média ponderada (escala de 0 a 10) das avaliações já corrigidas; nula se não há nenhuma.</summary>
    public double? Media
    {
        get
        {
            var corrigidas = _avaliacoes.Where(a => a.Nota.HasValue && a.Peso.Valor > 0).ToList();
            var pesoTotal = corrigidas.Sum(a => a.Peso.Valor);
            if (pesoTotal == 0) return null;
            return corrigidas.Sum(a => a.NotaNaEscalaDez!.Value * a.Peso.Valor) / pesoTotal;
        }
    }

    public void SincronizarAvaliacoes(IEnumerable<AvaliacaoDados> dados)
    {
        var lista = dados.ToList();
        if (lista.Sum(a => a.Peso) > 100.0001)
            throw new DomainException("A soma dos pesos das avaliações não pode passar de 100%.");

        var ordenadas = Sincronizacao.Sincronizar(_avaliacoes, lista, x => x.Id, e => e.Id,
            x => new Avaliacao(x), (e, x) => e.Aplicar(x));
        for (var i = 0; i < ordenadas.Count; i++) ordenadas[i].Ordem = i + 1;
    }

    // ─── Cards ────────────────────────────────────────────────────────────
    public Card ObterCard(Guid cardId) =>
        _cards.FirstOrDefault(c => c.Id == cardId) ?? throw new NaoEncontradoException("A tarefa");

    public Card AdicionarCard(CardDados dados, DateTime agoraUtc)
    {
        var card = new Card(Id, dados, agoraUtc);
        _cards.Add(card);
        return card;
    }

    public void AtualizarCard(Guid cardId, CardDados dados, DateTime agoraUtc) =>
        ObterCard(cardId).Atualizar(dados, agoraUtc);

    public void MoverCard(Guid cardId, KanbanColuna coluna, DateTime agoraUtc) =>
        ObterCard(cardId).MoverPara(coluna, agoraUtc);

    public void RemoverCard(Guid cardId) => _cards.Remove(ObterCard(cardId));
}
