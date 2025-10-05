using System.Text.Json.Serialization;

namespace BlazorKanban.Models
{
    public class TaskItem
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        //Chave estrangeira: KanbanList
        public int KanbanListId { get; set; }

        [JsonIgnore]
        public KanbanList? KanbanList { get; set; }
    }
}