﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Data;
using System.Linq;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Timers;

namespace ServerStudy
{
    public class ServNet
    {
		
        //主定时器
        Timer timer = new Timer(1000);

        //心跳时间
        public long heartBeatTime = 10;

        //数据库
        MySqlConnection sqlConn;

        //监听套接字
        public Socket listenfd;

        //客户端连接
        public Conn[] conns;

        //最大连接数
        public int maxConn = 50;

        //协议
        public ProtocolBase proto;

        //消息分发
        public HandleConnMsg handleConnMsg = new HandleConnMsg();
        public HandlePlayerMsg handlePlayerMsg = new HandlePlayerMsg();
        public HandlePlayerEvent handlePlayerEvent = new HandlePlayerEvent();

        //单例
        public static ServNet instance;
        public ServNet()
        {
            instance = this;
        }

        //开启服务器
        public void Start(string host,int port)
        {

			//数据库连接
			string connStr = "Database = msgboard;DataSource = 127.0.0.1;";
			connStr += "User Id = root;Password = 12345678;Port = 3306";
			sqlConn = new MySqlConnection (connStr);
			try
			{
				sqlConn.Open();
				Console.WriteLine("[数据库]连接成功");
			}
			catch(Exception e )
			{
				Console.WriteLine("[数据库]连接失败"+ e.Message);
				return;
			}
            //连接池
            conns = new Conn[maxConn];
            for (int i = 0; i < maxConn;i++)
            {
                conns[i] = new Conn();
            }
            //Socket
            listenfd = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //Bind
            IPAddress iPAddress = IPAddress.Parse(host);
            IPEndPoint iPEndPoint = new IPEndPoint(iPAddress, port);
            listenfd.Bind(iPEndPoint);

            //Listen
            listenfd.Listen(maxConn);

            //Accept
            listenfd.BeginAccept(AcceptCb, null);
            Console.WriteLine("[服务器]启动成功！");

           

            //定时器
            timer.Elapsed += new ElapsedEventHandler(HandleMainTimer);
            timer.AutoReset = false;
            timer.Enabled = true;
            timer.Start();

        }

        //Accept回调
        private void AcceptCb(IAsyncResult asyncResult)
        {
            try
            {
                Socket socket = listenfd.EndAccept(asyncResult);
                int index = NewIndex();
                if(index<0)
                {
                    socket.Close();
                    Console.Write("[警告]连接已满");
                }
                else
                {
                    Conn conn = conns[index];
                    conn.Init(socket);
                    string adr = conn.GetAddress();
                    Console.WriteLine("客户端连接[" + adr + "]  conn池ID:" + index);

                    //异步接收客户端数据
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
                }

                //再次调用 BeginAccept实现循环
                listenfd.BeginAccept(AcceptCb, null);
            }
            catch(Exception e)
            {
                Console.WriteLine("AcceptCb失败：" + e.Message);
            }
        }

        private void ReceiveCb(IAsyncResult asyncResult)
        {
            Conn conn = (Conn)asyncResult.AsyncState;
            lock(conn)
            {
                try
                {
                    //获取接收的字节数
                    int count = conn.socket.EndReceive(asyncResult);
                    //关闭信号
                    if (count <= 0)
                    {
                        Console.WriteLine("收到[" + conn.GetAddress() + "]断开连接");
                        conn.Close();
                        return;
                    }
                    conn.buffCount += count;
                    ProcessData(conn);

                    //继续接收，实现循环
                    conn.socket.BeginReceive(conn.readBuff, conn.buffCount, conn.BuffRemain(), SocketFlags.None, ReceiveCb, conn);
                }
                catch (Exception e)
                {
                    Console.WriteLine("收到[" + conn.GetAddress() + "]断开连接-异常触发  " + e.Message);
                    conn.Close();
                }
            }
        }
        //获取连接池索引，返回负数表示获取失败
        public int NewIndex()
        {
            if (conns == null)
            {
                return -1;
            }
            for (int i = 0; i < conns.Length; i++)
            {
                if (conns[i] == null)
                {
                    conns[i] = new Conn();
                    return i;
                }
                else if (conns[i].isUsed == false)
                {
                    return i;
                }

            }
            return -1;
        }

