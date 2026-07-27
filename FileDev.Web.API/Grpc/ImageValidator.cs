namespace FileDev.Web.API.Grpc;

/// <summary>
/// 图片格式验证器，基于文件头魔数（magic bytes）检测图片格式和尺寸。
/// 无需外部依赖，纯 .NET 实现。
/// </summary>
public static class ImageValidator
{
    /// <summary>
    /// 图片验证结果
    /// </summary>
    public readonly struct ValidationResult
    {
        public bool IsValid { get; init; }
        public string Format { get; init; }
        public string ErrorMessage { get; init; }
    }

    // 常见图片格式的魔数签名
    private static readonly Dictionary<string, byte[][]> ImageSignatures = new()
    {
        ["jpeg"] = [[0xFF, 0xD8, 0xFF]],
        ["png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        ["gif"] = [new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }],
        ["bmp"] = [[0x42, 0x4D]],
        ["webp"] = [[0x52, 0x49, 0x46, 0x46]], // RIFF container, need further check for WEBP
        ["ico"] = [[0x00, 0x00, 0x01, 0x00]],
    };

    /// <summary>
    /// 验证图片数据格式是否有效
    /// </summary>
    /// <param name="imageData">图片字节数据</param>
    /// <returns>验证结果</returns>
    public static ValidationResult Validate(byte[] imageData)
    {
        if (imageData is null || imageData.Length < 8)
            return new ValidationResult { IsValid = false, ErrorMessage = "图片数据过短，无法识别格式" };

        foreach (var (format, signatures) in ImageSignatures)
        {
            foreach (var signature in signatures)
            {
                if (MatchesSignature(imageData, signature))
                {
                    // WEBP 特殊处理：RIFF 容器，需要检查 WEBP 标识
                    if (format == "webp")
                    {
                        if (imageData.Length >= 12
                            && imageData[8] == 0x57 && imageData[9] == 0x45
                            && imageData[10] == 0x42 && imageData[11] == 0x50) // "WEBP"
                        {
                            return new ValidationResult { IsValid = true, Format = "webp" };
                        }
                        continue;
                    }
                    return new ValidationResult { IsValid = true, Format = format };
                }
            }
        }

        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = "不支持的图片格式，仅支持 JPEG、PNG、GIF、BMP、WebP、ICO"
        };
    }

    /// <summary>
    /// 获取图片尺寸（宽×高），仅支持 JPEG、PNG、GIF、BMP
    /// </summary>
    /// <param name="imageData">图片字节数据</param>
    /// <returns>(width, height)，解析失败返回 (0, 0)</returns>
    public static (int Width, int Height) GetDimensions(byte[] imageData)
    {
        if (imageData is null || imageData.Length < 8)
            return (0, 0);

        try
        {
            // JPEG: 搜索 SOF0 (0xFF 0xC0) 标记
            if (imageData[0] == 0xFF && imageData[1] == 0xD8)
                return GetJpegDimensions(imageData);

            // PNG: IHDR chunk 位于偏移 16 处
            if (imageData[0] == 0x89 && imageData[1] == 0x50)
                return GetPngDimensions(imageData);

            // GIF: 逻辑屏幕描述符位于偏移 6 处
            if (imageData[0] == 0x47 && imageData[1] == 0x49)
                return GetGifDimensions(imageData);

            // BMP: DIB header 位于偏移 18 处
            if (imageData[0] == 0x42 && imageData[1] == 0x4D)
                return GetBmpDimensions(imageData);
        }
        catch
        {
            // 解析失败返回默认值
        }

        return (0, 0);
    }

    private static bool MatchesSignature(byte[] data, byte[] signature)
    {
        if (data.Length < signature.Length)
            return false;
        for (int i = 0; i < signature.Length; i++)
        {
            if (data[i] != signature[i])
                return false;
        }
        return true;
    }

    private static (int Width, int Height) GetJpegDimensions(byte[] data)
    {
        int offset = 2;
        while (offset < data.Length - 1)
        {
            if (data[offset] != 0xFF)
                break;

            byte marker = data[offset + 1];

            // SOF markers contain dimensions
            if (marker >= 0xC0 && marker <= 0xC3
                || marker >= 0xC5 && marker <= 0xC7
                || marker >= 0xC9 && marker <= 0xCB
                || marker >= 0xCD && marker <= 0xCF)
            {
                if (offset + 8 < data.Length)
                {
                    int height = (data[offset + 5] << 8) | data[offset + 6];
                    int width = (data[offset + 7] << 8) | data[offset + 8];
                    return (width, height);
                }
                break;
            }

            // Skip markers without length
            if (marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
            {
                offset += 2;
                continue;
            }

            if (offset + 3 >= data.Length)
                break;

            int segmentLength = (data[offset + 2] << 8) | data[offset + 3];
            offset += 2 + segmentLength;
        }
        return (0, 0);
    }

    private static (int Width, int Height) GetPngDimensions(byte[] data)
    {
        if (data.Length < 24)
            return (0, 0);

        // Width at offset 16 (4 bytes big-endian), Height at offset 20
        int width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        int height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return (width, height);
    }

    private static (int Width, int Height) GetGifDimensions(byte[] data)
    {
        if (data.Length < 10)
            return (0, 0);

        // Width/Height at offset 6 and 8 (2 bytes little-endian)
        int width = data[6] | (data[7] << 8);
        int height = data[8] | (data[9] << 8);
        return (width, height);
    }

    private static (int Width, int Height) GetBmpDimensions(byte[] data)
    {
        if (data.Length < 26)
            return (0, 0);

        // Width at offset 18, Height at offset 22 (4 bytes little-endian)
        int width = data[18] | (data[19] << 8) | (data[20] << 16) | (data[21] << 24);
        int height = Math.Abs(data[22] | (data[23] << 8) | (data[24] << 16) | (data[25] << 24));
        return (width, height);
    }
}
