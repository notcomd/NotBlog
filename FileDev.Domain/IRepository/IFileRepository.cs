using File = FileDev.Domain.Entities.File;

namespace FileDev.Domain.IRepository;

public interface IFileRepository
{
    Task<File> FileByFileAllAsync();

    Task<File> FileByFileIdAsync(int id);
}