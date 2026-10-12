using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Application.Common;
using KanbanEstudantil.Application.Dtos;
using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Application.Services;

public class HoraComplementarService
{
    private readonly IHoraComplementarRepository _repo;
    private readonly IPerfilRepository _perfis;
    private readonly IUsuarioAtual _usuario;
    private readonly Alteracoes _alteracoes;

    public HoraComplementarService(IHoraComplementarRepository repo, IPerfilRepository perfis,
        IUsuarioAtual usuario, Alteracoes alteracoes)
    {
        _repo = repo;
        _perfis = perfis;
        _usuario = usuario;
        _alteracoes = alteracoes;
    }

    public async Task<IReadOnlyList<HoraComplementarDto>> ListarAsync(CancellationToken ct = default) =>
        (await _repo.ListarAsync(ct)).Select(h => h.ParaDto()).ToList();

    public async Task<ResumoHorasDto> ObterResumoAsync(CancellationToken ct = default)
    {
        var horas = await _repo.ListarAsync(ct);
        var perfil = await _perfis.ObterOuCriarAsync(ct);

        var total = horas.Sum(h => h.Horas);
        var exigidas = perfil.HorasComplementaresExigidas;
        var progresso = exigidas > 0 ? Math.Min(100, total / exigidas * 100) : 100;
        return new ResumoHorasDto(total, exigidas, progresso, horas.Count(h => h.Validado), Math.Max(0, exigidas - total));
    }

    public async Task AdicionarAsync(HoraComplementarDto dados, CancellationToken ct = default)
    {
        var hora = HoraComplementar.Criar(_usuario.UsuarioId, dados.Titulo, dados.Descricao, dados.Tipo,
            dados.Horas, dados.Data, dados.CertificadoUrl, dados.Validado);
        await _repo.AdicionarAsync(hora, ct);
        _alteracoes.Notificar();
    }

    public async Task RemoverAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _repo.RemoverAsync(id, ct)) throw new NaoEncontradoException("A hora complementar");
        _alteracoes.Notificar();
    }
}

public class TccService
{
    private readonly IProjetoTccRepository _repo;
    private readonly IUsuarioAtual _usuario;
    private readonly Alteracoes _alteracoes;

    public TccService(IProjetoTccRepository repo, IUsuarioAtual usuario, Alteracoes alteracoes)
    {
        _repo = repo;
        _usuario = usuario;
        _alteracoes = alteracoes;
    }

    private static readonly string[] FasesPadrao =
    {
        "Escolha do Tema", "Reuniões de Orientação", "Pesquisa Bibliográfica",
        "Escrita do TCC", "Revisão e Correções", "Entrega Final"
    };

    public async Task<ProjetoTccDto?> ObterAsync(CancellationToken ct = default) =>
        (await _repo.ObterAsync(ct))?.ParaDto();

    /// <summary>Cria o TCC com as fases padrão. Se já existe um, devolve o existente.</summary>
    public async Task<ProjetoTccDto> CriarComFasesPadraoAsync(CancellationToken ct = default)
    {
        var existente = await _repo.ObterAsync(ct);
        if (existente is not null) return existente.ParaDto();

        var tcc = ProjetoTcc.Criar(_usuario.UsuarioId, "Meu TCC", "",
            FasesPadrao.Select(n => new FaseTccDados(null, n, false, null, null)));
        await _repo.AdicionarAsync(tcc, ct);
        _alteracoes.Notificar();
        return tcc.ParaDto();
    }

    public async Task SalvarAsync(ProjetoTccDto dados, CancellationToken ct = default)
    {
        var fases = dados.Fases.Select(f => f.ParaDados()).ToList();
        var encontrado = await _repo.AtualizarAsync(t => t.Atualizar(dados.Titulo, dados.Orientador, fases), ct);
        if (!encontrado) throw new NaoEncontradoException("O TCC");
        _alteracoes.Notificar();
    }
}

public class PerfilService
{
    private readonly IPerfilRepository _repo;
    private readonly Alteracoes _alteracoes;

    public PerfilService(IPerfilRepository repo, Alteracoes alteracoes)
    {
        _repo = repo;
        _alteracoes = alteracoes;
    }

    public async Task<PerfilDto> ObterAsync(CancellationToken ct = default) =>
        (await _repo.ObterOuCriarAsync(ct)).ParaDto();

    public async Task SalvarAsync(PerfilDto dados, CancellationToken ct = default)
    {
        var cor = CorHex.De(dados.AvatarCor);
        await _repo.AtualizarAsync(p => p.Atualizar(dados.Nome, dados.Email, dados.Curso, dados.Semestre,
            dados.HorasComplementaresExigidas, cor), ct);
        _alteracoes.Notificar();
    }
}
