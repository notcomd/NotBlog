# Blog Article Creation & Modification API Spec

## Why
The blog service currently has a fully implemented domain model, repository layer, and CQRS command infrastructure for MarkDown articles, but lacks HTTP API endpoints (Controllers) to expose creation and modification functionality. This spec adds RESTful API endpoints enabling authenticated users to create and modify blog articles.

## What Changes
- Add `MarkdownController` with POST (create) and PUT (modify) endpoints
- Add `UpdateMarkdownCommand` and `UpdateMarkdownCommandHandler` for article modification
- Add `ICurrentUserService` + `CurrentUserService` to extract authenticated user from JWT claims
- Add request/response DTOs for API contracts
- Register DI services (CurrentUserService, HttpContextAccessor)
- Wire up JWT Bearer authentication in `Program.cs`
- Version history auto-creation on content modification via existing `OldMarkDown` entity

## Assumptions (Resolved Ambiguities)
1. **Auth**: User identity extracted from JWT Bearer token claims (`sub`/`NameIdentifier`) via `IHttpContextAccessor`, consistent with Message service pattern. The Gateway (YARP) already forwards authenticated requests.
2. **Existing Command**: The `CreateMarkdownCommand`/Handler is reused — the controller dispatches it via `INotMediator`.
3. **Modification scope**: Content (name, content, hash) + tags. Auth/Option modification not in scope. History snapshot is auto-created on each modification.
4. **Response format**: Standard ASP.NET Core `ActionResult<T>` with proper HTTP status codes. A lightweight `ApiResponse` wrapper is added for consistency.
5. **Validation**: Manual validation in domain entities and handlers, consistent with existing codebase patterns. No FluentValidation.
6. **Auth on creation**: Accepts optional `auth` field (MarkDownAuth) defaulting to `PublicMark`.

## Impact
- Affected specs: None (new capability)
- Affected code:
  - `Markdown.Web.API/Controllers/MarkdownController.cs` (new)
  - `Markdown.Web.API/Application/Commands/UpdateMarkdownCommand.cs` (new)
  - `Markdown.Web.API/Application/CommandHandlers/UpdateMarkdownCommandHandler.cs` (new)
  - `Markdown.Web.API/Application/Dto/` (new - request/response DTOs)
  - `Markdown.Web.API/Services/CurrentUserService.cs` (new)
  - `Markdown.Web.API/Program.cs` (modified - auth + DI registrations)
  - `Markdown.Domain/IServices/ICurrentUserService.cs` (new)

## ADDED Requirements

### Requirement: JWT Authentication
The system SHALL authenticate API requests using JWT Bearer tokens and extract the authenticated user's GUID from claims for vertical permission control.

#### Scenario: Authenticated request
- **WHEN** a request includes a valid JWT Bearer token
- **THEN** the user's GUID is extracted from the `sub` or `NameIdentifier` claim and made available via `ICurrentUserService`

#### Scenario: Unauthenticated request
- **WHEN** a request lacks a valid JWT token
- **THEN** the endpoint returns HTTP 401 Unauthorized

### Requirement: Create Blog Article API
The system SHALL provide a RESTful endpoint to create a new blog article.

#### Scenario: Successful creation
- **WHEN** an authenticated user sends a POST request to `/api/markdown` with valid `name`, `content`, optional `tags`, and optional `auth`
- **THEN** a new MarkDown article is created, persisted, and HTTP 201 Created is returned with the created article's GUID

#### Scenario: Invalid request body
- **WHEN** the request body fails model validation (empty name or content)
- **THEN** HTTP 400 Bad Request is returned with error details

### Requirement: Modify Blog Article API
The system SHALL provide a RESTful endpoint to modify an existing blog article with owner-only authorization.

#### Scenario: Successful modification by owner
- **WHEN** the authenticated article owner sends a PUT request to `/api/markdown/{guid}` with valid `name`, `content`, and optional `tags`
- **THEN** the article is updated, a history snapshot is created via `OldMarkDown`, and HTTP 200 OK is returned

#### Scenario: Modification by non-owner
- **WHEN** a user who is not the article owner attempts to modify
- **THEN** HTTP 403 Forbidden is returned

#### Scenario: Article not found
- **WHEN** the specified article GUID does not exist or is soft-deleted
- **THEN** HTTP 404 Not Found is returned

### Requirement: Vertical Permission Control
The system SHALL enforce owner-only authorization on all modification endpoints, verifying the authenticated user matches the article's `MarkUserGuid`.

#### Scenario: Owner permission check
- **WHEN** any modification action is attempted
- **THEN** the system verifies `currentUserGuid == article.MarkUserGuid` before allowing the operation

### Requirement: API Response Format
The system SHALL return structured API responses with consistent HTTP status codes.

#### Scenario: Success response
- **WHEN** an operation succeeds
- **THEN** the response includes `success: true`, relevant data, and an appropriate 2xx status code

#### Scenario: Error response
- **WHEN** an operation fails due to validation, authorization, or server error
- **THEN** the response includes `success: false`, an error message, and an appropriate 4xx/5xx status code
