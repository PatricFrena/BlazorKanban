using KanbanEstudantil.Application;
using KanbanEstudantil.Application.Abstractions;
using KanbanEstudantil.Application.Dtos;
using KanbanEstudantil.Application.Services;
using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Enums;
using KanbanEstudantil.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace KanbanEstudantil.Tests.Integracao;

/// <summary>Roda os serviços de ponta a ponta contra um banco SQLite de verdade (arquivo temporário).</summary>
public sealed class BancoDeTeste : IAsyncLifetime
{
    private readonly string _arquivo = Path.Combine(Path.GetTempPath(), $"kanban-teste-{Guid.NewGuid():N}.db");
    private readonly List<ServiceProvider> _providers = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        foreach (var p in _providers) p.Dispose();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        foreach (var f in Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(_arquivo) + "*"))
            try { File.Delete(f); } catch { /* arquivo ainda em uso: o sistema limpa o temp depois */ }
        return Task.CompletedTask;
    }

    /// <summary>Cria a "sessão" de um usuário, como o Blazor faria para cada conexão.</summary>
    public async Task<IServiceScope> AbrirSessaoAsync(string usuarioId)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddInfrastructure($"Data Source={_arquivo}");
        services.AddSingleton<IUsuarioAtual>(new UsuarioFixo(usuarioId));
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        await provider.InicializarBancoAsync();
        return provider.CreateScope();
    }

    private sealed record UsuarioFixo(string UsuarioId) : IUsuarioAtual;
}

public class PersistenciaTests : IClassFixture<BancoDeTeste>
{
    private readonly BancoDeTeste _banco;

    public PersistenciaTests(BancoDeTeste banco) => _banco = banco;

    private static CardDto NovoCard(string titulo = "Estudar") => new()
    {
        Titulo = titulo,
        Coluna = KanbanColuna.EmAndamento,
        DataEntrega = new DateOnly(2026, 12, 1),
        Tags = new() { "#Prova", "#Seminário" },
        PesoNota = 30,
        Subtarefas = new()
        {
            new SubtarefaDto { Titulo = "Lista 1" },
            new SubtarefaDto { Titulo = "Lista 2", Concluida = true }
        },
        Membros = new() { new MembroDto { Nome = "Carlos", Email = "c@uni.br", AvatarCor = "#f59e0b" } },
        Anexos = new() { new AnexoDto { Nome = "Slides", Url = "https://exemplo.com/slides" } }
    };

    [Fact]
    public async Task Disciplina_e_card_com_filhos_sobrevivem_a_uma_nova_leitura()
    {
        using var s = await _banco.AbrirSessaoAsync("ana");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Cálculo I", Professor = "Rodrigo", Cor = "#10b981", CargaHoraria = 80 });
        var cardId = await cards.CriarAsync(discId, NovoCard());

        var lida = (await disciplinas.ObterAsync(discId))!;
        var card = Assert.Single(lida.Cards);
        Assert.Equal(cardId, card.Id);
        Assert.Equal("#10b981", lida.Cor);
        Assert.Equal(new[] { "#Prova", "#Seminário" }, card.Tags);
        Assert.Equal(2, card.Subtarefas.Count);
        Assert.Equal(1, card.SubtarefasConcluidas);
        Assert.Equal("Carlos", Assert.Single(card.Membros).Nome);
        Assert.Equal("https://exemplo.com/slides", Assert.Single(card.Anexos).Url);
        Assert.Equal(new DateOnly(2026, 12, 1), card.DataEntrega);
        Assert.Single(card.Logs); // "Card criado..."
    }

    [Fact]
    public async Task Atualizar_card_adiciona_edita_e_remove_itens_sem_duplicar()
    {
        using var s = await _banco.AbrirSessaoAsync("bia");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Física", CargaHoraria = 60 });
        await cards.CriarAsync(discId, NovoCard());

        var card = (await disciplinas.ObterAsync(discId))!.Cards.Single().Clonar();
        card.Subtarefas.RemoveAll(x => x.Titulo == "Lista 1");
        card.Subtarefas.First(x => x.Titulo == "Lista 2").Concluida = false;
        card.Subtarefas.Add(new SubtarefaDto { Titulo = "Lista 3" });
        card.Membros.Clear();
        card.Tags.Remove("#Prova");
        card.Titulo = "Estudar muito";
        await cards.AtualizarAsync(discId, card);

        var depois = (await disciplinas.ObterAsync(discId))!.Cards.Single();
        Assert.Equal("Estudar muito", depois.Titulo);
        Assert.Equal(new[] { "Lista 2", "Lista 3" }, depois.Subtarefas.Select(x => x.Titulo).OrderBy(x => x));
        Assert.Equal(0, depois.SubtarefasConcluidas);
        Assert.Empty(depois.Membros);
        Assert.Equal(new[] { "#Seminário" }, depois.Tags);
        Assert.Equal(2, depois.Logs.Count);
    }

