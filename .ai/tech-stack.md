Frontend – Angular for dynamic and modular web app development:

Angular 19 provides a powerful and structured framework ideal for building scalable SPAs.
TypeScript 5 ensures static typing and enhanced code reliability.
Tailwind CSS enables consistent and responsive UI styling.
Dexcom Sandbox API integration provides real-time glucose data visualization.

Backend – .NET 8 for robust API and domain-driven logic:

ASP.NET Core 8 powers the main backend API, offering high performance and maintainability.
Entity Framework Core handles data persistence with LINQ-based access to the database.
CQRS and DDD patterns structure the domain and ensure separation of concerns.
JWT authentication secures user access.
Docker Compose provides containerized services for local and production environments.

Data and Integrations:

Dexcom API for continuous glucose monitoring data.
Future integrations: Nightscout for extended data sources.
PostgreSQL as the primary database for reliability and open-source flexibility.

CI/CD and Hosting:

GitHub Actions automates build, test, and deployment workflows.
Docker ensures environment consistency and simple deployment.

Testing & Quality Assurance:

**Unit Testing:**
- Backend: nUnit test framework with NSubstitute for mocking and NUnit assertions for readable assertions
- Frontend: Jest for component and service testing with @angular/core/testing utilities and TypeScript for type-safe tests

**Integration Testing:**
- TestContainers for Docker-based database integration tests
- EF Core In-Memory provider for domain layer testing
- Npgsql for PostgreSQL ADO.NET provider integration
- Direct HttpClient for API endpoint testing

**End-to-End Testing:**
- Playwright for browser automation (planned for MVP completion)
- Manual Insomnia Collection for API validation

**Performance Testing:**
- k6 for load testing and API performance benchmarking
- Chrome DevTools for frontend performance profiling
- SQL query analysis for database optimization

**Security Testing:**
- OWASP ZAP for vulnerability scanning
- Burp Suite Community for manual security assessment
- npm audit for frontend dependency scanning
- dotnet analyzer for backend code security analysis

**Code Quality & Coverage:**
- SonarQube for comprehensive code coverage and quality metrics
- Code Climate for technical debt tracking
- ESLint for frontend linting
- StyleCop for backend code style enforcement

Test coverage targets: ≥80% for backend domain/application layers, ≥70% for frontend components.
For comprehensive test scenarios and acceptance criteria, see TEST_PLAN.md.