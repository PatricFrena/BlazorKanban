using System.Text.Json;
using KanbanEstudantil.Domain.Entities;
using KanbanEstudantil.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KanbanEstudantil.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Disciplina> Disciplinas => Set<Disciplina>();
    public DbSet<HoraComplementar> HorasComplementares => Set<HoraComplementar>();
    public DbSet<ProjetoTcc> Tccs => Set<ProjetoTcc>();
    public DbSet<PerfilAcademico> Perfis => Set<PerfilAcademico>();

    private static readonly ValueConverter<CorHex, string> ConversorCor =
        new(v => v.Valor, s => CorHex.De(s));

    private static readonly ValueConverter<Peso, double> ConversorPeso =
        new(v => v.Valor, d => Peso.De(d));

    private static readonly ValueConverter<LimiteDeFaltas, double> ConversorLimite =
        new(v => v.Percentual, d => LimiteDeFaltas.De(d));

    private static readonly ValueConverter<List<string>, string> ConversorTags =
        new(v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            s => JsonSerializer.Deserialize<List<string>>(s, (JsonSerializerOptions?)null) ?? new List<string>());

    private static readonly ValueComparer<List<string>> ComparadorTags =
        new((a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t.GetHashCode())),
            v => v.ToList());

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigurarDisciplina(modelBuilder);
        ConfigurarCard(modelBuilder);
        ConfigurarHoras(modelBuilder);
        ConfigurarTcc(modelBuilder);
        ConfigurarPerfil(modelBuilder);
    }

    // Chaves Guid são geradas pelo Domain (ValueGeneratedNever). Assim o EF trata entidades novas
    // adicionadas a um agregado carregado como "Added" em vez de achar que já existem no banco.
    private static void Chave<T>(EntityTypeBuilder<T> b) where T : class
    {
        b.HasKey("Id");
        b.Property<Guid>("Id").ValueGeneratedNever();
    }

    private static void ConfigurarDisciplina(ModelBuilder mb)
    {
        mb.Entity<Disciplina>(b =>
        {
            Chave(b);
            b.Property(d => d.UsuarioId).HasMaxLength(450).IsRequired();
            b.Property(d => d.Nome).HasMaxLength(100).IsRequired();
            b.Property(d => d.Professor).HasMaxLength(100).IsRequired();
            b.Property(d => d.Cor).HasConversion(ConversorCor).HasMaxLength(7).IsRequired();
            b.Property(d => d.LimiteDeFaltas).HasConversion(ConversorLimite);
            b.HasIndex(d => d.UsuarioId);

            b.HasMany(d => d.Avaliacoes).WithOne().HasForeignKey("DisciplinaId").OnDelete(DeleteBehavior.Cascade);
            b.Navigation(d => d.Avaliacoes).UsePropertyAccessMode(PropertyAccessMode.Field);

            b.HasMany(d => d.Cards).WithOne().HasForeignKey(c => c.DisciplinaId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(d => d.Cards).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        mb.Entity<Avaliacao>(b =>
        {
            Chave(b);
            b.Property(a => a.Nome).HasMaxLength(80).IsRequired();
            b.Property(a => a.Peso).HasConversion(ConversorPeso);
        });
    }

    private static void ConfigurarCard(ModelBuilder mb)
    {
        mb.Entity<Card>(b =>
        {
            Chave(b);
            b.Property(c => c.Titulo).HasMaxLength(150).IsRequired();
            b.Property(c => c.Descricao).HasMaxLength(2000).IsRequired();
            b.Property(c => c.PesoNota).HasConversion(ConversorPeso);

            // As tags são uma lista de textos guardada em uma coluna (JSON).
            b.Ignore(c => c.Tags);
            b.Property<List<string>>("_tags")
                .HasColumnName("Tags")
                .HasConversion(ConversorTags, ComparadorTags)
                .IsRequired();

            Filho(b, c => c.Subtarefas);
            Filho(b, c => c.Membros);
            Filho(b, c => c.Anexos);
            Filho(b, c => c.SessoesPomodoro);
            Filho(b, c => c.Logs);
        });

        mb.Entity<Subtarefa>(b =>
        {
            Chave(b);
            b.Property(s => s.Titulo).HasMaxLength(200).IsRequired();
        });

        mb.Entity<MembroGrupo>(b =>
        {
            Chave(b);
            b.Property(m => m.Nome).HasMaxLength(100).IsRequired();
            b.Property(m => m.Email).HasMaxLength(200);
            b.Property(m => m.AvatarCor).HasConversion(ConversorCor).HasMaxLength(7).IsRequired();
        });

        mb.Entity<Anexo>(b =>
        {
            Chave(b);
            b.Property(a => a.Nome).HasMaxLength(200).IsRequired();
            b.Property(a => a.Url).HasMaxLength(2000).IsRequired();
        });

        mb.Entity<SessaoPomodoro>(Chave);

        mb.Entity<LogAtividade>(b =>
        {
            Chave(b);
            b.Property(l => l.Descricao).HasMaxLength(300).IsRequired();
            b.Property(l => l.Usuario).HasMaxLength(100).IsRequired();
        });
    }

    private static void Filho<TFilho>(EntityTypeBuilder<Card> b,
        System.Linq.Expressions.Expression<Func<Card, IEnumerable<TFilho>?>> navegacao) where TFilho : class
    {
        b.HasMany(navegacao).WithOne().HasForeignKey("CardId").OnDelete(DeleteBehavior.Cascade);
        b.Navigation(navegacao).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void ConfigurarHoras(ModelBuilder mb)
    {
        mb.Entity<HoraComplementar>(b =>
        {
            Chave(b);
            b.Property(h => h.UsuarioId).HasMaxLength(450).IsRequired();
            b.Property(h => h.Titulo).HasMaxLength(150).IsRequired();
            b.Property(h => h.Descricao).HasMaxLength(500).IsRequired();
            b.Property(h => h.CertificadoUrl).HasMaxLength(2000);
            b.HasIndex(h => h.UsuarioId);
        });
    }

    private static void ConfigurarTcc(ModelBuilder mb)
    {
        mb.Entity<ProjetoTcc>(b =>
        {
            Chave(b);
            b.Property(t => t.UsuarioId).HasMaxLength(450).IsRequired();
            b.Property(t => t.Titulo).HasMaxLength(200).IsRequired();
            b.Property(t => t.Orientador).HasMaxLength(100).IsRequired();
            b.HasIndex(t => t.UsuarioId).IsUnique(); // um TCC por usuário

            b.HasMany(t => t.Fases).WithOne().HasForeignKey("ProjetoTccId").OnDelete(DeleteBehavior.Cascade);
            b.Navigation(t => t.Fases).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        mb.Entity<FaseTcc>(b =>
        {
            Chave(b);
            b.Property(f => f.Nome).HasMaxLength(100).IsRequired();
            b.Property(f => f.Notas).HasMaxLength(2000).IsRequired();
        });
    }

    private static void ConfigurarPerfil(ModelBuilder mb)
    {
        mb.Entity<PerfilAcademico>(b =>
        {
            Chave(b);
            b.Property(p => p.UsuarioId).HasMaxLength(450).IsRequired();
            b.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            b.Property(p => p.Email).HasMaxLength(200);
            b.Property(p => p.Curso).HasMaxLength(100).IsRequired();
            b.Property(p => p.AvatarCor).HasConversion(ConversorCor).HasMaxLength(7).IsRequired();
            b.HasIndex(p => p.UsuarioId).IsUnique(); // um perfil por usuário
        });
    }
}
