namespace UnityEditor.Experimental.TerrainAPI
{
	public interface IBrushSpacingController : IBrushController
	{
		float brushSpacing { get; }
		bool allowPaint { get; set; }
	}
}
