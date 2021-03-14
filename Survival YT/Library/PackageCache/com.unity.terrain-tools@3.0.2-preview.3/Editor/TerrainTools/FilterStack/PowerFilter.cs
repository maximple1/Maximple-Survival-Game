using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class PowerFilter : Filter
    {
        [SerializeField]
        public float value = 2;
        
        public override string GetDisplayName()
        {
            return "Power";
        }

        public override string GetToolTip()
        {
            return "Applies an exponential function to each pixel on the Brush Mask. The function is pow(value, e), where e is the input value.";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            FilterUtility.builtinMaterial.SetFloat("_Pow", value);

            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Power );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            value = EditorGUI.FloatField(rect, value);
        }
    }
}