
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

namespace UnityEditor.Experimental.TerrainAPI
{
	/// <summary>
	/// Implement this interface to wrap all of the functionality required to render a
	/// terrain-brush at a particular UV co-ordinate on a particular terrain.
	/// </summary>
	public interface IBrushRenderWithTerrain : IPaintContextRender
	{
		#region BrushTransform
		/// <summary>
		/// Calculates the brush-transform on the specified terrain at the specified UV co-ordinates (taking into account scattering).
		/// </summary>
		/// <param name="terrain">The terrain to calculate the brush-transform for.</param>
		/// <param name="uv">The UV co-ordinate on that terrain.</param>
		/// <returns>The brush-transform on the terrain at the specified UV co-ordinates.</returns>
		/// <param name="size">The size of the brush.</param>
		/// <param name="rotation">The rotation about the Y axis of the brush.</param>
		/// <param name="brushTransform">The brush-transform on the terrain at the specified UV co-ordinates.</param>
		/// <returns>"true" if calculated successfully, "false" otherwise.</returns>
		bool CalculateBrushTransform(Terrain terrain, Vector2 uv, float size, float rotation, out BrushTransform brushTransform);
		
		/// <summary>
		/// Calculates the brush-transform on the specified terrain at the specified UV co-ordinates (taking into account scattering).
		/// </summary>
		/// <param name="terrain">The terrain to calculate the brush-transform for.</param>
		/// <param name="uv">The UV co-ordinate on that terrain.</param>
		/// <param name="size">The size of the brush.</param>
		/// <param name="brushTransform">The brush-transform on the terrain at the specified UV co-ordinates.</param>
		/// <returns>"true" if calculated successfully, "false" otherwise.</returns>
		bool CalculateBrushTransform(Terrain terrain, Vector2 uv, float size, out BrushTransform brushTransform);
		
		/// <summary>
		/// Calculates the brush-transform on the specified terrain at the specified UV co-ordinates (taking into account scattering).
		/// </summary>
		/// <param name="terrain">The terrain to calculate the brush-transform for.</param>
		/// <param name="uv">The UV co-ordinate on that terrain.</param>
		/// <param name="brushTransform">The brush-transform on the terrain at the specified UV co-ordinates.</param>
		/// <returns>"true" if calculated successfully, "false" otherwise.</returns>
		bool CalculateBrushTransform(Terrain terrain, Vector2 uv, out BrushTransform brushTransform);
		#endregion

		#region PaintContext
		/// <summary>
		/// Gets the PaintContext for the height-map at the bounds specified,
		/// you need to say whether this is to be writable upon acquisition.
		/// </summary>
		/// <param name="writable">"true" if we wish to allow writing to the height-map, "false" otherwise.</param>
		/// <param name="terrain">The initial terrain to acquire the height-map for..</param>
		/// <param name="boundsInTerrainSpace">The bounds of the height-map to use (in pixels).</param>
		/// <param name="extraBorderPixels">Extra padding on the bounds specified.</param>
		/// <returns>The paint context created.</returns>
		PaintContext AcquireHeightmap(bool writable, Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0);
		
		/// <summary>
		/// Gets the PaintContext for the texture-map at the bounds specified,
		/// you need to say whether this is to be writable upon acquisition.
		/// </summary>
		/// <param name="writable">"true" if we wish to allow writing to the texture-map, "false" otherwise.</param>
		/// <param name="terrain">The initial terrain to acquire the texture-map for..</param>
		/// <param name="boundsInTerrainSpace">The bounds of the texture-map to use (in pixels).</param>
		/// <param name="layer">The terrain layer to acquire the texture-map for.</param>
		/// <param name="extraBorderPixels">Extra padding on the bounds specified.</param>
		/// <returns>The paint context created.</returns>
		PaintContext AcquireTexture(bool writable, Terrain terrain, Rect boundsInTerrainSpace, TerrainLayer layer, int extraBorderPixels = 0);

		/// <summary>
		/// Gets the PaintContext for the normal-map at the bounds specified,
		/// you need to say whether this is to be writable upon acquisition.
		/// </summary>
		/// <param name="writable">"true" if we wish to allow writing to the normal-map, "false" otherwise.</param>
		/// <param name="terrain">The initial terrain to acquire the normal-map for..</param>
		/// <param name="boundsInTerrainSpace">The bounds of the normal-map to use (in pixels).</param>
		/// <param name="extraBorderPixels">Extra padding on the bounds specified.</param>
		/// <returns>The paint context created.</returns>
		PaintContext AcquireNormalmap(bool writable, Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0);

		/// <summary>
		/// Gets the PaintContext for the holes at the bounds specified,
		/// you need to say whether this is to be writable upon acquisition.
		/// </summary>
		/// <param name="writable">"true" if we wish to allow writing to the normal-map, "false" otherwise.</param>
		/// <param name="terrain">The initial terrain to acquire the normal-map for..</param>
		/// <param name="boundsInTerrainSpace">The bounds of the normal-map to use (in pixels).</param>
		/// <param name="extraBorderPixels">Extra padding on the bounds specified.</param>
		/// <returns>The paint context created.</returns>
		PaintContext AcquireHolesTexture(bool writable, Terrain terrain, Rect boundsInTerrainSpace, int extraBorderPixels = 0);

		/// <summary>
		/// Releases the PaintContext specified, if this was made writable when
		/// acquired then we write back into the texture at this point.
		/// </summary>
		/// <param name="paintContext">The paint context to be released.</param>
		void Release(PaintContext paintContext);
		#endregion
	}
	
	public interface IBrushRenderPreviewWithTerrain : IBrushRenderWithTerrain, IPaintContextRenderPreview
	{
	}
}
