using KanbanEstudantil.Application.Abstractions;

namespace KanbanEstudantil.Web.Servicos;

/// <summary>Usuário único usado até o login existir.</summary>
public class UsuarioLocal : IUsuarioAtual
{
    public const string Id = "usuario-local";

    public string UsuarioId => Id;
}
