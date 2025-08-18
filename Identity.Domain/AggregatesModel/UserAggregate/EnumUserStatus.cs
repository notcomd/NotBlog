namespace Identity.Domain.AggregatesModel.UserAggregate
{
    public enum EnumUserStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 锁定
        /// </summary>
        Locked = 1,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// 删除
        /// </summary>
        Deleted = 3,

       /// <summary>
       ///  未激活
       /// </summary>
       UnActive = 4,
    }
}