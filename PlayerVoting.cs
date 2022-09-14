using Microsoft.Xna.Framework;
using OTAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace PlayerVoting
{
    [ApiVersion(2, 1)]
    public partial class PlayerVoting : TerrariaPlugin
    {

        public override string Author => "z枳";
        public override string Description => "玩家投票";
        public override string Name => "PlayerVoting";
        public override Version Version => new Version(1, 0, 0, 0);

        public Vote vote = null;
        //被踢的人数组，用来记录有冷却时间的player即：/vote kick name time，无冷却的不需要这个,即：/vote kick name
        public List<Vplayer> kickedPlayers = new List<Vplayer>();

        public static Config config;

        public PlayerVoting(Main game) : base(game)
        {
        }


        public override void Initialize()
        {
            config = Config.LoadConfig();
            Hooks.Player.PreUpdate += OnPreUpdate;
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.ServerJoin.Register(this, OnServerjoin);
            GeneralHooks.ReloadEvent += OnReload;



            //指令
            Commands.ChatCommands.Add(new Command("playervote.vote", Vote, "vote", "VOTE")
            {
                HelpText = "输入 /vote help 来查看帮助"
            });

            Commands.ChatCommands.Add(new Command("playervote.clearv", ClearVotePlayer, "clearv", "CLEARV")
            {
                HelpText = "输入 /clearv help 来查看帮助"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Hooks.Player.PreUpdate -= OnPreUpdate;
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerjoin);
                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
    }
}
