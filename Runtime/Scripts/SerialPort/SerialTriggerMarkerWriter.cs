using System.IO.Ports;
using System.Linq;
using UnityEngine;

namespace BCIEssentials.SerialTriggers
{
    using LSLFramework;

    public class SerialTriggerMarkerWriter : MarkerWriter
    {
        public const byte UnresolvedByte = 0xff;
        public enum P300TriggerResolutionMode { MatchesTarget, StimulusIndex }

        [Header("Serial Port Configuration")]
        [Tooltip("Serial port name (e.g. COM3 on Windows, /dev/ttyUSB0 on Linux)")]
        public string PortName;

        [Tooltip("Milliseconds to hold the trigger value before resetting to 0")]
        public int PulseWidth = 10;

        [Tooltip("Trigger code resolution behaviour for p300 markers")]
        public P300TriggerResolutionMode triggerResolutionMode;

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


        protected virtual byte ResolveTriggerCode(IMarker marker)
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
            _ => UnresolvedByte
        };

        protected virtual byte ResolveEventMarkerTriggerCode(EventMarker marker)
        => marker switch
        {
            P300EventMarker p300Marker => ResolveP300MarkerTriggerCode(p300Marker),
            { TrainingTargetIndex: >= 0 } => (byte)(marker.TrainingTargetIndex + 1),
            _ => 0
        };

        protected virtual byte ResolveP300MarkerTriggerCode(P300EventMarker marker)
        {
            if (marker is SingleFlashP300EventMarker singleFlashMarker)
            {
                return triggerResolutionMode switch
                {
                    P300TriggerResolutionMode.StimulusIndex => (byte)singleFlashMarker.StimulusIndex,
                    _ => (byte)(singleFlashMarker.StimulusIndex == marker.TrainingTargetIndex ? 1 : 0)
                };
            }
            else if (marker is MultiFlashP300EventMarker multiFlashMarker)
            {
                if (triggerResolutionMode == P300TriggerResolutionMode.MatchesTarget)
                {
                    return (byte)(multiFlashMarker.StimulusIndices.Contains(marker.TrainingTargetIndex) ? 1 : 0);
                }
                else
                {
                    if (marker.ClassCount > 7)
                    {
                        Debug.LogWarning(
                            "Cannot meaningfully represent more than " +
                            "7 concurrent targets in a single byte"
                        );
                    }
                    byte stimulusFlags = 0;
                    foreach (int stimulusIndex in multiFlashMarker.StimulusIndices)
                    {
                        if (stimulusIndex < 8) stimulusFlags |= (byte)(1 << stimulusIndex);
                    }
                    return stimulusFlags;
                }
            }
            return UnresolvedByte;
        }
    }
}