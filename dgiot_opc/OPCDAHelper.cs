using System;
using System.Text;
using MQTTnet.Core.Client;
using TitaniumAS.Opc.Client.Da;
using TitaniumAS.Opc.Client.Common;

using MQTTnet.Core;
using System.Collections.Generic;

using MQTTnet.Core.Protocol;
using TitaniumAS.Opc.Client.Da.Browsing;

//https://github.com/titanium-as/TitaniumAS.Opc.Client
//https://github.com/chkr1011/MQTTnet

namespace dgiot_opc
{

    public class OPCDAHelper
    {
        private static string pubtopic = "dgiot_opc_da_ack";
        private static string scantopic = "dgiot_opc_da_scan";

        public static void do_opc_da(MqttClient mqttClient, Dictionary<string, object> json)
        {
            string cmdType = "read";
            if (json.ContainsKey("cmdtype"))
            {
                try
                {
                    cmdType = (string)json["cmdtype"];
                    switch (cmdType)
                    {
                        case "scan":
                            scan_opc_da(mqttClient,json);
                            break;
                        case "read":
                            read_opc_da(mqttClient, json);
                            break;
                        case "write":
                            break;
                        default:
                            read_opc_da(mqttClient, json);
                            break;
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.ToString());
                }
            }

        }

        private static void scan_opc_da(MqttClient mqttClient, Dictionary<string, object> json)
        {
            string opcserver = "Matrikon.OPC.Simulation.1";
      
            IList<OpcDaItemDefinition> itemlist = new List<OpcDaItemDefinition>();
            if (json.ContainsKey("opcserver"))
            {
                try
                {
                    opcserver = (string)json["opcserver"];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.ToString());
                }
            }

