using DomainCommonst;

namespace FileDev.Domain.DomainEntities
{
    public sealed class NotFileRepository : Entity, IAggregateRoot
    {
         public Guid UserGuid { get; private set; }

        public string NotFileRepositoryName { get; private set; } = null!;

        public List<FileGroup>? FileGroups { get; private set; } =new List<FileGroup>();

        public FileSafety FileSafety { get; private set; }

        public DateTime LastModified { get; private set; }

        public long FileCount { get; private set; } = 0;

        public string? RepositoryBrief { get; private set; }

        public Uri? RepositoryCover {  get; private set; }
    }
}