    [Fact]
    public async Task Itens_de_checklist_membros_e_avaliacoes_mantem_a_ordem_digitada()
    {
        using var s = await _banco.AbrirSessaoAsync("ordem");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Ordem", CargaHoraria = 60 });
        var card = NovoCard();
        card.Subtarefas = Enumerable.Range(1, 8).Select(i => new SubtarefaDto { Titulo = $"Passo {i:00}" }).Reverse().ToList();
        card.Membros = new() { new MembroDto { Nome = "Zeca" }, new MembroDto { Nome = "Ana" }, new MembroDto { Nome = "Bia" } };
        await cards.CriarAsync(discId, card);
        await disciplinas.SalvarAvaliacoesAsync(discId, new[]
        {
            new AvaliacaoDto { Nome = "Trabalho", Peso = 20 },
            new AvaliacaoDto { Nome = "P1", Peso = 40 },
            new AvaliacaoDto { Nome = "P2", Peso = 40 }
        });

        var lida = (await disciplinas.ObterAsync(discId))!;
        Assert.Equal(card.Subtarefas.Select(x => x.Titulo), lida.Cards.Single().Subtarefas.Select(x => x.Titulo));
        Assert.Equal(new[] { "Zeca", "Ana", "Bia" }, lida.Cards.Single().Membros.Select(x => x.Nome));
        Assert.Equal(new[] { "Trabalho", "P1", "P2" }, lida.Avaliacoes.Select(x => x.Nome));
    }

    [Fact]
    public async Task Mover_card_e_registrar_pomodoro_persistem()
    {
        using var s = await _banco.AbrirSessaoAsync("caio");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Redes", CargaHoraria = 60 });
        var cardId = await cards.CriarAsync(discId, NovoCard());

        await cards.MoverAsync(discId, cardId, KanbanColuna.Concluido);
        var agora = DateTime.UtcNow;
        await cards.RegistrarPomodoroAsync(discId, cardId, agora.AddMinutes(-25), agora);

        var card = (await disciplinas.ObterAsync(discId))!.Cards.Single();
        Assert.Equal(KanbanColuna.Concluido, card.Coluna);
        Assert.Equal(1, card.PomodorosConcluidos);
        Assert.Equal(2, card.Logs.Count);
    }

    [Fact]
    public async Task Regra_violada_nao_grava_nada()
    {
        using var s = await _banco.AbrirSessaoAsync("dani");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Banco de Dados", CargaHoraria = 60 });
        var invalido = NovoCard();
        invalido.Anexos.Add(new AnexoDto { Nome = "x", Url = "javascript:alert(1)" });

        await Assert.ThrowsAsync<DomainException>(() => cards.CriarAsync(discId, invalido));

        Assert.Empty((await disciplinas.ObterAsync(discId))!.Cards);
    }

    [Fact]
    public async Task Faltas_e_avaliacoes_sao_salvas_e_a_media_e_calculada()
    {
        using var s = await _banco.AbrirSessaoAsync("edu");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Estatística", CargaHoraria = 80 });
        await disciplinas.RegistrarFaltaAsync(discId, 17);
        await disciplinas.SalvarAvaliacoesAsync(discId, new[]
        {
            new AvaliacaoDto { Nome = "P1", Peso = 50, Nota = 6 },
            new AvaliacaoDto { Nome = "P2", Peso = 50, Nota = 8 }
        });

        var d = (await disciplinas.ObterAsync(discId))!;
        Assert.Equal(17, d.Faltas);
        Assert.True(d.EmRiscoDeFalta);
        Assert.False(d.ReprovadoPorFalta);
        Assert.Equal(7.0, d.Media!.Value, 3);
        Assert.Equal(2, d.Avaliacoes.Count);

        // Editar uma nota mantém o Id e não duplica.
        var p1 = d.Avaliacoes.First(a => a.Nome == "P1");
        p1.Nota = 10;
        await disciplinas.SalvarAvaliacoesAsync(discId, d.Avaliacoes);
        var depois = (await disciplinas.ObterAsync(discId))!;
        Assert.Equal(2, depois.Avaliacoes.Count);
        Assert.Equal(9.0, depois.Media!.Value, 3);
    }

    [Fact]
    public async Task Excluir_disciplina_apaga_cards_e_filhos()
    {
        using var s = await _banco.AbrirSessaoAsync("fabi");
        var disciplinas = s.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cards = s.ServiceProvider.GetRequiredService<CardService>();

        var discId = await disciplinas.CriarAsync(new NovaDisciplinaDto { Nome = "Sistemas Operacionais", CargaHoraria = 60 });
        await cards.CriarAsync(discId, NovoCard());

        await disciplinas.RemoverAsync(discId);

        Assert.Null(await disciplinas.ObterAsync(discId));
        Assert.Empty(await disciplinas.ListarAsync());
        await Assert.ThrowsAsync<NaoEncontradoException>(() => disciplinas.RemoverAsync(discId));
    }

