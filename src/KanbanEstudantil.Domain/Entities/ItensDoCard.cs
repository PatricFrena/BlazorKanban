using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Domain.Entities;

public record SubtarefaDados(Guid? Id, string Titulo, bool Concluida);
public record MembroDados(Guid? Id, string Nome, string? Email, string AvatarCor);
public record AnexoDados(Guid? Id, string Nome, string Url, TipoAnexo Tipo);

public class Subtarefa
{
    private Subtarefa() { }

    internal Subtarefa(SubtarefaDados dados) => Aplicar(dados);

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Ordem { get; internal set; }
    public string Titulo { get; private set; } = "";
    public bool Concluida { get; private set; }

    internal void Aplicar(SubtarefaDados dados)
    {
        Titulo = Guarda.Texto(dados.Titulo, "O título da subtarefa", 200);
        Concluida = dados.Concluida;
    }
}

public class MembroGrupo
{
    private MembroGrupo() { }

    internal MembroGrupo(MembroDados dados) => Aplicar(dados);

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Ordem { get; internal set; }
    public string Nome { get; private set; } = "";
    public string? Email { get; private set; }
    public CorHex AvatarCor { get; private set; } = CorHex.Padrao;

    internal void Aplicar(MembroDados dados)
    {
        Nome = Guarda.Texto(dados.Nome, "O nome do membro", 100);
        Email = Guarda.EmailOpcional(dados.Email, "O e-mail do membro");
        AvatarCor = CorHex.De(dados.AvatarCor);
    }
}

public class Anexo
{
    private Anexo() { }

    internal Anexo(AnexoDados dados, DateTime agoraUtc)
    {
        AdicionadoEmUtc = agoraUtc;
        Aplicar(dados);
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public int Ordem { get; internal set; }
    public string Nome { get; private set; } = "";
    public string Url { get; private set; } = "";
    public TipoAnexo Tipo { get; private set; } = TipoAnexo.Link;
    public DateTime AdicionadoEmUtc { get; private set; }

    internal void Aplicar(AnexoDados dados)
    {
        Url = Guarda.UrlHttp(dados.Url, "O link do anexo");
        var nome = Guarda.Texto(dados.Nome, "O nome do anexo", 200, obrigatorio: false);
        Nome = nome.Length == 0 ? Url : nome;
        Tipo = dados.Tipo;
    }
}

public class SessaoPomodoro
{
    private SessaoPomodoro() { }

    internal SessaoPomodoro(DateTime inicioUtc, DateTime fimUtc)
    {
        if (fimUtc < inicioUtc)
            throw new DomainException("O fim da sessão não pode ser anterior ao início.");
        InicioUtc = inicioUtc;
        FimUtc = fimUtc;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTime InicioUtc { get; private set; }
    public DateTime FimUtc { get; private set; }
}

public class LogAtividade
{
    private LogAtividade() { }

    internal LogAtividade(string descricao, DateTime agoraUtc, string usuario = "Você")
    {
        Descricao = descricao;
        DataHoraUtc = agoraUtc;
        Usuario = usuario;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Descricao { get; private set; } = "";
    public DateTime DataHoraUtc { get; private set; }
    public string Usuario { get; private set; } = "Você";
}
