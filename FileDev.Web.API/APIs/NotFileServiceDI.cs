using FileDev.Domain.IRepository;
using NotMediator;

namespace FileDev.Web.API.APIs;

public record NotFileServiceDI(INotFileRepository FileRepository, INotMediator NotMediator);