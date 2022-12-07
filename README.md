# PlayerVoting 泰拉瑞亚投票踢人插件
# 功能
添加投票系统，允许玩家投票踢人，限制被踢的人再次进入时的时间，或者ban人
# 权限和指令

- 权限1：playervote.vote
- 指令1.1：/vote help      【查询这类指令的帮助】
- 指令1.2: /vote kick name  【发起投票踢掉某人】
- 指令1.3: /vote kick name time   【发起投票踢掉某人，并且在time秒内，该玩家不能再次进入服务器，即时间阻碍】
- 指令1.4: /vote ban name   【发起投票ban掉某人】
- 指令1.5: /vote yes 或 /vote y       【在投票进行时，投出赞成票】
- 指令1.6: /vote no 或 /vote n       【在投票进行时，投出反对票】
- 指令1.7: /vote event string     【向大家询问一些问题，投票观察赞成率】
- 指令1.8: /vote num [数字]      【生成一个随机数，如果 [数字] 不填，范围[0,100]，否则范围[0,数字]】
-
- 权限2: playervote.supervote
- 指令2.1: /svote kick name time   【发起投票踢掉离线的某人，并且在time秒内，该玩家不能再次进入服务器，即时间阻碍】
- 指令2.2: /svote ban name   【发起投票ban掉离线的某人】
-
- 权限3: playervote.clearv
- 指令3.1: /clearv help  【查询这类指令的帮助】
- 指令3.2: /clearv kick name    【清除被踢玩家的进服时间阻碍】
- 指令3.3: /clearv kickall    【清除所有被踢玩家的进服时间阻碍】

# 配置文件
```
{
  "MiniNumberOfVoteForKick_kick投票活动最少人数": 3,            //kick投票活动至少几人才可以发起
  "MiniPassingRateOfVoteForKick_kick投票活动最少通过率": 0.58,  //kick投票活动至少多少通过率才能通过
  "MiniNumberOfVoteForBan_ban投票活动最少人数": 5,              //ban投票活动至少几人才可以发起
  "MiniPassingRateOfVoteForBan_ban投票活动最少通过率": 0.798,   //ban投票活动至少多少通过率才可以通过
  "GroupsAreVoted_可以被通过的组": [                            //那些组可以被投票活动投出
    "guest",
    "default",
    "vip"
  ],
  "CountdownToVoting_投票倒计时": 20                            //每个投票活动倒计时，单位秒
}
```

# 特点
- 绑定uuid进行投票，每人只能投一票，即使玩家换号进服也不能再次投票
- 踢人也增对uuid进行踢，可自定义阻碍被踢者进服的时间
- 可通过配置文件自定义投票时间限制，通过率，限制人数，限制被踢的用户组等
- 投票记录写入tshock日志，直接搜索“投票”关键字即可，不记名投票
- 游客无权投票
- 即使被投出的玩家在结果出来前逃走，vote kick time 和 vote ban 都依然有效，仍可以限制和封禁
- svote 系列的投票能够直接封禁或阻碍不在线的玩家，但是必须全员通过才会起效，发起人数遵循文件配置
- event 活动不限制人数不遵循文件配置，仅是统计大家意见而已
