using System;
using System.Collections.Generic;
using TShockAPI;

namespace PlayerVoting
{
    public enum VoteType//投票类型
    {
        kick, ban, events, dbkick, dbban
    }

    public enum VoteResults//投票结果
    {
        Yes, No, abstain //赞成，反对，弃权
    }

    public class Vote
    {
        public VoteType type;//投票事件类型
        public TSPlayer initiator = null;//发起者
        public int voteFor;//赞成数
        public int voteAgainst;//反对数
        public int voteAll;//总投票人数（包括弃权）, 这里统计的是有权力投票的人数
        public double Timer;//计时器
        public List<Vplayer> votePlayers = new List<Vplayer>();//投票玩家，非event活动，这里保存者有投票权限（可能投了可能还没投）的玩家，event活动，这里塞已经投过票的玩家
        public string reason;//原因

        public TSPlayer kickedplayer = null;//kick投票中，被投的人，其他情况一律为null
        public TSPlayer banedplayer = null;//ban投票中，被ban的人，其他情况一律为null
        public Vplayer dbkickedplayer = null;//dbkick投票中，被投的人，其他情况一律为null
        public Vplayer dbbanedplayer = null;//dbban投票中，被ban的人，其他情况一律为null


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">投票类型</param>
        /// <param name="initiator">发起者</param>
        public Vote(VoteType type, TSPlayer initiator, string reason)
        {
            this.type = type;
            Timer = 0;
            voteFor = 0;
            voteAgainst = 0;
            voteAll = 0;
            //如果是ban或kick类型的投票，需记录总投票人数
            if (type == VoteType.kick || type == VoteType.ban || type == VoteType.dbkick || type == VoteType.dbban)
            {
                foreach (TSPlayer ts in TShock.Players)
                {
                    if (ts != null && ts.IsLoggedIn && !ts.Group.Name.Equals("guest", StringComparison.OrdinalIgnoreCase))//游客不能投票，游客没权限发起投票，因为没有权限
                    {
                        votePlayers.Add(new Vplayer(ts.UUID, ts.Name, VoteResults.abstain));
                        voteAll++;
                    }
                }

            }
            this.reason = reason;
            this.initiator = initiator;
        }
    }


    //一个类，用于记录 投票玩家 和 被kick走玩家，被 ban 玩家的类
    public class Vplayer
    {
        //共享变量
        public string uuid;//用uuid绑定，防止多人刷票，或踢走多人
        public string name;
        //投票者变量
        public VoteResults vr;//投了什么票
        //被踢者变量
        public long Timer = 0; //封禁秒数
        public long ticks = 0;  //嘀嗒计时器

        /// <summary>
        /// 构造投票玩家
        /// </summary>
        /// <param name="u">投票玩家uuid</param>
        /// <param name="v">投票玩家投的什么</param>
        public Vplayer(string u, string n, VoteResults v)
        {
            uuid = u;
            name = n;
            vr = v;
        }

        /// <summary>
        /// 构造被踢玩家
        /// </summary>
        /// <param name="uuid">uuid</param>
        /// <param name="timer">被踢的计时器，在时间结束前不可再次进入服务器</param>
        public Vplayer(string u, string n, long t)
        {
            uuid = u;
            name = n;
            Timer = t;
            //以2020.1.1为基准点，要等到投票通过 嘀嗒计时器 才会开始记录时间，刚创建vote对象时，不确定是否通过，不能给ticks记录时刻
            ticks = 0;
        }


        /// <summary>
        /// 构造记录信息的人，用于dbbaned player或 dbkicked player
        /// </summary>
        /// <param name="uuid">uuid</param>
        /// <param name="name">name</param>
        public Vplayer(string uuid, string name)
        {
            this.uuid = uuid;
            this.name = name;
        }
    }
}
