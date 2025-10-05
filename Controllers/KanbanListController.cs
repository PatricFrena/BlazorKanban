using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlazorKanban.Data;
using BlazorKanban.Models;

namespace BlazorKanban.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KanbanListController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KanbanListController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<KanbanList>>> GetKanbanLists()
        {

            // CÓDIGO TEMPORÁRIO PARA TESTE (MOCK DATA)
            var mockData = new List<KanbanList>
            {
                new KanbanList
                {
                    Id = 1,
                    Title = "A Fazer (To Do)",
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem { Id = 101, Title = "Configurar Projeto", Description = "Confirmar .NET 8, Blazor e MySQL." },
                        new TaskItem { Id = 102, Title = "Criar a Interface", Description = "Adicionar botões e links." }
                    }
                },
                new KanbanList
                {
                    Id = 2,
                    Title = "Em Andamento (In Progress)",
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem { Id = 201, Title = "Corrigir Erro 500", Description = "Ligar o serviço MySQL." }
                    }
                },
                new KanbanList
                {
                    Id = 3,
                    Title = "Concluído (Done)",
                    Tasks = new List<TaskItem>
                    {
                        new TaskItem { Id = 301, Title = "Resolver 404", Description = "Adicionar app.MapControllers()." },
                        new TaskItem { Id = 302, Title = "Resolver BaseAddress", Description = "Usar NavigationManager no Kanban.razor." }
                    }
                }
            };

            // Retorna o código 200 (OK) com os dados de teste
            //return Ok(mockData);


            return await _context.KanbanLists.Include(k => k.Tasks).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<KanbanList>> GetKanbanList(int id)
        {
            var list = await _context.KanbanLists.Include(k => k.Tasks)
                                                 .FirstOrDefaultAsync(k => k.Id == id);
            if (list == null) return NotFound();

            return list;
        }

        [HttpPost]
        public async Task<ActionResult<KanbanList>> CreateKanbanList(KanbanList list)
        {
            _context.KanbanLists.Add(list);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetKanbanList), new { id = list.Id }, list);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKanbanList(int id, KanbanList list)
        {
            if (id != list.Id) return BadRequest();

            _context.Entry(list).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKanbanList(int id)
        {
            var list = await _context.KanbanLists.FindAsync(id);
            if (list == null) return NotFound();

            _context.KanbanLists.Remove(list);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
