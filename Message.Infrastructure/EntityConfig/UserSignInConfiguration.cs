namespace Message.Infrastructure.EntityConfig;

/// <summary>用户签到记录表配置（UserSignIns，(UserId, SignInDate) 唯一）。</summary>
public class UserSignInConfiguration : IEntityTypeConfiguration<UserSignIn>
{
    public void Configure(EntityTypeBuilder<UserSignIn> builder)
    {
        builder.ToTable("UserSignIns");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.SignInDate)
            .IsRequired();

        builder.Property(s => s.CreateTime)
            .IsRequired();

        // 同日重复签到防重（DB 约束兜底）
        builder.HasIndex(s => new { s.UserId, s.SignInDate }).IsUnique();
        builder.HasIndex(s => s.UserId);
    }
}
