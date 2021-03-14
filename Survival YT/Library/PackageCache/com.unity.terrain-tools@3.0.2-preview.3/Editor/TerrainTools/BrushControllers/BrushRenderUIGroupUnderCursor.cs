
using System;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	public abstract class BaseBrushRenderUIGroupUnderCursor<TBase> : IBrushRenderUnderCursor where TBase: BrushRenderWithTerrainUiGroup
	{
		protected readonly TBase m_BrushRenderWithTerrain;
		protected readonly Terrain m_TerrainAtCreation;
		protected readonly BrushTransform m_BrushTransformAtCreation;
		protected readonly bool m_ValidBrushTransform;
		
		protected Terrain terrainUnderCursor => uiGroup.terrainUnderCursor;
		protected RaycastHit raycastHitUnderCursor => uiGroup.raycastHitUnderCursor;
		protected Vector2 textureCoordUnderCursor => raycastHitUnderCursor.textureCoord;

		public IBrushUIGroup uiGroup => m_BrushRenderWithTerrain.uiGroup;
		public Texture brushTexture => m_BrushRenderWithTerrain.brushTexture;

		protected BaseBrushRenderUIGroupUnderCursor(IBrushUIGroup uiGroup, string name, Texture brushTexture)
		{
			object[] arguments = {uiGroup, name, brushTexture};
			
			m_BrushRenderWithTerrain = (TBase)Activator.CreateInstance(typeof(TBase), arguments);
			m_TerrainAtCreation = terrainUnderCursor;
			m_ValidBrushTransform = CalculateTransform(ref m_TerrainAtCreation, out m_BrushTransformAtCreation);				
		}

		#region Brush Transform
		protected virtual bool CalculateTransform(ref Terrain terrain, out BrushTransform brushTransform)
		{
			Vector2 uv = textureCoordUnderCursor;
			float brushSize = uiGroup.brushSize;
			float brushRotation = uiGroup.brushRotation;

			if(uiGroup.ScatterBrushStamp(ref terrain, ref uv))
			{
				brushTransform = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, brushSize, brushRotation);
				return true;
			}
			else
			{
				brushTransform = new BrushTransform();
				return false;
			}
		}
		
		public bool CalculateBrushTransform(out BrushTransform brushTransform)
		{
			if(m_ValidBrushTransform)
			{
				brushTransform = m_BrushTransformAtCreation;
				return true;
			}
			else
			{
				brushTransform = new BrushTransform();
				return false;
			}
		}
		#endregion

		#region Material Set-up
		public void SetupTerrainToolMaterialProperties(PaintContext paintContext, BrushTransform brushTransform, Material material)
		{
			m_BrushRenderWithTerrain.SetupTerrainToolMaterialProperties(paintContext, brushTransform, material);
		}
		#endregion

		#region Rendering
		public void RenderBrush(PaintContext paintContext, Material material, int pass)
		{
			m_BrushRenderWithTerrain.RenderBrush(paintContext, material, pass);
		}
		#endregion

		#region Texture Acquisition
		public PaintContext AcquireHeightmap(bool writable, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
		{
			return m_BrushRenderWithTerrain.AcquireHeightmap(writable, m_TerrainAtCreation, boundsInTerrainSpace, extraBorderPixels);
		}

		public PaintContext AcquireTexture(bool writable, Rect boundsInTerrainSpace, TerrainLayer layer, int extraBorderPixels = 0)
		{
			return m_BrushRenderWithTerrain.AcquireTexture(writable, m_TerrainAtCreation, boundsInTerrainSpace, layer, extraBorderPixels);
		}

		public PaintContext AcquireNormalmap(bool writable, Rect boundsInTerrainSpace, int extraBorderPixels = 0)
		{
			return m_BrushRenderWithTerrain.AcquireNormalmap(writable, m_TerrainAtCreation, boundsInTerrainSpace, extraBorderPixels);
		}

		public PaintContext AquireHolesTexture(bool writable, Rect boudsInTerrainSpace, int extraBorderPixels = 0)
		{
			return m_BrushRenderWithTerrain.AcquireHolesTexture(writable, m_TerrainAtCreation, boudsInTerrainSpace, extraBorderPixels);
		}

		public void Release(PaintContext paintContext)
		{
			m_BrushRenderWithTerrain.Release(paintContext);
		}
		#endregion

		#region IDisposable
		public void Dispose()
		{
			m_BrushRenderWithTerrain?.Dispose();
		}
		#endregion
	}
	
	public class BrushRenderUIGroupUnderCursor : BaseBrushRenderUIGroupUnderCursor<BrushRenderWithTerrainUiGroup>
	{
		public BrushRenderUIGroupUnderCursor(IBrushUIGroup uiGroup, string name, Texture brushTexture) : base(uiGroup, name, brushTexture)
		{
		}
	}
	
	public class BrushRenderPreviewUIGroupUnderCursor : BaseBrushRenderUIGroupUnderCursor<BrushRenderPreviewWithTerrainUiGroup>, IBrushRenderPreviewUnderCursor
	{
		public BrushRenderPreviewUIGroupUnderCursor(IBrushUIGroup uiGroup, string name, Texture brushTexture) : base(uiGroup, name, brushTexture)
		{
		}
		
		#region Brush Transform
		protected override bool CalculateTransform(ref Terrain terrain, out BrushTransform brushTransform)
		{
			Vector2 uv = textureCoordUnderCursor;
			float brushSize = uiGroup.brushSize;
			float brushRotation = uiGroup.brushRotation;

			// TODO: Remove this method and replace the preview with a radius effect and scatter at the correct position...
			brushTransform = TerrainPaintUtility.CalculateBrushTransform(terrain, uv, brushSize, brushRotation);
			return true;
		}
	
		#endregion

		#region Rendering
		public void RenderBrushPreview(PaintContext paintContext, TerrainPaintUtilityEditor.BrushPreview previewTexture, BrushTransform brushTransform, Material material, int pass)
		{
			m_BrushRenderWithTerrain.RenderBrushPreview(paintContext, previewTexture, brushTransform, material, pass);
		}
		#endregion
	}
}
