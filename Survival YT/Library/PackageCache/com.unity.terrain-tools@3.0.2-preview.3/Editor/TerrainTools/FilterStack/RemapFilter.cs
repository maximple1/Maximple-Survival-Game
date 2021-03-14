using System;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [Serializable]
    public class RemapFilter : Filter
    {
        private static GUIContent s_FromLabel = EditorGUIUtility.TrTextContent("From:");
        private static GUIContent s_ToLabel = EditorGUIUtility.TrTextContent("To:");

        [SerializeField] private Vector2 m_FromRange = new Vector2(0, 1);
        [SerializeField] private Vector2 m_ToRange = new Vector2(0, 1);

        public override string GetDisplayName()
        {
            return "Remap";
        }

        public override string GetToolTip()
        {
            return "Remaps each texture value in the input Texture to the target range.";
        }

        protected override void OnEval(FilterContext filterContext, RenderTexture source, RenderTexture dest)
        {
            FilterUtility.builtinMaterial.SetVector("_RemapRanges", new Vector4(m_FromRange.x, m_FromRange.y, m_ToRange.x, m_ToRange.y));
            Graphics.Blit(source, dest, FilterUtility.builtinMaterial, (int)FilterUtility.BuiltinPasses.Remap);
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            var fromLabelWidth = GUI.skin.label.CalcSize(s_FromLabel).x;
            var toLabelWidth = GUI.skin.label.CalcSize(s_ToLabel).x;
            var width = Mathf.Max(fromLabelWidth, toLabelWidth) + 4f;

            var fromLabelRect = new Rect(rect.x, rect.y, width, EditorGUIUtility.singleLineHeight);
            var fromRangeRect = new Rect(fromLabelRect.xMax, fromLabelRect.y, rect.width - width, fromLabelRect.height);
            var toLabelRect = new Rect(rect.x, fromLabelRect.yMax, width, EditorGUIUtility.singleLineHeight);
            var toRangeRect = new Rect(toLabelRect.xMax, toLabelRect.y, rect.width - width, toLabelRect.height);
            
            EditorGUI.LabelField(fromLabelRect, s_FromLabel);
            m_FromRange = EditorGUI.Vector2Field(fromRangeRect, string.Empty, m_FromRange);
            EditorGUI.LabelField(toLabelRect, s_ToLabel);
            m_ToRange = EditorGUI.Vector2Field(toRangeRect, string.Empty, m_ToRange);
        }

        public override float GetElementHeight() => EditorGUIUtility.singleLineHeight * 3;
    }
}