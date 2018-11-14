using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    public class Room
    {
        //房间状态
        public enum Status
        {
            Prepare = 1,
            Fight = 2,
        }


        public Status status = Status.Prepare;

        //玩家
        public int maxPlayers = 6;  //一个房间最多容纳6名玩家
        public Dictionary<string, Player> playerList = new Dictionary<string, Player>();


        //添加玩家
        public bool AddPlayer(Player player)
        {
            lock (playerList)
            {
                if (playerList.Count > maxPlayers)
                {
                    return false;
                }
                PlayerTempData tempData = player.tempData;
                tempData.room = this;
                tempData.team = SwitchTeam();
                tempData.status = PlayerTempData.Status.Room;

                if (playerList.Count == 0)
                {
                    tempData.isOwner = true;
                }
                string id = player.id;
                playerList.Add(id, player);
            }
            return true;
        }

        // 删除玩家
        public void DelPlayer(string id)
        {
            lock (playerList)
            {
                if (!playerList.ContainsKey(id))
                    return;
                bool isOwner = playerList[id].tempData.isOwner;
                playerList[id].tempData.status = PlayerTempData.Status.None;
                playerList.Remove(id);
                if (isOwner)
                {
                    //如果房主离开房间，则重新选取房主
                    UpdateOwner();
                }
            }
        }

        /// <summary>
        /// 更换房主，把所有玩家的isOwner设为false,再把列表中第一位玩家的isOwner设为true
        /// </summary>
        public void UpdateOwner()
        {
            lock (playerList)
            {
                if (playerList.Count <= 0)
                {
                    return;
                }
                foreach (Player player in playerList.Values)
                {
                    player.tempData.isOwner = false;
                }
                Player p = playerList.Values.First();
                p.tempData.isOwner = true;
            }
        }

        //房间信息
        public ProtocolBytes GetRoomInfo()
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("GetRoomInfo");

            //添加房间玩家数量
            protocol.AddInt(playerList.Count);

            //循环添加每个玩家的信息
            foreach (Player p in playerList.Values)
            {
                protocol.AddString(p.id);
                protocol.AddInt(p.tempData.team);
                protocol.AddInt(p.data.win);
                protocol.AddInt(p.data.fail);
                int isOwner = p.tempData.isOwner ? 1 : 0;
                protocol.AddInt(isOwner);
            }
            return protocol;

        }

        //广播
        public void Broadcast(ProtocolBase protocol)
        {
            foreach (Player player in playerList.Values)
            {
                player.Send(protocol);
            }
        }

        //分配队伍,计算两队的人数，返回人数较少的阵营
        public int SwitchTeam()
        {
            int count1 = 0;
            int count2 = 0;
            foreach (Player player in playerList.Values)
            {
                if (player.tempData.team == 1) count1++;
                if (player.tempData.team == 2) count2++;
            }
            if (count1 <= count2)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }

        //房间能否开战
        public bool IsCanStart()
        {
            if (status != Status.Prepare)
                return false;
            int count1 = 0;
            int count2 = 0;
            foreach(Player player in playerList.Values)
            {
                if (player.tempData.team == 1)
                    count1++;
                if (player.tempData.team == 2)
                    count2++;      
            }
            if (count1 < 1 || count2 < 1)
                return false;
            return true;
        }

        //开启战斗
        public void StartFight()
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("Fight");
            status = Status.Fight;
            int teamPos1 = 1;
            int teamPos2 = 1;
            lock(playerList)
            {
                protocol.AddInt(playerList.Count);
                foreach(Player p in playerList.Values)
                {
                    p.tempData.hp = 200;
                    protocol.AddString(p.id);
                    protocol.AddInt(p.tempData.team);
                    if(p.tempData.team==1)
                    {
                        protocol.AddInt(teamPos1++);
                    }
                    else
                    {
                        protocol.AddInt(teamPos2++);
                    }
                    p.tempData.status = PlayerTempData.Status.Fight;
                }
                Broadcast(protocol);
            }
        }

        //胜负判断
        private int IsWin()
        {
            if(status != Status.Fight)
            {
                return 0; //尚未分出结果
            }
            int count1 = 0;
            int count2 = 0;
            foreach(Player player in playerList.Values)
            {
                PlayerTempData pt = player.tempData;
                if (pt.team == 1 && pt.hp > 0)
                    count1++;
                if (pt.team == 2 && pt.hp > 0)
                    count2++;
            }
            if (count1 <= 0)
                return 2;
            if (count2 <= 0)
                return 1;
            return 0;
        }

        //更新输赢状态
        public void UpdateWin()
        {
            int isWin = IsWin();
            if (isWin == 0)
                return;
            //改变状态 数值处理
            lock(playerList)
            {
                status = Status.Prepare;
                foreach(Player player in playerList.Values)
                {
                    player.tempData.status = PlayerTempData.Status.Room;
                    if (player.tempData.team == isWin)
                        player.data.win++;
                    else
                        player.data.fail++;
                }
            }

            Console.WriteLine("开始广播战斗结果");
            //广播
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("Result");
            protocol.AddInt(isWin);
            Broadcast(protocol);
        }

        //中途退出战斗
        public void ExitFight(Player player)
        {
            //摧毁坦克
            if (playerList[player.id] != null)
                playerList[player.id].tempData.hp = -1;

            //广播消息
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("Hit");
            protocolRet.AddString(player.id);
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(999);
            Broadcast(protocolRet);

            //增加失败次数
            if (IsWin() == 0)
                player.data.fail++;
            //胜负判断
            UpdateWin();
        }

    }
}
