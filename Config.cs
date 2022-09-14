using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace PlayerVoting
{
    public class Config
    {
        static string configPath = Path.Combine(TShock.SavePath, "PlayerVotingConfig.json");

        public static Config LoadConfig()
        {
            if (!File.Exists(configPath))
            {
                Config config = new Config(3, 0.58, 5, 0.798, new List<string> { "guest", "default", "vip" }, 15);
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                return config;
            }
            else
            {
                Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
                return config;
            }
        }

        public Config(int n1, double d1, int n2, double d2, List<string> l1, int n3)
        {
            MiniNumberOfVoteForKick_kick投票活动最少人数 = n1;
            MiniPassingRateOfVoteForKick_kick投票活动最少通过率 = d1;
            MiniNumberOfVoteForBan_ban投票活动最少人数 = n2;
            MiniPassingRateOfVoteForBan_ban投票活动最少通过率 = d2;
            GroupsAreVoted_可以被通过的组 = l1;
            CountdownToVoting_投票倒计时 = n3;
        }

        public int MiniNumberOfVoteForKick_kick投票活动最少人数;
        public double MiniPassingRateOfVoteForKick_kick投票活动最少通过率;
        public int MiniNumberOfVoteForBan_ban投票活动最少人数;
        public double MiniPassingRateOfVoteForBan_ban投票活动最少通过率;
        public List<string> GroupsAreVoted_可以被通过的组;
        public int CountdownToVoting_投票倒计时;
    }
}
