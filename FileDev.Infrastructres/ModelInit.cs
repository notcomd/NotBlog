using Microsoft.Extensions.DependencyInjection;

// 修正拼写错误的命名空间引用
using DomainCommon;
using FileDev.Domain.DomainEntities;
using FileDev.Domain.IRepository;
using FileDev.Infrastructres.Repository;

namespace FileDev.Infrastructres
{
    // 假设 IModuleInitializer 来自某个命名空间，如果编译报错需要添加对应 using
    public class ModelInit : IModuleInitializer
    {
        public void Initialize(IServiceCollection service)
        {
            service.AddScoped<IFileGroupRepository, FileGroupRepository>();
// 根据错误信息，当前 NotFileRepository 类型无法转换为 INotFileRepository 接口，需要检查 NotFileRepository 是否正确实现了 INotFileRepository 接口。
// 此处暂时移除该服务注册，等待实现正确转换后再添加。
            service.AddScoped<INotFileRepository, FileRepository>();
        }
    }
}
