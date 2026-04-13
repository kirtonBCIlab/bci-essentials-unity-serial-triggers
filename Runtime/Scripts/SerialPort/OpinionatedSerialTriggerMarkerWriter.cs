
using System.Linq;
using UnityEngine;

namespace BCIEssentials.SerialTriggers
{
    using LSLFramework;

    public class OpinionatedSerialTriggerMarkerWriter : SerialTriggerMarkerWriter
    {
        public enum P300Encoding { MatchesTarget, StimulusIndex }

        [Header("Marker Encoding (lossy)")]
        [SerializeField] private P300Encoding _p300ResolutionMode;


        protected override byte ResolveP300MarkerTriggerCode(P300EventMarker marker)
        {
            if (marker is SingleFlashP300EventMarker singleFlashMarker)
            {
                return _p300ResolutionMode switch
                {
                    P300Encoding.StimulusIndex => (byte)singleFlashMarker.StimulusIndex,
                    _ => (byte)(singleFlashMarker.StimulusIndex == marker.TrainingTargetIndex ? 1 : 0)
                };
            }
            else if (marker is MultiFlashP300EventMarker multiFlashMarker)
            {
                if (_p300ResolutionMode == P300Encoding.MatchesTarget)
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