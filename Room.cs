using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    class Room
    {
        //房间状态
        public enum Status
        {
            Prepare =1,
            Fight=2,
        }


        public Status status = Status.Prepare;

        //玩家
        public int maxPlayers = 6;  //一个房间最多容纳6名玩家
        public Dictionary<string, Player> playerList = new Dictionary<string, Player>();


        //添加玩家
        public bool AddPlayer(Player player)
        {
            lock(playerList)
            {
                if(playerList.Count>maxPlayers)
                {
                    return false;
                }
                PlayerTempData tempData = player.tempData;
                tempData.room = this;
                tempData.team = SwitchTeam();
                tempData.status = PlayerTempData.Status.Room;

                if(playerList.Count ==0)
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
            lock(playerList)
            {
                if (!playerList.ContainsKey(id))
                    return;
                bool isOwner = playerList[id].tempData.isOwner;
                playerList[id].tempData.status = PlayerTempData.Status.None;
                playerList.Remove(id);
                if(isOwner)
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
            lock(playerList)
            {
                if (playerList.Count <= 0)
                {
                    return;
                }
                foreach(Player player in playerList.Values)
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
            foreach(Player p in playerList.Values)
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
            foreach(Player player in playerList.Values)
            {
                player.Send(protocol);
            }
        }

        //分配队伍,计算两队的人数，返回人数较少的阵营
        public int SwitchTeam()
        {
            int count1 = 0;
            int count2 = 0;
            foreach(Player player in playerList.Values)
            {
                if (player.tempData.team == 1) count1++;
                if (player.tempData.team == 2) count2++;
            }
            if(count1<=count2)
            {
                return 1;
            }
            else
            {
                return 2;
            }
        }
    }
}
