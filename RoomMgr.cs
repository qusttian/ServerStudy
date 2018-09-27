using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    class RoomMgr
    {
        //房间列表
        public List<Room> roomList = new List<Room>();

        //单例
        public static RoomMgr instance;
        public RoomMgr()
        {
            instance = this;
        }

        //创建房间
        public void CreateRoom(Player player)
        {
            Room room = new Room();
            lock(roomList)
            {
                roomList.Add(room);
                room.AddPlayer(player);
            }
        }


        /// <summary>
        /// 玩家不在房间中，不存在离开房间的情况
        /// 获取玩家所在的房间，从房间中删除，如果房间已空，把房间删掉
        /// </summary>
        public void LeaveRoom(Player player)
        {
            PlayerTempData tempData = player.tempData;
            if(tempData.status == PlayerTempData.Status.None)
            {
                return;
            }
            Room room = tempData.room;
            lock(roomList)
            {
                room.DelPlayer(player.id);
                if(room.playerList.Count==0)
                {
                    roomList.Remove(room);
                }
            }
        }

        /// <summary>
        /// 构建返回房间列表协议GetRoomList,
        /// 第一个参数为房间数量count,紧接着附带count个房间数据，依次为房间里的玩家数量count,房间状态status
        /// </summary>
        /// <returns></returns>
        public ProtocolBytes GetRoomList()
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("GetRoomList");
            int count = roomList.Count;

            //往协议添加房间数量
            protocol.AddInt(count);

            //循环添加每个房间的信息
            for(int i=0;i<count;i++)
            {
                Room room = roomList[i];
                protocol.AddInt(room.playerList.Count);
                protocol.AddInt((int)room.status);
            }
            return protocol;
        }

    }
}
