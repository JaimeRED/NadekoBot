﻿using Discord;
using Discord.Commands;
using NadekoBot.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Services;
using NadekoBot.Services.Database.Models;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Administration
{
    public partial class Administration
    {
        [Group]
        public class AutoAssignRoleCommands
        {
            private static Logger _log { get; }
            //guildid/roleid
            private static ConcurrentDictionary<ulong, ulong> AutoAssignedRoles { get; }

            static AutoAssignRoleCommands()
            {
                AutoAssignedRoles = new ConcurrentDictionary<ulong, ulong>(NadekoBot.AllGuildConfigs.Where(x => x.AutoAssignRoleId != 0)
                    .ToDictionary(k => k.GuildId, v => v.AutoAssignRoleId));
                _log = LogManager.GetCurrentClassLogger();
                NadekoBot.Client.UserJoined += (user) =>
                {
                    var t = Task.Run(async () =>
                    {
                        try
                        {
                            ulong roleId = 0;
                            AutoAssignedRoles.TryGetValue(user.Guild.Id, out roleId);

                            if (roleId == 0)
                                return;

                            var role = user.Guild.Roles.FirstOrDefault(r => r.Id == roleId);

                            if (role != null)
                                await user.AddRolesAsync(role).ConfigureAwait(false);
                        }
                        catch (Exception ex) { _log.Warn(ex); }
                    });
                    return Task.CompletedTask;
                };
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(GuildPermission.ManageRoles)]
            public async Task AutoAssignRole(IUserMessage umsg, [Remainder] IRole role = null)
            {
                var channel = (ITextChannel)umsg.Channel;

                GuildConfig conf;
                using (var uow = DbHandler.UnitOfWork())
                {
                    conf = uow.GuildConfigs.For(channel.Guild.Id, set => set);
                    if (role == null)
                    {
                        conf.AutoAssignRoleId = 0;
                        ulong throwaway;
                        AutoAssignedRoles.TryRemove(channel.Guild.Id, out throwaway);
                    }
                    else
                    {
                        conf.AutoAssignRoleId = role.Id;
                        AutoAssignedRoles.AddOrUpdate(channel.Guild.Id, role.Id, (key, val) => role.Id);
                    }

                    await uow.CompleteAsync().ConfigureAwait(false);
                }

                if (role == null)
                {
                    await channel.SendConfirmAsync("🆗 **Auto assign role** on user join is now **disabled**.").ConfigureAwait(false);
                    return;
                }

                await channel.SendConfirmAsync("✅ **Auto assign role** on user join is now **enabled**.").ConfigureAwait(false);
            }
        }
    }
}
