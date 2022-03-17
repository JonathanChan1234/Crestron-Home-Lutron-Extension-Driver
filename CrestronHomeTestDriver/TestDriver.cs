using Crestron.RAD.Common.BasicDriver;
using Crestron.RAD.Common.Enums;
using Crestron.RAD.Common.Interfaces;
using Crestron.RAD.Common.Interfaces.ExtensionDevice;
using Crestron.RAD.Common.Transports;
using Crestron.RAD.DeviceTypes.ExtensionDevice;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.CrestronThread;
using CrestronHomeTestDriver.Emulator;
using System;

namespace CrestronHomeTestDriver
{
    public class TestDriver : AExtensionDevice, ITcp
    {
        #region constant
        // Property Key
        private const string LightIconKey = "LightIcon";
        private const string BatteryIconKey = "BatteryIcon";
        private const string LightStatusLabelKey = "LightStatusLabel";
        private const string LightStatusKey = "LightStatus";
        private const string LightLevelKey = "LightLevel";

        // Light Icon
        private const string LightOnIcon = "icLightsOnRegular";
        private const string LightOffIcon = "icLightsOffRegular";

        // Battery Icon
        private const string BatteryNormalIcon = "";
        private const string BatteryLowIcon = "icBatteryLow";

        // Actions
        private const string ToggleLightAction = "ToggleLight";
        #endregion

        #region property
        private PropertyValue<string> _lightIconProperty;
        private PropertyValue<string> _batteryIconProperty;
        private PropertyValue<string> _lightStatusLabelProperty;
        private PropertyValue<bool> _lightStatusProperty;
        private PropertyValue<int> _lightLevelProperty;
        #endregion

        private LightEmulator _lightEmulator;
        private TestDriverProtocol _testDriverProtocol;

        private string _ipAddress;
        private int _port;

        #region constructor
        public TestDriver()
        {
            CreateDeviceDefinition();
            try
            {
                AddCapabilities();
            }
            catch (TypeLoadException)
            {
                if (EnableLogging) Log("Type Load Exception");
            }
        }
        #endregion

        private void CreateDeviceDefinition()
        {
            _lightIconProperty = CreateProperty<string>(new PropertyDefinition(LightIconKey, null, DevicePropertyType.String));
            _batteryIconProperty = CreateProperty<string>(new PropertyDefinition(BatteryIconKey, null, DevicePropertyType.String));
            _lightStatusLabelProperty = CreateProperty<string>(new PropertyDefinition(LightStatusLabelKey, null, DevicePropertyType.String));
            _lightStatusProperty = CreateProperty<bool>(new PropertyDefinition(LightStatusKey, null, DevicePropertyType.Boolean));
            _lightLevelProperty = CreateProperty<int>(new PropertyDefinition(LightLevelKey, null, DevicePropertyType.Int32, 0, 100, 1));
        }

        private void Refresh()
        {
            _lightIconProperty.Value = _lightEmulator.Power ? LightOnIcon : LightOffIcon;
            _batteryIconProperty.Value = BatteryNormalIcon;
            _lightStatusProperty.Value = _lightEmulator.Power;
            _lightStatusLabelProperty.Value = _lightEmulator.Power ? "ON" : "OFF";
            _lightLevelProperty.Value = _lightEmulator.Brightness;
            Commit();
        }

        public void AddCapabilities()
        {
            var tcp2Capability = new Tcp2Capability(Initialize);
            Capabilities.RegisterInterface(typeof(ITcp2), tcp2Capability);
        }

        public override void Connect()
        {
            Connected = true;
            ConnectionTransport.Start();
            Refresh();
        }

        public void Initialize(IPAddress ipAddress, int port)
        {
            if (EnableLogging)
            {
                Log($"IP Address: {ipAddress}");
                Log($"Port: {port}");
            }
            _ipAddress = ipAddress.ToString();
            _port = port;
            _lightEmulator = new LightEmulator();
            _lightEmulator.StateChangedEvent += LightEmulatorEventHandler;
            var tcpTransport = new TcpTransport
            {
                EnableAutoReconnect = EnableAutoReconnect,
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug,
            };
            tcpTransport.Initialize(ipAddress, port);
            ConnectionTransport = tcpTransport;
            _testDriverProtocol = new TestDriverProtocol(tcpTransport, Id)
            {
                EnableLogging = EnableLogging,
                CustomLogger = CustomLogger
            };
            _testDriverProtocol.IsConnectionChange += ConnectionChangeHandler;
            DeviceProtocol = _testDriverProtocol;
        }

