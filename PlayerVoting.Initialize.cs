using Microsoft.Xna.Framework;
using OTAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace PlayerVoting
{
    public partial class PlayerVoting : TerrariaPlugin
    {
        private void OnGameUpdate(EventArgs args)
        {
            if (vote != null)
            {
                vote.Timer++;
                if (vote.type == VoteType.kick)
                {
                    //赞成票必须大于0.58并且(总人数不能少于3人,在创建vote就已经判断过了）
                    if (vote.voteFor * 1.0 / vote.voteAll > config.MiniPassingRateOfVoteForKick_kick投票活动最少通过率)
                    {
                        //先确定kickedPlayers里是否含有被踢玩家，确定是否是/vote name time指令生成的
                        Vplayer temp = kickedPlayers.Find(i => i.uuid == vote.kickedplayer.UUID);
                        //无time类型的kick投票
                        if (temp == null)
                        {
                            vote.kickedplayer.Kick("您已被投票踢出！", true);
                            TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出", new Color(0, 150, 255));
                            TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出", TraceLevel.Info);
                        }
                        //有time类型的kick投票
                        else
                        {
                            temp.isReal = true;
                            DateTime centuryBegin = new DateTime(2022, 9, 14);
                            long elapsedTicks = DateTime.Now.Ticks - centuryBegin.Ticks;
                            temp.ticks = elapsedTicks + temp.Timer * 10000000L;

                            vote.kickedplayer.Kick($"您已被投票踢出！kick维持时间：{temp.Timer}秒", true);
                            TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出，kick维持时间：{temp.Timer}秒", new Color(0, 150, 255));
                            TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出，kick维持时间：{temp.Timer}秒", TraceLevel.Info);
                        }
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        vote = null;//重置vote
                    }
                    else if (vote.voteFor + vote.voteAgainst == vote.voteAll)//如果都投票了，没有弃权的但是票未通过
                    {
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.kickedplayer.Name}]不会被踢出", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.kickedplayer.Name}]不会被踢出", TraceLevel.Info);
                        //由于未投出成功，kickPlayers里的东西移除
                        kickedPlayers.RemoveAll(i => i.uuid == vote.kickedplayer.UUID);
                        vote = null;//重置vote
                    }
                    else if (vote.Timer > 60 * config.CountdownToVoting_投票倒计时)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.kickedplayer.Name}]不会被踢出", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.kickedplayer.Name}]不会被踢出", TraceLevel.Info);
                        kickedPlayers.RemoveAll(i => i.uuid == vote.kickedplayer.UUID);
                        vote = null;//重置vote
                    }
                }
                else if (vote.type == VoteType.ban)
                {
                    //赞成票必须大于0.并且(总人数不能少于4人,在创建vote就已经判断过了）
                    if (vote.voteFor * 1.0 / vote.voteAll >= config.MiniPassingRateOfVoteForBan_ban投票活动最少通过率)
                    {
                        vote.banedplayer.Ban("您已被投票踢出！", true);
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.banedplayer.Name}]封禁", new Color(0, 150, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.banedplayer.Name}]封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                    else if (vote.voteFor + vote.voteAgainst == vote.voteAll)//如果都投票了，没有弃权的但是票未通过
                    {
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                    else if (vote.Timer > 60 * 15)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                }
            }
        }


        private void Vote(CommandArgs args)
        {
            if (!args.Parameters.Any())
            {
                args.Player.SendInfoMessage("指令错误，请输入 /vote help 来查看帮助");
                return;
            }

            //help
            if (args.Parameters[0] == "help" || args.Parameters[0] == "HELP")
            {
                args.Player.SendInfoMessage("输入 /vote kick [name] 来投票踢人");
                args.Player.SendInfoMessage("输入 /vote kick [name] [second] 来投票踢人，second指踢走后阻止他再次进入服务器的时间，单位 秒");
                args.Player.SendInfoMessage("输入 /vote ban [name] 来投票踢人");
                args.Player.SendInfoMessage("输入 /vote yes 或 /vote y 来投赞成票");
                args.Player.SendInfoMessage("输入 /vote no 或 /vote n 来投反对票");
                return;
            }

            //kicK投票
            if ((args.Parameters[0] == "kick" || args.Parameters[0] == "KICK") && args.Parameters.Count >= 2)
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (!tsplayers.Any())
                {
                    args.Player.SendInfoMessage("未找到该玩家");
                    return;
                }
                else if (!config.GroupsAreVoted_可以被通过的组.Contains(tsplayers.First().Group.Name))
                {
                    args.Player.SendInfoMessage($"玩家[{args.Player.Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.kick, args.Player);
                    if (vote.voteAll < config.MiniNumberOfVoteForKick_kick投票活动最少人数)
                    {
                        vote = null;
                        args.Player.SendErrorMessage("游戏人数太少，不能发起投票");
                    }
                    else
                    {
                        vote.kickedplayer = tsplayers.First();
                        //有time的类型指令，需要添加kickedplayers
                        if (args.Parameters.Count == 3)
                        {
                            if (!long.TryParse(args.Parameters[2], out long r))
                            {
                                vote = null;
                                args.Player.SendInfoMessage("输入错误，可能是拼写错误或数字溢出，请输入 /vote help 来查看帮助");
                                return;
                            }
                            else
                            {
                                //吧被踢玩家加入被踢玩家数组中
                                kickedPlayers.Add(new Vplayer(vote.kickedplayer.UUID, vote.kickedplayer.Name, r));
                                TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{tsplayers.First().Name}]，kick维持时常：{r}秒", new Color(255, 168, 0));
                                TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{tsplayers.First().Name}]，kick维持时常：{r}秒", TraceLevel.Info);
                            }
                        }
                        //无time类型的指令
                        else if (args.Parameters.Count == 2)
                        {
                            TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{tsplayers.First().Name}]", new Color(255, 168, 0));
                            TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{tsplayers.First().Name}]", TraceLevel.Info);
                        }
                    }
                }
                return;
            }

            //yes
            if (args.Parameters[0] == "yes" || args.Parameters[0] == "YES" || args.Parameters[0] == "y" || args.Parameters[0] == "Y")
            {
                if (vote != null)
                {
                    bool haspower = false;//没权限投票的人的标记
                    for (int i = 0; i < vote.votePlayers.Count; i++)
                    {
                        //只有同uuid并未投票状态（就是弃权）的玩家可以投票
                        if (vote.votePlayers[i].uuid == args.Player.UUID && vote.votePlayers[i].vr == VoteResults.abstain)
                        {
                            haspower = true;
                            vote.voteFor++;
                            vote.votePlayers[i].vr = VoteResults.Yes;
                            args.Player.SendMessage("投票成功！(您的投票已保密)", Color.LimeGreen);
                            TSPlayer.All.SendInfoMessage($"目前已有：{vote.voteAgainst + vote.voteFor}人投票，赞成：{vote.voteFor}，反对：{vote.voteAgainst}，未投：{vote.voteAll - vote.voteFor - vote.voteAgainst}，总票数：{vote.voteAll}，倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒");
                            break;
                        }
                        else if (vote.votePlayers[i].uuid == args.Player.UUID && vote.votePlayers[i].vr != VoteResults.abstain)
                        {
                            haspower = true;
                            args.Player.SendInfoMessage("您已投过请不要重复投票");
                            break;
                        }
                    }
                    if (!haspower)
                    {
                        args.Player.SendInfoMessage("你没有权限投票!");
                    }
                }
                else
                {
                    args.Player.SendInfoMessage("目前没有任何投票活动！");
                }
                return;
            }

            //no
            if (args.Parameters[0] == "no" || args.Parameters[0] == "NO" || args.Parameters[0] == "n" || args.Parameters[0] == "N")
            {
                if (vote != null)
                {
                    bool haspower = false;
                    for (int i = 0; i < vote.votePlayers.Count; i++)
                    {
                        if (vote.votePlayers[i].uuid == args.Player.UUID && vote.votePlayers[i].vr == VoteResults.abstain)
                        {
                            haspower = true;
                            vote.voteAgainst++;
                            vote.votePlayers[i].vr = VoteResults.No;
                            args.Player.SendMessage("投票成功！(您的投票已保密)", Color.LimeGreen);
                            TSPlayer.All.SendInfoMessage($"目前已有：{vote.voteAgainst + vote.voteFor}人投票，赞成：{vote.voteFor}，反对：{vote.voteAgainst}，未投：{vote.voteAll - vote.voteFor - vote.voteAgainst}，总票数：{vote.voteAll}，倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒");
                            break;
                        }
                        else if (vote.votePlayers[i].uuid == args.Player.UUID && vote.votePlayers[i].vr != VoteResults.abstain)
                        {
                            haspower = true;
                            args.Player.SendInfoMessage("您已投过请不要重复投票");
                            break;
                        }
                    }
                    if (!haspower)
                    {
                        args.Player.SendInfoMessage("你没有权限投票!");
                    }
                }
                else
                {
                    args.Player.SendInfoMessage("目前没有任何投票活动！");
                }
                return;
            }

            //ban
            if ((args.Parameters[0] == "ban" || args.Parameters[0] == "BAN") && args.Parameters.Count >= 2)
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (!tsplayers.Any())
                {
                    args.Player.SendInfoMessage("未找到该玩家");
                    return;
                }
                else if (!config.GroupsAreVoted_可以被通过的组.Contains(tsplayers.First().Group.Name))
                {
                    args.Player.SendInfoMessage($"玩家[{args.Player.Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.ban, args.Player);
                    if (vote.voteAll < config.MiniNumberOfVoteForBan_ban投票活动最少人数)
                    {
                        vote = null;
                        args.Player.SendErrorMessage("游戏人数太少，不能发起投票");
                    }
                    else
                    {
                        vote.banedplayer = tsplayers.First();
                        TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：是否 ban 玩家 [{tsplayers.First().Name}]", new Color(255, 168, 0));
                        TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：是否 ban 玩家 [{tsplayers.First().Name}]", TraceLevel.Info);
                    }
                }
                return;
            }

            //其他乱输入的
            args.Player.SendInfoMessage("指令错误，请输入 /vote help 来查看帮助");
        }


        private void ClearVotePlayer(CommandArgs args)
        {
            if (!args.Parameters.Any())
            {
                args.Player.SendInfoMessage("指令错误，请输入 /clearvp help 来查看帮助");
                return;
            }

            //help
            if (args.Parameters[0] == "help" || args.Parameters[0] == "HELP")
            {
                args.Player.SendInfoMessage("输入 /clearv kick [name] 来解除kick投票对被踢玩家的维持时间");
                args.Player.SendInfoMessage("输入 /clearv kickall 来解除kick投票对所有被踢玩家的维持时间");
                return;
            }

            if (args.Parameters.Count == 2)
            {
                if (args.Parameters[0] == "kick" || args.Parameters[0] == "KICK")
                {
                    List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                    if (!tsplayers.Any())
                    {
                        args.Player.SendInfoMessage("未找到该玩家");
                        return;
                    }
                    else
                    {
                        kickedPlayers.RemoveAll(i => i.name == tsplayers.First().Name);
                        args.Player.SendMessage($"玩家[{tsplayers.First().Name}]的kick投票的维持时间已消除", Color.LimeGreen);
                    }
                }
                return;
            }

            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0] == "kickall" || args.Parameters[0] == "KICKALL")
                {
                    kickedPlayers = new List<Vplayer>();
                    args.Player.SendMessage($"所有玩家的kick投票的维持时间已消除", Color.LimeGreen);
                }
                return;
            }

            args.Player.SendInfoMessage("输入 /clearv help 来查看帮助");
        }


        private HookResult OnPreUpdate(Player player, ref int i)
        {
            if (vote == null)
                return HookResult.Continue;

            if (vote.type == VoteType.kick)
            {
                bool hastheplayer = false;
                foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                {
                    if (temp.uuid == TShock.Players[player.whoAmI].UUID)//查看该玩家是否有权限投票
                    {
                        hastheplayer = true;
                    }
                    if (temp.uuid == TShock.Players[player.whoAmI].UUID && temp.vr != VoteResults.abstain)
                    {
                        return HookResult.Continue;//投过了，不再向该玩家发送信息
                    }
                }
                //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                if (hastheplayer && vote.Timer % 60 == 0)
                {
                    TSPlayer tsplayer = TShock.Players[player.whoAmI];
                    string text = $"kick投票：\n请选择 [/vote yes /vote y 或 /vote no /vote n] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                    tsplayer.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(0, 255, 255).packedValue, player.Center.X, player.Center.Y);
                }
            }
            else if (vote.type == VoteType.ban)
            {
                bool hastheplayer = false;
                foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                {
                    if (temp.uuid == TShock.Players[player.whoAmI].UUID)//查看该玩家是否有权限投票
                    {
                        hastheplayer = true;
                    }
                    if (temp.uuid == TShock.Players[player.whoAmI].UUID && temp.vr != VoteResults.abstain)
                    {
                        return HookResult.Continue;//投过了，不再向该玩家发送信息
                    }
                }
                //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                if (hastheplayer && vote.Timer % 60 == 0)
                {
                    TSPlayer tsplayer = TShock.Players[player.whoAmI];
                    string text = $"ban投票：\n请选择 [/vote yes 或 /vote no] 倒计时：{15 - (int)(vote.Timer / 60)}秒";
                    tsplayer.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(0, 255, 255).packedValue, player.Center.X, player.Center.Y);
                }
            }

            return HookResult.Continue;
        }


        private void OnServerjoin(JoinEventArgs args)
        {
            //排除异常情况
            if (args == null || TShock.Players[args.Who] == null)
                return;

            Vplayer temp = kickedPlayers.Find(i => i.uuid == TShock.Players[args.Who].UUID);
            if (temp != null)
            {
                DateTime centuryBegin = new DateTime(2022, 9, 14);
                long elapsedTicks = DateTime.Now.Ticks - centuryBegin.Ticks;
                if (temp.ticks > elapsedTicks)
                {
                    TShock.Players[args.Who].Kick($"您还未到 kick 投票结果的维持时间，倒计时：{(temp.ticks - elapsedTicks) / 10000000L}秒");
                }
                else
                {
                    //成功进入后移除kick记录
                    kickedPlayers.RemoveAll(i => i.uuid == TShock.Players[args.Who].UUID);
                }
            }
        }


        private void OnReload(ReloadEventArgs e)
        {
            config = Config.LoadConfig();
        }
    }
}
