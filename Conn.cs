using System;
using System.Net;
using System.Net.Sockets;


namespace ServerStudy
{
    public class Conn
    {
        //玩家
        public Player player;
        //常量
        public const int BUFFER_SIZE = 1024;

        //Socket
        public Socket socket;

        //是否使用
        public bool isUsed = false;

        //Buffer
        public byte[] readBuff;
        public int buffCount = 0;

        //分包粘包
        //msgLength 存储包的数据长度值
        //lenBytes  存储代表包长度的4位字节
        public int msgLength;
        public byte[] lenBytes = new byte[sizeof(UInt32)];

        //心跳时间
        public long lastTickTime = long.MinValue;

        //构造函数
        public Conn()
        {
            readBuff = new byte[BUFFER_SIZE];
        }

        //初始化
        public void Init(Socket socket)
        {
            this.socket = socket;
            isUsed = true;
            buffCount = 0;
            //心跳处理
            lastTickTime = Sys.GetTimeStamp();
        }

        //缓冲区剩余的字节数
        public int BuffRemain()
        {
            return BUFFER_SIZE - buffCount;
        }

        //获取客户端地址
        public string GetAddress()
        {
            if(!isUsed)
            {
                return "无法获取地址！";
            }
            return socket.RemoteEndPoint.ToString();
        }

        //关闭
        public void Close()
        {
            if (!isUsed)
                return;
            if(player !=null)
            {
                //玩家退出处理，关闭连接之前先保存玩家数据
                player.Logout();
                return;
            }
            Console.WriteLine("[断开连接]" + GetAddress());
            socket.Close();
            isUsed = false;
        }

        //发送协议，稍后实现
        //public  void Send(ProtocolBase protocol)
        //{
        //ServNet.instance.Send(this,protocol);
        //}

    }
}
