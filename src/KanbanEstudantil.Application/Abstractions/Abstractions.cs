using KanbanEstudantil.Domain.Entities;

namespace KanbanEstudantil.Application.Abstractions;

/// <summary>Quem está usando o sistema. Todos os dados são filtrados por este usuário.</summary>
public interface IUsuarioAtual
{
    string UsuarioId { get; }
}

// Repositórios: cada método abre e fecha sua própria unidade de trabalho. As alterações em um agregado
// são feitas por um delegate que roda entre o carregamento e o salvamento. Se o delegate lançar uma
// DomainException, nada é gravado. Isso evita compartilhar um DbContext entre componentes Blazor.

public interface IDisciplinaRepository
{
    Task<IReadOnlyList<Disciplina>> ListarAsync(CancellationToken ct = default);
    Task<Disciplina?> ObterAsync(Guid id, CancellationToken ct = default);
    Task AdicionarAsync(Disciplina disciplina, CancellationToken ct = default);
    Task<bool> AtualizarAsync(Guid id, Action<Disciplina> alterar, CancellationToken ct = default);
    Task<bool> RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface IHoraComplementarRepository
{
    Task<IReadOnlyList<HoraComplementar>> ListarAsync(CancellationToken ct = default);
    Task AdicionarAsync(HoraComplementar hora, CancellationToken ct = default);
    Task<bool> AtualizarAsync(Guid id, Action<HoraComplementar> alterar, CancellationToken ct = default);
    Task<bool> RemoverAsync(Guid id, CancellationToken ct = default);
}

public interface IProjetoTccRepository
{
    Task<ProjetoTcc?> ObterAsync(CancellationToken ct = default);
    Task AdicionarAsync(ProjetoTcc tcc, CancellationToken ct = default);
    Task<bool> AtualizarAsync(Action<ProjetoTcc> alterar, CancellationToken ct = default);
}

public interface IPerfilRepository
{
    Task<PerfilAcademico> ObterOuCriarAsync(CancellationToken ct = default);
    Task AtualizarAsync(Action<PerfilAcademico> alterar, CancellationToken ct = default);
}
