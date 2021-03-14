using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class MaxFilter : Filter
    {
        [SerializeField]
        public float value;
        
        public override string GetDisplayName()
        {
            return "Max";
        }

        public override string GetToolTip()
        {
            return "Sets all pixels of the current mask to whichever is greater, the current pixel value or the input value.";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            FilterUtility.builtinMaterial.SetFloat("_Max", value);

            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Max );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            value = EditorGUI.FloatField(rect, value);
        }
    }
}