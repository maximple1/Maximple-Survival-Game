using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [CustomEditor(typeof(FilterStack))]
    public class FilterStackEditor : Editor
    {
        private FilterStackView m_view;

        void OnEnable()
        {
            m_view = new FilterStackView( new GUIContent("Image Filter Stack"), serializedObject );
        }

        public override void OnInspectorGUI()
        {
            m_view.OnGUI();
        }
    }
}