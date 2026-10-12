using KanbanEstudantil.Domain.Enums;

namespace KanbanEstudantil.Domain.Entities;

/// <summary>Nomes em português dos enums, usados em mensagens e na interface.</summary>
public static class Rotulos
{
    public static string ToLabel(this KanbanColuna coluna) => coluna switch
    {
        KanbanColuna.AFazer => "A Fazer",
        KanbanColuna.EmAndamento => "Em Andamento",
        KanbanColuna.AguardandoFeedback => "Aguardando Feedback",
        KanbanColuna.Concluido => "Concluído",
        _ => coluna.ToString()
    };

    public static string ToLabel(this TipoHoraComplementar tipo) => tipo switch
    {
        TipoHoraComplementar.Curso => "Curso",
        TipoHoraComplementar.Palestra => "Palestra",
        TipoHoraComplementar.SemanaAcademica => "Semana Acadêmica",
        TipoHoraComplementar.Projeto => "Projeto",
        TipoHoraComplementar.Extensao => "Extensão",
        _ => "Outro"
    };

    public static string ToLabel(this TipoAnexo tipo) => tipo switch
    {
        TipoAnexo.Link => "🔗 Link",
        TipoAnexo.PDF => "📄 PDF",
        TipoAnexo.Doc => "📝 Doc",
        TipoAnexo.Imagem => "🖼️ Imagem",
        _ => "📎 Outro"
    };
}
