# Backend Testing Guide

This directory contains all backend tests for the Glyloop API, organized by project layer following Clean Architecture principles.

## Test Projects

### 1. Glyloop.Domain.Tests
Unit tests for domain entities, value objects, and business logic.

**Focus Areas:**
- Value object validation
- Entity behavior
- Domain events
- Business rule enforcement

**Example:**
```csharp
[Test]
public void Create_ShouldReturnSuccess_WhenValidUserId()
{
    // Arrange
    var validUserId = Guid.NewGuid();

    // Act
    var result = UserId.Create(validUserId);

    // Assert
    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value.Value, Is.EqualTo(validUserId));
}
```

### 2. Glyloop.Application.Tests
Unit tests for application layer handlers (CQRS commands and queries).

**Focus Areas:**
- Command handler logic
- Query handler logic
- Validation behavior
- Business workflow orchestration
- Dependency interactions (mocked)

**Example:**
```csharp
[Test]
public async Task Handle_ShouldReturnSuccess_WhenCommandIsValid()
{
    // Arrange
    var mockRepository = Substitute.For<IEventRepository>();
    var command = new CreateEventCommand { /* ... */ };

    // Act
    var result = await handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.That(result.IsSuccess, Is.True);
    await mockRepository.Received(1).AddAsync(Arg.Any<Event>());
}
```

### 3. Glyloop.Infrastructure.Tests
Integration tests for infrastructure concerns (database, external services).

**Focus Areas:**
- Database operations with TestContainers
- EF Core repository implementations
- External API integrations
- Data persistence and retrieval

**Key Tools:**
- TestContainers for PostgreSQL integration tests
- EF Core In-Memory provider for unit tests

### 4. Glyloop.API.Tests
Integration tests for API endpoints.

**Focus Areas:**
- HTTP request/response handling
- Authentication and authorization
- API endpoint behavior
- Controller logic

**Key Tools:**
- Microsoft.AspNetCore.Mvc.Testing for WebApplicationFactory
- HttpClient for API testing

## Testing Frameworks and Tools

### NUnit
Primary testing framework for all backend tests.

**Attributes:**
- `[TestFixture]` - Marks a test class
- `[Test]` - Marks a test method
- `[TestCase(...)]` - Parameterized tests
- `[SetUp]` / `[TearDown]` - Per-test initialization/cleanup
- `[Category("...")]` - Test categorization
- `[Parallelizable(ParallelScope.All)]` - Parallel execution

### NSubstitute
Mocking framework for creating test doubles.

**Common Patterns:**
```csharp
// Create mock
var mock = Substitute.For<IRepository>();

// Setup return value
mock.GetById(Arg.Any<Guid>()).Returns(entity);

// Verify call
await mock.Received(1).SaveAsync(Arg.Any<Entity>());

// Setup async
mock.SaveAsync(Arg.Any<Entity>()).Returns(Task.CompletedTask);
```

### AutoFixture
Test data builder for generating test objects.

**Usage:**
```csharp
var fixture = new Fixture();
var userId = fixture.Create<Guid>();
var userIds = fixture.CreateMany<Guid>(10);
```

### TestContainers
Docker-based integration testing for PostgreSQL.

**Usage:**
```csharp
// Infrastructure.Tests only
var container = new PostgreSqlBuilder()
    .WithImage("postgres:16")
    .Build();

await container.StartAsync();
```

## Running Tests

### All Tests
```bash
dotnet test
```

### Specific Project
```bash
dotnet test Glyloop.Domain.Tests
```

### With Coverage
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

### By Category
```bash
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

## Best Practices

### 1. Naming Convention
`Method_ShouldDoX_WhenY`

Example: `Create_ShouldReturnFailure_WhenUserIdIsEmpty`

### 2. AAA Pattern
```csharp
[Test]
public void TestMethod()
{
    // Arrange - Set up test data and mocks
    
    // Act - Execute the method under test
    
    // Assert - Verify expected outcomes
}
```

### 3. One Behavior Per Test
Each test should verify one specific behavior. Use `Assert.Multiple` for related checks.

### 4. Deterministic Tests
- No I/O or external services (mock them)
- No real time (inject `ITimeProvider`)
- No randomness (use AutoFixture with seeds if needed)

### 5. Mock Only External Dependencies
- Mock: Repositories, external APIs, time providers
- Don't Mock: Domain value objects, entities

### 6. Parameterized Tests
Use `[TestCase]` for testing multiple inputs:
```csharp
[TestCase(0)]
[TestCase(-1)]
[TestCase(-100)]
public void Create_ShouldReturnFailure_WhenInvalidAmount(decimal amount)
{
    var result = Carbohydrate.Create(amount);
    Assert.That(result.IsFailure, Is.True);
}
```

## Coverage Targets

- **Domain Layer**: ≥80%
- **Application Layer**: ≥80%
- **Infrastructure Layer**: ≥60%
- **API Layer**: ≥70%

## Sample Test Files

Reference the sample test files for comprehensive examples:
- `Glyloop.Domain.Tests/SampleDomainTests.cs` - Domain testing patterns
- `Glyloop.Application.Tests/SampleCommandHandlerTests.cs` - Application testing patterns

## Continuous Integration

Tests run automatically on:
- Pull requests
- Commits to main branch
- Release workflows

See `.github/workflows/` for CI configuration.

