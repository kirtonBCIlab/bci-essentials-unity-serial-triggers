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

        protected SerialPortPulseWriter _triggerCodeWriter;


        private void OnDestroy() => _triggerCodeWriter?.Disconnect();


        public override void PushMarker(IMarker marker)
        {
            base.PushMarker(marker);

            _triggerCodeWriter ??= InitializeTriggerCodeWriter();
            _triggerCodeWriter.PrintLogs = PrintLogs;

            byte resolvedValue = ResolveTriggerCode(marker);
            _triggerCodeWriter.QueuePulse(resolvedValue, PulseWidth);
        }


        protected virtual SerialPortPulseWriter InitializeTriggerCodeWriter()
        {
            SerialPortPulseWriter writer = new();
            writer.Connect(PortName, _baudRate, _parity, _dataBits, _stopBits, _writeTimeout);
            return writer;
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