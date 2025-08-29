namespace Identity.Domain.AggregatesModel.RoleAggregate
{
    public enum EnRoleStatus
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 禁用
        /// </summary>
        Disabled = 1,

        /// <summary>
        ///  异常
        /// </summary>
        Error = 3,

        /// <summary>
        /// 删除
        /// </summary>
        Deleted = 2
    }
}