## BACKEND

### Guidelines for DOTNET

#### ENTITY_FRAMEWORK

- Use the repository and unit of work patterns to abstract data access logic and simplify testing
- Implement eager loading with Include() to avoid N+1 query problems for {{entity_relationships}}
- Use migrations for database schema changes and version control with proper naming conventions
- Apply appropriate tracking behavior (AsNoTracking() for read-only queries) to optimize performance
- Implement query optimization techniques like compiled queries for frequently executed database operations
- Use value conversions for complex property transformations and proper handling of {{custom_data_types}}


#### ASP_NET

- Use minimal APIs for simple endpoints in .NET 6+ applications to reduce boilerplate code
- Implement the mediator pattern with MediatR for decoupling request handling and simplifying cross-cutting concerns
- Use API controllers with model binding and validation attributes for {{complex_data_models}}
- Apply proper response caching with cache profiles and ETags for improved performance on {{high_traffic_endpoints}}
- Implement proper exception handling with ExceptionFilter or middleware to provide consistent error responses
- Use dependency injection with scoped lifetime for request-specific services and singleton for stateless services
- Implement proper error logging using built-in .NET logging or a third-party logger.
- Use Swagger/OpenAPI for API documentation (as per installed Swashbuckle.AspNetCore package).

 Follow the official Microsoft documentation and ASP.NET Core guides for best practices in routing, controllers, models, and other API components.

#### Folder Structure
Glyloop.API/
  Controllers/
  Middlewares/
  Endpoints/         # optional minimal APIs
  DI/                # composition root
  Program.cs

Glyloop.Application/
  Abstractions/      # IUnitOfWork, IDateTime, IUserContext
  Behaviors/         # pipeline behaviors (logging, validation, tx)
  Commands/
    <Feature>/
      <Action>Command.cs
      <Action>Handler.cs
  Queries/
    <Feature>/
      <Query>.cs
      <Handler>.cs
  DTOs/
  Results/
  Mapping/           # if using Mapster/AutoMapper profiles
  Validators/        # FluentValidation validators (optional)

Glyloop.Domain/
  Common/            # BaseEntity, ValueObject, DomainEvent
  Aggregates/
    UserProfile/
      UserProfile.cs
      Events/
      Specs/
  Services/          # domain services (pure)
  Errors/            # domain error codes/messages

Glyloop.Infrastructure/
  Persistence/
    GlyloopDbContext.cs
    Configurations/  # EF Core IEntityTypeConfiguration<T>
    Migrations/
  Repositories/
  Services/          # adapters (Email, Nightscout client in future)
  Options/           # settings bindings


### Architecture rules (non-negotiable)
Dependency direction: API → Application → Domain; Infrastructure is used only by API (via DI) and implements interfaces from Application/Domain.
Domain purity: no EF/HTTP/logging in Domain. Only business logic.
CQRS: write = Command, read = Query. No business logic in controllers/handlers beyond orchestration.
Validation: input validation in API (DTO) + business invariants in Domain.
Transactions: start/commit in Infrastructure (Unit of Work). Handlers request UoW via interface.
Errors: domain failures → typed Result/exceptions mapped to HTTP in error middleware.