//------------------------------------------------------------------------------
// DataMgr 封装数据库操作的方法
//------------------------------------------------------
using System;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


namespace ServerStudy
{
	public class DataMgr
	{
		MySqlConnection sqlConn;

		//单例模式
		public static DataMgr instance;
		public DataMgr ()
		{
			instance = this;
			Connect ();
		}

		//连接
		public void Connect()
		{
			//数据库连接
			string connStr = "Database = game;DataSource = 127.0.0.1;";
			connStr += "User Id = root;Password = 12345678;Port = 3306";
			sqlConn = new MySqlConnection (connStr);
			try
			{
				sqlConn.Open();
				Console.WriteLine("[DataMgr数据库]连接成功");
			}
			catch(Exception e )
			{
				Console.WriteLine("[DataMgr数据库]连接失败"+ e.Message);
				return;
			}
		}

		//是否存在该用户
		private bool CanRegister(string id)
		{
			//防SQL注入
			if (!IsSafeStr (id)) 
			{
				return false;
			}
			//查询ID是否存在
			string cmdStr = string.Format ("select * from user where id = '{0}';", id);
			MySqlCommand cmd = new MySqlCommand (cmdStr, sqlConn);
			try
			{
				//当数据库表中查询到此ID对应的行信息后，表示此ID不能再注册，返回false
				MySqlDataReader dataReader = cmd.ExecuteReader();
				bool hasRows = dataReader.HasRows;
				dataReader.Close();
				return !hasRows;
			}
			catch(Exception e)
			{
				Console.WriteLine("[DataMgr]CanRegister Fail"+e.Message);
				return false;
			}

		}

		//注册
		public bool Register(string id,string pw)
		{
			//防SQL注入
			if (!IsSafeStr (id) || !IsSafeStr (pw)) 
			{
				Console.WriteLine("[DataMgr]Register 使用非法字符");
				return false;
			}

			//能否注册
			if (!CanRegister (id)) 
			{
				Console.WriteLine("[DataMgr]Register !CanRegister");
				return false;
			}

			//写入数据库 User 表
			string cmdStr = string.Format ("insert into user set id = '{0}',pw = '{1}';",id,pw);
			MySqlCommand cmd = new MySqlCommand (cmdStr, sqlConn);
			try
			{
				cmd.ExecuteNonQuery();
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine("[DataMgr]Resister "+e.Message);
				return false;
			}
		}

		//创建角色
		public bool CreatePlayer(string id)
		{
			//防sql注入
			if (!IsSafeStr (id)) 
			{
				return false;
			}
			//将playerData对象序列化为二进制数据
			IFormatter formatter = new BinaryFormatter ();
			MemoryStream stream = new MemoryStream ();
			PlayerData playerData = new PlayerData ();
			try
			{
				formatter.Serialize(stream,playerData);
			}
			catch(Exception e)
			{
				Console.WriteLine("[DataMgr]CreatePlayer序列化"+e.Message);
				return false;
			}
			byte[] byteArr = stream.ToArray ();

			//写入数据库
			string cmdStr = string.Format ("insert into player set id = '{0}',data = @data;", id);
			MySqlCommand cmd = new MySqlCommand (cmdStr, sqlConn);
			cmd.Parameters.Add ("@data", MySqlDbType.Blob);
			cmd.Parameters [0].Value = byteArr;
			try
			{
				cmd.ExecuteNonQuery();
				return true;
			}
			catch(Exception e)
			{
				Console.WriteLine("[DataMgr]CreatePlayer 写入"+e.Message);
				return false;
			}
		}

		//登录 检测用户名和密码
		public bool CheckPassword(string id,string pw)
		{
			//防SQL注入
			if (!IsSafeStr (id) || !IsSafeStr (pw))
				return false;
			//查询
			string cmdStr = string.Format ("select * from user where id = '{0}' and pw = '{1}';", id, pw);
			MySqlCommand cmd = new MySqlCommand (cmdStr, sqlConn);
			try
			{
				MySqlDataReader dataReader = cmd.ExecuteReader();

                bool hasRows = dataReader.HasRows;
				dataReader.Close();
				return hasRows;
			}
			catch(Exception e)
			{
				Console.WriteLine("[DataMgr]CheckPassword"+e.Message);
				return false;
			}
		}

        //保存角色数据
        public bool SavePlayer(Player player)
        {
            string id = player.id;
            PlayerData playerData = player.data;

            //序列化
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            try
            {
                formatter.Serialize(stream, playerData);
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]SavePlayer 序列化" + e.Message);
                return false;
            }

            byte[] byteArr = stream.ToArray();
            
            //写入数据库
            string formatStr= "update player set data= @data where id='{0}';";
            string cmdStr = string.Format(formatStr, player.id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            cmd.Parameters.Add("@data", MySqlDbType.Blob);
            cmd.Parameters[0].Value = byteArr;
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]SavePlayer 写入" + e.Message);
                return false;
            }
        }

        //获取玩家数据
        //public long Getbytes(int i,long dataIndex,byte[] buffer,int bufferIndex,int length)
        //i          ->从零开始的序列号，下述语句中0代表id,1代表data
        //dataIndex  ->字段中的索引，从dataIndex处开始读取操作
        //buffer     ->要将字节流读入的缓冲区
        //bufferIndex->buffer中写入操作开始位置的索引
        //length     ->要复制到缓冲区中的最大长度
        //返回值     ->实际读取到的字节流长度
        
        public PlayerData GetPlayerData(string id)
        {
            PlayerData playerData = null;

            //防SQL注入
            if (!IsSafeStr(id))
                return playerData;
            //查询
            string cmdStr = string.Format("Select * from player where id='{0}';", id);
            MySqlCommand cmd = new MySqlCommand(cmdStr, sqlConn);
            byte[] buffer = new byte[1];
            try
            {
                MySqlDataReader dataReader = cmd.ExecuteReader();
                if(!dataReader.HasRows)
                {
                    dataReader.Close();
                    return playerData;
                }
                dataReader.Read();
                //读取数据集中第二个字段的方法，第一个参数1代表第二个字段data
                long len = dataReader.GetBytes(1, 0, null, 0, 0);
                buffer = new byte[len];
                dataReader.GetBytes(1, 0, buffer,0, (int)len);
                dataReader.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]GetPlayerData查询" + e.Message);
                return playerData;
            }

            //反序列化
            MemoryStream stream = new MemoryStream(buffer);
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                playerData = (PlayerData)formatter.Deserialize(stream);
                return playerData;
            }
            catch(Exception e)
            {
                Console.WriteLine("[DataMgr]GetPlayerData反序列化" + e.Message);
                return playerData;
            }
        }

		//判定安全字符串
		public bool IsSafeStr(string str)
		{
			return !Regex.IsMatch (str, @"[-|;|,|\/|\(|\)|\[|\}|\}|%|@|\*|!|\']");
		}
	}
}

