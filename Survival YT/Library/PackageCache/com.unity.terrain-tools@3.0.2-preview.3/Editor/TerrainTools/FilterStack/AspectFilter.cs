using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class AspectFilter : Filter
    {
        static readonly int RemapTexWidth = 1024;

        [SerializeField]
        private float m_Epsilon = 1.0f; //kernel size
        [SerializeField]
        private float m_EffectStrength = 1.0f;  //overall strength of the effect

        //We bake an AnimationCurve to a texture to control value remapping
        [SerializeField]
        private AnimationCurve m_RemapCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
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
        ComputeShader m_AspectCS;
        ComputeShader GetComputeShader()
        {
            if (m_AspectCS == null)
            {
                m_AspectCS = (ComputeShader)Resources.Load("Aspect");
            }
            return m_AspectCS;
        }

        public override string GetDisplayName() => "Aspect";
        public override string GetToolTip() => "Uses the slope aspect of the heightmap to mask the effect of the chosen Brush, and uses Brush rotation to control the aspect direction.";
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
                int kidx = cs.FindKernel("AspectRemap");

                Texture2D remapTex = GetRemapTexture();

                float rotRad = (fc.brushRotation - 90.0f) * Mathf.Deg2Rad;

                cs.SetTexture(kidx, "In_BaseMaskTex", sourceHandle);
                cs.SetTexture(kidx, "In_HeightTex", fc.rtHandleCollection[FilterContext.Keywords.Heightmap]);
                cs.SetTexture(kidx, "OutputTex", destHandle);
                cs.SetTexture(kidx, "RemapTex", remapTex);
                cs.SetInt("RemapTexRes", remapTex.width);
                cs.SetFloat("EffectStrength", m_EffectStrength);
                cs.SetVector("TextureResolution", new Vector4(source.width, source.height, 0.0f, 0.0f));
                cs.SetVector("AspectValues", new Vector4(Mathf.Cos(rotRad), Mathf.Sin(rotRad), m_Epsilon, 0.0f));
                cs.Dispatch(kidx, source.width, source.height, 1);
                
                Graphics.Blit(destHandle, dest);
            }
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            // Calculate dimensions
            float epsilonLabelWidth = GUI.skin.label.CalcSize(epsilonLabel).x;
            float modeLabelWidth = GUI.skin.label.CalcSize(modeLabel).x;
            float strengthLabelWidth = GUI.skin.label.CalcSize(strengthLabel).x;
            float curveLabelWidth = GUI.skin.label.CalcSize(curveLabel).x;
            float labelWidth = Mathf.Max(Mathf.Max(Mathf.Max(modeLabelWidth, epsilonLabelWidth), strengthLabelWidth), curveLabelWidth) + 4.0f;

            //Strength Slider
            Rect strengthLabelRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(strengthLabelRect, strengthLabel);
            Rect strengthSliderRect = new Rect(strengthLabelRect.xMax, strengthLabelRect.y, rect.width - labelWidth, strengthLabelRect.height);
            m_EffectStrength = EditorGUI.Slider(strengthSliderRect, m_EffectStrength, 0.0f, 1.0f);

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

        protected override void OnSceneGUI(SceneView sceneView, FilterContext filterContext)
        {
            Quaternion windRot = Quaternion.AngleAxis(filterContext.brushRotation, new Vector3(0.0f, 1.0f, 0.0f));
            Handles.ArrowHandleCap(0, filterContext.brushPos, windRot, 0.5f * filterContext.brushSize, EventType.Repaint);
        }

        public override float GetElementHeight() => EditorGUIUtility.singleLineHeight * 4;

        private static GUIContent strengthLabel = EditorGUIUtility.TrTextContent("Strength", "Controls the strength of the masking effect.");
        private static GUIContent epsilonLabel = EditorGUIUtility.TrTextContent("Feature Size", "Specifies the scale of Terrain features that affect the mask.");
        private static GUIContent modeLabel = EditorGUIUtility.TrTextContent("Mode");
        private static GUIContent curveLabel = EditorGUIUtility.TrTextContent("Remap Curve", "Remaps the concavity input before computing the final mask.");
    }
}