using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class DefaultBrushSmoother : IBrushSmoothController {

        public int kernelSize { get; set; }

        Material m_Material = null;
        Material GetMaterial() {
            if (m_Material == null)
                m_Material = new Material(Shader.Find("Hidden/TerrainTools/SmoothHeight"));
            return m_Material;
        }

        public DefaultBrushSmoother(string name) {
            //m_SmoothTool = new SmoothHeightTool();
        }

        public bool active { get { return Event.current.shift; } }

        public void OnEnterToolMode() {}
        public void OnExitToolMode() {}
        public void OnSceneGUI(Terrain terrain, IOnSceneGUI editContext) {
            //m_SmoothTool.OnSceneGUI(terrain, editContext);
        }

        public void OnInspectorGUI(Terrain terrain, IOnInspectorGUI editContext) {
            //maybe have a UI here at some point? (To select different blur tools, etc...)
        }

        public bool OnPaint(Terrain terrain, IOnPaint editContext, float brushSize, float brushRotation, float brushStrength, Vector2 uv) {
            if (Event.current != null && Event.current.shift) {
                BrushTransform brushXform = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, brushSize, brushRotation);
                PaintContext paintContext = TerrainPaintUtility.BeginPaintHeightmap(terrain, brushXform.GetBrushXYBounds());

                Material mat = GetMaterial();//TerrainPaintUtility.GetBuiltinPaintMaterial();

                float m_direction = 0.0f; //TODO: UI for this

                Vector4 brushParams = new Vector4(brushStrength, 0.0f, 0.0f, 0.0f);
                mat.SetTexture("_BrushTex", editContext.brushTexture);
                mat.SetVector("_BrushParams", brushParams);
                Vector4 smoothWeights = new Vector4(
                    Mathf.Clamp01(1.0f - Mathf.Abs(m_direction)),   // centered
                    Mathf.Clamp01(-m_direction),                    // min
                    Mathf.Clamp01(m_direction),                     // max
                    0);                                          
                mat.SetInt("_KernelSize", (int)Mathf.Max(1, kernelSize)); // kernel size
                mat.SetVector("_SmoothWeights", smoothWeights);
                
                var texelCtx = Utility.CollectTexelValidity(paintContext.originTerrain, brushXform.GetBrushXYBounds(), 1);
                Utility.SetupMaterialForPaintingWithTexelValidityContext(paintContext, texelCtx, brushXform, mat);
                
                paintContext.sourceRenderTexture.wrapMode = TextureWrapMode.Clamp;

                var temp = RTUtils.GetTempHandle( paintContext.destinationRenderTexture.descriptor );
                temp.RT.wrapMode = TextureWrapMode.Clamp;
                mat.SetVector("_BlurDirection", Vector2.right);
                Graphics.Blit(paintContext.sourceRenderTexture, temp, mat);
                mat.SetVector("_BlurDirection", Vector2.up);
                Graphics.Blit(temp, paintContext.destinationRenderTexture, mat);

                TerrainPaintUtility.EndPaintHeightmap(paintContext, "Terrain Paint - Smooth Height");
                
                texelCtx.Cleanup();
                RTUtils.Release(temp);

                return true;
            }
            return false;
        }
    }
}
