using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class MultiplyFilter : Filter
    {
        [SerializeField]
        public float value = 1;
        public override string GetDisplayName() => "Multiply";
        public override string GetToolTip() => "Multiply the Brush Mask filter stack by a constant";
        protected override void OnEval(FilterContext filterContext, RenderTexture source, RenderTexture dest)
        {
            FilterUtility.builtinMaterial.SetFloat("_Multiply", value);
            Graphics.Blit(source, dest, FilterUtility.builtinMaterial, (int)FilterUtility.BuiltinPasses.Multiply);
        }
        protected override void OnDrawGUI(Rect rect, FilterContext filterContext) => value = EditorGUI.FloatField(rect, value);
    }
}