namespace BlazorKanban.Models
{
    public class KanbanList
    {
        public int Id { get; set; }
         public string Title { get; set; }

        //Chave estrangeira: Board
        public int BoardId {  get; set; }
        public Board Board { get; set; }

        //Relacionamento:Uma lista possui várias tarefas
        public List<TaskItem> Tasks { get; set; } = new();
    }
}
