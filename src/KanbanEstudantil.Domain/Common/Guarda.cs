using System.Net.Mail;

namespace KanbanEstudantil.Domain.Common;

/// <summary>Validações reutilizadas pelas entidades.</summary>
internal static class Guarda
{
    public static string Texto(string? valor, string campo, int max, bool obrigatorio = true)
    {
        var limpo = (valor ?? "").Trim();
        if (obrigatorio && limpo.Length == 0)
            throw new DomainException($"{campo} é obrigatório.");
        if (limpo.Length > max)
            throw new DomainException($"{campo} deve ter no máximo {max} caracteres.");
        return limpo;
    }

    public static string? EmailOpcional(string? valor, string campo)
    {
        var limpo = Texto(valor, campo, 200, obrigatorio: false);
        if (limpo.Length == 0) return null;
        if (!MailAddress.TryCreate(limpo, out var addr) || addr.Address != limpo)
            throw new DomainException($"{campo} não é um e-mail válido.");
        return limpo;
    }

    /// <summary>Aceita apenas http/https. Evita esquemas perigosos como javascript: em links.</summary>
    public static string UrlHttp(string? valor, string campo, bool obrigatorio = true)
    {
        var limpo = Texto(valor, campo, 2000, obrigatorio);
        if (limpo.Length == 0) return limpo;
        if (!Uri.TryCreate(limpo, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException($"{campo} deve começar com http:// ou https://.");
        return limpo;
    }

    public static void Intervalo(double valor, double min, double max, string campo)
    {
        if (double.IsNaN(valor) || valor < min || valor > max)
            throw new DomainException($"{campo} deve estar entre {min:0.##} e {max:0.##}.");
    }
}
