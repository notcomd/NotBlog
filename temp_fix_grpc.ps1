$ErrorActionPreference = 'Stop'
$p = 'f:\NotBlog\FileDev.Web.API\Grpc\FileStorageServiceGRPC.cs'
$c = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
$n = 0
function R([string]$o, [string]$x) {
    $script:n++
    if (-not $c.Contains($o)) { Write-Output ("NOTFOUND[" + $script:n + "] " + $o); return }
    $script:c = $script:c.Replace($o, $x)
    Write-Output ("OK[" + $script:n + "]")
}

# ---- #13 异常泄露：catch 块返回安全通用错误消息 ----
R 'return new GetFileInfoResponse { Success = false, ErrorMessage = ex.Message };' 'return new GetFileInfoResponse { Success = false, ErrorMessage = "获取文件信息失败" };'
R 'return new ListUserFilesResponse { Success = false, ErrorMessage = ex.Message };' 'return new ListUserFilesResponse { Success = false, ErrorMessage = "获取文件列表失败" };'
R 'return new DeleteFileResponse { Success = false, ErrorMessage = ex.Message };' 'return new DeleteFileResponse { Success = false, ErrorMessage = "删除文件失败" };'
R 'return new UpdateFileInfoResponse { Success = false, ErrorMessage = ex.Message };' 'return new UpdateFileInfoResponse { Success = false, ErrorMessage = "更新文件信息失败" };'
R 'return new UploadFileResponse { Success = false, ErrorMessage = ex.Message };' 'return new UploadFileResponse { Success = false, ErrorMessage = "上传文件失败" };'
R 'return new InitChunkUploadResponse { Success = false, ErrorMessage = ex.Message };' 'return new InitChunkUploadResponse { Success = false, ErrorMessage = "初始化分片上传失败" };'
R 'return new GetChunkStatusResponse { Success = false, ErrorMessage = ex.Message };' 'return new GetChunkStatusResponse { Success = false, ErrorMessage = "查询分片状态失败" };'
R 'return new MergeChunksResponse { Success = false, ErrorMessage = ex.Message };' 'return new MergeChunksResponse { Success = false, ErrorMessage = "合并分片失败" };'
R 'return new CancelChunkUploadResponse { Success = false, ErrorMessage = ex.Message };' 'return new CancelChunkUploadResponse { Success = false, ErrorMessage = "取消分片上传失败" };'
R 'return new UploadImageResponse { Success = false, ErrorMessage = ex.Message };' 'return new UploadImageResponse { Success = false, ErrorMessage = "上传图片失败" };'
R 'return new GetImageInfoResponse { Success = false, ErrorMessage = ex.Message };' 'return new GetImageInfoResponse { Success = false, ErrorMessage = "获取图片信息失败" };'
R '                ErrorMessage = ex.Message' '                ErrorMessage = "上传分片失败"'

# ---- #37 ExpectedMd5 命名与实际算法(SHA256)不符：内部统一改用 expectedFileHash ----
R '            if (!string.IsNullOrEmpty(request.ExpectedMd5))' '            var expectedFileHash = request.ExpectedMd5; if (!string.IsNullOrEmpty(expectedFileHash))'
R 'if (!string.Equals(actualHash, request.ExpectedMd5, StringComparison.OrdinalIgnoreCase))' 'if (!string.Equals(actualHash, expectedFileHash, StringComparison.OrdinalIgnoreCase))'
R '{request.ExpectedMd5}' '{expectedFileHash}'
R 'ExpectedHash = request.ExpectedMd5' 'ExpectedHash = expectedFileHash'
R '包含用户ID、文件名、文件内容和预期MD5的请求' '包含用户ID、文件名、文件内容和预期文件哈希（SHA256）的请求'

# ---- #4 UploadFile 返回真实落库 FileId：SaveEntitiesAsync 后反查实体 ----
$old17 = '            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);' + "`r`n" + "`r`n" + '            return new UploadFileResponse'
$new17 = '            await _notFileRepository.UnitOfWork.SaveEntitiesAsync(context.CancellationToken);' + "`r`n" + "`r`n" + '            // 实体的 FileId 在实体构造函数中自生成（与文件存储路径所用的临时 fileGuid 不同），' + "`r`n" + '            // 必须反查落库实体，以真实 FileId 作为响应返回，保证客户端后续按 FileId 查询/删除一致' + "`r`n" + '            var savedFile = (await _notFileService.GetFilesByUserIdAsync(userId))' + "`r`n" + '                .FirstOrDefault(f => string.Equals(f.FileUri?.ToString(), fileUri.ToString(), StringComparison.Ordinal));' + "`r`n" + '            if (savedFile is null)' + "`r`n" + '                throw new InvalidOperationException("文件元数据保存失败，无法获取真实 FileId");' + "`r`n" + "`r`n" + '            return new UploadFileResponse'
R $old17 $new17

$old18 = 'FileId = fileGuid.ToString(),' + "`r`n" + '                FileUri = fileUri.ToString(),' + "`r`n" + '                FileMd5 = storageResult.ActualHash ?? string.Empty,' + "`r`n" + '                FileSize = content.Length'
$new18 = 'FileId = savedFile.FileId.ToString(),' + "`r`n" + '                FileUri = fileUri.ToString(),' + "`r`n" + '                FileMd5 = storageResult.ActualHash ?? string.Empty,' + "`r`n" + '                FileSize = content.Length'
R $old18 $new18

[System.IO.File]::WriteAllText($p, $c, (New-Object System.Text.UTF8Encoding($true)))
Write-Output ("TOTAL=" + $n)
