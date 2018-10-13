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
    //处理连接协议
    public partial class HandleConnMsg
    {
        //心跳
        //协议参数：无
        public void MsgHeartBeat(Conn conn,ProtocolBase protoBase)
        {
            conn.lastTickTime = Sys.GetTimeStamp();
            //Console.WriteLine("[更新心跳时间 ]" + conn.GetAddress());
        }

        //注册
        //协议参数：str 用户名，str 密码
        //返回协议：-1 表示失败，0表示成功
        public void MsgRegister(Conn conn,ProtocolBase protoBase)
        {
            //获取数值
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            string strFormat = "[HandleConnMsg -> MsgRegister() ] 收到  ["+conn.GetAddress()+"]  的注册协议    ";
            Console.WriteLine(strFormat + "用户名：" + id + " 密码：" + pw);
            
            //构建返回协议
            protocol = new ProtocolBytes();
            protocol.AddString("Register");

            //注册
            if(DataMgr.instance.Register(id,pw))
            {
                protocol.AddInt(0);
            }
            else
            {
                protocol.AddInt(-1);
            }
            //创建角色
            DataMgr.instance.CreatePlayer(id);

            //返回协议给客户端
            conn.Send(protocol);
        }

        //登录
        //协议参数：str用户名，str密码
        //返回协议：-1表示失败，0表示成功
        public void MsgLogin(Conn conn,ProtocolBase protoBase)
        {
            //获取数值
            int start = 0;
            ProtocolBytes protocol = (ProtocolBytes)protoBase;
            string protoName = protocol.GetString(start, ref start);
            string id = protocol.GetString(start, ref start);
            string pw = protocol.GetString(start, ref start);
            string strFormat = "[HandleConnMsg -> MsgLogin() ] 收到  [" + conn.GetAddress() + "]  的登录协议    ";
            Console.WriteLine(strFormat + "用户名：" + id + " 密码：" + pw);
      
            //构建返回协议
            ProtocolBytes protocolRet = new ProtocolBytes();
            protocolRet.AddString("Login");

            
            //验证
            if(!DataMgr.instance.CheckPassword(id,pw))
            {
                Console.WriteLine("[登录检查失败]");
                protocolRet.AddInt(-1);
                conn.Send(protocolRet);
                return;
            }
            
            //是否已经登录
            ProtocolBytes protocolLogout = new ProtocolBytes();
            protocolLogout.AddString("Logout");
            if(!Player.KickOff(id,protocolLogout))
            {
                protocolRet.AddInt(-1);
                conn.Send(protocolRet);
                return;
            }
            
            //获取玩家数据
            PlayerData playerData = DataMgr.instance.GetPlayerData(id);
            if(playerData==null)
            {
                protocolRet.AddInt(-1);
                conn.Send(protocolRet);
                return;
            }
            conn.player = new Player(id, conn);
            conn.player.data = playerData;
            
            //事件触发
            ServNet.instance.handlePlayerEvent.OnLogin(conn.player);
            
            //返回
            protocolRet.AddInt(0);
            conn.Send(protocolRet);
            return;
        }

        //下线
        //协议参数：无
        //返回协议：0 正常下线
        public void MsgLogout(Conn conn, ProtocolBase protocolBase)
        {
            ProtocolBytes protocol = new ProtocolBytes();
            protocol.AddString("Logout");
            protocol.AddInt(0);
            if (conn.player == null)
            {
                conn.Send(protocol);
                conn.Close();
            }
            else
            {
                conn.Send(protocol);
                conn.player.Logout();
            }
        }
    }
}
