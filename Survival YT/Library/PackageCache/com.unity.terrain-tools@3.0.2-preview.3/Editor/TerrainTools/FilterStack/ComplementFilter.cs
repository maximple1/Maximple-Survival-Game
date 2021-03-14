using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class ComplementFilter : Filter
    {
        [SerializeField]
        public float value = 1;
        
        public override string GetDisplayName()
        {
            return "Complement";
        }

        public override string GetToolTip()
        {
            return "Subtracts each pixel value in the current Brush Mask from the specified constant. To invert the mask results, leave the complement value unchanged as 1.";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            FilterUtility.builtinMaterial.SetFloat("_Complement", value);

            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Complement );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            value = EditorGUI.FloatField(rect, value);
        }
    }
}