        private void ConnectionChangeHandler(object sender, Crestron.RAD.Common.Events.ValueEventArgs<bool> e)
        {
            if (EnableLogging) Log($"Connection changed: {e.Value}");
        }

        private void Initialize(string address, int port)
        {
            if (EnableLogging)
            {
                Log($"IP Address: {address}");
                Log($"Port: {port}");
            }
            _ipAddress = address;
            _port = port;
            _lightEmulator = new LightEmulator();
            _lightEmulator.StateChangedEvent += LightEmulatorEventHandler;

            var tcpTransport = new TcpTransport
            {
                EnableAutoReconnect = EnableAutoReconnect,
                EnableLogging = InternalEnableLogging,
                CustomLogger = InternalCustomLogger,
                EnableRxDebug = InternalEnableRxDebug,
                EnableTxDebug = InternalEnableTxDebug
            };
            tcpTransport.Initialize(address, port);
            ConnectionTransport = tcpTransport;
            _testDriverProtocol = new TestDriverProtocol(tcpTransport, Id)
            {
                EnableLogging = EnableLogging,
                CustomLogger = CustomLogger
            };
            DeviceProtocol = _testDriverProtocol;
        }

        private void LightEmulatorEventHandler(object sender, LightEmulatorEventArgs e)
        {
            switch (e.EventType)
            {
                case EventType.PowerStateChanged:
                    bool power = (bool)e.EventData;
                    _lightIconProperty.Value = power ? LightOnIcon : LightOffIcon;
                    _lightStatusProperty.Value = power;
                    _lightStatusLabelProperty.Value = power ? "ON" : "OFF";
                    break;
                case EventType.BrightnessChanged:
                    int brightness = (int)e.EventData;
                    _lightIconProperty.Value = brightness > 0 ? LightOnIcon : LightOffIcon;
                    _lightLevelProperty.Value = brightness;
                    break;
                default:
                    break;
            }
            Commit();
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string propertyKey, T value)
        {
            switch (propertyKey)
            {
                case LightStatusKey:
                    var status = value as bool?;
                    if (status == null) return new OperationResult(OperationResultCode.Error, "The value provided cannot be converted to bool");
                    _lightEmulator.SetPowerState((bool)status);
                    return new OperationResult(OperationResultCode.Success);
                case LightLevelKey:
                    var brightness = value as int?;
                    if (brightness == null) return new OperationResult(OperationResultCode.Error, "The value provided cannot be converted to int");
                    _lightEmulator.SetBrightness((int)brightness);
                    return new OperationResult(OperationResultCode.Success);
            }
            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult SetDriverPropertyValue<T>(string objectId, string propertyKey, T value)
        {
            return new OperationResult(OperationResultCode.Success);
        }

        protected override IOperationResult DoCommand(string command, string[] parameters)
        {
            // ReSharper disable once ObjectCreationAsStatement
            new Thread(DoCommandThreadCallback, new DoCommandObject(command, parameters));
            return new OperationResult(OperationResultCode.Success);
        }

        private object DoCommandThreadCallback(Object obj)
        {
            var commandObj = (DoCommandObject)obj;
            var command = commandObj.Command;
            var parameters = commandObj.Parameters;
            switch (command)
            {
                case ToggleLightAction:
                    if (EnableLogging)
                    {
                        Log($"IP address: {_ipAddress}");
                        Log($"Port: {_port}");
                        Log($"TCP Client Status: {ConnectionTransport.IsConnected}");
                    }
                    bool power = _lightEmulator.ToggleLight();
                    _testDriverProtocol.SendCmd($"#OUTPUT,15,1,{(power ? 100 : 0)},2,0");
                    break;
                default:
                    Log("invalid command");
                    break;
            }
            return null;
        }
    }

    internal class DoCommandObject
    {
        public string Command;
        public string[] Parameters;
        public DoCommandObject(string command, string[] parameters)
        {
            Command = command;
            Parameters = parameters;
        }
    }
}
