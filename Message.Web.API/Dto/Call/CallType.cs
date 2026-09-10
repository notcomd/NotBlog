using System.Text.Json.Serialization;

namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话类型（语音 / 视频）。
/// <para>前端契约以字符串（"Audio"/"Video"）收发该枚举（SignalR invoke 参数与推送 DTO 字段，
/// 见 call.ts / CallPanel.vue），故标注 JsonStringEnumConverter 使线缆格式与前端一致（勿移除）。</para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
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
