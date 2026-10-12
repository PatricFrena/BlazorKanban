using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Application.Common;
using KanbanEstudantil.Application.Dtos;
using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Application.Services;

public class DisciplinaService
{
    private readonly IDisciplinaRepository _repo;
    private readonly IUsuarioAtual _usuario;
    private readonly TimeProvider _relogio;
    private readonly Alteracoes _alteracoes;

    public DisciplinaService(IDisciplinaRepository repo, IUsuarioAtual usuario, TimeProvider relogio, Alteracoes alteracoes)
    {
        _repo = repo;
        _usuario = usuario;
        _relogio = relogio;
        _alteracoes = alteracoes;
    }

    private DateOnly Hoje => DateOnly.FromDateTime(_relogio.GetLocalNow().DateTime);

    public async Task<IReadOnlyList<DisciplinaDto>> ListarAsync(CancellationToken ct = default)
    {
        var disciplinas = await _repo.ListarAsync(ct);
        var hoje = Hoje;
        return disciplinas.Select(d => d.ParaDto(hoje)).ToList();
    }

    public async Task<DisciplinaDto?> ObterAsync(Guid id, CancellationToken ct = default)
    {
        var disciplina = await _repo.ObterAsync(id, ct);
        return disciplina?.ParaDto(Hoje);
    }

    public async Task<Guid> CriarAsync(NovaDisciplinaDto dados, CancellationToken ct = default)
    {
        var disciplina = Disciplina.Criar(_usuario.UsuarioId, dados.Nome, dados.Professor,
            CorHex.De(dados.Cor), dados.CargaHoraria);
        await _repo.AdicionarAsync(disciplina, ct);
        _alteracoes.Notificar();
        return disciplina.Id;
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.RemoverAsync(id, ct)) throw new NaoEncontradoException("A disciplina");
        _alteracoes.Notificar();
    }

    public Task RegistrarFaltaAsync(Guid id, int quantidade, CancellationToken ct = default) =>
        Alterar(id, d => d.RegistrarFalta(quantidade), ct);

    public Task SalvarAvaliacoesAsync(Guid id, IEnumerable<AvaliacaoDto> avaliacoes, CancellationToken ct = default)
    {
        var dados = avaliacoes.Select(a => a.ParaDados()).ToList();
        return Alterar(id, d => d.SincronizarAvaliacoes(dados), ct);
    }

    private async Task Alterar(Guid id, Action<Disciplina> alteracao, CancellationToken ct)
    {
        if (!await _repo.AtualizarAsync(id, alteracao, ct)) throw new NaoEncontradoException("A disciplina");
        _alteracoes.Notificar();
    }
}

public class CardService
{
    private readonly IDisciplinaRepository _repo;
    private readonly TimeProvider _relogio;
    private readonly Alteracoes _alteracoes;

    public CardService(IDisciplinaRepository repo, TimeProvider relogio, Alteracoes alteracoes)
    {
        _repo = repo;
        _relogio = relogio;
        _alteracoes = alteracoes;
    }

    private DateTime AgoraUtc => _relogio.GetUtcNow().UtcDateTime;

    public async Task<Guid> CriarAsync(Guid disciplinaId, CardDto card, CancellationToken ct = default)
    {
        var criado = Guid.Empty;
        var agora = AgoraUtc;
        await Alterar(disciplinaId, d => criado = d.AdicionarCard(card.ParaDados(), agora).Id, ct);
        return criado;
    }

    public Task AtualizarAsync(Guid disciplinaId, CardDto card, CancellationToken ct = default)
    {
        var agora = AgoraUtc;
        return Alterar(disciplinaId, d => d.AtualizarCard(card.Id, card.ParaDados(), agora), ct);
    }

    public Task MoverAsync(Guid disciplinaId, Guid cardId, KanbanColuna coluna, CancellationToken ct = default)
    {
        var agora = AgoraUtc;
        return Alterar(disciplinaId, d => d.MoverCard(cardId, coluna, agora), ct);
    }

    public Task RemoverAsync(Guid disciplinaId, Guid cardId, CancellationToken ct = default) =>
        Alterar(disciplinaId, d => d.RemoverCard(cardId), ct);

    public Task RegistrarPomodoroAsync(Guid disciplinaId, Guid cardId, DateTime inicioUtc, DateTime fimUtc,
        CancellationToken ct = default) =>
        Alterar(disciplinaId, d => d.ObterCard(cardId).RegistrarPomodoro(inicioUtc, fimUtc), ct);

    private async Task Alterar(Guid disciplinaId, Action<Disciplina> alteracao, CancellationToken ct)
    {
        if (!await _repo.AtualizarAsync(disciplinaId, alteracao, ct)) throw new NaoEncontradoException("A disciplina");
        _alteracoes.Notificar();
    }
}
