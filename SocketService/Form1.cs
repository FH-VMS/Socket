
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SocketService
{
    public partial class Form1 : Form
    {
         
        
        private IPAddress serverIP = IPAddress.Parse("127.0.0.1");//以本机作测试



        private IPEndPoint serverFullAddr;//完整终端地址
        private Socket sock;
        Thread myThead = null;
        bool isListening = false;
        

        //保存了服务器端所有负责和客户端通信发套接字  
        Dictionary<string, Socket> dictSocket = new Dictionary<string, Socket>();
        //保存了服务器端所有负责调用通信套接字.Receive方法的线程  
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();


        public Form1()
        {
            InitializeComponent();
        }
        //启动
        private void btnConn_Click(object sender, EventArgs e)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverIP = IPAddress.Parse(tbxIP.Text);   //IP
            serverFullAddr = new IPEndPoint(serverIP, int.Parse(tbxPort.Text));//设置IP，端口


            isListening = true;
            //指定本地主机地址和端口号
            sock.Bind(serverFullAddr);
            sock.Listen(10);//设置监听频率

            myThead = new Thread(BeginListen);
            myThead.IsBackground = true;
            myThead.Start();
            lbxMessage.Invoke(new SetTextCallback(SetText), "启动成功 时间:" + DateTime.Now, 1);
            btnStart.Enabled = false;
            btnstop.Enabled = true;

        }
        private void BeginListen(object obj)
        {
            
            while (isListening)
            {
                try
                {
                    Socket newSocket = sock.Accept();//阻塞方式
                    dictSocket.Add(newSocket.RemoteEndPoint.ToString(), newSocket);
                    if (newSocket != null && newSocket.Connected)
                    {
                        ParameterizedThreadStart pts = new ParameterizedThreadStart(ReceiveMessage);
                        Thread receiveThread = new Thread(pts);
                        try
                        {
                            
                            receiveThread.Start(newSocket);
                            lbxMessage.Invoke(new SetTextCallback(SetText), "客户端连接成功:" + DateTime.Now, 1);
                            dictThread.Add(newSocket.RemoteEndPoint.ToString(), receiveThread);
                        }
                        catch(Exception ex)
                        {
                            receiveThread.Abort();
                        }
                       
                    }
                    else
                    {
                        isListening = false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    isListening = false;
                    myThead.Abort();
                    
                    lbxMessage.Invoke(new SetTextCallback(SetText), "ddddd", 1);
                    break;
                }
                
            }
        }

        /// <summary>  
        /// 接收消息  
        /// </summary>  
        /// <param name=""></param>  
        private void ReceiveMessage(object clientSocket)
        {
            
            Socket myClientSocket = clientSocket as Socket; 
            
            while (isListening)
            {
                if (myClientSocket != null && myClientSocket.Connected)
                {
                    byte[] message = new byte[1024];
                    string mess = "";
                    try
                    {
                        //通过clientSocket接收数据  
                        int receiveNumber = myClientSocket.Receive(message);
                        
                        mess = Encoding.UTF8.GetString(message, 0, receiveNumber);
                        if (string.IsNullOrEmpty(mess.Trim()))
                        {
                            myClientSocket.Close();
                            break;
                        }
                        //接收数据的转换
                        HandleHexByte(mess);
                        lbxMessage.Invoke(new SetTextCallback(SetText), mess, 1);
                        Thread.Sleep(10);
                    }
                    catch (Exception ex)
                    {
                        lbxMessage.Invoke(new SetTextCallback(SetText), mess + ex, 1);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void SendMsg(string sendMsg)
        {
            if (string.IsNullOrEmpty(iptext.Text))
            {
                MessageBox.Show("请选择通信IP！");
            }

            else
            {
                byte[] strSendMsg = Encoding.UTF8.GetBytes(sendMsg);
                string strClientKey = iptext.Text;//通过Key匹配对应ip地址的客户端  

                dictSocket[strClientKey].Send(strSendMsg);


            }
        }

        //处理机器消息
        private void HandleHexByte(string info)
        { 
            
            byte[] byteInfo = Utility.strToToHexByte(info);
            //包头
            string infoHead = byteInfo[0].ToString();
            //大小
            int infoSize = Convert.ToInt32(byteInfo[1]);
            //验证码
            string infoVerify = byteInfo[2].ToString();
            //数据
            byte[] data = byteInfo.Skip(3).Take(infoSize).ToArray();
            //string machine_num = Encoding.ASCII.GetString(data, 1, 4); 
            //验证是否为有效包
            if (!IsValidPackage(infoVerify, data))
            {
                return;
            }
            //验证通过
            switch (Utility.Ten2Hex(data[0].ToString()).ToUpper())
            {
                case "A1": //告警信息
                    
                    break;
                case "A2": //销售信息
                    string machineNum = Utility.GenerateRealityData(data.Skip(1).Take(4).ToArray(), "intval");
                    string serialNum = Utility.GenerateRealityData(data.Skip(5).Take(4).ToArray(), "intval");
                    string tunnelNum = Utility.GenerateRealityData(data.Skip(9).Take(5).ToArray(), "stringval");
                    break;
                case "A6": //满仓信息 (一键补货)
                    break;
                case "A0": //心跳包
                    break;
            }
        }

        //11个字节做异或处理
        private bool IsValidPackage(string infoVerify,byte[] data)
        {
            string finalResult = string.Empty;
            byte result = new byte();
            for(int i = 0; i < data.Length; i++)
            {
                result ^= data[i];
            }


            return result.ToString()== infoVerify;
        }

        


        #region//声名委托
        delegate void SetTextCallback(string text, int num);
        private void SetText(string text, int num)
        {
            lbxMessage.Items.Add(text);
        }
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            btnStart.Enabled = true;
            btnstop.Enabled = false;
        }
        //停止
        private void btnstop_Click(object sender, EventArgs e)
        {
            try
            {
                isListening = false;
                sock.Close();
                myThead.Abort();
                btnStart.Enabled = true;
                btnstop.Enabled = false;
                lbxMessage.Items.Add("停止成功 时间:" + DateTime.Now);
            }
            catch (Exception ee)
            {
                lbxMessage.Text = "停止失败。。" + ee;
            }
        }


        
        private void btnSend_Click(object sender, EventArgs e)
        {
             


        }



        private void Sending(IAsyncResult rec_socket)
        {
            Socket socket = (Socket)rec_socket.AsyncState;
            try
            {
                if (socket.Connected)
                {
                    byte[] msgBuff = Encoding.UTF8.GetBytes(textBox1.Text);
                    socket.Send(msgBuff);
                }
                else
                {
                    Console.WriteLine("Error!", "Error!");
                }
            }
            catch
            {
                Console.WriteLine("Error!", "Error!");
            }
        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }
         

 

    }
}
