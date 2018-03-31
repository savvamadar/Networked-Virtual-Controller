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
using vJoyInterfaceWrap;
using System.Net.Sockets;
using System.Net;

namespace multiplayerControllerSimulator
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        static public vJoy joystick;
        static public vJoy.JoystickState iReport;

        static public uint id = 1;

        bool startListening = false;
        int isLan = -1;
        string input = "a";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            consoleOut("Hover over a button for more info.\n");
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (id > 0 && id <= 16)
            {
                if (joystick != null)
                {
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
                    if (vJoySetup())
                    {
                        startNetListen();
                    }
                    else
                    {
                        tipUpdate("Couldn't load the vJoy dll correctly. Check the console output.");
                    }
                }
                else
                {
                    keyMapper();
                    //startNetSend();
                }
            }
        }

        int port = -1;
        string IP = "";
        string localIP = "";
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
            catch(Exception ex)
            {
                tipUpdate("Couldn't get a Public IP. Check if you are connected to the internet.");
                consoleOut("Couldn't get a Public IP.");
            }

            tipUpdate("Tell your partner to connect: \n- Public IP: " +IP+"\n- Local IP: "+localIP+"\n- Port: "+port);

            var udpListener = new Thread(() => {
                UdpClient udpServer = new UdpClient(port);
                IPEndPoint otherPC = new IPEndPoint(IPAddress.Any, port);
                bool firstConnection = false;
                while (true)
                {
                    byte[] receivedBytes = udpServer.Receive(ref otherPC);      // Receive the information from the client as byte array
                    if(firstConnection == false)
                    {
                        firstConnection = true;
                        consoleOut("Connection established!");
                        tipUpdate("Connection from: " + otherPC.Address.ToString()+":"+ otherPC.Port.ToString());
                    }
                    string clientMessage = Encoding.UTF8.GetString(receivedBytes);
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
                tipUpdate("Use the mapped buttons to send inputs.\nIf the host isn't getting your inputs:\n- The host isn't portforwarded on UDP port " +port+"\n- You fat fingered something.");
                consoleOut("Connected to: " + IP + ":" + port);
                consoleOut("(If they are listening)");
                while (true)
                {
                    if (startListening)
                    {
                        string inputs = "";
                        for (int i = 0; i < mappedKeys.Length; i++)
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
                        byte[] data = Encoding.UTF8.GetBytes(inputs);
                        udpClient.Send(data, data.Length, otherPC);
                    }
                    Thread.Sleep(10);
                }
            });
            udpSender.IsBackground = true;
            udpSender.Start();
        }


        private bool vJoySetup()
        {
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();
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
                consoleOut("Failed to acquire vJoy device number "+id+".");
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

        private void keyBoardListenerLocal()
        {
            var keyBoardListener = new Thread(() =>
            {
                while (true)
                {
                    if (startListening)
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
            catch(Exception e)
            {
                this.richTextBox1.BeginInvoke((MethodInvoker)delegate () {
                    richTextBox1.Text += s+"\n";
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
        private void keyMapper()
        {
            consoleOut("Select how many keys you would like to map.");
            tipUpdate("Select how many keys you would like to map.");
            int numKeysToMap = 32;
            

            //key .ini saver/loader ?

            while ((!int.TryParse(input, out numKeysToMap)) || (numKeysToMap>32 || numKeysToMap<=0))
            {
                input = Microsoft.VisualBasic.Interaction.InputBox("How many keys do you want to assign?", "Key Amount", "32", -1, -1);
            }


            mappedKeys = new Keys[numKeysToMap];
            var keyListener = new Thread(() => {
                consoleOut("Press any key, or mouse button, to map it.");
                tipUpdate("Press any key, or mouse button, to map it.");
                var values = Enum.GetValues(typeof(Keys)).Cast<Keys>();
                while (numKeysToMap > 0)
                {
                    bool keyAdded = true;
                    try
                    {
                        //one key press can match multiple keys stored in the values list above.
                        //for example if you press left shift it will match with shift and left shift
                        //in this case we want the more specific one, which happens to be further in the list, so we just find the biggest valid index
                        int mostSpecificKeyValue = 0;
                        //we also want it to reste every loo[

                        //this loops goes through every key in the values list and checks if it's pressed
                        for (int i = 1; i < values.Count(); i++)
                        {
                            if ((GetAsyncKeyState((int)(values.ElementAt(i))) & 0x8000) > 0)
                            {
                                //if it's pressed we have to loop through our already mapped keys and make sure it's not a repeat
                                for (int j = numKeysToMap - 1; j < mappedKeys.Length; j++)
                                {
                                    if ((int)mappedKeys[j] == (int)values.ElementAt(i))
                                    {
                                        //this means the key already exists
                                        keyAdded = false;
                                        mostSpecificKeyValue = -1;
                                    }
                                }
                                if (keyAdded == true)
                                {
                                    //this is only entered if the key was not a duplicate
                                    mostSpecificKeyValue = i;
                                }
                            }
                        }
                        if (mostSpecificKeyValue > 0)
                        {
                            consoleOut("Key "+(mappedKeys.Length - numKeysToMap+1)+" = "+values.ElementAt(mostSpecificKeyValue).ToString());
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
                startListening = true;
                tipUpdate("The setup has completed succesfully. Your keys are now mapped!");
                consoleOut("The setup has completed succesfully. Your keys are now mapped!");
                if(isLan == 1)
                {
                    startNetSend();
                }
                Thread.CurrentThread.Abort();
            });
            keyListener.IsBackground = true;
            keyListener.Start();
        }
    }
}
