using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    public class HandlePlayerEvent
    {
        //上线
        public void OnLogin(Player player)
        {
            Scene.instance.AddPlayer(player.id);
        }

        //下线
        public void OnLogout(Player player)
        {
            if(player.tempData.status ==PlayerTempData.Status.Room)
            {
                Room room = player.tempData.room;
                RoomMgr.instance.LeaveRoom(player);
                if(room!=null)
                {
                    room.Broadcast(room.GetRoomInfo());
                }
            }
            //Scene.instance.DeletePlayer(player.id);

            //战斗中
            if(player.tempData.status==PlayerTempData.Status.Fight)
            {
                Room room = player.tempData.room;
                room.ExitFight(player);
                RoomMgr.instance.LeaveRoom(player);
            }
        }
    }
}
