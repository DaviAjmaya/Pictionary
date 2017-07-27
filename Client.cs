﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;

namespace Pictionary
{
    public partial class Client : Form
    {
        // Server
        bool isServer;
        Server server;

        // Client
        Socket client;
        string userName;
        bool connected = false;

        private byte[] byteData = new byte[1024];


        // Drawing stuff
        private Graphics g;
        Point p1 = new Point();
        Point p2 = new Point();
        bool down = false;



        public Client(bool isServer, string userName, string ip, int port)
        {
            this.isServer = isServer;
            this.userName = userName;
            if (isServer)
            {
                server = new Server(port);
            }
            byteData = new byte[1024];
            InitializeComponent();
            ConnectToServer(ip, port);
            playerList.AddPlayer(userName);
            g = drawingBoard.CreateGraphics();
        }

        private void ConnectToServer(string ip, int port)
        {
            try
            {
                client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                IPAddress ipAddress;
                if (ip == "")
                {
                    ipAddress = IPAddress.Parse("127.0.0.1");
                }
                else
                {
                    ipAddress = IPAddress.Parse(ip);
                }

                //Server is listening on port 1000
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

                //Connect to the server
                client.BeginConnect(ipEndPoint, new AsyncCallback(OnConnect), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnSend(IAsyncResult ar)
        {
            try
            {
                client.EndSend(ar);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void OnConnect(IAsyncResult ar)
        {
            try
            {
                client.EndConnect(ar);
                SendMessage(null, Command.Login);
                connected = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SendMessage(string message, Command command)
        {
            try
            {
                //Fill the info for the message to be send
                Data msgToSend = new Data();

                msgToSend.strName = userName;
                msgToSend.strMessage = message;
                msgToSend.cmdCommand = command;

                byte[] byteData = msgToSend.ToByte();

                //Send it to the server
                client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                client.EndReceive(ar);

                Data msgReceived = new Data(byteData, false);
                //Accordingly process the message received
                switch (msgReceived.cmdCommand)
                {
                    // If a new user has connected
                    case Command.Login:
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.AddPlayer(msgReceived.strName);
                        });
                        chat.Invoke((MethodInvoker)delegate {
                            chat.Text += "<<<" + msgReceived.strName + " has joined the room>>>\r\n";
                        });
                        break;

                    // If a user has disconnected
                    case Command.Logout:
                        playerList.Invoke((MethodInvoker)delegate
                        {
                            playerList.RemovePlayer(msgReceived.strName);
                        });
                        break;
                    case Command.Drawing:
                        //playerList.Invoke((MethodInvoker)delegate {
                        //    playerList.ClearSelected();
                        //    playerList.SelectedItem = msgReceived.strName;
                        //});
                        break;

                    case Command.Message:
                        break;
                    case Command.Ready:
                        playerList.Invoke((MethodInvoker)delegate
                        {
                            playerList.Ready(msgReceived.strName);
                        });
                        break;

                    case Command.List:
                        playerList.Invoke((MethodInvoker)delegate
                        {
                            string[] names = msgReceived.strMessage.Split('*');
                            for (int i = 0; i < names.Length-2; i++)
                            {
                                playerList.AddPlayer(names[i]);
                            }
                        });
                        break;
                }

                if (msgReceived.strMessage != null && msgReceived.cmdCommand != Command.List)
                    chat.Invoke((MethodInvoker)delegate {
                        chat.Text += msgReceived.strMessage + "\r\n";
                    });

                byteData = new byte[1024];

                client.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);

            }
            catch (ObjectDisposedException)
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                SendMessage(input.Text, Command.Message);
                input.Clear();
            }
        }

        private void Client_Load(object sender, EventArgs e)
        {
            // Start receiving from server when client has loaded
            if (connected)
            {
                // Get list of players
                SendMessage(null, Command.List);

                // Begin listening
                client.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
            }
            else
            {
                Close();
            }
        }

        private void btnReady_Click(object sender, EventArgs e)
        {
            SendMessage(null, Command.Ready);
        }

        private void drawingBoard_MouseMove(object sender, MouseEventArgs e)
        {
            System.Drawing.SolidBrush myBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
            Rectangle r = new Rectangle();

            switch (e.Button)
            {
                case MouseButtons.Left:

                    if (down)
                    {
                        p2.X = e.X; p2.Y = e.Y;
                        g.DrawLine(System.Drawing.Pens.Black, p1, p2);
                    }
                    down = true;
                    p1.X = e.X; p1.Y = e.Y;
                    break;
            }
        }

        private void drawingBoard_MouseUp(object sender, MouseEventArgs e)
        {
            down = false;
        }

        private void drawingBoard_MouseDown(object sender, MouseEventArgs e)
        {
            g.FillRectangle(System.Drawing.Brushes.Black, e.X, e.Y, 1, 1);
        }
    }
}