        //数据的分包粘包处理
        private void ProcessData(Conn conn)
        {

            //小于长度字节
            if (conn.buffCount < sizeof(Int32))
            {
                return;
            }

            //消息长度
            Array.Copy(conn.readBuff, conn.lenBytes, sizeof(Int32));
            conn.msgLength = BitConverter.ToInt32(conn.lenBytes, 0);
            if (conn.buffCount < conn.msgLength + sizeof(Int32))
            {
                return;
            }
           
            //处理消息
            ProtocolBase protocol = proto.Decode(conn.readBuff, sizeof(Int32), conn.msgLength);
            HandleMsg(conn, protocol);

            //清除已处理的消息
            int count = conn.buffCount - conn.msgLength - sizeof(Int32);
            Array.Copy(conn.readBuff, sizeof(Int32) + conn.msgLength, conn.readBuff, 0, count);
            conn.buffCount = count;
            if(conn.buffCount>0)
            {
                ProcessData(conn);
            }
        }

        private void HandleMsg(Conn conn,ProtocolBase protoBase)
        {
            string name = protoBase.GetName();
            string methodName = "Msg" + name;

            //连接协议分发
            if (conn.player == null || name == "HeartBeat" || name == "Logout")
            {
                MethodInfo mm = handleConnMsg.GetType().GetMethod(methodName);
                if(mm==null)
                {
                    string str = "[警告]HandleMsg没有处理连接方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new object[] { conn, protoBase };
                Console.WriteLine("[处理连接消息]" + conn.GetAddress() + ":" + name);
                mm.Invoke(handleConnMsg, obj);
            }

            //角色协议分发
            else
            {
                MethodInfo mm = handlePlayerMsg.GetType().GetMethod(methodName);
                if(mm==null)
                {
                    string str = "[警告]HandleMsg没有处理玩家方法";
                    Console.WriteLine(str + methodName);
                    return;
                }
                Object[] obj = new object[] { conn, protoBase };
                Console.WriteLine("[处理玩家消息]" + conn.player.id + ":" + name);
                mm.Invoke(handlePlayerMsg, obj);
            }

            Console.WriteLine("[收到协议]->" + name);
            if (name == "HeartBeat")
            {
                Console.WriteLine("[更新心跳时间]" + conn.GetAddress());
                conn.lastTickTime = Sys.GetTimeStamp();
            }
            //回射
            Send(conn, protoBase);
        }
        //发送
        public void Send(Conn conn,ProtocolBase protocol)
        {
            byte[] bytes = protocol.Encode();
            byte[] length = BitConverter.GetBytes(bytes.Length);
            byte[] sendBuff = length.Concat(bytes).ToArray();
            try
            {
                conn.socket.BeginSend(sendBuff, 0, sendBuff.Length, SocketFlags.None, null, null);
            }
            catch(Exception e)
            {
                Console.WriteLine("[发送消息]" + conn.GetAddress() + ":" + e.Message);
            }
        }

        //广播
        public void Broadcast(ProtocolBase protocol)
        {
            for(int i=0;i<conns.Length;i++)
            {
                if (!conns[i].isUsed) continue;
                if (conns[i].player == null) continue;
                Send(conns[i], protocol);
            }
        }

        //主定时器
        public void HandleMainTimer(object sender,System.Timers.ElapsedEventArgs e)
        {
            //处理心跳
            //HeartBeat();
            //timer.Start();
        }
        //心跳
        public void HeartBeat()
        {
            //Console.WriteLine("[主定时器执行]");
            long timeNow = Sys.GetTimeStamp();
            for(int i =0;i<conns.Length;i++)
            {
                Conn conn = conns[i];
                if (conn == null) continue;
                if (!conn.isUsed) continue;
                if(conn.lastTickTime<timeNow - heartBeatTime)
                {
                    Console.WriteLine("[心跳引起断开连接]" + conn.GetAddress());
                    lock(conn)
                    {
                        conn.Close();
                    }
                }
            }
        }

        //打印信息
        public void Print()
        {
            Console.WriteLine("========服务器登录信息==========");
            for(int i=0;i<conns.Length;i++)
            {
                if (conns[i] == null) continue;
                if (!conns[i].isUsed) continue;

                string str = "连接[" + conns[i].GetAddress() + "]";
                if (conns[i].player != null)
                    str += "玩家id" + conns[i].player.id;
                Console.WriteLine(str);
            }
        }
    }
}
