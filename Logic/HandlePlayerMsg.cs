using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//---------------------------------------------
//  handleConnMsg 和 handlePlayerMsg中使用
//  Msg+协议名 来处理对应的协议
//  HandleConnMsg和HandlePlayerMsg使用partial修饰，
//  表明是局部类型，它允许我们将一个类、结构或接口分成几个部分，
//  分别实现再几个不同的.cs文件中。考虑到游戏中有成百上千条协议，
//  难以全部放到同一个文件中，必要时可根据功能模块将逻辑代码分到多个文件。
//---------------------------------------------

namespace ServerStudy
{
    //处理角色协议
    public partial class HandlePlayerMsg
    {
        //获取分数
        //协议参数：
        //返回协议： int 分数
        public void MsgGetScore(Player player, ProtocolBase protocolBase)
        {
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("GetScore");
            protocolRet.AddInt(player.data.score);
            player.Send(protocolRet);
            Console.WriteLine("MsgGetScore" + player.id + player.data.score);
        }

        //增加分数
        //协议参数：
        public void MsgAddScore(Player player,ProtocolBase protocolBase)
        {
            //获取数值
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            //处理
            player.data.score += 1;
            Console.WriteLine("MsgAddScore()-> " + player.id + "  " + player.data.score.ToString());
        }

        //获取玩家列表
        public void MsgGetList(Player player,ProtocolBase protocolBase)
        {
            Scene.instance.SendPlayerList(player);
        }

        //更新信息
        public void MsgUpdateInfo(Player player,ProtocolBase protocolBase)
        {
            //获取数值
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protocolBase;
            string protoName = protocol.GetString(start, ref start);
            float x = protocol.GetFloat(start, ref start);
            float y = protocol.GetFloat(start, ref start);
            float z = protocol.GetFloat(start, ref start);
            int score = player.data.score;
            Scene.instance.UpdateInfo(player.id, x, y, z, score);

            //广播
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("UpdateInfo");
            protocolRet.AddString(player.id);
            protocolRet.AddFloat(x);
            protocolRet.AddFloat(y);
            protocolRet.AddFloat(z);
            protocolRet.AddInt(score);
            ServNet.instance.Broadcast(protocolRet);
        }

        //获取玩家成绩
        public void MsgGetAchieve(Player player,ProtocolBase protocol)
        {
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("GetAchieve");
            protocolRet.AddInt(player.data.win);
            protocolRet.AddInt(player.data.fail);
            player.Send(protocolRet);
            Console.WriteLine("MsgGetScore " + player.id + player.data.win);
        }

    }
}
