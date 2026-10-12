namespace KanbanEstudantil.Domain.Common;

internal static class Sincronizacao
{
    /// <summary>
    /// Faz a lista da entidade refletir os dados recebidos: atualiza os itens com Id conhecido,
    /// cria os novos e remove os que não vieram. Ids desconhecidos são tratados como novos.
    /// Devolve os itens na ordem em que os dados chegaram.
    /// </summary>
    public static IReadOnlyList<TEntidade> Sincronizar<TEntidade, TDados>(
        List<TEntidade> atual,
        IEnumerable<TDados> dados,
        Func<TDados, Guid?> idDosDados,
        Func<TEntidade, Guid> idDaEntidade,
        Func<TDados, TEntidade> criar,
        Action<TEntidade, TDados> atualizar)
    {
        var resultado = new List<TEntidade>();

        foreach (var item in dados)
        {
            var id = idDosDados(item);
            var existente = id.HasValue
                ? atual.FirstOrDefault(e => idDaEntidade(e) == id.Value && !resultado.Contains(e))
                : default;

            if (existente is not null)
            {
                atualizar(existente, item);
                resultado.Add(existente);
            }
            else
            {
                resultado.Add(criar(item));
            }
        }

        foreach (var removido in atual.Where(e => !resultado.Contains(e)).ToList())
            atual.Remove(removido);

        foreach (var novo in resultado.Where(r => !atual.Contains(r)))
            atual.Add(novo);

        return resultado;
    }
}
