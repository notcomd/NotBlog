using Microsoft.Extensions.Logging;

namespace FileDev.Web.API.Grpc;

/// <summary>
/// 图片格式验证器，基于文件头魔数（magic bytes）检测图片格式和尺寸。
/// 无需外部依赖，纯 .NET 实现。
/// </summary>
/// <remarks>
/// <para><b>格式判定设计（单一来源）</b>：所有支持的图片魔数统一维护在
/// <see cref="ImageSignatures"/>，由 <see cref="ResolveFormat"/> 集中解析格式。
/// <see cref="Validate"/>（格式校验）与 <see cref="GetDimensions"/>（尺寸解析）
/// 均复用该解析结果，避免魔数在多个位置硬编码漂移。</para>
/// <para><b>新增图片格式步骤</b>：① 在 <see cref="ImageSignatures"/> 追加魔数签名；
/// ② 若需尺寸解析，在 <see cref="GetDimensions"/> 的 switch 中追加分发分支并实现
/// 对应解析方法；③ 若新格式与前缀存在冲突（如 WEBP 的 RIFF 容器），还需在
/// <see cref="ResolveFormat"/> 内做二次校验。</para>
/// </remarks>
public static class ImageValidator
{
    private static ILogger? _logger;

    /// <summary>
    /// 供宿主启动时注入日志工厂（静态工具类无法走构造注入）
    /// </summary>
    public static void Configure(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(typeof(ImageValidator));
    }

    /// <summary>
    /// 图片验证结果
    /// </summary>
    public readonly struct ValidationResult
    {
        public bool IsValid { get; init; }
        public string Format { get; init; }
        public string ErrorMessage { get; init; }
    }

    /// <summary>
    /// 常见图片格式的魔数签名（唯一来源）。键为格式名，与 <see cref="ResolveFormat"/>
    /// 返回的格式名一一对应；同一格式可有多个签名（如 GIF 分 87a/89a 两版）。
    /// 注意：WEBP 仅识别 RIFF 容器前缀，该前缀也被 AVI/WAV 使用，需在
    /// <see cref="ResolveFormat"/> 中二次校验 "WEBP" 标识。
    /// </summary>
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

