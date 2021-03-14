using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class AbsFilter : Filter
    {
        public override string GetDisplayName() => "Abs";
        public override string GetToolTip() => "Sets all pixels of an existing Brush Mask to their absolute values";
        protected override void OnEval(FilterContext filterContext, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Abs );
        }
    }
}