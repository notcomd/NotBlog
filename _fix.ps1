$ErrorActionPreference = 'Stop'

function Replace-InFile($path, $old, $new) {
    $bytes = [IO.File]::ReadAllBytes($path)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $enc = New-Object System.Text.UTF8Encoding($hasBom)
    $text = [IO.File]::ReadAllText($path)
    $newText = $text.Replace($old, $new)
    if ($newText -ne $text) {
        [IO.File]::WriteAllText($path, $newText, $enc)
        Write-Host "OK: $path"
    } else {
        Write-Host "NOCHANGE: $path"
    }
}

# ===== #21/#23: NotFile.cs =====
Replace-InFile 'f:\NotBlog\FileDev.Domain\Entities\NotFile.cs' '    public string FileDescription { get; set; } = string.Empty;' '    public string FileDescription { get; private set; } = string.Empty;'

Replace-InFile 'f:\NotBlog\FileDev.Domain\Entities\NotFile.cs' "    public bool IsEquesFile(string fileMd5)`r`n    {`r`n        return fileMd5 == FileMd5;`r`n    }`r`n`r`n" ''

Replace-InFile 'f:\NotBlog\FileDev.Domain\Entities\NotFile.cs' "        private FileType _fileType;`r`n" ''

Replace-InFile 'f:\NotBlog\FileDev.Domain\Entities\NotFile.cs' "        public NotFileBuilder WithFileType(FileType fileType)`r`n        {`r`n            _fileType = fileType;`r`n            return this;`r`n        }`r`n`r`n" ''

# ===== #15/#40: FileDev.Domain.csproj =====
Replace-InFile 'f:\NotBlog\FileDev.Domain\FileDev.Domain.csproj' '<TargetFramework>net9.0</TargetFramework>' '<TargetFramework>net10.0</TargetFramework>'

Replace-InFile 'f:\NotBlog\FileDev.Domain\FileDev.Domain.csproj' "<PackageReference Include=`"Microsoft.Extensions.Configuration.Abstractions`" Version=`"10.0.9`"/>`r`n        <PackageReference Include=`"Microsoft.Extensions.DependencyInjection.Abstractions`" Version=`"10.0.9`"/>`r`n        " ''

# ===== #22: NotFileRepository.cs =====
Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\Repository\NotFileRepository.cs' 'x.FileTags.Intersect(tags).Any()' 'x.FileTags.Any(t => tags.Contains(t))'

Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\Repository\NotFileRepository.cs' "    public async Task<IEnumerable<NotFile>?> FileByFileAllAsync(Guid userId)`r`n    {`r`n        return await _notFileDbContext`r`n            .NotFiles`r`n            .Where(en => en.UserId.Equals(userId) && !en.IsDeleted)`r`n            .ToListAsync();`r`n    }`r`n    `r`n" ''

# ===== #24: Entity.cs =====
$ePath = 'f:\NotBlog\FileDev.Domain\SeedWork\Entity.cs'
$eBytes = [IO.File]::ReadAllBytes($ePath)
$eBom = $eBytes.Length -ge 3 -and $eBytes[0] -eq 0xEF -and $eBytes[1] -eq 0xBB -and $eBytes[2] -eq 0xBF
$eEnc = New-Object System.Text.UTF8Encoding($eBom)
$eText = [IO.File]::ReadAllText($ePath)
$eText = $eText.Replace('_domainEventbus', '_domainEventBus')
$eText = $eText.Replace('DomainEventbus', 'DomainEventBus')
[IO.File]::WriteAllText($ePath, $eText, $eEnc)
Write-Host "OK: $ePath"

# ===== #33: NotFileStorageOptions.cs =====
Replace-InFile 'f:\NotBlog\FileDev.Domain\Options\NotFileStorageOptions.cs' "    /// <summary>最大文件大小（默认10GB）</summary>`r`n    public long MaxFileSize { get; set; } = 10L * 1024 * 1024 * 1024;" "    /// <summary>最大文件大小（默认100MB，与 Kestrel 请求体大小限制保持一致）</summary>`r`n    public long MaxFileSize { get; set; } = 100 * 1024 * 1024;"

# ===== #41: CreateFileGroupEvent.cs =====
Replace-InFile 'f:\NotBlog\FileDev.Domain\Events\CreateFileGroupEvent.cs' "    public string FileName { get; } = fileGroupName;`r`n`r`n" ''

# ===== #42: NotFileStorageInfo.cs =====
Replace-InFile 'f:\NotBlog\FileDev.Domain\Dto\Response\NotFileStorageInfo.cs' "    public string FileSize { get; set; } = string.Empty;`r`n    public string FileType { get; set; } = string.Empty;" "    public long FileSize { get; set; }`r`n    public FileType FileType { get; set; }"

# ===== #24 配套: EntityConfig 三个文件 =====
Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\EntityConfig\NotFileEntityConfig.cs' 'en => en.DomainEventbus' 'en => en.DomainEventBus'
Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\EntityConfig\NotFileGroupEntityConfig.cs' 'en => en.DomainEventbus' 'en => en.DomainEventBus'
Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\EntityConfig\FileChunkRecordEntityConfig.cs' 'e => e.DomainEventbus' 'e => e.DomainEventBus'

# ===== #23 配套: NotFileService.cs 移除 WithFileType 调用 =====
Replace-InFile 'f:\NotBlog\FileDev.Infrastructure\Service\NotFileService.cs' "            .WithFileType(fileType)`r`n" ''

# ===== #24 配套: MediatorExtensions.cs 反射兼容 =====
$mPath = 'f:\NotBlog\DomainInfrastructure\MediatorExtensions.cs'
$mBytes = [IO.File]::ReadAllBytes($mPath)
$mBom = $mBytes.Length -ge 3 -and $mBytes[0] -eq 0xEF -and $mBytes[1] -eq 0xBB -and $mBytes[2] -eq 0xBF
$mEnc = New-Object System.Text.UTF8Encoding($mBom)
$mText = [IO.File]::ReadAllText($mPath)
$mText = $mText.Replace('const string propertyName = "DomainEventbus";', 'const string propertyName = "DomainEventBus";')
$mText = $mText.Replace('BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);', 'BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);')
$mText = $mText.Replace('缓存实体类型的 DomainEventbus 属性信息', '缓存实体类型的 DomainEventBus 属性信息')
$mText = $mText.Replace('通过反射获取实体类型的 DomainEventbus 属性', '通过反射获取实体类型的 DomainEventBus 属性')
$mText = $mText.Replace('值：(DomainEventbus 属性, ClearDomainEvents 方法)', '值：(DomainEventBus 属性, ClearDomainEvents 方法)')
[IO.File]::WriteAllText($mPath, $mText, $mEnc)
Write-Host "OK: $mPath"

Write-Host 'ALL DONE'
