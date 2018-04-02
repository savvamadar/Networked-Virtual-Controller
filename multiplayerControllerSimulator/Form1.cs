using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using vGenInterfaceWrap;
using System.Net.Sockets;
using System.Net;

namespace multiplayerControllerSimulator
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        static public vGen joystick;
        static public vGen.JoystickState iReport;

        static public uint id = 1;

        bool startNetworking = false;
        int isLan = -1;
        int isHost = -1;
        string input = "a";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            checkBox1.Enabled = false;
            checkBox1.Visible = false;
            checkBox2.Enabled = false;
            checkBox2.Visible = false;
            consoleOut("Hover over a button for more info.\n");
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (id > 0 && id <= 16)
            {
                if (joystick != null)
                {
                    if(joystick.isVBusExist() == 0)
                    {
                        for (int i = 1; i < 5; i++)
                        {
                            bool exists = false;
                            joystick.isControllerPluggedIn((uint)i, ref exists);
                            if (exists)
                            {
                                joystick.UnPlugForce((uint)i);
                            }
                        }
                    }
                    joystick.RelinquishVJD(id);
                }
            }
        }

        //network button
        private void button2_Click(object sender, EventArgs e)
        {
            buttonManager(1);
        }

        //offline button
        private void button1_Click(object sender, EventArgs e)
        {
            buttonManager(0);
        }

        private void buttonManager(int i)
        {
            if (isLan == -1)
            {
                isLan = i;
                if (i == 1)
                {
                    button1.Text = "Host";
                    toolTip1.SetToolTip(button1, "Click this if the game will be running on your PC.\nYou will need a screen sharing program.");
                    button2.Text = "Join";
                    toolTip2.SetToolTip(button2, "Click this if the game will NOT be running on your PC.\nThe host will need to share their screen with you.");
                    tipUpdate("If your PC will be running the game click 'Host'.\nOtherwise click 'Join'.");
                }
                else
                {
                    button1.Enabled = false;
                    button2.Enabled = false;
                    checkBox2.Text = "Xbox Input";
                    tipUpdate("Checking to make sure all the vJoy dll has loaded correctly.");
                    if (vJoySetup())
                    {
                        keyMapper();
                        keyBoardListenerLocal();
                    }
                    else
                    {
                        tipUpdate("Couldn't load the vJoy dll correctly. Check the console output.");
                    }
                }
            }
            else
            {
                button1.Enabled = false;
                button2.Enabled = false;
                if (i == 0)
                {
                    toolTip5.SetToolTip(checkBox2, "By checking this you allow the connected party to simulate using an xbox controller.");
                    toolTip4.SetToolTip(checkBox1, "By checking this you allow the connected party to simulate pressing your actual keyboard rather than a virtual controller.");
                    if (vJoySetup())
                    {
                        isHost = 1;
                        startNetListen();
                    }
                    else
                    {
                        tipUpdate("Couldn't load the vJoy dll correctly. Check the console output.");
                    }
                }
                else
                {
                    checkBox1.Text = "Send Raw Input";
                    checkBox2.Text = "Send as Xbox Input";
                    isHost = 0;
                    toolTip5.SetToolTip(checkBox2, "By checking this you will be sending input as if you were using an Xbox Controller.");
                    toolTip4.SetToolTip(checkBox1, "By checking this you will be sending actual keyboard input rather than a virtual controller input.");
                    keyMapper();
                }
            }
        }

        int port = -1;
        string IP = "";
        string localIP = "";

        int keyDown = 1;
        int keyUp = 2;

        private void startNetListen()
        {
            input = "";
            while ((!int.TryParse(input, out port)) || (port > 65535 || port < 1))
            {
                input = Microsoft.VisualBasic.Interaction.InputBox("What port would you like to host on: ", "Port Select", "25003", -1, -1);
            }

            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();
                }
            }
            catch (Exception ex)
            {
                tipUpdate("Couldn't get a local IP, try restarting your pc.");
                consoleOut("Couldn't get local IP... Something went horribly wrong.");
            }

            try
            {
                WebClient client = new WebClient();
                byte[] response = client.DownloadData("http://checkip.dyndns.org");
                IP = new UTF8Encoding().GetString(response).Split(':')[1].Split('<')[0].Trim(' ');
            }
            catch (Exception ex)
            {
                tipUpdate("Couldn't get a Public IP. Check if you are connected to the internet.");
                consoleOut("Couldn't get a Public IP.");
            }

            tipUpdate("Tell your partner to connect: \n- Public IP: " + IP + "\n- Local IP: " + localIP + "\n- Port: " + port);

            var udpListener = new Thread(() => {
                UdpClient udpServer = new UdpClient(port);
                IPEndPoint otherPC = new IPEndPoint(IPAddress.Any, port);
                List<int> heldKeys = new List<int>();
                bool firstConnection = false;
                while (true)
                {
                    byte[] receivedBytes = udpServer.Receive(ref otherPC);
                    if (!firstConnection)
                    {
                        firstConnection = true;
                        consoleOut("Connection established!");
                        tipUpdate("Connection from: " + otherPC.Address.ToString() + ":" + otherPC.Port.ToString());
                        if (enableCheckboxes == false)
                        {
                            this.checkBox2.BeginInvoke((MethodInvoker)delegate ()
                            {
                                checkBox2.Visible = true;
                                checkBox2.Enabled = true;
                            });
                            this.checkBox1.BeginInvoke((MethodInvoker)delegate ()
                            {
                                checkBox1.Visible = true;
                                checkBox1.Enabled = true;
                            });
                            enableCheckboxes = true;
                        }
                    }
                    string clientMessage = Encoding.UTF8.GetString(receivedBytes);
                    if (!rawInput)
                    {
                        if (!xboxInput)
                        {
                            for (int i = 0; i < clientMessage.Length; i++)
                            {
                                if (clientMessage[i] == '1')
                                {
                                    joystick.SetBtn(true, id, (uint)(i + 1));
                                }
                                else
                                {
                                    joystick.SetBtn(false, id, (uint)(i + 1));
                                }
                            }
                        }
                        else
                        {
                            if (clientMessage.Length == 24)
                            {
                                    if (clientMessage[23] == '1')
                                    {
                                        joystick.SetAxisLy(1, 32767);
                                    }
                                    else if (clientMessage[22] == '1')
                                    {
                                        joystick.SetAxisLy(1, -32768);
                                    }
                                    else
                                    {
                                        joystick.SetAxisLy(1, 0);
                                    }
                                    if (clientMessage[21] == '1')
                                    {
                                        joystick.SetAxisLx(1, -32768);
                                    }
                                    else if (clientMessage[20] == '1')
                                    {
                                        joystick.SetAxisLx(1, 32767);
                                    }
                                    else
                                    {
                                        joystick.SetAxisLx(1, 0);
                                    }
                                    if (clientMessage[19] == '1')
                                    {
                                        joystick.SetAxisRy(1, 32767);
                                    }
                                    else if (clientMessage[18] == '1')
                                    {
                                        joystick.SetAxisRy(1, -32768);
                                    }
                                    else
                                    {
                                        joystick.SetAxisRy(1, 0);
                                    }
                                    if (clientMessage[17] == '1')
                                    {
                                        joystick.SetAxisRx(1, -32768);
                                    }
                                    else if (clientMessage[16] == '1')
                                    {
                                        joystick.SetAxisRx(1, 32767);
                                    }
                                    else
                                    {
                                        joystick.SetAxisRx(1, 0);
                                    }
                                    if (clientMessage[15] == '1')
                                    {
                                        joystick.SetTriggerL(1, 255);
                                    }
                                    else
                                    {
                                        joystick.SetTriggerL(1, 0);
                                    }
                                    if (clientMessage[14] == '1')
                                    {
                                        joystick.SetTriggerR(1, 255);
                                    }
                                    else
                                    {
                                        joystick.SetTriggerR(1, 0);
                                    }
                                    if (clientMessage[13] == '1')
                                    {
                                        joystick.SetButton(1, 0x1000, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x1000, false);
                                    }
                                    if (clientMessage[12] == '1')
                                    {
                                        joystick.SetButton(1, 0x2000, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x2000, false);
                                    }
                                    if (clientMessage[11] == '1')
                                    {
                                        joystick.SetButton(1, 0x4000, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x4000, false);
                                    }
                                    if (clientMessage[10] == '1')
                                    {
                                        joystick.SetButton(1, 0x8000, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x8000, false);
                                    }
                                    if (clientMessage[9] == '1')
                                    {
                                        joystick.SetButton(1, 0x010, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x010, false);
                                    }
                                    if (clientMessage[8] == '1')
                                    {
                                        joystick.SetButton(1, 0x0200, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x0200, false);
                                    }
                                    if (clientMessage[7] == '1')
                                    {
                                        joystick.SetButton(1, 0x0020, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x0020, false);
                                    }
                                    if (clientMessage[6] == '1')
                                    {
                                        joystick.SetButton(1, 0x0010, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x0010, false);
                                    }
                                    if (clientMessage[5] == '1')
                                    {
                                        joystick.SetButton(1, 0x0040, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x0040, false);
                                    }
                                    if (clientMessage[4] == '1')
                                    {
                                        joystick.SetButton(1, 0x0080, true);
                                    }
                                    else
                                    {
                                        joystick.SetButton(1, 0x0080, false);
                                    }
                                    if (clientMessage[3] == '1')
                                    {
                                        joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_UP);
                                    }
                                    else if (clientMessage[2] == '1')
                                    {
                                        joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_DOWN);
                                    }
                                    else if (clientMessage[1] == '1')
                                    {
                                        joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_LRFT); //uhhh lrft should be left
                                    }
                                    else if (clientMessage[0] == '1')
                                    {
                                        joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_RIGHT);
                                    }
                                    else
                                    {
                                        joystick.SetDpad(1, 0);
                                    }
                            }
                        }
                    }
                    else
                    {
                        List<string> keyIntStrs = clientMessage.Split(',').ToList();
                        for (int i = heldKeys.Count - 1; i >= 0; i--)
                        {
                            if (!keyIntStrs.Contains(heldKeys[i] + ""))
                            {
                                keybd_event((byte)heldKeys[i], 0, keyUp, 0);
                                heldKeys.RemoveAt(i);
                            }
                            else
                            {
                                keyIntStrs.Remove(heldKeys[i] + "");
                            }
                        }
                        for (int i = 0; i < keyIntStrs.Count; i++)
                        {
                            heldKeys.Add(int.Parse(keyIntStrs[i]));
                            keybd_event((byte)heldKeys[heldKeys.Count - 1], 0, keyDown, 0);
                        }
                    }
                }
            });
            udpListener.IsBackground = true;
            udpListener.Start();
            consoleOut("Started listening on port: " + port);
            consoleOut("Public IP: " + IP);
            consoleOut("Local IP: " + localIP);
        }

        private void startNetSend()
        {
            while (IP == "")
            {
                IP = Microsoft.VisualBasic.Interaction.InputBox("Type in the IP to join: ", "IP Select", "127.0.0.1", -1, -1).Trim();
            }

            input = "";
            while ((!int.TryParse(input, out port)) || (port > 65535 || port < 1))
            {
                input = Microsoft.VisualBasic.Interaction.InputBox("Port to join: ", "Port Select", "25003", -1, -1);
            }


            var udpSender = new Thread(() =>
            {
                UdpClient udpClient = new UdpClient(port);
                IPEndPoint otherPC = new IPEndPoint(IPAddress.Parse(IP), port);
                tipUpdate("Use the mapped buttons to send inputs.\nIf the host isn't getting your inputs:\n- The host isn't portforwarded on UDP port " + port + "\n- You fat fingered something.");
                consoleOut("Connected to: " + IP + ":" + port);
                consoleOut("(If they are listening)");
                while (true)
                {
                    if (startNetworking)
                    {
                        if (enableCheckboxes == false)
                        {
                            this.checkBox2.BeginInvoke((MethodInvoker)delegate ()
                            {
                                checkBox2.Visible = true;
                                checkBox2.Enabled = true;
                            });
                            this.checkBox1.BeginInvoke((MethodInvoker)delegate ()
                            {
                                checkBox1.Visible = true;
                                checkBox1.Enabled = true;
                            });
                            enableCheckboxes = true;
                        }
                        string inputs = "";
                        if (!xboxMapped)
                        {
                            for (int i = 0; i < mappedKeys.Length; i++)
                            {
                                if (!rawInput)
                                {
                                    if ((GetAsyncKeyState((int)(mappedKeys[i])) & 0x8000) > 0)
                                    {
                                        inputs += "1";
                                    }
                                    else
                                    {
                                        inputs += "0";
                                    }
                                }
                                else if ((GetAsyncKeyState((int)(mappedKeys[i])) & 0x8000) > 0)
                                {
                                    inputs += (int)(mappedKeys[i]) + ",";
                                }
                            }
                            if (inputs.IndexOf(",") != -1)
                            {
                                inputs = inputs.Substring(0, inputs.Length - 1);
                            }
                        }
                        else
                        {
                            for (int i = 0; i < xboxMappedKeys.Length; i++)
                            {
                                if ((GetAsyncKeyState((int)(xboxMappedKeys[i])) & 0x8000) > 0)
                                {
                                    inputs += "1";
                                }
                                else
                                {
                                    inputs += "0";
                                }
                            }
                        }
                        if (inputs.Trim().Length != 0)
                        {
                            byte[] data = Encoding.UTF8.GetBytes(inputs);
                            udpClient.Send(data, data.Length, otherPC);
                        }
                    }
                    Thread.Sleep(10);
                }
            });
            udpSender.IsBackground = true;
            udpSender.Start();
        }


        private bool vJoySetup()
        {
            joystick = new vGen();
            iReport = new vGen.JoystickState();
            if (id <= 0 || id > 16)
            {
                consoleOut("Illegal device ID " + id + "!");
            }
            if (!joystick.vJoyEnabled())
            {
                consoleOut("vJoy driver not enabled: Failed Getting vJoy attributes.");
                return false;
            }
            else
            {
                consoleOut("Vendor: " + joystick.GetvJoyManufacturerString() + "\nProduct: " + joystick.GetvJoyProductString() + "\nVersion Number: " + joystick.GetvJoySerialNumberString() + ".");
            }
            VjdStat st = joystick.GetVJDStatus(id);
            switch (st)
            {
                case VjdStat.VJD_STAT_OWN:
                    consoleOut("vJoy Device " + id + " is already owned by this feeder.");
                    break;
                case VjdStat.VJD_STAT_FREE:
                    consoleOut("vJoy Device " + id + " is free.");
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    joystick.RelinquishVJD(id);
                    consoleOut("vJoy Device " + id + " is already owned by another feeder - Trying to recover.");
                    return false;
                case VjdStat.VJD_STAT_MISS:
                    consoleOut("vJoy Device " + id + " is not installed or disabled - Cannot continue.");
                    return false;
                default:
                    consoleOut("vJoy Device " + id + " general error - Cannot continue.");
                    return false;
            };

            st = joystick.GetVJDStatus(id);

            if ((st == VjdStat.VJD_STAT_OWN) || ((st == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                consoleOut("Failed to acquire vJoy device number " + id + ".");
            }
            else
            {
                consoleOut("Acquired vJoy device number " + id + ".");
            }
            int nButtons = joystick.GetVJDButtonNumber(id);
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
            {
                consoleOut("Version of Driver Matches DLL Version (" + DllVer + ")");
            }
            else
            {
                consoleOut("Version of Driver (" + DrvVer + ") does NOT match DLL Version (" + DllVer + ")");
            }
            joystick.ResetVJD(id);
            for (uint i = 1; i <= nButtons; i++)
            {
                consoleOut(i + ") OFF: " + joystick.SetBtn(false, id, i));
            }
            consoleOut("\nvJoy setup completed succesfully!\n");
            return true;
        }

        bool enableCheckboxes = false;
        private void keyBoardListenerLocal()
        {
            var keyBoardListener = new Thread(() =>
            {
                while (true)
                {
                    if (startNetworking)
                    {
                        if(enableCheckboxes == false)
                        {
                            this.checkBox2.BeginInvoke((MethodInvoker)delegate ()
                            {
                                checkBox2.Visible = true;
                                checkBox2.Enabled = true;
                            });
                            enableCheckboxes = true;
                        }
                        if (!xboxInput)
                        {
                            for (int i = 0; i < mappedKeys.Length; i++)
                            {
                                if ((GetAsyncKeyState((int)(mappedKeys[i])) & 0x8000) > 0)
                                {
                                    joystick.SetBtn(true, id, (uint)(i + 1));
                                }
                                else
                                {
                                    joystick.SetBtn(false, id, (uint)(i + 1));
                                }
                            }
                        }
                        else if (xboxMapped)
                        {
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[23])) & 0x8000) > 0)
                            {
                                joystick.SetAxisLy(1, 32767);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[22])) & 0x8000) > 0)
                            {
                                joystick.SetAxisLy(1, -32768);
                            }
                            else
                            {
                                joystick.SetAxisLy(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[21])) & 0x8000) > 0)
                            {
                                joystick.SetAxisLx(1, -32768);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[20])) & 0x8000) > 0)
                            {
                                joystick.SetAxisLx(1, 32767);
                            }
                            else
                            {
                                joystick.SetAxisLx(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[19])) & 0x8000) > 0)
                            {
                                joystick.SetAxisRy(1, 32767);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[18])) & 0x8000) > 0)
                            {
                                joystick.SetAxisRy(1, -32768);
                            }
                            else
                            {
                                joystick.SetAxisRy(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[17])) & 0x8000) > 0)
                            {
                                joystick.SetAxisRx(1, -32768);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[16])) & 0x8000) > 0)
                            {
                                joystick.SetAxisRx(1, 32767);
                            }
                            else
                            {
                                joystick.SetAxisRx(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[15])) & 0x8000) > 0)
                            {
                                joystick.SetTriggerL(1, 255);
                            }
                            else
                            {
                                joystick.SetTriggerL(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[14])) & 0x8000) > 0)
                            {
                                joystick.SetTriggerR(1, 255);
                            }
                            else
                            {
                                joystick.SetTriggerR(1, 0);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[13])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x1000, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x1000, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[12])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x2000, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x2000, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[11])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x4000, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x4000, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[10])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x8000, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x8000, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[9])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0100, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0100, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[8])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0200, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0200, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[7])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0020, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0020, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[6])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0010, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0010, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[5])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0040, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0040, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[4])) & 0x8000) > 0)
                            {
                                joystick.SetButton(1, 0x0080, true);
                            }
                            else
                            {
                                joystick.SetButton(1, 0x0080, false);
                            }
                            if ((GetAsyncKeyState((int)(xboxMappedKeys[3])) & 0x8000) > 0)
                            {
                                joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_UP);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[2])) & 0x8000) > 0)
                            {
                                joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_DOWN);
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[1])) & 0x8000) > 0)
                            {
                                joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_LRFT); //uhhh lrft should be left
                            }
                            else if ((GetAsyncKeyState((int)(xboxMappedKeys[0])) & 0x8000) > 0)
                            {
                                joystick.SetDpad(1, vGen.XINPUT_GAMEPAD_DPAD_RIGHT);
                            }
                            else
                            {
                                joystick.SetDpad(1, 0);
                            }
                        }
                    }
                    Thread.Sleep(10);
                }
            });
            keyBoardListener.IsBackground = true;
            keyBoardListener.Start();
        }

        private void consoleOut(String s)
        {
            try
            {
                richTextBox1.Text += s + "\n";
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
            }
            catch (Exception e)
            {

                this.richTextBox1.BeginInvoke((MethodInvoker)delegate () {
                    richTextBox1.Text += s + "\n";
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.ScrollToCaret();
                    
                });
            }
        }

        private void consoleOut(String s, bool b)
        {
            try
            {
                richTextBox1.Text += s + (b ? "\n" : "");
                richTextBox1.SelectionStart = richTextBox1.TextLength;
                richTextBox1.ScrollToCaret();
            }
            catch (Exception e)
            {
                this.richTextBox1.BeginInvoke((MethodInvoker)delegate () {
                    richTextBox1.Text += s + (b ? "\n" : "");
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.ScrollToCaret();
                });
            }
        }

        private void tipUpdate(String s)
        {
            try
            {
                toolTip3.SetToolTip(label2, s);
            }
            catch (Exception e)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    toolTip3.SetToolTip(label2, s);
                });
            }
        }

        Keys[] mappedKeys = new Keys[0];
        Keys[] xboxMappedKeys = new Keys[0];

        private void keyMapper()
        {
            consoleOut("Select how many keys you would like to map.");
            tipUpdate("Select how many keys you would like to map.");
            int numKeysToMap = 32;

            while ((!int.TryParse(input, out numKeysToMap)) || (numKeysToMap > 32 || numKeysToMap <= 0))
            {
                input = Microsoft.VisualBasic.Interaction.InputBox("How many keys do you want to assign?", "Key Amount", "32", -1, -1);
            }


            mappedKeys = new Keys[numKeysToMap];
            var keyListener = new Thread(() => {
                Thread.Sleep(1000);
                consoleOut("Press any key, or mouse button, to map it.");
                tipUpdate("Press any key, or mouse button, to map it.");
                var values = Enum.GetValues(typeof(Keys)).Cast<Keys>();
                while (numKeysToMap > 0)
                {
                    bool keyAdded = true;
                    try
                    {
                        int mostSpecificKeyValue = 0;
                        for (int i = 1; i < values.Count(); i++)
                        {
                            if ((GetAsyncKeyState((int)(values.ElementAt(i))) & 0x8000) > 0)
                            {
                                for (int j = numKeysToMap - 1; j < mappedKeys.Length; j++)
                                {
                                    if ((int)mappedKeys[j] == (int)values.ElementAt(i))
                                    {
                                        keyAdded = false;
                                        mostSpecificKeyValue = -1;
                                    }
                                }
                                if (keyAdded)
                                {
                                    mostSpecificKeyValue = i;
                                }
                            }
                        }
                        if (mostSpecificKeyValue > 0)
                        {
                            consoleOut("Key " + (mappedKeys.Length - numKeysToMap + 1) + " = " + values.ElementAt(mostSpecificKeyValue).ToString());
                            mappedKeys[numKeysToMap - 1] = values.ElementAt(mostSpecificKeyValue);
                            numKeysToMap--;
                        }
                    }
                    catch (Exception ex)
                    {
                        consoleOut(ex.ToString());
                    }
                    Thread.Sleep(10);
                }
                startNetworking = true;
                tipUpdate("The setup has completed succesfully. Your keys are now mapped!");
                consoleOut("The setup has completed succesfully. Your keys are now mapped!");
                if (isLan == 1)
                {
                    startNetSend();
                }
                Thread.CurrentThread.Abort();
            });
            keyListener.IsBackground = true;
            keyListener.Start();
        }


        bool xboxMapped = false;
        private void xboxKeyMapper()
        {
            consoleOut("Beginning key mapping process.");
            tipUpdate("Beginning key mapping process.");

            xboxMappedKeys = new Keys[24];

            var xboxKeyListener = new Thread(() => {
                Thread.Sleep(1000);
                consoleOut("Press any key or mouse button to map it to an xbox button: ");
                tipUpdate("Press any key or mouse button to map it to an xbox button: ");
                Thread.Sleep(1000);
                var values = Enum.GetValues(typeof(Keys)).Cast<Keys>();

                int numKeysToMap = 24;
                int oldKeysLeft = 0;

                while (numKeysToMap > 0)
                {
                    bool keyAdded = true;
                    if (oldKeysLeft != numKeysToMap)
                    {
                        oldKeysLeft = numKeysToMap;
                        if (numKeysToMap == 24)
                        {
                            consoleOut("Left Stick Up = ", false);
                        }
                        else if (numKeysToMap == 23)
                        {
                            consoleOut("Left Stick Down = ", false);
                        }
                        else if (numKeysToMap == 22)
                        {
                            consoleOut("Left Stick Left = ", false);
                        }
                        else if (numKeysToMap == 21)
                        {
                            consoleOut("Left Stick Right = ", false);
                        }
                        else if (numKeysToMap == 20)
                        {
                            consoleOut("Right Stick Up = ", false);
                        }
                        else if (numKeysToMap == 19)
                        {
                            consoleOut("Right Stick Down = ", false);
                        }
                        else if (numKeysToMap == 18)
                        {
                            consoleOut("Right Stick Left = ", false);
                        }
                        else if (numKeysToMap == 17)
                        {
                            consoleOut("Right Stick Right = ", false);
                        }
                        else if (numKeysToMap == 16)
                        {
                            consoleOut("Left Trigger = ", false);
                        }
                        else if (numKeysToMap == 15)
                        {
                            consoleOut("Right Trigger = ", false);
                        }
                        else if (numKeysToMap == 14)
                        {
                            consoleOut("A = ", false);
                        }
                        else if (numKeysToMap == 13)
                        {
                            consoleOut("B = ", false);
                        }
                        else if (numKeysToMap == 12)
                        {
                            consoleOut("X = ", false);
                        }
                        else if (numKeysToMap == 11)
                        {
                            consoleOut("Y = ", false);
                        }
                        else if (numKeysToMap == 10)
                        {
                            consoleOut("Left Shoulder = ", false);
                        }
                        else if (numKeysToMap == 9)
                        {
                            consoleOut("Right Shoulder = ", false);
                        }
                        else if (numKeysToMap == 8)
                        {
                            consoleOut("Back = ", false);
                        }
                        else if (numKeysToMap == 7)
                        {
                            consoleOut("Start = ", false);
                        }
                        else if (numKeysToMap == 6)
                        {
                            consoleOut("Left Thumbstick Click = ", false);
                        }
                        else if (numKeysToMap == 5)
                        {
                            consoleOut("Right Thumbstick Click = ", false);
                        }
                        else if (numKeysToMap == 4)
                        {
                            consoleOut("DPAD Up = ", false);
                        }
                        else if (numKeysToMap == 3)
                        {
                            consoleOut("DPAD Down = ", false);
                        }
                        else if (numKeysToMap == 2)
                        {
                            consoleOut("DPAD Left = ", false);
                        }
                        else if (numKeysToMap == 1)
                        {
                            consoleOut("DPAD Right = ", false);
                        }

                    }
                    try
                    {
                        int mostSpecificKeyValue = 0;

                        for (int i = 1; i < values.Count(); i++)
                        {
                            if ((GetAsyncKeyState((int)(values.ElementAt(i))) & 0x8000) > 0)
                            {
                                for (int j = numKeysToMap - 1; j < xboxMappedKeys.Length; j++)
                                {
                                    if ((int)xboxMappedKeys[j] == (int)values.ElementAt(i))
                                    {
                                        keyAdded = false;
                                        mostSpecificKeyValue = -1;
                                    }
                                }
                                if (keyAdded)
                                {
                                    mostSpecificKeyValue = i;
                                }
                            }
                        }
                        if (mostSpecificKeyValue > 0)
                        {
                            consoleOut(values.ElementAt(mostSpecificKeyValue).ToString(),true);
                            xboxMappedKeys[numKeysToMap - 1] = values.ElementAt(mostSpecificKeyValue);
                            numKeysToMap--;
                        }
                    }
                    catch (Exception ex)
                    {
                        consoleOut(ex.ToString());
                    }
                    Thread.Sleep(10);
                }
                startNetworking = true;
                tipUpdate("The xbox setup has completed succesfully. Your keys are now mapped!");
                consoleOut("The xbox setup has completed succesfully. Your keys are now mapped!");
                xboxMapped = true;
                Thread.CurrentThread.Abort();
            });
            xboxKeyListener.IsBackground = true;
            xboxKeyListener.Start();
        }

        bool rawInput = false;
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox2.Checked = false;
            }
            if (isLan == 1)
            {
                rawInput = checkBox1.Checked;
                if (rawInput)
                {
                    if (isHost == 1)
                    {
                        toolTip4.SetToolTip(checkBox1, "By unchecking this you force the connected party to simulate pressing a virtual controller.");
                    }
                    else
                    {
                        toolTip4.SetToolTip(checkBox1, "By unchecking this you will simulate pressing a virtual controller.");
                    }
                }
                else
                {
                    if (isHost == 1)
                    {
                        toolTip4.SetToolTip(checkBox1, "By checking this you allow the connected party to simulate pressing your actual keyboard rather than a virtual controller.");
                    }
                    else
                    {
                        toolTip4.SetToolTip(checkBox1, "By checking this you will be sending actual keyboard input rather than a virtual controller input.");
                    }
                }
            }
        }

        uint stat;
        private void vJoyXboxSetup()
        {
            joystick = new vGen();
            iReport = new vGen.JoystickState();
            stat = joystick.isVBusExist();
            if (stat == 0)
            {
                stat = joystick.PlugIn(1);
                if (stat == 0)
                {
                    consoleOut("vXbox device 1 was plugged in.");
                    if (!xboxMapped && isLan==0)
                    {
                        xboxKeyMapper();
                    }
                }
                else
                {
                    consoleOut("Couldn't plugin vXbox Controller, but driver exists.");
                    checkBox2.Checked = false;
                }

            }
            else
            {
                consoleOut("Virtual Xbox Driver not installed.");
                checkBox2.Checked = false;
                checkBox2.Enabled = false;
            }

        }

        bool xboxInput = false;
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (id > 0 && id <= 16)
            {
                if (joystick != null)
                {
                    if (joystick.isVBusExist() == 0)
                    {
                        for (int i = 1; i < 5; i++)
                        {
                            bool exists = false;
                            joystick.isControllerPluggedIn((uint)i, ref exists);
                            if (exists)
                            {
                                joystick.UnPlugForce((uint)i);
                            }
                        }
                    }
                    joystick.RelinquishVJD(id);
                }
            }
            if (checkBox2.Checked)
            {
                checkBox1.Checked = false;
                xboxInput = true;
                if ((isLan == 1 && isHost==1) || (isLan==0))
                {
                    vJoyXboxSetup();
                }
                else if(isLan==1 && isHost != 1)
                {
                    xboxKeyMapper();
                }
            }
            else
            {
                xboxInput = false;
                if ((isLan == 1 && isHost == 1) || (isLan == 0))
                {
                    vJoySetup();
                }
            }
        }
    }
}
