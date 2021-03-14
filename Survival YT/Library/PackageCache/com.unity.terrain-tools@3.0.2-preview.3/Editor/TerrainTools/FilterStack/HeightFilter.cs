using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class HeightFilter : Filter
    {
        static readonly int RemapTexWidth = 1024;

        [SerializeField]
        private float m_ConcavityStrength = 1.0f;  //overall strength of the effect

        [SerializeField]
        private Vector2 m_Height = new Vector2(0.0f, 1.0f);
        [SerializeField]
        private float m_HeightFeather = 0.0f;

        //We bake an AnimationCurve to a texture to control value remapping
        [SerializeField]
        private AnimationCurve m_RemapCurve = new AnimationCurve(new Keyframe(0.0f, 0.0f), new Keyframe(0.25f, 1.0f), new Keyframe(0.75f, 1.0f), new Keyframe(1.0f, 0.0f));
        Texture2D m_RemapTex;

        Texture2D GetRemapTexture()
        {
            if (m_RemapTex == null)
            {
                m_RemapTex = new Texture2D(RemapTexWidth, 1, TextureFormat.RFloat, false, true);
                m_RemapTex.wrapMode = TextureWrapMode.Clamp;

                Utility.AnimationCurveToRenderTexture(m_RemapCurve, ref m_RemapTex);
            }

            return m_RemapTex;
        }

        //Compute Shader resource helper
        ComputeShader m_HeightCS;
        ComputeShader GetComputeShader()
        {
            if (m_HeightCS == null)
            {
                m_HeightCS = (ComputeShader)Resources.Load("Height");
            }
            return m_HeightCS;
        }

        public override string GetDisplayName() => "Height";
        public override string GetToolTip() => "Uses the height of the heightmap to mask the effect of the chosen Brush.";
        public override bool ValidateFilter(FilterContext filterContext, out string message)
        {
            message = string.Empty;

            if (!SystemInfo.supportsComputeShaders)
            {
                message = "The current Graphics API does not support compute shaders.";
                return false;
            }

            if (!SystemInfo.IsFormatSupported(filterContext.targetFormat, FormatUsage.LoadStore))
            {
                message = $"The current Graphics API does not support UAV resource access for GraphicsFormat.{filterContext.targetFormat}.";
                return false;
            }
            
            return true;
        }

        protected override void OnEval(FilterContext fc, RenderTexture source, RenderTexture dest)
        {
            var desc = dest.descriptor;
            desc.enableRandomWrite = true;
            var sourceHandle = RTUtils.GetTempHandle(desc);
            var destHandle = RTUtils.GetTempHandle(desc);
            using (sourceHandle.Scoped())
            using (destHandle.Scoped())
            {
                Graphics.Blit(source, sourceHandle);
                Graphics.Blit(dest, destHandle);

                ComputeShader cs = GetComputeShader();
                int kidx = cs.FindKernel("HeightRemap");

                Texture2D remapTex = GetRemapTexture();

                cs.SetTexture(kidx, "In_BaseMaskTex", sourceHandle);
                cs.SetTexture(kidx, "In_HeightTex", fc.rtHandleCollection[FilterContext.Keywords.Heightmap]);
                cs.SetTexture(kidx, "OutputTex", destHandle);
                cs.SetTexture(kidx, "RemapTex", remapTex);
                cs.SetInt("RemapTexRes", remapTex.width);
                cs.SetFloat("EffectStrength", m_ConcavityStrength);
                cs.SetVector("HeightRange", new Vector4(m_Height.x, m_Height.y, m_HeightFeather, 0.0f));
                cs.Dispatch(kidx, source.width, source.height, 1);
                
                Graphics.Blit(destHandle, dest);
            }
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            // Calculate dimensions
            float strengthLabelWidth = GUI.skin.label.CalcSize(strengthLabel).x;
            float rangeLabelWidth = GUI.skin.label.CalcSize(rangeLabel).x;
            float featherLabelWidth = GUI.skin.label.CalcSize(featherLabel).x;
            float curveLabelWidth = GUI.skin.label.CalcSize(curveLabel).x;
            float labelWidth = Mathf.Max(Mathf.Max(Mathf.Max(rangeLabelWidth, featherLabelWidth), strengthLabelWidth), curveLabelWidth) + 4.0f;

            // Strength Slider
            Rect strengthLabelRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(strengthLabelRect, strengthLabel);
            Rect strengthSliderRect = new Rect(strengthLabelRect.xMax, strengthLabelRect.y, rect.width - labelWidth, strengthLabelRect.height);
            m_ConcavityStrength = EditorGUI.Slider(strengthSliderRect, m_ConcavityStrength, 0.0f, 1.0f);

            // Height Range Slider
            Rect rangeLabelRect = new Rect(rect.x, strengthSliderRect.yMax, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(rangeLabelRect, rangeLabel);
            Rect rangeLeftRect = new Rect(rangeLabelRect.xMax, rangeLabelRect.y, (rect.width - labelWidth) / 2, rangeLabelRect.height);
            Rect rangeRightRect = new Rect(rangeLeftRect.xMax, rangeLabelRect.y, (rect.width - labelWidth) / 2, rangeLabelRect.height);
            m_Height.x = EditorGUI.FloatField(rangeLeftRect, m_Height.x);
            m_Height.y = EditorGUI.FloatField(rangeRightRect, m_Height.y);

            //Value Remap Curve
            Rect curveLabelRect = new Rect(rect.x, rangeLeftRect.yMax, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(curveLabelRect, curveLabel);
            Rect curveRect = new Rect(curveLabelRect.xMax, curveLabelRect.y, rect.width - labelWidth, curveLabelRect.height);

            EditorGUI.BeginChangeCheck();
            m_RemapCurve = EditorGUI.CurveField(curveRect, m_RemapCurve);
            if (EditorGUI.EndChangeCheck())
            {
                var tex = GetRemapTexture();
                Utility.AnimationCurveToRenderTexture(m_RemapCurve, ref tex);
            }
        }

        public override float GetElementHeight() => EditorGUIUtility.singleLineHeight * 4;

        private static GUIContent strengthLabel = EditorGUIUtility.TrTextContent("Strength", "Controls the strength of the masking effect.");
        private static GUIContent rangeLabel = EditorGUIUtility.TrTextContent("Height Range", "Specifics the height range to which to apply the effect.");
        private static GUIContent featherLabel = EditorGUIUtility.TrTextContent("Feather");
        private static GUIContent curveLabel = EditorGUIUtility.TrTextContent("Remap Curve", "Remaps the height input before computing the final mask.");
    }
}