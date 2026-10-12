using KanbanEstudantil.Domain.Common;

namespace KanbanEstudantil.Domain.ValueObjects;

/// <summary>
/// Percentual máximo de faltas da carga horária antes da reprovação (a regra comum é 25%,
/// mas cada instituição pode definir a sua). O alerta começa em 80% desse limite.
/// </summary>
public readonly record struct LimiteDeFaltas
{
    private const double FracaoDoAlerta = 0.8;

    public double Percentual { get; }

    private LimiteDeFaltas(double percentual) => Percentual = percentual;

    public static LimiteDeFaltas Padrao => new(25);

    public double PercentualDeAlerta => Percentual * FracaoDoAlerta;

    public static LimiteDeFaltas De(double percentual)
    {
        if (double.IsNaN(percentual) || percentual < 1 || percentual > 100)
            throw new DomainException("O limite de faltas deve estar entre 1% e 100%.");
        return new LimiteDeFaltas(percentual);
    }
}
