using System.IO.Ports;
using UnityEngine;

namespace BCIEssentials.SerialTriggers
{
    using LSLFramework;

    public abstract class SerialTriggerMarkerWriter : MarkerWriter
    {
        public const byte UnresolvedByte = 0xff;

        [Header("Serial Port Configuration")]
        [Tooltip("Serial port name (e.g. COM3 on Windows, /dev/ttyUSB0 on Linux)")]
        public string PortName;

        [Tooltip("Milliseconds to hold the trigger value before resetting to 0")]
        public int PulseWidth = 10;

        [Header("Advanced Port Settings")]
        [SerializeField] private int _writeTimeout = 500;
        [SerializeField] private int _baudRate = 9600;
        [SerializeField] private Parity _parity = Parity.None;
        [SerializeField] private int _dataBits = 8;
        [SerializeField] private StopBits _stopBits = StopBits.One;

        private SerialPortPulseWriter _writer;


        public override void PushMarker(IMarker marker)
        {
            base.PushMarker(marker);

            if (_writer == null)
            {
                _writer = new();
                _writer.Connect(PortName, _baudRate, _parity, _dataBits, _stopBits, _writeTimeout);
            }
            _writer.PulseWidth = PulseWidth;
            _writer.PrintLogs = PrintLogs;

            byte resolvedValue = ResolveTriggerCode(marker);
            _writer.SendPulse(resolvedValue);
        }


        public virtual byte ResolveTriggerCode(IMarker marker)
        => marker switch
        {
            IStatusMarker statusMarker => ResolveStatusMarkerTriggerCode(statusMarker),
            EventMarker eventMarker => ResolveEventMarkerTriggerCode(eventMarker),
            _ => UnresolvedByte
        };

        protected virtual byte ResolveStatusMarkerTriggerCode(IStatusMarker marker)
        => marker switch
        {
            TrialStartedMarker => 0xf1,
            TrialEndsMarker => 0xf0,
            TrainingCompleteMarker => 0xf2,
            TrainClassifierMarker => 0xf3,
            UpdateClassifierMarker => 0xf4,
            DoneWithRestingStateCollectionMarker => 0xf5,
            _ => UnresolvedByte
        };

        protected abstract byte ResolveEventMarkerTriggerCode(EventMarker marker);
    }
}