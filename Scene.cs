using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerStudy
{
    public class Scene
    {
        //单例
        public static Scene instance;
        public Scene()
        {
            instance = this;
        }

        //场景中的角色列表
        List<ScenePlayer> playerList = new List<ScenePlayer>(); 

        //根据名字获取ScenePlayer
        private ScenePlayer GetScenePlayer(string id)
        {
            for(int i=0;i<playerList.Count;i++)
            {
                if(playerList[i].id==id)
                {
                    return playerList[i];
                }
            }
            return null;
        }

        //添加玩家,多个线程可能同时操作列表，需要加锁
        public void AddPlayer(string id)
        {
            lock(playerList)
            {
                ScenePlayer p = new ScenePlayer();
                p.id = id;
                playerList.Add(p);
            }
        }

        //删除玩家
        public void DeletePlayer(string id)
        {
            lock(playerList)
            {
                ScenePlayer p = GetScenePlayer(id);
                if(p!=null)
                {
                    playerList.Remove(p);
                }
            }
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("PlayerLeave");
            protocol.AddString(id);
            ServNet.instance.Broadcast(protocol);
        }

        //发送列表
        public void SendPlayerList(Player player)
        {
            int count = playerList.Count;
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("GetList");
            protocol.AddInt(count);
            for(int i=0;i<count;i++)
            {
                ScenePlayer p = playerList[i];
                protocol.AddString(p.id);
                protocol.AddFloat(p.x);
                protocol.AddFloat(p.y);
                protocol.AddFloat(p.z);
                protocol.AddInt(p.score);
            }
            player.Send(protocol);
        }

        //更新信息
        public void UpdateInfo(string id,float x,float y,float z,int score)
        {
            int count = playerList.Count;
            ProtocolBytes protocol = new ProtocolBytes();
            ScenePlayer p = GetScenePlayer(id);
            if(p==null)
            {
                return; 
            }
            p.x = x;
            p.y = y;
            p.z = z;
            p.score = score;
        }
    }
}
