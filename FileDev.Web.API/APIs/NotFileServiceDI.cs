using FileDev.Domain.IRepository;
using NotMediator;

namespace FileDev.Web.API.APIs;

public record NotFileServiceDI(IFileRepository  FileRepository,INotMediator NotMediator);