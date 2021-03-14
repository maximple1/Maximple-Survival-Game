using UnityEditor;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [CustomEditor(typeof(NoiseSettings))]
    public class NoiseSettingsEditor : Editor
    {
        NoiseSettingsGUI gui = new NoiseSettingsGUI();

        void OnEnable()
        {
            gui.Init(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            gui.OnGUI();
        }
    }
}