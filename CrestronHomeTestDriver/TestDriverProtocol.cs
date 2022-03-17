using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Transports;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CrestronHomeTestDriver
{
    class TestDriverProtocol : ABaseDriverProtocol
    {
        public Regex LoginPattern = new Regex(@"login[: ]*$");
        public Regex PasswordPattern = new Regex(@"password[: ]*$");
        public Regex PromptPattern = new Regex(@"QNET>\s*$");
        public const string LOGIN = "lutron";
        public const string PASSWORD = "integration";
        public const string SET_MONITORING = "#MONITORING,5,1";

        public bool IsLoginDone = false;

        public TestDriverProtocol(TcpTransport transport, byte id) : base(transport, id)
        {
            Transport = transport;
        }

        public void SendCmd(string cmd)
        {
            Log($"TCP Client Status: {Transport.IsConnected}");
            Transport.Send($"{cmd}\r\n", new object[] { });
        }

        public void SendControlCmd(string cmd)
        {
            if (!IsLoginDone)
            {
                Log("Login Failed");
                return;
            }
            SendCmd(cmd);
        }

        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
        }


        public override void DataHandler(string rx)
        {
            Log($"-----------message received-----");
            Log(rx);
            // Lutron HWQS Telnet Login
            if (LoginPattern.IsMatch(rx))
            {
                Log("Telent Login: Username");
                SendCmd(LOGIN);
            }
            else if (PasswordPattern.IsMatch(rx))
            {
                Log("Telnet Login: Password");
                SendCmd(PASSWORD);
            }
            else if (PromptPattern.IsMatch(rx.Substring(2)))
            {
                Log("Telnet Login: Success");
                IsLoginDone = true;
                SendCmd(SET_MONITORING);
            }
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            Transport.IsConnected = connection;
            IsConnectionChange?.Invoke(this, new ValueEventArgs<bool>(connection));
        }

        public event EventHandler<ValueEventArgs<bool>> IsConnectionChange;
    }
}
