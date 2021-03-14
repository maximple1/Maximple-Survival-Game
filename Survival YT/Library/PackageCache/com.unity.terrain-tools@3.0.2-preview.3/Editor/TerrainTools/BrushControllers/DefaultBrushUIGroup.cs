
using System;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class DefaultBrushUIGroup : BaseBrushUIGroup {
        [Flags]
        public enum Feature
        {
            Size = 1 << 0,
            Rotation = 1 << 1,
            Strength = 1 << 2,
            Spacing = 1 << 3,
            Scatter = 1 << 4,
            Smoothing = 1 << 5,
            
            All = Size | Rotation | Strength | Spacing | Scatter | Smoothing,

            NoScatter = All & ~Scatter,
            NoSpacing = All & ~Spacing,
        }
        
        public DefaultBrushUIGroup(string name, Func<TerrainToolsAnalytics.IBrushParameter[]> analyticsCall = null, Feature feature = Feature.All) : base(name, analyticsCall) {
            //Scatter must be first.
            if ((feature & Feature.Scatter) != 0) {
                AddScatterController(new BrushScatterVariator(name, this, this));
            }


            if ((feature & Feature.Size) != 0)
            {                
                AddSizeController(new BrushSizeVariator(name, this, this));
            }
            if((feature & Feature.Rotation) != 0)
            {                
                AddRotationController(new BrushRotationVariator(name, this, this));
            }
            if((feature & Feature.Strength) != 0)
            {                
                AddStrengthController(new BrushStrengthVariator(name, this, this));
            }
            if((feature & Feature.Spacing) != 0)
            {                
                AddSpacingController(new BrushSpacingVariator(name, this, this));
            }

            if((feature & Feature.Smoothing) != 0)
            {                
                AddSmoothingController(new DefaultBrushSmoother(name));
            }

            AddModifierKeyController(new DefaultBrushModifierKeys());
        }
    }
}
