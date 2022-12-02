using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace PlayerVoting
{
    public partial class PlayerVoting : TerrariaPlugin
    {
        /// <summary>
        /// 对投票进行加时和计算是否过期
        /// </summary>
        /// <param name="args"></param>
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
                            if (vote.kickedplayer != null && vote.kickedplayer.IsLoggedIn)
                            {
                                vote.kickedplayer.Kick("您已被投票踢出！", true);
                            }
                            else
                            {
                                TSPlayer.All.SendMessage("玩家已逃走，建议使用/vote kick time或/vote ban指令", new Color(255, 0, 0));
                            }
                            TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出", new Color(0, 150, 255));
                            TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.kickedplayer.Name}]踢出", TraceLevel.Info);
                        }
                        //有time类型的kick投票
                        else
                        {
                            DateTime centuryBegin = new DateTime(2022, 9, 14);
                            long elapsedTicks = DateTime.Now.Ticks - centuryBegin.Ticks;
                            temp.ticks = elapsedTicks + temp.Timer * 10000000L;
                            //如果该玩家还没逃走
                            if (vote.kickedplayer != null && vote.kickedplayer.IsLoggedIn)
                            {
                                vote.kickedplayer.Kick($"您已被投票踢出！kick维持时间：{temp.Timer}秒", true);
                            }
                            else//这里逃走对时间限制没影响
                            {
                                TSPlayer.All.SendMessage($"玩家已逃走！投票效果依然存在。kick维持时间：{temp.Timer}秒", new Color(3, 110, 6));
                            }
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
                    if (vote.voteFor * 1.0 / vote.voteAll > config.MiniPassingRateOfVoteForBan_ban投票活动最少通过率)
                    {
                        //如果这个破玩家还没跑
                        if (vote.banedplayer != null && vote.banedplayer.IsLoggedIn)
                        {
                            vote.banedplayer.Ban("您已被投票踢出！", "ServerConsole by vote " + vote.initiator.Name);
                            TSPlayer.All.SendMessage($"玩家已逃走！投票效果依然存在。已封禁！", new Color(3, 110, 6));
                        }
                        else//逃走了直接在数据库里写入
                        {
                            TShock.Bans.InsertBan("uuid:" + vote.banedplayer.UUID, "投票封禁", "ServerConsole by vote " + vote.initiator.Name, DateTime.UtcNow, DateTime.MaxValue);
                        }
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
                    else if (vote.Timer > 60 * config.CountdownToVoting_投票倒计时)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.banedplayer.Name}]不会被封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                }


                else if (vote.type == VoteType.events)
                {
                    if (vote.Timer > 60 * config.CountdownToVoting_投票倒计时)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        double d = 0;
                        if (vote.voteFor + vote.voteAgainst > 0)
                        {
                            d = vote.voteFor * 1.0 / (vote.voteFor + vote.voteAgainst) * 100;
                        }
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，通过率：{d.ToString("0.00")}%", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，通过率：{d.ToString("0.00")}%", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                }


                else if (vote.type == VoteType.dbban)
                {
                    //通过率必须100%(总人数不能少于x人,但这个在创建vote就已经判断过了）
                    if (vote.voteFor > 0 && vote.voteAll == vote.voteFor)
                    {
                        //如果这个破玩家还在服务器，偷偷换名进服捣乱
                        List<TSPlayer> ts = new List<TSPlayer>();
                        foreach (TSPlayer p in TShock.Players)
                        {
                            if (p != null && p.IsLoggedIn && p.UUID == vote.dbbanedplayer.uuid)
                            {
                                ts.Add(p);
                            }
                        }

                        if (ts.Count > 0)
                        {
                            ts.First().Ban("您已被投票封禁！", "ServerConsole by vote " + vote.initiator.Name);
                        }
                        else//离线直接在数据库里写入
                        {
                            TShock.Bans.InsertBan("uuid:" + vote.dbbanedplayer.uuid, "投票封禁", "ServerConsole by vote " + vote.initiator.Name, DateTime.UtcNow, DateTime.MaxValue);
                            TSPlayer.All.SendMessage($"离线封禁[{vote.dbbanedplayer.name}]", new Color(3, 110, 6));
                        }

                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbbanedplayer.name}]封禁", new Color(0, 150, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbbanedplayer.name}]封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                    else if (vote.voteFor + vote.voteAgainst == vote.voteAll)//如果都投票了，没有弃权的但是票未通过
                    {
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbbanedplayer.name}]不会被封禁", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbbanedplayer.name}]不会被封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                    else if (vote.Timer > 60 * config.CountdownToVoting_投票倒计时)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbbanedplayer.name}]不会被封禁", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbbanedplayer.name}]不会被封禁", TraceLevel.Info);
                        vote = null;//重置vote
                    }
                }


                else if (vote.type == VoteType.dbkick)
                {
                    //通过率必须100%(总人数不能少于3人,在创建vote就已经判断过了）
                    if (vote.voteFor > 0 && vote.voteAll == vote.voteFor)
                    {
                        //先确定kickedPlayers里是否含有被踢玩家，确定是否是/vote name time指令生成的
                        Vplayer temp = kickedPlayers.Find(i => i.uuid == vote.dbkickedplayer.uuid);
                        //无time类型的kick投票
                        if (temp == null)
                        {
                            TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbkickedplayer.name}]踢出。不可预料的错误，请联系插件作者", new Color(0, 150, 255));
                            TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbkickedplayer.name}]踢出。不可预料的错误，请联系插件作者", TraceLevel.Info);
                        }
                        //有time类型的dbkick投票
                        else
                        {
                            DateTime centuryBegin = new DateTime(2022, 9, 14);
                            long elapsedTicks = DateTime.Now.Ticks - centuryBegin.Ticks;
                            temp.ticks = elapsedTicks + temp.Timer * 10000000L;
                            //如果这个破玩家在服务器。（换名字进服捣乱是吧）
                            List<TSPlayer> ts = new List<TSPlayer>();
                            foreach (TSPlayer p in TShock.Players)
                            {
                                if (p != null && p.IsLoggedIn && p.UUID == vote.dbkickedplayer.uuid)
                                {
                                    ts.Add(p);
                                }
                            }
                            if (ts.Count > 0)
                            {
                                ts.First().Kick($"您已被投票踢出！kick维持时间：{temp.Timer}秒", true);
                            }
                            else//这里逃走对时间限制没影响
                            {
                                TSPlayer.All.SendMessage($"离线踢出。kick维持时间：{temp.Timer}秒", new Color(3, 110, 6));
                            }
                            TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbkickedplayer.name}]踢出，kick维持时间：{temp.Timer}秒", new Color(0, 150, 255));
                            TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票通过，已将[{vote.dbkickedplayer.name}]踢出，kick维持时间：{temp.Timer}秒", TraceLevel.Info);
                            
                        }
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        vote = null;//重置vote
                    }
                    else if (vote.voteFor + vote.voteAgainst == vote.voteAll)//如果都投票了，没有弃权的但是票未通过
                    {
                        TSPlayer.All.SendMessage("票数已决定本次投票结果，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbkickedplayer.name}]不会被踢出", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbkickedplayer.name}]不会被踢出", TraceLevel.Info);
                        //由于未投出成功，kickPlayers里的东西移除
                        kickedPlayers.RemoveAll(i => i.uuid == vote.dbkickedplayer.uuid);
                        vote = null;//重置vote
                    }
                    else if (vote.Timer > 60 * config.CountdownToVoting_投票倒计时)//如果超时了
                    {
                        TSPlayer.All.SendMessage("投票时间已截止，投票结束", new Color(255, 168, 0));
                        TSPlayer.All.SendMessage($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbkickedplayer.name}]不会被踢出", new Color(150, 0, 255));
                        TShock.Log.Write($"投票结果：{vote.voteFor}赞成，{vote.voteAgainst}反对，{vote.voteAll - vote.voteFor - vote.voteAgainst}弃权，投票未通过，[{vote.dbkickedplayer.name}]不会被踢出", TraceLevel.Info);
                        kickedPlayers.RemoveAll(i => i.uuid == vote.dbkickedplayer.uuid);
                        vote = null;//重置vote
                    }
                }
            }
        }


        /// <summary>
        /// 玩家头顶悬浮字体
        /// </summary>
        /// <param name="args"></param>
        private void OnGameUpdate2(EventArgs args)
        {
            if (vote == null)
                return;

            foreach (TSPlayer player in TShock.Players)
            {
                if (vote.type == VoteType.kick && player != null && player.IsLoggedIn)
                {
                    bool hastheplayer = false;
                    foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                    {
                        if (temp.uuid == player.UUID)//查看该玩家是否有权限投票
                        {
                            hastheplayer = true;
                        }
                        if (temp.uuid == player.UUID && temp.vr != VoteResults.abstain)
                        {
                            return;//投过了，不再向该玩家发送信息
                        }
                    }
                    //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                    if (hastheplayer && vote.Timer % 60 == 0)
                    {
                        string text = $"Kick投票：{vote.reason}\n请选择 [/vote yes 或 /vote no] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                        player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(0, 255, 125).packedValue, player.X, player.Y);
                    }
                }


                if (vote.type == VoteType.ban && player != null && player.IsLoggedIn)
                {
                    bool hastheplayer = false;
                    foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                    {
                        if (temp.uuid == player.UUID)//查看该玩家是否有权限投票
                        {
                            hastheplayer = true;
                        }
                        if (temp.uuid == player.UUID && temp.vr != VoteResults.abstain)
                        {
                            return;//投过了，不再向该玩家发送信息
                        }
                    }
                    //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                    if (hastheplayer && vote.Timer % 60 == 0)
                    {
                        string text = $"Ban投票：{vote.reason}\n请选择 [/vote yes 或 /vote no] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                        player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(0, 255, 0).packedValue, player.X, player.Y);
                    }
                }


                if (vote.type == VoteType.events && player != null && player.IsLoggedIn)
                {
                    foreach (TSPlayer tp in TShock.Players)
                    {
                        if (tp != null && tp.IsLoggedIn && !tp.Group.Name.Equals("guest", StringComparison.OrdinalIgnoreCase))
                        {
                            //从tshock里查是否有玩家已经投过（也就是说，votePlayers里存在这个玩家）
                            if (!vote.votePlayers.Exists(x => x.uuid == tp.UUID))
                            {//如果不存在，发消息催他投票
                                if (vote.Timer % 60 == 0)
                                {
                                    string text = $"Event投票：{vote.reason}\n请选择 [/vote yes 或 /vote no] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                                    player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(255, 255, 255).packedValue, player.X, player.Y);
                                }
                            }
                        }
                    }
                }


                if (vote.type == VoteType.dbkick && player != null && player.IsLoggedIn)
                {
                    bool hastheplayer = false;
                    foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                    {
                        if (temp.uuid == player.UUID)//查看该玩家是否有权限投票
                        {
                            hastheplayer = true;
                        }
                        if (temp.uuid == player.UUID && temp.vr != VoteResults.abstain)
                        {
                            return;//投过了，不再向该玩家发送信息
                        }
                    }
                    //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                    if (hastheplayer && vote.Timer % 60 == 0)
                    {
                        string text = $"离线Kick投票：{vote.reason}\n请选择 [/vote yes 或 /vote no] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                        player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(225, 125, 0).packedValue, player.X, player.Y);
                    }
                }


                if (vote.type == VoteType.dbban && player != null && player.IsLoggedIn)
                {
                    bool hastheplayer = false;
                    foreach (var temp in vote.votePlayers)//循环查找自己是否已投一票过
                    {
                        if (temp.uuid == player.UUID)//查看该玩家是否有权限投票
                        {
                            hastheplayer = true;
                        }
                        if (temp.uuid == player.UUID && temp.vr != VoteResults.abstain)
                        {
                            return;//投过了，不再向该玩家发送信息
                        }
                    }
                    //遍历一圈发现有权限投，但是还没投票，那么发送信息，60s发送一次
                    if (hastheplayer && vote.Timer % 60 == 0)
                    {
                        string text = $"离线Ban投票：{vote.reason}\n请选择 [/vote yes 或 /vote no] 倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒";
                        player.SendData(PacketTypes.CreateCombatTextExtended, text, (int)new Color(255, 0, 0).packedValue, player.X, player.Y);
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
            if (args.Parameters[0].Equals("help", StringComparison.OrdinalIgnoreCase) && args.Parameters.Count == 1)
            {
                args.Player.SendInfoMessage("输入 /vote kick [name]   来投票踢掉玩家\n" +
                                            "输入 /vote kick [name] [second]   来投票踢人，second 指踢走后禁止让他再次进入服务器的时间，单位秒\n" +
                                            "输入 /vote ban [name]   来投票封禁玩家\n" +
                                            "输入 /vote yes 或 /vote y   来投赞成票\n" +
                                            "输入 /vote event [你的意见]   来发起一场意见投票\n" +
                                            "输入 /vote num [Maxnum]   来生成一个随机数，范围 0 ~ Maxnum，如果不写Maxnum，默认范围 0 ~ 100\n" +
                                            "输入 /svote kick [name] [second]   来投票踢不在线的玩家，second 指禁止让他再次进入服务器的时间，单位秒\n" +
                                            "输入 /svote ban [name]   来投票封禁不在线的玩家\n");
                return;
            }

            //kicK投票
            if (args.Parameters[0].Equals("kick", StringComparison.OrdinalIgnoreCase) && (args.Parameters.Count == 2 || args.Parameters.Count == 3))
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (tsplayers.Count != 1)
                {
                    args.Player.SendInfoMessage($"未找到该玩家或玩家不唯一  [数目：{tsplayers.Count}]");
                    return;
                }
                else if (!config.GroupsAreVoted_可以被通过的组.Exists(x => x.Equals(tsplayers.First().Group.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    args.Player.SendInfoMessage($"玩家[{tsplayers.First().Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.kick, args.Player, $"是否踢出 [{tsplayers.First().Name}]");
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
                                args.Player.SendInfoMessage("输入错误，请输入合理的数字，可能是因为拼写错误或数字溢出，请输入 /vote help 来查看帮助");
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
            if (args.Parameters[0].Equals("yes", StringComparison.OrdinalIgnoreCase) || args.Parameters[0].Equals("y", StringComparison.OrdinalIgnoreCase))
            {
                if (vote != null)
                {
                    //kick和ban dbkick dbban类型的
                    if (vote.type == VoteType.kick || vote.type == VoteType.ban || vote.type == VoteType.dbkick || vote.type == VoteType.dbban)
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

                    //events类型的
                    else if (vote.type == VoteType.events)
                    {
                        if (args.Player == null || !args.Player.IsLoggedIn || args.Player.Group.Name.Equals("guest", StringComparison.OrdinalIgnoreCase))
                        {
                            args.Player.SendInfoMessage("你没有权限投票!");
                            return;
                        }
                        //投过了吗
                        bool isVote = false;
                        for (int i = 0; i < vote.votePlayers.Count; i++)
                        {
                            if (vote.votePlayers[i].uuid == args.Player.UUID)//这个情况events下votePlayers只记录投过票的玩家，而不是所有可以投票的玩家，因此vote.votePlayers[i].vr没有意义
                                isVote = true;
                        }
                        if (isVote)
                        {
                            args.Player.SendInfoMessage("您已投过请不要重复投票");
                            return;
                        }
                        else
                        {
                            vote.votePlayers.Add(new Vplayer(args.Player.UUID, args.Player.Name, VoteResults.Yes));
                            vote.voteFor++;
                            args.Player.SendMessage("投票成功！(您的投票已保密)", Color.LimeGreen);
                            TSPlayer.All.SendInfoMessage($"目前已有：{vote.voteAgainst + vote.voteFor}人投票，赞成：{vote.voteFor}，反对：{vote.voteAgainst}，通过率：{(vote.voteFor * 1.0f / (vote.voteFor + vote.voteAgainst) * 100).ToString("0.00")}%，倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒");
                        }
                    }
                }
                else
                {
                    args.Player.SendInfoMessage("目前没有任何投票活动！");
                }
                return;
            }

            //no
            if (args.Parameters[0].Equals("no", StringComparison.OrdinalIgnoreCase) || args.Parameters[0].Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                if (vote != null)
                {
                    //kick和ban dbkick dbban类型的
                    if (vote.type == VoteType.kick || vote.type == VoteType.ban || vote.type == VoteType.dbkick || vote.type == VoteType.dbban)
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


                    //events类型的
                    else if (vote.type == VoteType.events)
                    {
                        if (args.Player == null || !args.Player.IsLoggedIn || args.Player.Group.Name.Equals("guest", StringComparison.OrdinalIgnoreCase))
                        {
                            args.Player.SendInfoMessage("你没有权限投票!");
                            return;
                        }
                        //投过了吗
                        bool isVote = false;
                        for (int i = 0; i < vote.votePlayers.Count; i++)
                        {
                            if (vote.votePlayers[i].uuid == args.Player.UUID)//这个情况events下votePlayers只记录投过票的玩家，而不是所有可以投票的玩家，因此vote.votePlayers[i].vr没有意义
                                isVote = true;
                        }
                        if (isVote)
                        {
                            args.Player.SendInfoMessage("您已投过请不要重复投票");
                            return;
                        }
                        else
                        {
                            vote.votePlayers.Add(new Vplayer(args.Player.UUID, args.Player.Name, VoteResults.No));
                            vote.voteAgainst++;
                            args.Player.SendMessage("投票成功！(您的投票已保密)", Color.LimeGreen);
                            TSPlayer.All.SendInfoMessage($"目前已有：{vote.voteAgainst + vote.voteFor}人投票，赞成：{vote.voteFor}，反对：{vote.voteAgainst}，通过率：{(vote.voteFor * 1.0f / (vote.voteFor + vote.voteAgainst) * 100).ToString("0.00")}%，倒计时：{config.CountdownToVoting_投票倒计时 - (int)(vote.Timer / 60)}秒");
                        }
                    }
                }
                else
                {
                    args.Player.SendInfoMessage("目前没有任何投票活动！");
                }
                return;
            }

            //ban
            if ((args.Parameters[0].Equals("ban", StringComparison.OrdinalIgnoreCase)) && args.Parameters.Count == 2)
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (tsplayers.Count != 1)
                {
                    args.Player.SendInfoMessage($"未找到该玩家或玩家不唯一  [数目：{tsplayers.Count}]");
                    return;
                }
                else if (!config.GroupsAreVoted_可以被通过的组.Exists(x => x.Equals(tsplayers.First().Group.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    args.Player.SendInfoMessage($"玩家[{tsplayers.First().Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.ban, args.Player, $"是否封禁 [{tsplayers.First().Name}]");
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
                    return;
                }
            }

            //event 胡乱事件投票
            if ((args.Parameters[0].Equals("event", StringComparison.OrdinalIgnoreCase)) && args.Parameters.Count == 2)
            {
                if (string.IsNullOrWhiteSpace(args.Parameters[1]))
                {
                    args.Player.SendInfoMessage("您不能发起毫无意义的投票！");
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.events, args.Player, args.Parameters[1]);
                    TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：{args.Parameters[1]}", new Color(255, 168, 0));
                    TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：{args.Parameters[1]}", TraceLevel.Info);
                    return;
                }
            }

            //num 掷出点数
            if ((args.Parameters[0].Equals("num", StringComparison.OrdinalIgnoreCase)) && (args.Parameters.Count == 1 || args.Parameters.Count == 2))
            {
                if (args.Parameters.Count == 1)
                {
                    args.Player.SendMessage($"玩家 [{args.Player.Name}] 掷出点数 {Main.rand.Next(101)}，范围[0, 100]", new Color(150, 100, 255));
                    TShock.Log.Write($"玩家 [{args.Player.Name}] 掷出点数 {Main.rand.Next(101)}", TraceLevel.Info);
                    return;
                }
                if (args.Parameters.Count == 2)
                {
                    if (int.TryParse(args.Parameters[1], out int value))
                    {
                        TShock.Log.Write($"玩家 [{args.Player.Name}] 掷出点数 {Main.rand.Next(value + 1)}，范围[0, {value}]", TraceLevel.Info);
                        args.Player.SendMessage($"玩家 [{args.Player.Name}] 掷出点数 {Main.rand.Next(value + 1)}，范围[0, {value}]", new Color(150, 100, 255));
                    }
                    else
                    {
                        args.Player.SendInfoMessage("指令错误，请输入数字");
                    }
                    return;
                }
            }


            //其他乱输入的
            args.Player.SendInfoMessage("指令错误，请输入 /vote help 来查看帮助");
        }


        private void SVote(CommandArgs args)
        {
            if (!args.Parameters.Any())
            {
                args.Player.SendInfoMessage("指令错误，请输入 /svote help 来查看帮助");
                return;
            }

            //svote help
            if (args.Parameters[0].Equals("help", StringComparison.OrdinalIgnoreCase) && args.Parameters.Count == 1)
            {
                args.Player.SendInfoMessage("输入 /svote kick [name] [second]   来投票踢不在线的玩家，second 指禁止让他再次进入服务器的时间，单位秒\n" +
                                            "输入 /svote ban [name]   来投票封禁不在线的玩家\n");
                return;
            }


            //svote ban
            if ((args.Parameters[0].Equals("ban", StringComparison.OrdinalIgnoreCase)) && args.Parameters.Count == 2)
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (tsplayers.Count != 0)
                {
                    args.Player.SendInfoMessage($"该玩家在线，请使用 /vote ban 系列指令");
                    return;
                }

                UserAccount userAccount = TShock.UserAccounts.GetUserAccountByName(args.Parameters[1]);
                if (userAccount == null)
                {
                    args.Player.SendInfoMessage($"该玩家不存在");
                    return;
                }


                if (!config.GroupsAreVoted_可以被通过的组.Exists(x => x.Equals(userAccount.Group, StringComparison.OrdinalIgnoreCase)))
                {
                    args.Player.SendInfoMessage($"玩家[{userAccount.Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.dbban, args.Player, $"是否封禁离线玩家 [{userAccount.Name}]");
                    if (vote.voteAll < config.MiniNumberOfVoteForBan_ban投票活动最少人数)
                    {
                        vote = null;
                        args.Player.SendErrorMessage("游戏人数太少，不能发起投票");
                    }
                    else
                    {
                        vote.dbbanedplayer = new Vplayer(userAccount.UUID, userAccount.Name);
                        TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：是否 ban 离线玩家 [{userAccount.Name}]", new Color(255, 168, 0));
                        TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：是否 ban 离线玩家 [{userAccount.Name}]", TraceLevel.Info);
                    }
                    return;
                }
            }


            //svote kicK投票
            if (args.Parameters[0].Equals("kick", StringComparison.OrdinalIgnoreCase) && args.Parameters.Count == 3)
            {
                List<TSPlayer> tsplayers = TSPlayer.FindByNameOrID(args.Parameters[1]);
                if (tsplayers.Count != 0)
                {
                    args.Player.SendInfoMessage($"该玩家在线，请使用 /vote kick 系列指令");
                    return;
                }

                UserAccount userAccount = TShock.UserAccounts.GetUserAccountByName(args.Parameters[1]);
                if (userAccount == null)
                {
                    args.Player.SendInfoMessage($"该玩家不存在");
                    return;
                }

                if (!config.GroupsAreVoted_可以被通过的组.Exists(x => x.Equals(userAccount.Group, StringComparison.OrdinalIgnoreCase)))
                {
                    args.Player.SendInfoMessage($"玩家[{userAccount.Name}]所在用户组禁止被投票通过！");
                    return;
                }
                else
                {
                    if (vote != null)
                    {
                        args.Player.SendInfoMessage("上个投票未结束，不能再次发起投票");
                        return;
                    }
                    vote = new Vote(VoteType.dbkick, args.Player, $"是否阻碍离线玩家 [{userAccount.Name}]");
                    if (vote.voteAll < config.MiniNumberOfVoteForKick_kick投票活动最少人数)
                    {
                        vote = null;
                        args.Player.SendErrorMessage("游戏人数太少，不能发起投票");
                    }
                    else
                    {
                        vote.dbkickedplayer = new Vplayer(userAccount.UUID, userAccount.Name);
                        //有time的类型指令，需要添加kickedplayers
                        if (args.Parameters.Count == 3)
                        {
                            if (!long.TryParse(args.Parameters[2], out long r))
                            {
                                vote = null;
                                args.Player.SendInfoMessage("输入错误，请输入合理的数字，可能是因为拼写错误或数字溢出，请输入 /svote help 来查看帮助");
                                return;
                            }
                            else
                            {
                                //把被踢玩家加入被踢玩家数组中
                                kickedPlayers.Add(new Vplayer(vote.dbkickedplayer.uuid, vote.dbkickedplayer.name, r));
                                TSPlayer.All.SendMessage($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{userAccount.Name}]，kick维持时常：{r}秒", new Color(255, 168, 0));
                                TShock.Log.Write($"玩家[{args.Player.Name}]发起了游戏内投票：是否 kick 玩家 [{userAccount.Name}]，kick维持时常：{r}秒", TraceLevel.Info);
                            }
                        }
                    }
                }
                return;
            }

            //其他乱输入的
            args.Player.SendInfoMessage("指令错误，请输入 /svote help 来查看帮助");
        }


        private void ClearVotePlayer(CommandArgs args)
        {
            if (!args.Parameters.Any())
            {
                args.Player.SendInfoMessage("指令错误，请输入 /clearvp help 来查看帮助");
                return;
            }

            //help
            if (args.Parameters[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                args.Player.SendInfoMessage("输入 /clearv kick [name] 来解除kick投票对被踢玩家的维持时间");
                args.Player.SendInfoMessage("输入 /clearv kickall 来解除kick投票对所有被踢玩家的维持时间");
                return;
            }

            if (args.Parameters.Count == 2)
            {
                if (args.Parameters[0].Equals("kick", StringComparison.OrdinalIgnoreCase))
                {
                    UserAccount userAccount = TShock.UserAccounts.GetUserAccountByName(args.Parameters[1]);
                    if (userAccount == null)
                    {
                        args.Player.SendInfoMessage("未找到该玩家");
                        return;
                    }
                    else
                    {
                        kickedPlayers.RemoveAll(i => i.uuid == userAccount.UUID);
                        args.Player.SendMessage($"玩家[{userAccount.Name}]的kick投票的维持时间已消除", Color.LimeGreen);
                    }
                }
                return;
            }

            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0].Equals("kickall", StringComparison.OrdinalIgnoreCase))
                {
                    kickedPlayers = new List<Vplayer>();
                    args.Player.SendMessage($"所有玩家的kick投票的维持时间已消除", Color.LimeGreen);
                }
                return;
            }
            args.Player.SendInfoMessage("输入 /clearv help 来查看帮助");
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
