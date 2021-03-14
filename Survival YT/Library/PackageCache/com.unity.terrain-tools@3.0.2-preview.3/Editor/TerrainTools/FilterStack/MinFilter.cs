using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class MinFilter : Filter
    {
        [SerializeField]
        public float value = 1;
        
        public override string GetDisplayName()
        {
            return "Min";
        }

        public override string GetToolTip()
        {
            return "Sets all pixels of the current mask to whichever is smaller, the current pixel value or the input value.";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            FilterUtility.builtinMaterial.SetFloat("_Min", value);

            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Min );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            value = EditorGUI.FloatField(rect, value);
        }
    }
}