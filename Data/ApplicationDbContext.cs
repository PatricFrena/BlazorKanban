using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BlazorKanban.Models;

namespace BlazorKanban.Data
{
    // O DbContext herda de IdentityDbContext para suportar o ApplicationUser, o que está correto.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Seus DbSet's estão corretos.
        public DbSet<Board> Boards { get; set; }
        public DbSet<KanbanList> KanbanLists { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // O IdentityDbContext precisa que o método base seja chamado primeiro
            base.OnModelCreating(builder);

            // =================================================================
            // ADIÇÃO DE DADOS INICIAIS (SEED DATA) PARA AS LISTAS KANBAN
            // Isso garante que os Status existam no banco para evitar o erro de Foreign Key.
            // =================================================================

            // Assegure-se de que a lista de Board com Id=1 exista
            builder.Entity<Board>().HasData(
                new Board { Id = 1, Name = "Board Padrão" }
            );

            // Seed Data para KanbanLists: Usamos o BoardId 1
            builder.Entity<KanbanList>().HasData(
                new KanbanList { Id = 1, Title = "A Fazer", BoardId = 1 },
                new KanbanList { Id = 2, Title = "Em Andamento", BoardId = 1 },
                new KanbanList { Id = 3, Title = "Concluída", BoardId = 1 }
            );

            // O seu código para definir o comprimento máximo de strings
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(string) && property.GetMaxLength() == null)
                    {
                        property.SetMaxLength(255);
                    }
                }
            }
        }
    }
}