namespace Identity.Infrastructure.EntityConfig;

public class NotClientEntityTypeConfiguration : IEntityTypeConfiguration<NotClient>
{
    public void Configure(EntityTypeBuilder<NotClient> builder)
    {
        builder.ToTable("NotClient");

        builder.Ignore(b => b.DomainEvents);

        builder.Property(x => x.Id).UseHiLo("NotClientseq");

        // 主键
        builder.HasKey(xn => xn.NotClientId);
        builder.Property(x => x.NotClientId).HasColumnName("client_guid").IsRequired();

        // OAuth client_id（唯一索引）
        builder.Property(x => x.ClientId)
            .HasColumnName("client_id")
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(x => x.ClientId).IsUnique();

        // 应用基本信息
        builder.Property(x => x.ApplicationName)
            .HasColumnName("application_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ApplicationDescription)
            .HasColumnName("application_description")
            .HasMaxLength(500);

        builder.Property(x => x.ApplicationIcon)
            .HasColumnName("application_icon")
            .HasMaxLength(500);

        builder.Property(x => x.HomepageUri)
            .HasColumnName("homepage_uri")
            .HasMaxLength(500);

        builder.Property(x => x.PrivacyPolicyUri)
            .HasColumnName("privacy_policy_uri")
            .HasMaxLength(500);

        builder.Property(x => x.TermsOfServiceUri)
            .HasColumnName("terms_of_service_uri")
            .HasMaxLength(500);

        // 联系方式
        builder.Property(x => x.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(200);

        // OAuth 2.0 核心字段
        builder.Property(x => x.ClientSecret)
            .HasColumnName("client_secret")
            .HasMaxLength(500)
            .IsRequired();

        // HashSet<string> → PostgreSQL text[] 数组
        builder.Property(x => x.RedirectUris)
            .HasColumnName("redirect_uris")
            .HasColumnType("text[]");

        builder.Property(x => x.PostLogoutRedirectUris)
            .HasColumnName("post_logout_redirect_uris")
            .HasColumnType("text[]");

        builder.Property(x => x.AllowedScopes)
            .HasColumnName("allowed_scopes")
            .HasColumnType("text[]");

        builder.Property(x => x.AllowedGrantTypes)
            .HasColumnName("allowed_grant_types")
            .HasColumnType("text[]");

        // 安全与类型
        builder.Property(x => x.ApplicationType)
            .HasColumnName("application_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.TokenEndpointAuthMethod)
            .HasColumnName("token_endpoint_auth_method")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.RequirePkce)
            .HasColumnName("require_pkce")
            .IsRequired();

        builder.Property(x => x.RequireConsent)
            .HasColumnName("require_consent")
            .IsRequired();

        // CORS 来源
        builder.Property(x => x.AllowedCorsOrigins)
            .HasColumnName("allowed_cors_origins")
            .HasColumnType("text[]");

        // 状态
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // 时间戳
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
    }
}
