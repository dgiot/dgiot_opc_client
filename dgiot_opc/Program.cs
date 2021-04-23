using System;

namespace dgiot_opc
{
    class Program
    {
        static void Main(string[] args)
        {  
            string opcType = "opcda";
            string cmdType = "read";
            string server = "prod.iotn2n.com";
            string payload = "";
            if (args.Length < 3)
            {
                Console.WriteLine("dgiot_opc opcda|opcua read|write|bridge [server] [payload]");
                return;
            }
            try
            {
                opcType = args[0];
                cmdType = args[1];
                server = args[2];
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}: ", ex.ToString());
            }
            switch (opcType)
            {
                case "opcda":
                    switch (cmdType)
                    {
                        case "read":
                            payload = args[3];
                            // Console.WriteLine("item: " + item.ToString());
                            //data = Encoding.UTF8.GetString(Convert.FromBase64String(args[2]));
                            //JavaScriptSerializer serializer = new JavaScriptSerializer();
                            //Dictionary<string, object> report = (Dictionary<string, object>)serializer.DeserializeObject(data);
                            break;
                        case "write":
                            break;
                        case "bridge":
                            Console.WriteLine("{0}", server);
                            MqttHelper mymqtt = MqttHelper.GetInstance();
                            mymqtt.start(server);
                            break;
                        default:
                            Console.WriteLine("dgiot_opc opcda|opcua  read|write|bridge [server] [payload]");
                            break;
                    };
                    break;
                case "opcua":
                    break;
                default:
                    Console.WriteLine("dgiot_opc opcda|opcua  read|write|bridge [server] [payload]");
                    break;
            }

            Console.ReadLine();
            Console.WriteLine("回车退出");
            Console.ReadLine();
            Console.ReadLine();
        }

    }

}
