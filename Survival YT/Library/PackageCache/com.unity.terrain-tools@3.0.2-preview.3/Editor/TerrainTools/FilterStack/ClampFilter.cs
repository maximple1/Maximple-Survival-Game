using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class ClampFilter : Filter
    {
        [SerializeField]
        public Vector2 range = new Vector2(0, 1);
        
        public override string GetDisplayName()
        {
            return "Clamp";
        }

        public override string GetToolTip()
        {
            return "Clamps the pixels of a mask to the specified range. Change the X value to specify the low end of the range, and change the Y value to specify the high end of the range.";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            FilterUtility.builtinMaterial.SetVector("_ClampRange", range);

            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, FilterUtility.builtinMaterial, ( int )FilterUtility.BuiltinPasses.Clamp );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            range = EditorGUI.Vector2Field(rect, "", range);
        }
    }
}