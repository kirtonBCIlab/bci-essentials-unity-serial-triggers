using UnityEditor;
using UnityEngine;

namespace BCIEssentials.SerialTriggers.Editor
{
    using BCIEssentials.Editor;

    [CustomEditor(typeof(SerialTriggerMarkerWriter))]
    public class SerialMarkerWriterEditor : ExtendedAttributeInspector
    {
        private string[] _cachedPortDescriptions;

        public override void DrawInspector()
        {
            if (GUILayout.Button("Scan Ports"))
            {
                _cachedPortDescriptions = SerialPortUtilities.GetAvailablePortsWithDescriptions();
                if (_cachedPortDescriptions.Length == 0)
                    Debug.Log("SerialMarkerWriter: no serial ports found.");
                else
                    Debug.Log(
                        "SerialMarkerWriter: available ports\n  "
                        + string.Join("\n  ", _cachedPortDescriptions)
                    );
            }

            if (_cachedPortDescriptions != null)
            {
                if (_cachedPortDescriptions.Length == 0)
                {
                    EditorGUILayout.HelpBox("No Ports Found", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Available ports:\n" + string.Join("\n", _cachedPortDescriptions),
                        MessageType.Info
                    );
                }
            }

            base.DrawInspector();
        }
    }
}