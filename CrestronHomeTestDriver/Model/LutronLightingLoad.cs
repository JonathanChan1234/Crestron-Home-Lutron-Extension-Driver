using System;

namespace CrestronHomeTestDriver.Model
{
    public class LutronLightingLoad
    {
        private LutronHWQSTelnetProtocol _protocol;
        private bool _power = false;
        private int _brightness = 0;

        public LutronLightingLoad(LutronHWQSTelnetProtocol protocol)
        {
            _protocol = protocol;
            _protocol.BrightnessChange += BrightnessChangedEventHandler;
        }

        private void BrightnessChangedEventHandler(object sender, Crestron.RAD.Common.Events.ValueEventArgs<int> e)
        {
            int brightness = e.Value;
            StateChangedEvent?.Invoke(this, new LutronLightingEventArgs(EventType.PowerStateChanged, brightness > 0));
            StateChangedEvent?.Invoke(this, new LutronLightingEventArgs(EventType.BrightnessChanged, brightness));
        }

        public bool Power
        {
            get { return _power; }
            private set
            {
                if (_power == value) return;
                _power = value;
                StateChangedEvent?.Invoke(this, new LutronLightingEventArgs(EventType.PowerStateChanged, _power));
            }
        }

        public int Brightness
        {
            get { return _brightness; }
            private set
            {
                if (_brightness == value) return;
                _brightness = value;
                StateChangedEvent?.Invoke(this, new LutronLightingEventArgs(EventType.BrightnessChanged, _brightness));
            }
        }

        public void ToggleLight()
        {
            SetBrightness(Power ? 0 : 100);
        }

        public void SetPowerState(bool power)
        {
            SetBrightness(power ? 100 : 0);
        }

        public void SetBrightness(int brightness)
        {
            _protocol.setBrightness(brightness, 2, 0);
            Power = brightness > 0;
            Brightness = brightness;
        }

        public event EventHandler<LutronLightingEventArgs> StateChangedEvent;
    }

    public class LutronLightingEventArgs
    {
        public LutronLightingEventArgs(EventType eventType, object eventData)
        {
            EventType = eventType;
            EventData = eventData;
        }
        public EventType EventType { get; private set; }
        public object EventData { get; private set; }
    }

    public enum EventType
    {
        PowerStateChanged,
        BrightnessChanged
    }
}
