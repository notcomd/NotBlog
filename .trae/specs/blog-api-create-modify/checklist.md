# Verification Checklist

## Authentication & Authorization
- [x] JWT Bearer authentication is configured in `Program.cs` with proper token validation
- [x] `ICurrentUserService.GetUserId()` returns the authenticated user's GUID from JWT claims
- [x] `MarkdownController` has `[Authorize]` attribute on all endpoints
- [x] PUT endpoint rejects non-owner modifications with HTTP 403
- [x] POST endpoint requires authentication

## API Endpoints
- [x] `POST /api/markdown` creates a blog article and returns HTTP 201 with the created GUID
- [x] `POST /api/markdown` returns HTTP 400 for invalid input (empty name/content)
- [x] `POST /api/markdown` returns HTTP 401 when unauthenticated
- [x] `PUT /api/markdown/{guid}` updates an article and returns HTTP 200
- [x] `PUT /api/markdown/{guid}` returns HTTP 403 when non-owner attempts modification
- [x] `PUT /api/markdown/{guid}` returns HTTP 404 when article not found
- [x] `PUT /api/markdown/{guid}` returns HTTP 401 when unauthenticated
- [x] All endpoints follow RESTful conventions (proper HTTP methods, resource-oriented URLs)

## Domain Logic
- [x] Article ownership is verified on modification (`MarkUserGuid` check)
- [x] History snapshot (`OldMarkDown`) is created before content modification
- [x] Content hash is recomputed on modification
- [x] Soft-deleted articles are excluded from modification
- [x] Tags are properly updated on modification

## Code Quality
- [x] Solution compiles without errors (`dotnet build`)
- [x] No new warnings introduced
- [x] All existing tests pass
- [x] New code follows existing DDD project structure (Commands, CommandHandlers, Dto, Controllers)
- [x] New code follows existing naming conventions (MarkDown prefix, async suffix, etc.)
- [x] No new properties added to existing domain entities without approval

## DI Registration
- [x] `IHttpContextAccessor` is registered
- [x] `ICurrentUserService` is registered as scoped service
- [x] JWT Bearer authentication services are registered
