namespace BlazorKanban.Models
{
    public class Board
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Relacionamento: Um board possui várias listas
        public List<KanbanList> Lists { get; set; } = new();
    }
}