    [Fact]
    public async Task Um_usuario_nao_enxerga_nem_altera_dados_de_outro()
    {
        using var ana = await _banco.AbrirSessaoAsync("usuario-ana");
        using var gui = await _banco.AbrirSessaoAsync("usuario-gui");
        var dAna = ana.ServiceProvider.GetRequiredService<DisciplinaService>();
        var dGui = gui.ServiceProvider.GetRequiredService<DisciplinaService>();
        var cGui = gui.ServiceProvider.GetRequiredService<CardService>();

        var idDaAna = await dAna.CriarAsync(new NovaDisciplinaDto { Nome = "Privada da Ana", CargaHoraria = 60 });

        Assert.Empty(await dGui.ListarAsync());
        Assert.Null(await dGui.ObterAsync(idDaAna));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => dGui.RegistrarFaltaAsync(idDaAna, 1));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => cGui.CriarAsync(idDaAna, NovoCard()));
        await Assert.ThrowsAsync<NaoEncontradoException>(() => dGui.RemoverAsync(idDaAna));

        var intacta = (await dAna.ObterAsync(idDaAna))!;
        Assert.Equal(0, intacta.Faltas);
        Assert.Empty(intacta.Cards);
    }

    [Fact]
    public async Task Tcc_e_criado_com_fases_padrao_e_a_ordem_e_mantida()
    {
        using var s = await _banco.AbrirSessaoAsync("gabi");
        var tccs = s.ServiceProvider.GetRequiredService<TccService>();

        Assert.Null(await tccs.ObterAsync());
        var criado = await tccs.CriarComFasesPadraoAsync();
        Assert.Equal(6, criado.Fases.Count);

        // Segunda chamada devolve o mesmo TCC em vez de criar outro.
        Assert.Equal(criado.Id, (await tccs.CriarComFasesPadraoAsync()).Id);

        var tcc = (await tccs.ObterAsync())!;
        tcc.Titulo = "IA na educação";
        tcc.Orientador = "Profa. Marta";
        tcc.Fases[0].Concluida = true;
        tcc.Fases[1].Notas = "Reunião dia 10";
        tcc.Fases.Reverse();
        tcc.Fases.Add(new FaseTccDto { Nome = "Defesa" });
        await tccs.SalvarAsync(tcc);

        var depois = (await tccs.ObterAsync())!;
        Assert.Equal("IA na educação", depois.Titulo);
        Assert.Equal(7, depois.Fases.Count);
        Assert.Equal("Entrega Final", depois.Fases[0].Nome);
        Assert.Equal("Defesa", depois.Fases[^1].Nome);
        Assert.Equal("Escolha do Tema", depois.Fases[^2].Nome);
        Assert.True(depois.Fases[^2].Concluida);
    }

    [Fact]
    public async Task Horas_complementares_e_resumo()
    {
        using var s = await _banco.AbrirSessaoAsync("henrique");
        var horas = s.ServiceProvider.GetRequiredService<HoraComplementarService>();
        var perfis = s.ServiceProvider.GetRequiredService<PerfilService>();

        var perfil = await perfis.ObterAsync();
        perfil.Nome = "Henrique";
        perfil.HorasComplementaresExigidas = 100;
        await perfis.SalvarAsync(perfil);

        await horas.AdicionarAsync(new HoraComplementarDto { Titulo = "Curso", Tipo = TipoHoraComplementar.Curso, Horas = 40, Validado = true, Data = new DateOnly(2026, 5, 1) });
        await horas.AdicionarAsync(new HoraComplementarDto { Titulo = "Palestra", Tipo = TipoHoraComplementar.Palestra, Horas = 10, Data = new DateOnly(2026, 6, 1) });

        var resumo = await horas.ObterResumoAsync();
        Assert.Equal(50, resumo.Total);
        Assert.Equal(100, resumo.Exigidas);
        Assert.Equal(50, resumo.Progresso);
        Assert.Equal(1, resumo.Validadas);
        Assert.Equal(50, resumo.Restantes);

        var lista = await horas.ListarAsync();
        Assert.Equal("Palestra", lista[0].Titulo); // mais recente primeiro

        await horas.RemoverAsync(lista[0].Id);
        Assert.Single(await horas.ListarAsync());
    }

    [Fact]
    public async Task Perfil_comeca_com_valores_padrao_e_valida_ao_salvar()
    {
        using var s = await _banco.AbrirSessaoAsync("iris");
        var perfis = s.ServiceProvider.GetRequiredService<PerfilService>();

        var perfil = await perfis.ObterAsync();
        Assert.Equal("Estudante", perfil.Nome);

        perfil.Email = "email-invalido";
        await Assert.ThrowsAsync<DomainException>(() => perfis.SalvarAsync(perfil));

        perfil.Email = "iris@uni.br";
        perfil.Curso = "Direito";
        perfil.Semestre = 4;
        await perfis.SalvarAsync(perfil);

        var salvo = await perfis.ObterAsync();
        Assert.Equal("iris@uni.br", salvo.Email);
        Assert.Equal(4, salvo.Semestre);
    }
}
