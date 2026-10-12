using KanbanEstudantil.Domain.Common;

namespace KanbanEstudantil.Domain.ValueObjects;

/// <summary>Peso em porcentagem, de 0 a 100.</summary>
public readonly record struct Peso
{
    public double Valor { get; }

    private Peso(double valor) => Valor = valor;

    public static Peso Zero => new(0);

    public static Peso De(double valor)
    {
        if (double.IsNaN(valor) || valor < 0 || valor > 100)
            throw new DomainException("O peso deve estar entre 0% e 100%.");
        return new Peso(valor);
    }
}