            Uri url = UrlBuilder.Build(opcserver);
            Console.WriteLine("opcserver {0}", opcserver.ToString());
            try
            {    
                using (var server = new OpcDaServer(url))
                {
                    // Connect to the server first.
                    server.Connect();
                    var browser = new OpcDaBrowserAuto(server);
                    JsonObject scan = new JsonObject();
                    BrowseChildren(scan, browser);
                    var appMsg = new MqttApplicationMessage(scantopic, Encoding.UTF8.GetBytes(scan.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                    Console.WriteLine("appMsg {0}", scan.ToString());
                    mqttClient.PublishAsync(appMsg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" error {0} ", ex.GetBaseException().ToString());
                JsonObject result = new JsonObject();
                result.Add("TimeStamp", FromDateTime(DateTime.UtcNow));
                result.Add("opcserver", opcserver);
                result.Add("status", ex.GetHashCode());
                result.Add("err", ex.ToString());
                var appMsg = new MqttApplicationMessage(pubtopic, Encoding.UTF8.GetBytes(result.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                Console.WriteLine("appMsg {0}", appMsg.ToString());
                mqttClient.PublishAsync(appMsg);
            }
        }

        private static void BrowseChildren(JsonObject json,IOpcDaBrowser browser, string itemId = null, int indent = 0)
        {
            // When itemId is null, root elements will be browsed.
            OpcDaBrowseElement[] elements = browser.GetElements(itemId);
            JsonArray array = new JsonArray();
            Boolean flag = false;
            foreach (OpcDaBrowseElement element in elements)
            {
                // Skip elements without children.
               if (!element.HasChildren){       
                    array.Add(element);
                    flag = true;
                    continue;
                }     
                // Output children of the element.
                BrowseChildren(json, browser, element.ItemId, indent + 2);
            }

            if (flag){
                if (null != itemId)
                {
                    json.Add(itemId, array);
                } 
            }     
        }

        private static void read_opc_da(MqttClient mqttClient, Dictionary<string, object>  json)
        {
            string opcserver = "Matrikon.OPC.Simulation.1";
            string group = "addr";
            IList<OpcDaItemDefinition> itemlist = new List<OpcDaItemDefinition>();
            if (json.ContainsKey("opcserver"))
            {
                try
                {
                    opcserver = (string)json["opcserver"];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.ToString());
                }
            }

            if (json.ContainsKey("group"))
            {
                try
                {
                    group = (string)json["group"];
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.ToString());
                }
            }

            if (json.ContainsKey("items"))
            {
                try
                {
                    string items = (string)json["items"];
                    Console.WriteLine(" from task {0} {1} {2} ", opcserver, group, items);
                    string[] arry = items.Split(',');
                    JsonObject data = new JsonObject();
                    try
                    { 
                        JsonObject result = new JsonObject();
                        read_group(mqttClient, opcserver, group, arry, data);
                        result.Add("status", 0);
                        result.Add(group, data);
                        Console.WriteLine("result {0}", result.ToString());
                        var appMsg = new MqttApplicationMessage(pubtopic, Encoding.UTF8.GetBytes(result.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                        mqttClient.PublishAsync(appMsg);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("{0}", ex.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("{0}", ex.ToString());
                }
            }

        }

        private static void read(MqttClient mqttClient, string opcserver, string group_name, string[] arry, JsonObject items)
        {
            Uri url = UrlBuilder.Build(opcserver);
            try
            {
                using (var server = new OpcDaServer(url))
                {
                    // Connect to the server first.
                    
                    foreach (string id in arry)
                    {
                        server.Connect();
                        OpcDaGroup group = server.AddGroup(group_name);
                        var definition = new OpcDaItemDefinition
                        {
                            ItemId = id,
                            IsActive = true
                        };
                        group.IsActive = true;
                        OpcDaItemDefinition[] definitions = { definition };
                        OpcDaItemResult[] results = group.AddItems(definitions);
                        OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);
                        foreach (OpcDaItemValue item in values)
                        {
                            Console.WriteLine(" {0}  {1} {2} {3} {4}", pubtopic, id, item.GetHashCode(), item.Value, item.Timestamp);
                            items.Add(id, item.Value);
                        }
                        server.Disconnect();
                    }   
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" error {0} ", ex.GetBaseException().ToString());
                JsonObject result = new JsonObject();
                result.Add("opcserver", opcserver);
                result.Add("status", ex.GetHashCode());
                result.Add("err", ex.ToString());
                var appMsg = new MqttApplicationMessage(pubtopic, Encoding.UTF8.GetBytes(result.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                mqttClient.PublishAsync(appMsg);
            }
        }
        private static void read_group(MqttClient mqttClient, string opcserver, string group_name, string[] arry, JsonObject items)
        {
            Uri url = UrlBuilder.Build(opcserver);
            try
            {
                using (var server = new OpcDaServer(url))
                {
                    // Connect to the server first.
                    server.Connect();
                    // Create a group with items.
                    OpcDaGroup group = server.AddGroup(group_name);
                    IList<OpcDaItemDefinition> definitions = new List<OpcDaItemDefinition>();
                    int i = 0;
                    foreach (string id in arry)
                    {
                        var definition = new OpcDaItemDefinition
                        {
                            ItemId = id,
                            IsActive = true
                        };
                        definitions.Insert(i++, definition);
                    }
                    group.IsActive = true;
                    OpcDaItemResult[] results = group.AddItems(definitions);
                    OpcDaItemValue[] values = group.Read(group.Items, OpcDaDataSource.Device);

                  
                    // Handle adding results.
                    JsonObject data = new JsonObject();
                    foreach (OpcDaItemValue item in values)
                    {
                        Console.WriteLine(" {0}  {1} {2} {3}", pubtopic, item.Item.ItemId, item.Value, item.Timestamp);
                        data.Add(item.Item.ItemId, item.Value);
                    }
                    items.Add("status", 0);
                    items.Add(group_name, data);
                    Console.WriteLine("items {0}", items.ToString());
                    var appMsg = new MqttApplicationMessage(pubtopic, Encoding.UTF8.GetBytes(items.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                    mqttClient.PublishAsync(appMsg);
                    server.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ex {0}", ex.ToString());
                read(mqttClient,opcserver, group_name, arry, items);
            }
        }

        private static void subscription_opc_da(MqttClient mqttClient, string opcserver, string name)
        {
            Uri url = UrlBuilder.Build(opcserver);
            try
            {
                using (var server = new OpcDaServer(url))
                {
                    // Connect to the server first.
                    server.Connect();
                    // Create a group with items.
                    OpcDaGroup group = server.AddGroup("Group1");
                    group.IsActive = true;

                    var definition = new OpcDaItemDefinition
                    {
                        ItemId = name,
                        IsActive = true
                    };
                    
                    OpcDaItemDefinition[] definitions = { definition };
                   
                    OpcDaItemResult[] results = group.AddItems(definitions);
                   
                    group.ValuesChanged += OnGroupValuesChanged;
                    group.UpdateRate = TimeSpan.FromMilliseconds(100);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(" error {0} ", ex.GetBaseException().ToString());
                JsonObject result = new JsonObject();
                result.Add("opcserver", opcserver);
                result.Add("name", name);
                result.Add("status", ex.GetHashCode());
                result.Add("err", ex.ToString());
                var appMsg = new MqttApplicationMessage(pubtopic, Encoding.UTF8.GetBytes(result.ToString()), MqttQualityOfServiceLevel.AtLeastOnce, false);
                mqttClient.PublishAsync(appMsg);
            }
        }

        static void OnGroupValuesChanged(object sender, OpcDaItemValuesChangedEventArgs args)
        {
            // Output values.
            foreach (OpcDaItemValue value in args.Values)
            {
                Console.WriteLine("ItemId: {0}; Value: {1}; Quality: {2}; Timestamp: {3}",
                    value.Item.ItemId, value.Value, value.Quality, value.Timestamp);
            }
        }

        private static DateTime BaseTime = new DateTime(1970, 1, 1);

        /// <summary>   
        /// ��unixtimeת��Ϊ.NET��DateTime   
        /// </summary>   
        /// <param name="timeStamp">����</param>   
        /// <returns>ת�����ʱ��</returns>   
        public static DateTime FromUnixTime(long timeStamp)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(timeStamp * 10000000 + BaseTime.Ticks));
        }

        /// <summary>   
        /// ��.NET��DateTimeת��Ϊunix time   
        /// </summary>   
        /// <param name="dateTime">��ת����ʱ��</param>   
        /// <returns>ת�����unix time</returns>   
        public static long FromDateTime(DateTime dateTime)
        {
            return (TimeZone.CurrentTimeZone.ToUniversalTime(dateTime).Ticks - BaseTime.Ticks) / 10000000;
        }

    }

    }