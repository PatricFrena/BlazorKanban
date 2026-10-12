namespace KanbanEstudantil.Application.Common;

/// <summary>
/// Avisa os componentes abertos (menu lateral, painel etc.) de que os dados mudaram.
/// Um por sessão do usuário.
/// </summary>
public class Alteracoes
{
    public event Action? Alterado;

    public void Notificar() => Alterado?.Invoke();
}
