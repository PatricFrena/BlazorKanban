using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.ValueObjects;

namespace KanbanEstudantil.Domain.Entities;

public class PerfilAcademico
{
    private PerfilAcademico() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UsuarioId { get; private set; } = "";
    public string Nome { get; private set; } = "Estudante";
    public string? Email { get; private set; }
    public string Curso { get; private set; } = "";
    public int Semestre { get; private set; } = 1;
    public double HorasComplementaresExigidas { get; private set; } = 200;
    public CorHex AvatarCor { get; private set; } = CorHex.Padrao;

    public static PerfilAcademico CriarPadrao(string usuarioId) =>
        new() { UsuarioId = Guarda.Texto(usuarioId, "O usuário", 450) };

    public void Atualizar(string nome, string? email, string? curso, int semestre,
        double horasComplementaresExigidas, CorHex avatarCor)
    {
        if (semestre < 1 || semestre > 20)
            throw new DomainException("O semestre deve estar entre 1 e 20.");
        Guarda.Intervalo(horasComplementaresExigidas, 0, 5000, "As horas complementares exigidas");

        Nome = Guarda.Texto(nome, "O nome", 100);
        Email = Guarda.EmailOpcional(email, "O e-mail");
        Curso = Guarda.Texto(curso, "O curso", 100, obrigatorio: false);
        Semestre = semestre;
        HorasComplementaresExigidas = horasComplementaresExigidas;
        AvatarCor = avatarCor;
    }
}
