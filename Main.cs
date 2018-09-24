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

            Scene scene = new Scene();
            DataMgr dataMgr = new DataMgr();
            ServNet servNet = new ServNet();
            servNet.proto = new ProtocolBytes();
            servNet.Start("192.168.1.106", 1234);

            while(true)
            {
                string str = Console.ReadLine();
                switch (str)
                {
                    case "quit":
                        servNet.Close();
                        return;
                    case "print":
                        servNet.Print();
                        break;
                }

            }
        }
	}
}
