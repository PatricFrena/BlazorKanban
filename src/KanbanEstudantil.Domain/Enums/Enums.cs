namespace KanbanEstudantil.Domain.Enums;

public enum KanbanColuna
{
    AFazer = 0,
    EmAndamento = 1,
    AguardandoFeedback = 2,
    Concluido = 3
}

public enum Prioridade
{
    Alta = 1,
    Media = 2,
    Baixa = 3
}

public enum CriticidadeCard
{
    Normal,
    Atencao,
    Urgente,
    Hoje,
    Atrasado
}

public enum TipoAnexo { Link, PDF, Doc, Imagem, Outro }

public enum TipoHoraComplementar
{
    Curso,
    Palestra,
    SemanaAcademica,
    Projeto,
    Extensao,
    Outro
}
