using Markdown.Domain.IRepository;
using NotMediator;
using Markdown.Domain.IServices;
namespace Markdown.Web.API.Apis;

public record IDServiceProvider(INotMediator NotMediator, 
ICurrentUserService CurrentUserService,
 IMarkdownRepository MarkdownRepository
 ,IMarkReviewRepository MarkReviewRepository);
