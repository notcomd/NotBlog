# Tasks

- [x] Task 1: Create `ICurrentUserService` interface and implementation
  - [x] Create `ICurrentUserService` in `Markdown.Domain/IServices/`
  - [x] Create `CurrentUserService` in `Markdown.Web.API/Services/` that extracts user GUID from JWT claims via `IHttpContextAccessor`
  - [x] Register `IHttpContextAccessor` and `ICurrentUserService` in DI container in `Program.cs`

- [x] Task 2: Configure JWT Bearer authentication in `Program.cs`
  - [x] Add `Microsoft.AspNetCore.Authentication.JwtBearer` package reference
  - [x] Configure JWT Bearer authentication with token validation parameters
  - [x] Add `app.UseAuthentication()` before `app.UseAuthorization()` in pipeline

- [x] Task 3: Create API request/response DTOs
  - [x] Create `Dto/` folder under `Markdown.Web.API/Application/`
  - [x] Create `CreateMarkdownRequest` DTO with name, content, tags, auth fields
  - [x] Create `UpdateMarkdownRequest` DTO with name, content, tags fields
  - [x] Create `ApiResponse` and `ApiResponse<T>` wrapper classes

- [x] Task 4: Create `UpdateMarkdownCommand` and `UpdateMarkdownCommandHandler`
  - [x] Create `UpdateMarkdownCommand` record implementing `IRequest<bool>`
  - [x] Create `UpdateMarkdownCommandHandler` that validates ownership, loads entity, creates history snapshot, updates content/tags, and saves

- [x] Task 5: Create `MarkdownController`
  - [x] Create `Controllers/MarkdownController.cs` with `[Authorize]` attribute
  - [x] Implement `POST /api/markdown` — creates article via existing `CreateMarkdownCommand`
  - [x] Implement `PUT /api/markdown/{markDownGuid}` — modifies article via `UpdateMarkdownCommand`
  - [x] Add proper model validation, error handling, and HTTP status codes

- [x] Task 6: Verify compilation and existing tests
  - [x] Run `dotnet build` to ensure no compilation errors
  - [x] Run existing tests to ensure no regressions

# Task Dependencies
- Task 2 depends on Task 1 (auth needs CurrentUserService DI registration)
- Task 4 depends on Task 1 (handler needs ICurrentUserService)
- Task 5 depends on Tasks 1, 2, 3, 4 (controller needs all infrastructure)
- Task 6 depends on Tasks 1-5
- Tasks 1 and 3 can run in parallel
