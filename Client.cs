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
        // Client
        Socket client;
        string userName;
        private byte[] byteData = new byte[1024];

        bool mouseDown, isDrawing;
        
        Timer timerCountDown;               // Timer for countdown down seconds when player is drawing

        Results result;

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImportAttribute("user32.dll")]
        public static extern bool ReleaseCapture();

        bool timeLowered;
        bool serverClosing;

        public Client(string userName, Socket client)
        {
            this.userName = userName;
            this.client = client;
            byteData = new byte[1024];
            SendMessage(null, Command.Login);
            InitializeComponent();
            mouseDown = isDrawing = false;
            timerCountDown = new Timer();
            timerCountDown.Interval = 1000;
            timerCountDown.Tick += new EventHandler(LabelCountDownTick);
            serverClosing = false;
            //isDrawing = true;
            //drawingBoard.StartDrawing();
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

        private void SendMessageDrawing(string message, Command command, Point point, int size, int shape, Color color)
        {
            try
            {
                //Fill the info for the message to be send
                Data msgToSend = new Data();

                msgToSend.strName = userName;
                msgToSend.strMessage = message;
                msgToSend.cmdCommand = command;
                msgToSend.point = point;
                msgToSend.color = color;
                msgToSend.size = size;
                msgToSend.shape = shape;

                byte[] byteData = msgToSend.DrawingToByte();

                //Send it to the server
                client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to send message to the server.", "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowChooseWordControl(string word1, string word2, string word3)
        {
            chooseWord.Invoke((MethodInvoker)delegate
            {
                chooseWord.SwitchWords(word1, word2, word3);
                chooseWord.Enabled = chooseWord.Visible = true;
            }); 
        }
        private void HideChooseWordControl()
        {
            chooseWord.Invoke((MethodInvoker)delegate
            {
                chooseWord.Enabled = chooseWord.Visible = false;
            });
        }

        private void StopCountDown()
        {
            countdown.Invoke((MethodInvoker)delegate
            {
                timerCountDown.Stop();
                countdown.Enabled = countdown.Visible = false;
            });
        }

        private void StartCountDown()
        {
            countdown.Invoke((MethodInvoker)delegate
            {
                timerCountDown.Start();
                countdown.Enabled = countdown.Visible = true;
            });
        }

        private void LabelCountDownTick(object sender, EventArgs e)
        {
            if (countdown.Text == "0")
            {
                StopCountDown();
            }
            else
            {
                countdown.Text = (Int32.Parse(countdown.Text)-1).ToString();
            }
        }

        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                client.EndReceive(ar);
                Data msgReceived;
                msgReceived = new Data(byteData, false);
                if (msgReceived.cmdCommand == Command.Stroke || msgReceived.cmdCommand == Command.NewStroke)
                {
                    msgReceived = new Data(byteData, true);
                }
                //Accordingly process the message received
                switch (msgReceived.cmdCommand)
                {
                    // If a , user has connected
                    case Command.Login:
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.AddPlayer(msgReceived.strName);
                        });
                        break;

                    // If a user has disconnected
                    case Command.Logout:
                        playerList.Invoke((MethodInvoker)delegate
                        {
                            playerList.RemovePlayer(msgReceived.strName);
                        });
                        break;
                    case Command.StartGame:
                        btnReady.Invoke((MethodInvoker)delegate {
                            btnReady.Visible = btnReady.Enabled = false;
                        });
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.NewGame();
                        }); 
                        HideLabel();
                        break;
                    case Command.ChooseWord:
                        timeLowered = false;
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.Clear();
                            drawingBoard.StopDrawing();
                            isDrawing = false;
                        });
                        if (result != null)
                        {
                            result.Invoke((MethodInvoker)delegate
                            {
                                drawingBoard.Controls.Remove(result);
                            });
                        }
                        if (msgReceived.strName == userName)
                        {
                            string[] words = msgReceived.strMessage.Split('*');
                            ShowChooseWordControl(words[0], words[1], words[2]);
                            ChangeLabelText("Please choose a word");
                        }
                        else
                        {
                            ChangeLabelText(msgReceived.strName + " is choosing a word");
                        }
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.SetChoosingWord(msgReceived.strName);
                        });
                        StopCountDown();
                        break;
                    case Command.StartDrawing:
                        if (msgReceived.strName == userName)
                        {
                            drawingBoard.Invoke((MethodInvoker)delegate
                            {
                                drawingBoard.StartDrawing();

                            });
                            isDrawing = true;
                        }
                        else
                        {
                            ChangeLabelText(string.Concat(Enumerable.Repeat("_ ", Int32.Parse(msgReceived.strMessage))));
                            isDrawing = false;
                        }
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.SetDrawing(msgReceived.strName);
                        });
                        countdown.Invoke((MethodInvoker)delegate
                        {
                            countdown.Text = "90";
                            StartCountDown();
                        });
                        break;
                    case Command.WordHint:
                        lblWaiting.Invoke((MethodInvoker)delegate
                        {
                            char[] text = lblWaiting.Text.ToCharArray();
                            text[Int32.Parse(msgReceived.strMessage[1].ToString())*2] = msgReceived.strMessage[0];
                            ChangeLabelText(new string(text));
                        });
                        break;
                    case Command.CorrectGuess:
                        playerList.Invoke((MethodInvoker)delegate {
                            if (!timeLowered)
                            {
                                countdown.Text = "30";
                                timeLowered = true;
                            }

                            playerList.AddScore(msgReceived.strName, Int32.Parse(msgReceived.strMessage));
                        });
                        break;
                    case Command.RoundOver:
                        HideLabel();
                        isDrawing = false;
                        StopCountDown();
                        result = new Results(playerList.GetPlayers(), msgReceived.strMessage);
                        result.KeyPress += chat_KeyPress;
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            result.Left = (drawingBoard.Width - result.Width) / 2;
                            result.Top = (drawingBoard.Height - result.Height) / 2;
                            drawingBoard.Controls.Add(result);
                        });
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.EndRound();
                        });
                        break;
                    case Command.GameOver:
                        HideLabel();
                        isDrawing = false;
                        StopCountDown();
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.EndRound();
                        });
                        result.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.Controls.Remove(result);
                        });
                        result = new Results(playerList.GetPlayers());
                        result.KeyPress += chat_KeyPress;
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            result.Left = (drawingBoard.Width - result.Width) / 2;
                            result.Top = (drawingBoard.Height - result.Height) / 2;
                            drawingBoard.Controls.Add(result);
                        });
                        break;
                    case Command.GameOverFinal:
                        result.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.Controls.Remove(result);
                        });
                        ChangeLabelText("Waiting for players to get ready");
                        btnReady.Invoke((MethodInvoker)delegate {
                            btnReady.Visible = btnReady.Enabled = true;
                        });
                        playerList.Invoke((MethodInvoker)delegate {
                            playerList.NewGame();
                        });
                        break;
                    case Command.NewStroke:
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.NewStroke(msgReceived.point, msgReceived.size, msgReceived.color);
                        });
                        break;
                    case Command.Stroke:
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.AddPointToStroke(msgReceived.point);
                        });
                        break;
                    case Command.Message:
                        chat.Invoke((MethodInvoker)delegate {
                            if (chat.Text.Length == 0)
                                chat.AppendText(msgReceived.strMessage);
                            else
                                chat.AppendText("\r\n" + msgReceived.strMessage );
                        });
                        break;
                    case Command.Ready:
                        playerList.Invoke((MethodInvoker)delegate
                        {
                            playerList.Ready(msgReceived.strName);
                        });
                        break;
                    case Command.Clear:
                        drawingBoard.Invoke((MethodInvoker)delegate
                        {
                            drawingBoard.Clear();
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
                            playerList.AddPlayer(userName);
                        });
                        break;
                }
                byteData = new byte[1024];

                client.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);

            }
            catch (ObjectDisposedException) { }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10054)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        serverClosing = true;
                        MessageBox.Show("Server was shut down. Your game will now close.", "Server shutdown");
                        this.Close();
                    });
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Hide the label text
        private void HideLabel()
        {
            lblWaiting.Invoke((MethodInvoker)delegate {
                lblWaiting.Visible = lblWaiting.Enabled = false;
            });
        }

        // Change the label text and make it visible
        private void ChangeLabelText(string text)
        {
            lblWaiting.Invoke((MethodInvoker)delegate {
                lblWaiting.Text = text;
                lblWaiting.Visible = lblWaiting.Enabled = true;
            });
        }


        private void Client_Load(object sender, EventArgs e)
        {
            // Start receiving from server when client has loaded

            // Get list of players
            SendMessage(null, Command.List);

            // Begin listening
            client.BeginReceive(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(OnReceive), null);
        }

        private void btnReady_Click(object sender, EventArgs e)
        {
            SendMessage(null, Command.Ready);
        }


        private void drawingBoard_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                mouseDown = false;
            }
        }

        private void drawingBoard_MouseDown(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                mouseDown = true;
                drawingBoard.Invoke((MethodInvoker)delegate
                {
                    drawingBoard.NewStroke(e.Location);
                });
                SendMessageDrawing(null, Command.NewStroke, e.Location, drawingBoard.GetPenSize(), 0, drawingBoard.GetPenColor());                
            }
        }

        private void drawingBoard_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                if (mouseDown)
                {
                    drawingBoard.Invoke((MethodInvoker)delegate
                    {
                        drawingBoard.AddPointToStroke(e.Location);
                    });
                    SendMessageDrawing(null, Command.Stroke, e.Location, drawingBoard.GetPenSize(), 0, drawingBoard.GetPenColor());
                } 
            }
        }

        private void Client_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!serverClosing)
            {
                if (MessageBox.Show("Are you sure you want to exit?", "Pictionary", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
                try
                {
                    //Send a message to logout of the server
                    Data msgToSend = new Data();
                    msgToSend.cmdCommand = Command.Logout;
                    msgToSend.strName = userName;
                    msgToSend.strMessage = null;

                    byte[] b = msgToSend.ToByte();
                    client.Send(b, 0, b.Length, SocketFlags.None);
                    client.Close();
                }
                catch (ObjectDisposedException)
                { }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Pictionary", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void chooseWord_NewWordChosen(object sender, EventArgs e)
        {
            Button b = (Button)sender;        
            HideChooseWordControl();
            SendMessage(b.Text, Command.ChooseWord);
            ChangeLabelText("You are drawing " + b.Text);
        }
        private void input_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                if (input.TextLength > 0)
                {
                    SendMessage(input.Text, Command.Message);
                    input.Clear();
                }
                e.Handled = true;
            }
        }
        private void chat_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Char.IsControl(e.KeyChar))
                input.AppendText(e.KeyChar.ToString());
            else
                input_KeyPress(sender, e);
            input.Focus();
            e.Handled = true;
        }

        private void drawingBoard_ButtonClearClick(object sender, EventArgs e)
        {
            SendMessage(null, Command.Clear);
        }

        private void DragWindow(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void btnMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnMinimize_MouseEnter(object sender, EventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimizeclick;
        }

        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.closeclick;
        }

        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            btnClose.BackgroundImage = Properties.Resources.close;
        }

        private void btnMinimize_MouseLeave(object sender, EventArgs e)
        {
            btnMinimize.BackgroundImage = Properties.Resources.minimize;
        }

        private void Client_Enter(object sender, EventArgs e)
        {
            input.Focus();
        }
    }
}