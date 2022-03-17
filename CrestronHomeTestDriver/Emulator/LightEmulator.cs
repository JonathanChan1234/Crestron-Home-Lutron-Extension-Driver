using System;

namespace CrestronHomeTestDriver.Emulator
{
    public class LightEmulator
    {
        private bool _power = false;
        private int _brightness = 0;

        public bool Power
        {
            get { return _power; }
            private set
            {
                if (_power == value) return;
                _power = value;
                StateChangedEvent?.Invoke(this, new LightEmulatorEventArgs(EventType.PowerStateChanged, _power));
            }
        }

        public int Brightness
        {
            get { return _brightness; }
            private set
            {
                if (_brightness == value) return;
                _brightness = value;
                StateChangedEvent?.Invoke(this, new LightEmulatorEventArgs(EventType.BrightnessChanged, _brightness));
            }
        }

        public bool ToggleLight()
        {
            Brightness = Power ? 0 : 100;
            Power = !Power;
            return Power;
        }

        public void SetPowerState(bool power)
        {
            Power = power;
            Brightness = power ? 100 : 0;
        }

        public void SetBrightness(int brightness)
        {
            Power = brightness > 0;
            Brightness = brightness;
        }

        public event EventHandler<LightEmulatorEventArgs> StateChangedEvent;
    }

    public class LightEmulatorEventArgs
    {
        public LightEmulatorEventArgs(EventType eventType, object eventData)
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
