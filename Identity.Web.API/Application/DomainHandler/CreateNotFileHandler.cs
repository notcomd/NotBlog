using FileDev.Domain.Entities;
using FileDev.Domain.Events;
using FileDev.Domain.IRepository;
using FileDev.Domain.SeedWork;

namespace Identity.Web.API.Application.DomainHandler;

public class CreateNotFileHandler(
    INotFileRepository notFileRepository,
    ILogger<INotFileRepository> logger,
    IUnitOfWork unitOfWork,
    INotFileGroupRepository notFileGroupRepository) : INotificationHandler<CreateNotFileEvent>
{
    public async Task Handler(CreateNotFileEvent notifications,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var fileData = await notFileRepository.GetFileByIdAsync(notifications.NotFile.FileId);
        if (fileData is null)
        {
            var fileGroupData = await notFileGroupRepository.GetNotFileGroupByIdAsync(notifications.UserGuid);
            if (fileGroupData is null)
            {
                var fileGroup = new NotFileGroup.NotFileGroupBuilder()
                    .WithUserId(notifications.UserGuid)
                    .WithFileGroupName(notifications.FileName)
                    .WithFileGroupTags(new HashSet<string>())
                    .WithFileType(notifications.FileType)
                    .WithFileIdentity(notifications.FileIdentity)
                    .Build();
                await notFileGroupRepository.InsertNotFileGroupAsync(fileGroup);
            }

            logger.LogInformation("File not found {FileId}", notifications.NotFile.FileId);
            await notFileRepository.InsertFileAsync(notifications.NotFile);

            await unitOfWork.SavaChangesAsync(cancellationToken);
        }
    }
}