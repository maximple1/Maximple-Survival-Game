
using System;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class MeshBrushUIGroup : BaseBrushUIGroup
    {
        public MeshBrushUIGroup( string name, Func<TerrainToolsAnalytics.IBrushParameter[]> analyticsCall = null ) : base( name, analyticsCall)
        {
            AddSizeController(new BrushSizeVariator(name, this, this));             
            AddRotationController(new BrushRotationVariator(name, this, this));
            AddStrengthController(new BrushStrengthVariator(name, this, this, .075f));
            AddSmoothingController(new DefaultBrushSmoother(name));
        }
    }
}
