using System.Globalization;

namespace KanbanEstudantil.Web.Servicos;

/// <summary>Formatação de valores que vão para dentro de CSS e SVG.</summary>
public static class Formato
{
    /// <summary>Número com ponto decimal, independente da cultura do servidor (CSS não aceita vírgula).</summary>
    public static string Css(double valor, string formato = "0.0") =>
        valor.ToString(formato, CultureInfo.InvariantCulture);

    public static string Percentual(double valor) => Css(Math.Clamp(valor, 0, 100)) + "%";
}
