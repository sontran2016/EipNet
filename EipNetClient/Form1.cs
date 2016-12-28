using EIPNET.EIP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EipNetClient
{
    public partial class Form1 : Form
    {
        private static string _server = "192.168.1.64";//"192.168.1.117"
        private static int _port = 44818;
        private static SessionInfo _si;
        public Form1()
        {
            InitializeComponent();

            btnConnect.Enabled = true;
            btnDisConnect.Enabled = false;
            btnSend.Enabled = false;
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            txtServer.Text = _server;
            txtPort.Text = ""+_port;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_si!=null && _si.Connected)
            {
                SendData("Exit", EncapsCommand.UnRegisterSession);
                SessionManager.UnRegister(_si);                
            }
        }
        private static EncapsReply SendData(string data, EncapsCommand cmd= EncapsCommand.NOP)
        {
            byte[] bytes = Encoding.Default.GetBytes(data);
            EncapsPacket request;
            switch (cmd)
            {
                case EncapsCommand.UnRegisterSession:
                    request = EncapsPacketFactory.CreateUnRegisterSession(_si.SessionHandle, _si.SenderContext);
                    break;
                default:
                    request = EncapsPacketFactory.CreateNOP(bytes);
                    break;
            }
            //var request = new EncapsPacket();
            //request.EncapsData = bytes;
            //request.Length = (ushort)data.Length;
            //request.Command = (ushort)cmd;
            var msg = request.Pack();
            var ar = _si.SendData_WaitReply(msg);
            var reply=new EncapsReply();
            int tmp;
            if(ar!=null)
                reply.Expand(ar,0,out tmp);
            return reply;
        }
        private static string GetData(byte[] bytes)
        {
            var txt = Encoding.Default.GetString(bytes, 0, bytes.Length);
            return txt;
        }
        private void ChangeEnableButton()
        {
            btnConnect.Enabled = !btnConnect.Enabled;
            btnDisConnect.Enabled = !btnDisConnect.Enabled;
            btnSend.Enabled = !btnSend.Enabled;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                var hostName = Dns.GetHostName();
                //IPHostEntry ipHostInfo = Dns.GetHostEntry(hostName);
                //IPAddress ipAddress = ipHostInfo.AddressList.Single(x => x.AddressFamily == AddressFamily.InterNetwork);

                var context = Encoding.Default.GetBytes(hostName);
                _si = SessionManager.CreateAndRegister(txtServer.Text, int.Parse(txtPort.Text),context);
                if(!_si.Connected)
                    throw new Exception(_si.LastSessionErrorString);
                ChangeEnableButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnDisConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (_si.Connected)
                {
                    var reply = SendData("Exit", EncapsCommand.UnRegisterSession);
                    if (reply.Length > 0)
                        SessionManager.UnRegister(_si);
                }
                ChangeEnableButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (_si.Connected)
                {
                    var reply = SendData(txtMsg.Text);
                    if (reply.Length > 0)
                    {
                        var st = GetData(reply.EncapsData);
                        if (txtReceive.Lines.Length == 0)
                            txtReceive.Text = st;
                        else
                            txtReceive.AppendText("\r\n" + st);
                    }
                    else
                    {
                        ChangeEnableButton();
                        throw new Exception("Server not reply");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
