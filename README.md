# 📚 StudyBoard — Kanban Universitário

Sistema Kanban para estudantes universitários, em **Blazor Server + C# (.NET 8)**, com banco de dados
SQLite e arquitetura em camadas.

## Como rodar

Pré-requisito: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) ou superior.

```bash
dotnet run --project src/KanbanEstudantil.Web
```

Na primeira execução o banco é criado em `src/KanbanEstudantil.Web/App_Data/kanban.db` e as migrations são
aplicadas sozinhas. Em desenvolvimento, um conjunto de dados de exemplo é criado (configuração
`Seed:DadosDeExemplo` em `appsettings.Development.json`). Para começar do zero, apague a pasta `App_Data`.

## Testes

```bash
dotnet test
```

Os testes cobrem as regras do Domain e rodam os serviços de ponta a ponta contra um SQLite de verdade.

## Estrutura

```
KanbanEstudantil.sln
├── src/
│   ├── KanbanEstudantil.Domain          Entidades, value objects e regras de negócio (sem dependências)
│   ├── KanbanEstudantil.Application     Serviços (casos de uso), DTOs e interfaces de repositório
│   ├── KanbanEstudantil.Infrastructure  EF Core + SQLite, repositórios, migrations, dados de exemplo
│   └── KanbanEstudantil.Web             Blazor Server (páginas, componentes, Program.cs)
└── tests/
    └── KanbanEstudantil.Tests           Testes de domínio e de integração
```

Dependências: `Web → Application, Infrastructure`, `Infrastructure → Application → Domain`.
O Domain não conhece EF Core nem Blazor.

### Decisões de arquitetura

- **Agregados:** `Disciplina` é a raiz e controla avaliações, faltas e cards. `ProjetoTcc`, `HoraComplementar`
  e `PerfilAcademico` são agregados próprios. Alterações passam por métodos da entidade, que validam as regras
  (peso máximo de 100%, nota dentro da escala, link só `http(s)`, cor `#rrggbb` etc.).
- **Repositórios:** cada operação abre e fecha seu próprio `DbContext` e altera o agregado dentro de um
  delegate. Se uma regra for violada, nada é gravado. Isso evita compartilhar um `DbContext` entre componentes
  Blazor, que dá erro de concorrência.
- **Usuário:** todo dado tem `UsuarioId` e todo repositório filtra por `IUsuarioAtual`. Hoje o site usa um
  usuário único (`UsuarioLocal`); o login vai trocar essa implementação.
- **Migrations:** `dotnet tool run dotnet-ef migrations add <Nome> --project src/KanbanEstudantil.Infrastructure --output-dir Persistence/Migrations`

## Próximas etapas

1. ~~Fundação: arquitetura em camadas, banco de dados, testes~~
2. Login e segurança (ASP.NET Core Identity, dados por usuário, cabeçalhos de segurança)
3. API REST + Swagger
4. Redesign visual
5. Colaboração entre usuários, notificações de prazo, upload de certificados
