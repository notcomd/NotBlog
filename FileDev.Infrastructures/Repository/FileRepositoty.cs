using FileDev.Domain.IRepository;
using File = FileDev.Domain.Entities.File;

namespace ConsoleApp1.Repository;

public class FileRepositoty : IFileRepository
{

    public async Task<File> FileByFileAllAsync()
    {
        throw new NotImplementedException();
    }

    public async Task<File> FileByFileIdAsync(int id)
    {
        throw new NotImplementedException();
    }
}