using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Notcomd.Identity.Server.Config;
using Notcomd.Identity.Server.IServer;
using Notcomd.Identity.Server.Model;

namespace Notcomd.Identity.Server.HostServer
{
    public class Notcomd_Original_User : INotcomd_Original_User
    {
        //private readonly UserManager<Notcomd_User_Module> _userManager;
        //private readonly RoleManager<Notcomd_Role_Module> _roleManager;
        private readonly INotcomd_Identity_Factory _Identity_Factory;
        private readonly ILogger _Iloggers;
        private readonly UserRole _userRole;
        private readonly IOptionsSnapshot<StatUserSetting> _optionsSnapshot;
        //private bool Running = true;

        public Notcomd_Original_User(IOptionsSnapshot<StatUserSetting> optionsSnapshot,UserRole userRole, ILogger<INotcomd_Original_User> iloggers, INotcomd_Identity_Factory identity_Factory)
        {
            _Identity_Factory = identity_Factory;
            _Iloggers = iloggers;
            _userRole = userRole;
            _optionsSnapshot=optionsSnapshot;
        }

        public async Task WorkAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!_Identity_Factory.IsFindUserAsync(_optionsSnapshot.Value.statUserEmail).Result)
                {

                    if (!await _Identity_Factory.RoleExistsAsync(_userRole.UserRoot))
                    {
                        var rolediv = new Notcomd_Role_Model { Name = _userRole.UserRoot };
                        var rd = await _Identity_Factory.CreateAsync(rolediv);
                        await _Identity_Factory.AddClaimAsync(new System.Security.Claims.Claim(ClaimTypes.Role, rolediv.Name), rolediv);
                        if (!rd.Succeeded)
                        {
                            _Iloggers.LogWarning($"INFO:Message[(っ °Д °;)っ创建角色失败！，时间:{DateTime.UtcNow}]");
                        }
                    }

                    var userdata = await _Identity_Factory.FindByEmailAsync(_optionsSnapshot.Value.statUserEmail);
                    if (!await _Identity_Factory.IsFindUserAsync(_optionsSnapshot.Value.statUserEmail))
                    {
                         userdata = new Notcomd_User_Model { UserName = _optionsSnapshot.Value.statUserEmail, Email = _optionsSnapshot.Value.statUserEmail };
                        var result = await _Identity_Factory.CreateAsync(userdata, _optionsSnapshot.Value.statPassword);
                        await _Identity_Factory.AddToRoleAsync(userdata, _userRole.UserRoot);
                        if (!result.Succeeded)
                        {
                            _Iloggers.LogWarning($"INFO:Message [ヽ(*。>Д<)o゜ 创建用户失败！{DateTime.UtcNow}");
                        }
                    }

                    if(await _Identity_Factory.IsInRoleAsync(userdata, _userRole.UserRoot))
                    {
                        var result = await _Identity_Factory.AddToRoleAsync(userdata, _userRole.UserRoot);
                        await _Identity_Factory.UpdateSecurityStampAsync(userdata);
                        if (result.Succeeded)
                        {
                            _Iloggers.LogError($"INFO:Message [ヽ(*。>Д<)o゜ 用户初始化失败! 时间：{DateTime.UtcNow}]");
                        }
                        _Iloggers.LogInformation($"INFO:Message [( •̀ ω •́ )✧ 用户初始化完成，请尽快修改密码和信息！时间：{DateTime.UtcNow}]");
                    }
                    
                }
                _Iloggers.LogInformation($"INFO:Message[(。・∀・)ノ用户以存在，无需创建！]");
            }

        }
    }
}
