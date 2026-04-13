
using System.Linq;
using UnityEngine;

namespace BCIEssentials.SerialTriggers
{
    using LSLFramework;

    public class OpinionatedSerialTriggerMarkerWriter : SerialTriggerMarkerWriter
    {
        public enum P300Encoding { MatchesTarget, StimulusIndex }

        [Header("Marker Encoding (lossy)")]
        public P300Encoding P300ResolutionMode;
        public byte TargetByte = 0x01;
        public byte NonTargetByte = 0x02;


        protected override byte ResolveEventMarkerTriggerCode(EventMarker marker)
        => marker switch
        {
            P300EventMarker p300Marker => ResolveP300MarkerTriggerCode(p300Marker),
            { TrainingTargetIndex: >= 0 } => (byte)(marker.TrainingTargetIndex + 1),
            _ => 0
        };

        protected virtual byte ResolveP300MarkerTriggerCode(P300EventMarker marker)
        => marker switch {
            SingleFlashP300EventMarker singleFlashMarker =>
            P300ResolutionMode switch
            {
                P300Encoding.StimulusIndex => (byte)(singleFlashMarker.StimulusIndex + 1),
                _ => singleFlashMarker.StimulusIndex == marker.TrainingTargetIndex
                    ? TargetByte : NonTargetByte
            },
            MultiFlashP300EventMarker multiFlashMarker
            => ResolveP300MultiFlashMarkerTriggerCode(multiFlashMarker),
            _ => UnresolvedByte
        };

        protected virtual byte ResolveP300MultiFlashMarkerTriggerCode(MultiFlashP300EventMarker marker)
        {
            if (P300ResolutionMode == P300Encoding.MatchesTarget)
            {
                return marker.StimulusIndices.Contains(marker.TrainingTargetIndex)
                    ? TargetByte : NonTargetByte;
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
                foreach (int stimulusIndex in marker.StimulusIndices)
                {
                    if (stimulusIndex < 8) stimulusFlags |= (byte)(1 << stimulusIndex);
                }
                return stimulusFlags;
            }
        }
    }
}