using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class NegateFilter : Filter
    {
        public override string GetDisplayName() => "Negate";
        public override string GetToolTip() => "Reverses the sign of all pixels in the current mask. For example, 1 becomes -1, 0 remains the same, and -1 becomes 1.";
        protected override void OnEval(FilterContext filterContext, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Negate );
        }
    }
}