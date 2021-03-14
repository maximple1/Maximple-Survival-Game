using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class SlopeFilter : Filter
    {
        static readonly int RemapTexWidth = 1024;

        [SerializeField]
        private float m_Epsilon = 1.0f; //kernel size
        [SerializeField]
        private float m_FilterStrength = 1.0f;  //overall strength of the effect

        //We bake an AnimationCurve to a texture to control value remapping
        [SerializeField]
        private AnimationCurve m_RemapCurve = new AnimationCurve(new Keyframe(0.0f, 1.0f), new Keyframe(0.25f, 0.0f), new Keyframe(1.0f, 0.0f));
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

        ComputeShader m_ConcavityCS;
        ComputeShader GetComputeShader()
        {
            if (m_ConcavityCS == null)
            {
                m_ConcavityCS = (ComputeShader)Resources.Load("Slope");
            }
            return m_ConcavityCS;
        }

        public override string GetDisplayName() => "Slope";
        public override string GetToolTip() => "Uses the slope angle of the heightmap to mask the effect of the chosen Brush.";
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
                int kidx = cs.FindKernel("GradientMultiply");

                Texture2D remapTex = GetRemapTexture();

                cs.SetTexture(kidx, "In_BaseMaskTex", sourceHandle);
                cs.SetTexture(kidx, "In_HeightTex", fc.rtHandleCollection[FilterContext.Keywords.Heightmap]);
                cs.SetTexture(kidx, "OutputTex", destHandle);
                cs.SetTexture(kidx, "RemapTex", remapTex);
                cs.SetInt("RemapTexRes", remapTex.width);
                cs.SetFloat("EffectStrength", m_FilterStrength);
                cs.SetVector("TerrainDimensions", fc.vectorProperties.ContainsKey("_TerrainSize") ? fc.vectorProperties["_TerrainSize"] : Vector4.one);
                cs.SetVector("TextureResolution", new Vector4(sourceHandle.RT.width, sourceHandle.RT.height, m_Epsilon, fc.floatProperties[FilterContext.Keywords.TerrainScale]));
                cs.Dispatch(kidx, sourceHandle.RT.width, sourceHandle.RT.height, 1);
                
                Graphics.Blit(destHandle, dest);
            }
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            //Calculate dimensions
            float epsilonLabelWidth = GUI.skin.label.CalcSize(epsilonLabel).x;
            float modeLabelWidth = GUI.skin.label.CalcSize(modeLabel).x;
            float strengthLabelWidth = GUI.skin.label.CalcSize(strengthLabel).x;
            float curveLabelWidth = GUI.skin.label.CalcSize(curveLabel).x;
            float labelWidth = Mathf.Max(Mathf.Max(Mathf.Max(modeLabelWidth, epsilonLabelWidth), strengthLabelWidth), curveLabelWidth) + 4.0f;

            //Strength Slider
            Rect strengthLabelRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(strengthLabelRect, strengthLabel);
            Rect strengthSliderRect = new Rect(strengthLabelRect.xMax, strengthLabelRect.y, rect.width - labelWidth, strengthLabelRect.height);
            m_FilterStrength = EditorGUI.Slider(strengthSliderRect, m_FilterStrength, 0.0f, 1.0f);

            //Epsilon (kernel size) Slider
            Rect epsilonLabelRect = new Rect(rect.x, strengthSliderRect.yMax, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(epsilonLabelRect, epsilonLabel);
            Rect epsilonSliderRect = new Rect(epsilonLabelRect.xMax, epsilonLabelRect.y, rect.width - labelWidth, epsilonLabelRect.height);
            m_Epsilon = EditorGUI.Slider(epsilonSliderRect, m_Epsilon, 1.0f, 20.0f);

            //Value Remap Curve
            Rect curveLabelRect = new Rect(rect.x, epsilonSliderRect.yMax, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(curveLabelRect, curveLabel);
            Rect curveRect = new Rect(curveLabelRect.xMax, curveLabelRect.y, rect.width - labelWidth, curveLabelRect.height);

            EditorGUI.BeginChangeCheck();
            m_RemapCurve = EditorGUI.CurveField(curveRect, m_RemapCurve);
            if(EditorGUI.EndChangeCheck())
            {
                var tex = GetRemapTexture();
                Utility.AnimationCurveToRenderTexture(m_RemapCurve, ref tex);
            }
        }

        public override float GetElementHeight() => EditorGUIUtility.singleLineHeight * 5;

        private static GUIContent strengthLabel = EditorGUIUtility.TrTextContent("Strength", "Controls the strength of the masking effect.");
        private static GUIContent epsilonLabel = EditorGUIUtility.TrTextContent("Feature Size", "Specifies the scale of Terrain features that affect the mask.");
        private static GUIContent modeLabel = EditorGUIUtility.TrTextContent("Mode");
        private static GUIContent curveLabel = EditorGUIUtility.TrTextContent("Remap Curve", "Remaps the slope input before computing the final mask. This helps you visualize how the Terrain's slope affects the generated mask.");
    }
}