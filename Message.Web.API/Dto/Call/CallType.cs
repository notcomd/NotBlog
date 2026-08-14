namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话类型（语音 / 视频）。
/// </summary>
public enum CallType
{
    /// <summary>
    /// 语音通话（仅音频流）
    /// </summary>
    Audio = 0,

    /// <summary>
    /// 视频通话（音频 + 视频流）
    /// </summary>
    Video = 1
}
