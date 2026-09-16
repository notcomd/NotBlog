using System.Text.Json.Serialization;

namespace Message.Web.API.Dto.Call;

/// <summary>
/// 通话形态（即时呼叫 / 常驻房间）。
/// <para>前端契约以字符串（"Instant"/"Room"）收发该枚举（与 <see cref="CallType"/> 同风格），
/// 故标注 JsonStringEnumConverter 使线缆格式与前端一致（勿移除）。</para>
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CallRoomKind
{
    /// <summary>
    /// 即时呼叫（1:1 私聊，响铃 → 接通 → 挂断即结束）
    /// </summary>
    Instant = 0,

    /// <summary>
    /// 常驻房间（群组/频道语音房或视频房，创建者关闭才销毁，成员可自由加入）
    /// </summary>
    Room = 1
}
