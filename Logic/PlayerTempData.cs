using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    public class PlayerTempData
    {
        public Status status;
        public Room room;      //玩家所在的房间
        public int team = 1;   //玩家的阵营,1代表阵营1，2代表阵营2
        public bool isOwner = false;
        public float hp = 0;     //玩家的血量

        public float posX;
        public float posY;
        public float posZ;
        public float lastUpdateTime;
        public float lastShootTime;


        public PlayerTempData()
        {
            status = Status.None;
        }


        //状态
        public enum Status
        {
            None,
            Room,
            Fight,
        }

    }
}
