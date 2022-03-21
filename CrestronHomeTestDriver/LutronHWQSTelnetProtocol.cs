using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Events;
using Crestron.RAD.Common.Transports;
using System;
using System.Text.RegularExpressions;

namespace CrestronHomeTestDriver
{
    public class LutronHWQSTelnetProtocol : ABaseDriverProtocol
    {
        // User Attribute Parameter ID
        private const string IntegrationIdKey = "IntergrationId";

        private Regex LoginPattern = new Regex(@"login[: ]*$");
        private Regex PasswordPattern = new Regex(@"password[: ]*$");
        private const string PromptPattern = "QNET>";
        private const string LOGIN = "lutron";
        private const string PASSWORD = "integration";
        private const string MONITORING_ENABLE_COMMAND = "#MONITORING,5,1";
        private const string MONITORING_ENABLE_RESPONSE = "~MONITORING,5,1";
        private Regex OutputPattern = new Regex(@"~OUTPUT,[0-9]+,[0-9]+,[0-9]+");

        private bool _isLoginDone = false;
        private bool _monitoringCommandSent = false;

        // The integration id of the device
        private int _integrationId = 0;

        public event EventHandler<ValueEventArgs<bool>> IsConnectionChange;
        public event EventHandler<ValueEventArgs<int>> BrightnessChange;

        public LutronHWQSTelnetProtocol(TcpTransport transport, byte id) : base(transport, id)
        {
            Transport = transport;
        }

        public void SendCmd(string cmd)
        {
            Log($"Command sent: {cmd}");
            Transport.Send($"{cmd}\r\n", new object[] { });
        }

        public void SendControlCmd(string cmd)
        {
            if (!_isLoginDone)
            {
                Log("Login Failed");
                return;
            }
            SendCmd(cmd);
        }

        public void setBrightness(int brightness, int fade, int delay)
        {
            brightness = (brightness > 100) ? 100 : (brightness < 0) ? 0 : brightness;
            fade = (fade > 10) ? 10 : (fade < 0) ? 0 : fade;
            delay = (delay > 10) ? 10 : (delay < 0) ? 0 : delay;
            SendControlCmd($"#OUTPUT,{_integrationId},{brightness},{fade},{delay}");
        }

        public override void SetUserAttribute(string attributeId, ushort value)
        {
            switch (attributeId)
            {
                case IntegrationIdKey:
                    _integrationId = value;
                    break;
                default:
                    Log($"Attribute {attributeId} is not supported");
                    break;
            }
        }

        public override void DataHandler(string rx)
        {
            Log($"-----------message received-----------");
            Log(rx);
            // Lutron HWQS Telnet Login Username
            if (LoginPattern.IsMatch(rx))
            {
                Log("Telent Login: Username");
                SendCmd(LOGIN);
            }
            // Lutron HWQS Telnet Login Password
            if (PasswordPattern.IsMatch(rx))
            {
                Log("Telnet Login: Password");
                SendCmd(PASSWORD);
            }
            // Send the monitoring enable command once when the prompt was sent
            if (rx.Contains(PromptPattern))
            {
                Log("Telnet Login: Success");
                _isLoginDone = true;
                if (!_monitoringCommandSent) SendControlCmd(MONITORING_ENABLE_COMMAND);
            }
            // Set the monitoringCommandSent flag to true when the feedback is received
            if (rx.Contains(MONITORING_ENABLE_RESPONSE))
            {
                _monitoringCommandSent = true;
            }

            // Brightness feedback
            if (OutputPattern.IsMatch(rx))
            {
                BrightnessChangedEvent(rx);
            }
        }

        private void BrightnessChangedEvent(string rx)
        {
            string[] parameters = rx.Split(new char[] { ',' });
            if (parameters.Length < 4) return;
            try
            {
                int brightness = int.Parse(parameters[3]);
                BrightnessChange?.Invoke(this, new ValueEventArgs<int>(brightness));
            }
            catch (Exception e)
            {
                Log("BrightnessChangedEvent Error: ");
                Log(e.Message);
            }
        }

        protected override void ConnectionChangedEvent(bool connection)
        {
            Transport.IsConnected = connection;
            IsConnectionChange?.Invoke(this, new ValueEventArgs<bool>(connection));
        }

        protected override void ChooseDeconstructMethod(ValidatedRxData validatedData)
        {
        }
    }
}
