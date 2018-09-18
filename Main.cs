using System;
using System.Timers;
using System.Threading;

namespace ServerStudy
{
	class MainClass
	{
		static string str ="";
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");

            DataMgr dataMgr = new DataMgr();
            ServNet servNet = new ServNet();
            servNet.proto = new ProtocolBytes();
            servNet.Start("127.0.0.1", 1234);

            Console.ReadLine();




        }
	}
}
