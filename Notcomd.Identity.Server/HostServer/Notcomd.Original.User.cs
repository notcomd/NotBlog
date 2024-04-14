using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using Notcomd.Identity.Server.Module;

namespace Notcomd.Identity.Server.HostServer
{
    public class Notcomd_Original_User : INotcomd_Original_User
    {
        private readonly UserManager<Notcomd_User_Module> _userManager;
        private readonly RoleManager<Notcomd_Role_Module> _roleManager;
        private readonly ILogger _Iloggers;
        private bool Running = true;

        public Notcomd_Original_User(UserManager<Notcomd_User_Module> userManager, RoleManager<Notcomd_Role_Module> roleManager, ILogger iloggers)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _Iloggers = iloggers;
        }

        public async Task WorkAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                while (Running)
                {
                    if (await _roleManager.RoleExistsAsync("admin") is false)
                    {
                        using var rolediv = new Notcomd_Role_Module() { Name = "admin" };
                        var datadiv = await _roleManager.CreateAsync(rolediv);
                        if (!datadiv.Succeeded)
                        {
                            _Iloggers.LogWarning($"INFO:Message[(っ °Д °;)っ创建角色失败！，时间:{DateTime.UtcNow}]");
                        }
                    }
                    var userdiv = await _userManager.FindByEmailAsync("admin@nmail.com");
                    if (userdiv is null)
                    {
                        using var div = new Notcomd_User_Module() { Email = "admin@nmail.com", UserName = "notcomd" };
                        userdiv = div;
                        var pont = await _userManager.CreateAsync(userdiv, "admin@nmail.com");
                        if (pont.Succeeded)
                        {
                            _Iloggers.LogWarning($"INFO:Message [ヽ(*。>Д<)o゜ 创建用户失败！{DateTime.UtcNow}");
                        }
                    }
                    if (await _userManager.IsInRoleAsync(userdiv, "admin"))
                    {
                        var result = await _userManager.AddToRoleAsync(userdiv, "admin");
                        if (result.Succeeded)
                        {
                            _Iloggers.LogError($"INFO:Message [ヽ(*。>Д<)o゜ 用户初始化失败! 时间：{DateTime.UtcNow}]");
                        }
                    }
                    _Iloggers.LogInformation($"INFO:Message [( •̀ ω •́ )✧ 用户初始化完成，请尽快修改密码和信息！时间：{DateTime.UtcNow}]");
                    Running = false;
                }
            }

        }
    }
}