        var format = ResolveFormat(imageData);
        return format is null
            ? new ValidationResult
            {
                IsValid = false,
                ErrorMessage = "不支持的图片格式，仅支持 JPEG、PNG、GIF、BMP、WebP、ICO"
            }
            : new ValidationResult { IsValid = true, Format = format };
    }

    /// <summary>
    /// 由魔数签名解析图片格式（单一来源，<see cref="Validate"/> 与 <see cref="GetDimensions"/> 共用）
    /// </summary>
    /// <remarks>
    /// WEBP 的签名是 RIFF 容器（"RIFF"），该前缀同样被 AVI/WAV 等格式使用，
    /// 因此匹配到 webp 签名后还需校验偏移 8-11 处为 "WEBP" 标识，避免误报。
    /// </remarks>
    /// <param name="imageData">图片字节数据</param>
    /// <returns>格式名（jpeg/png/gif/bmp/webp/ico），无法识别返回 null</returns>
    private static string? ResolveFormat(byte[] imageData)
    {
        foreach (var (format, signatures) in ImageSignatures)
        {
            foreach (var signature in signatures)
            {
                if (!MatchesSignature(imageData, signature))
                    continue;

                // WEBP 特殊处理：RIFF 容器，需要检查 WEBP 标识
                if (format == "webp")
                {
                    if (imageData.Length >= 12
                        && imageData[8] == 0x57 && imageData[9] == 0x45
                        && imageData[10] == 0x42 && imageData[11] == 0x50) // "WEBP"
                    {
                        return "webp";
                    }
                    continue;
                }

                return format;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取图片尺寸（宽×高），支持 JPEG、PNG、GIF、BMP、WebP、ICO
    /// </summary>
    /// <param name="imageData">图片字节数据</param>
    /// <returns>
    /// (Width, Height, IsSupported)：IsSupported=true 表示尺寸解析成功；
    /// false 表示格式无法识别、数据损坏或解析失败（此时 Width/Height 为 0）
    /// </returns>
    public static (int Width, int Height, bool IsSupported) GetDimensions(byte[] imageData)
    {
        if (imageData is null || imageData.Length < 8)
            return (0, 0, false);

        try
        {
            // 由 ResolveFormat 统一判定格式，避免魔数在 Validate/GetDimensions 两处重复维护
            var (width, height) = ResolveFormat(imageData) switch
            {
                "jpeg" => GetJpegDimensions(imageData), // SOF0 (0xFF 0xC0) 标记
                "png" => GetPngDimensions(imageData),   // IHDR chunk 位于偏移 16 处
                "gif" => GetGifDimensions(imageData),   // 逻辑屏幕描述符位于偏移 6 处
                "bmp" => GetBmpDimensions(imageData),   // DIB header 位于偏移 18 处
                "webp" => GetWebpDimensions(imageData), // VP8X/VP8/VP8L chunk 头部
                "ico" => GetIcoDimensions(imageData),   // ICONDIR 头部
                _ => (0, 0)                             // 无法识别的格式
            };

            // 尺寸解析成功（>0）才视为受支持，区分「合法但未知尺寸」与「解析失败」
            return (width, height, width > 0 && height > 0);
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            _logger?.LogDebug(ex, "图片尺寸解析失败: Length={Length}", imageData.Length);
            return (0, 0, false);
        }
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

    /// <summary>
    /// 解析 BMP 尺寸。宽/高为 4 字节小端整数，位于 DIB header 偏移 18/22 处。
    /// </summary>
    /// <remarks>
    /// <para><b>高度字段语义</b>：BMP 高度是有符号数，负值表示位图自上而下绘制
    /// （top-down），正值表示自下而上（bottom-up），解析时需取绝对值。</para>
    /// <para><b>溢出陷阱</b>：不能直接用 <c>Math.Abs(int.MinValue)</c>——当高度字段为
    /// <c>0x80000000</c> 时 <see cref="Math.Abs(int)"/> 会抛出
    /// <see cref="OverflowException"/>，导致整个尺寸解析失败返回 (0, 0)。
    /// 因此改用位掩码 <c>rawHeight &amp; 0x7FFFFFFF</c> 安全取绝对值。</para>
    /// </remarks>
    private static (int Width, int Height) GetBmpDimensions(byte[] data)
    {
        if (data.Length < 26)
            return (0, 0);

        // Width at offset 18, Height at offset 22 (4 bytes little-endian)
        int width = data[18] | (data[19] << 8) | (data[20] << 16) | (data[21] << 24);
        int rawHeight = data[22] | (data[23] << 8) | (data[24] << 16) | (data[25] << 24);
        int height = rawHeight < 0 ? rawHeight & 0x7FFFFFFF : rawHeight;
        return (width, height);
    }

    /// <summary>
    /// 解析 WebP 尺寸。容器为 RIFF/WEBP，尺寸位于首个 chunk 头部：
    /// <list type="bullet">
    /// <item>VP8X：偏移 22/25 处各 24 位小端（canvas 宽/高 - 1）</item>
    /// <item>VP8（有损）：同步码 0x9D 0x01 0x2A 后各 14 位小端</item>
    /// <item>VP8L（无损）：签名 0x2F 后 28 位打包（各 14 位，宽高 - 1）</item>
    /// </list>
    /// </summary>
    private static (int Width, int Height) GetWebpDimensions(byte[] data)
    {
        if (data.Length < 20)
            return (0, 0);

        // VP8X: "VP8X" + 4 字节保留/标志位 + 3 字节宽(-1) + 3 字节高(-1)
        if (data[12] == 0x56 && data[13] == 0x50 && data[14] == 0x38 && data[15] == 0x58)
        {
            if (data.Length < 28)
                return (0, 0);
            int width = 1 + (data[22] | (data[23] << 8) | (data[24] << 16));
            int height = 1 + (data[25] | (data[26] << 8) | (data[27] << 16));
            return (width, height);
        }

        // VP8 (lossy): "VP8 " + 帧标签 0x9D 0x01 0x2A + 2 字节宽 + 2 字节高（14 位，小端）
        if (data[12] == 0x56 && data[13] == 0x50 && data[14] == 0x38 && data[15] == 0x20)
        {
            if (data.Length < 27)
                return (0, 0);
            if (data[20] != 0x9D || data[21] != 0x01 || data[22] != 0x2A)
                return (0, 0);
            int width = (data[23] | (data[24] << 8)) & 0x3FFF;
            int height = (data[25] | (data[26] << 8)) & 0x3FFF;
            return (width, height);
        }

        // VP8L (lossless): "VP8L" + 签名 0x2F + 4 字节打包（低 14 位宽-1，次 14 位高-1）
        if (data[12] == 0x56 && data[13] == 0x50 && data[14] == 0x38 && data[15] == 0x4C)
        {
            if (data.Length < 25)
                return (0, 0);
            if (data[20] != 0x2F)
                return (0, 0);
            uint bits = (uint)(data[21] | (data[22] << 8) | (data[23] << 16) | (data[24] << 24));
            int width = 1 + (int)(bits & 0x3FFF);
            int height = 1 + (int)((bits >> 14) & 0x3FFF);
            return (width, height);
        }

        return (0, 0);
    }

    /// <summary>
    /// 解析 ICO 尺寸。ICONDIR 头部（6 字节）后第一项 ICONDIRENTRY（16 字节）
    /// 的宽/高各 1 字节（0 表示 256）。
    /// </summary>
    private static (int Width, int Height) GetIcoDimensions(byte[] data)
    {
        if (data.Length < 22)
            return (0, 0);

        int width = data[6];
        int height = data[7];
        return (width == 0 ? 256 : width, height == 0 ? 256 : height);
    }
}
