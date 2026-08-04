using FileDev.Infrastructure.Idempotent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FileDev.Infrastructure.EntityConfig;

public class ClientRequestTypeConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("ClientRequest");
        // 幂等请求表：ClientRequestId 设为主键（唯一约束），并发插入相同请求时依赖数据库主键冲突兜底
        builder.HasKey(en => en.ClientRequestId);
    }
}