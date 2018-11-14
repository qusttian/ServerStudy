using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    public partial class HandlePlayerMsg
    {
        /// <summary>
        /// 获取房间列表
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgGetRoomList(Player player,ProtocolBase protocolBase)
        {
            player.Send(RoomMgr.instance.GetRoomList());
        }



        /// <summary>
        /// 创建房间
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgCreateRoom(Player player,ProtocolBase protocolBase)
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("CreateRoom");

            //条件检测,如果玩家的状态是在房间中或是战斗中，则不能创建房间，返回创建房间失败
            if(player.tempData.status!=PlayerTempData.Status.None)
            {
                Console.WriteLine("MsgCreateRoom Fail" + player.id);
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            //创建房间并返回成功信息
            RoomMgr.instance.CreateRoom(player);
            protocol.AddInt(0);
            player.Send(protocol);
            Console.WriteLine("MsgCreateRoom OK" + player.id);
        }


        /// <summary>
        /// 加入房间
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgEnterRoom(Player player,ProtocolBase protocolBase)
        {
            //获取数值
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            int index = protocol.GetInt(start, ref start);
            Console.WriteLine("[收到 MsgEnterRoom] " + player.id + "  加入房间号： " + index);

            //构建进入房间的返回协议
            protocol = new ProtocolBytes();
            protocol.AddString("EnterRoom");

            //判断房间是否存在
            if(index<0 || index>=RoomMgr.instance.roomList.Count)
            {
                Console.WriteLine("MsgEnterRoom index error " + player.id);
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }


            //判断房间的状态,只有处于“准备中”的房间才允许玩家加入
            Room room = RoomMgr.instance.roomList[index];
            if(room.status!=Room.Status.Prepare)
            {
                Console.WriteLine("MsgEnterRoom status error " + player.id);
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            //把玩家添加到房间,加入成功返回 0，加入失败返回 -1
            if(room.AddPlayer(player))
            {
                room.Broadcast(room.GetRoomInfo());
                protocol.AddInt(0);
                player.Send(protocol);
            }
            else
            {
                Console.WriteLine("MsgEnterRoom maxPlayer error " + player.id);
                protocol.AddInt(-1);
                player.Send(protocol);
            }
        }

        /// <summary>
        /// 获取房间信息
        /// 如果玩家不在房间中，无需获取房间信息
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgGetRoomInfo(Player player, ProtocolBase protocolBase)
        {
            if(player.tempData.status!=PlayerTempData.Status.Room)
            {
                Console.WriteLine("MsgGetRoomInfo status error " + player.id);
                return;
            }

            Room room = player.tempData.room;
            player.Send(room.GetRoomInfo());
        }


        /// <summary>
        /// 离开房间
        /// </summary>
        /// <param name="player"></param>
        /// <param name="protocolBase"></param>
        public void MsgLeaveRoom(Player player,ProtocolBase protocolBase)
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("LeaveRoom");

            //条件检测,如果玩家不在房间，则返回 -1，表示离开房间失败
            if(player.tempData.status!=PlayerTempData.Status.Room)
            {
                Console.WriteLine("MsgLeaveRoom status error " + player.id);
                protocol.AddInt(-1);
                player.Send(protocol);
                return;
            }

            //处理,返回 0 表示离开房间成功
            protocol.AddInt(0);
            player.Send(protocol);
            Room room = player.tempData.room;
            RoomMgr.instance.LeaveRoom(player);
            if(room ==null)
            {
                room.Broadcast(room.GetRoomInfo());
            }
        }

    }
}
