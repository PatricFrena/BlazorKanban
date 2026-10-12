using KanbanEstudantil.Domain.Common;
using KanbanEstudantil.Domain.Enums;

namespace KanbanEstudantil.Domain.Entities;

public class HoraComplementar
{
    private HoraComplementar() { }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string UsuarioId { get; private set; } = "";
    public string Titulo { get; private set; } = "";
    public string Descricao { get; private set; } = "";
    public TipoHoraComplementar Tipo { get; private set; }
    public double Horas { get; private set; }
    public DateOnly Data { get; private set; }
    public string? CertificadoUrl { get; private set; }
    public bool Validado { get; private set; }

    public static HoraComplementar Criar(string usuarioId, string titulo, string? descricao,
        TipoHoraComplementar tipo, double horas, DateOnly data, string? certificadoUrl, bool validado)
    {
        if (!Enum.IsDefined(tipo)) throw new DomainException("Tipo inválido.");
        Guarda.Intervalo(horas, 0.5, 1000, "As horas");

        var certificado = Guarda.UrlHttp(certificadoUrl, "O link do certificado", obrigatorio: false);

        return new HoraComplementar
        {
            UsuarioId = Guarda.Texto(usuarioId, "O usuário", 450),
            Titulo = Guarda.Texto(titulo, "O título", 150),
            Descricao = Guarda.Texto(descricao, "A descrição", 500, obrigatorio: false),
            Tipo = tipo,
            Horas = horas,
            Data = data,
            CertificadoUrl = certificado.Length == 0 ? null : certificado,
            Validado = validado
        };
    }

    public void DefinirValidacao(bool validado) => Validado = validado;
}
