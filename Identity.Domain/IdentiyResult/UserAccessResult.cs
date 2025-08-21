using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Domain.IdentiyResult
{
    public enum UserAccessResult
    {
        /// <summary>
        /// 成功
        /// </summary>
        Success = 0,
        /// <summary>
        /// 锁定
        /// </summary>
        Locked = 2,
        /// <summary>
        /// 错误
        /// </summary>
        Error = 3,
        /// <summary>
        /// 未激活
        /// </summary>
        NotActive = 4,
        /// <summary>
        /// 已存在
        /// </summary>
        AlreadyExists = 5,
        /// <summary>
        /// 邮箱未验证
        /// </summary>
        EmailNotVerify = 6,
        /// <summary>
        ///  不存在
        /// </summary>
        NotFund = 7,
        /// <summary>
        /// 权限不足
        /// </summary>
        InsufficientPermissions = 8,
        /// <summary>
        /// 未分配
        /// </summary>
        NotAssigned = 9,   

    }
}
