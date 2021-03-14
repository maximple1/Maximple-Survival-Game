
using System;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	/// <summary>
	/// Implement this to handle rendering of terrain-brushes for both preview purposes and to modify the terrain.
	/// NOTE: This uses the GPU so material properties will also need to be dealt with prior to rendering.
	/// </summary>
	public interface IPaintContextRender : IDisposable
	{
		/// <summary>
		/// Sets up the material properties required when rendering a terrain-brush.
		/// </summary>
		/// <param name="paintContext">The paint-context to use.</param>
		/// <param name="brushTransform">The brush-transform to be rendered.</param>
		/// <param name="material">The material whose properties are to be initialised.</param>
		void SetupTerrainToolMaterialProperties(PaintContext paintContext, BrushTransform brushTransform, Material material);
				
		/// <summary>
		/// Renders the terrain-brush using the specified material/pass to the paint-context provided.
		/// </summary>
		/// <param name="paintContext">The paint-context to modify.</param>
		/// <param name="material">The material to use when rendering to the terrain.</param>
		/// <param name="pass">The pass on the material to use.</param>
		void RenderBrush(PaintContext paintContext, Material material, int pass);
	}

	public interface IPaintContextRenderPreview : IPaintContextRender
	{
		/// <summary>
		/// Renders a preview of the terrain-brush using the specified material/pass and paint-context provided.
		/// </summary>
		/// <param name="paintContext">The paint-context to preview the changes against.</param>
		/// <param name="previewTexture">The type of texture to preview.</param>
		/// <param name="brushTransform">The brush-transform to be rendered.</param>
		/// <param name="material">The material to use when rendering to the terrain.</param>
		/// <param name="pass">The pass on the material to use.</param>
		void RenderBrushPreview(PaintContext paintContext, TerrainPaintUtilityEditor.BrushPreview previewTexture, BrushTransform brushTransform, Material material, int pass);
	}
}
