using System.Text.RegularExpressions;
using KanbanEstudantil.Domain.Common;

namespace KanbanEstudantil.Domain.ValueObjects;

/// <summary>Cor no formato #rrggbb. Validada porque é usada em estilos inline na interface.</summary>
public sealed record CorHex
{
    private static readonly Regex Formato = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public static readonly CorHex Padrao = new("#6366f1");

    public string Valor { get; }

    private CorHex(string valor) => Valor = valor;

    public static CorHex De(string? valor)
    {
        if (valor is null || !Formato.IsMatch(valor))
            throw new DomainException("A cor deve estar no formato #rrggbb.");
        return new CorHex(valor.ToLowerInvariant());
    }

    public override string ToString() => Valor;
}
