namespace KanbanEstudantil.Domain.Common;

/// <summary>Violação de uma regra de negócio. A mensagem é segura para mostrar ao usuário.</summary>
public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem) { }
}

public class NaoEncontradoException : DomainException
{
    public NaoEncontradoException(string entidade) : base($"{entidade} não encontrado(a).") { }
}
