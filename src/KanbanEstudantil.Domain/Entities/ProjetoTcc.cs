using KanbanEstudantil.Domain.Common;

namespace KanbanEstudantil.Domain.Entities;

public record FaseTccDados(Guid? Id, string Nome, bool Concluida, DateOnly? Prazo, string? Notas);

public class FaseTcc
{
    private FaseTcc() { }

    internal FaseTcc(FaseTccDados dados, int ordem)
    {
        Ordem = ordem;
        Aplicar(dados);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Ordem { get; internal set; }
    public string Nome { get; private set; } = "";
    public bool Concluida { get; private set; }
    public DateOnly? Prazo { get; private set; }
    public string Notas { get; private set; } = "";

    internal void Aplicar(FaseTccDados d)
    {
        Nome = Guarda.Texto(d.Nome, "O nome da fase", 100);
        Notas = Guarda.Texto(d.Notas, "As anotações", 2000, obrigatorio: false);
        Concluida = d.Concluida;
        Prazo = d.Prazo;
    }
}

public class ProjetoTcc
{
    private readonly List<FaseTcc> _fases = new();

    private ProjetoTcc() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UsuarioId { get; private set; } = "";
    public string Titulo { get; private set; } = "";
    public string Orientador { get; private set; } = "";

    public IReadOnlyList<FaseTcc> Fases => _fases.OrderBy(f => f.Ordem).ToList();

    /// <summary>Posição da primeira fase não concluída (ou a última, se tudo foi concluído).</summary>
    public int FaseAtual
    {
        get
        {
            var fases = Fases;
            var indice = fases.ToList().FindIndex(f => !f.Concluida);
            return indice >= 0 ? indice : Math.Max(0, fases.Count - 1);
        }
    }

    public double Progresso => _fases.Count == 0 ? 0 : _fases.Count(f => f.Concluida) * 100.0 / _fases.Count;

    public static ProjetoTcc Criar(string usuarioId, string titulo, string? orientador, IEnumerable<FaseTccDados> fases)
    {
        var tcc = new ProjetoTcc { UsuarioId = Guarda.Texto(usuarioId, "O usuário", 450) };
        tcc.Atualizar(titulo, orientador, fases);
        return tcc;
    }

    public void Atualizar(string titulo, string? orientador, IEnumerable<FaseTccDados> fases)
    {
        var lista = fases.ToList();
        if (lista.Count > 30) throw new DomainException("Use no máximo 30 fases.");

        Titulo = Guarda.Texto(titulo, "O título do TCC", 200);
        Orientador = Guarda.Texto(orientador, "O nome do orientador", 100, obrigatorio: false);

        var ordenadas = Sincronizacao.Sincronizar(_fases, lista, x => x.Id, e => e.Id,
            x => new FaseTcc(x, 0), (e, x) => e.Aplicar(x));

        // A ordem recebida é a ordem das fases.
        for (var i = 0; i < ordenadas.Count; i++) ordenadas[i].Ordem = i + 1;
    }
}
