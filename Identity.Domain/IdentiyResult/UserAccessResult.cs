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
        /// 用户不存在
        /// </summary>
        UserNotFound = 1,
        /// <summary>
        /// 用户被锁定
        /// </summary>
        UserLocked = 2,
        /// <summary>
        /// 用户密码错误
        /// </summary>
        PasswordError = 3,
        /// <summary>
        /// 用户未激活
        /// </summary>
        UserNotActive = 4,
        /// <summary>
        /// 用户已存在
        /// </summary>
        UserAlreadyExists = 5,
      


    }
}
