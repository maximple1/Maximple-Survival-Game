using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Provides information for generating images based on Terrain texture data ie. procedural brush masks
    /// </summary>
    public class FilterContext : System.IDisposable
    {
        private bool m_Disposed;

        /// <summary>
        /// The position of the brush in world space coordinates
        /// </summary>
        public Vector3 brushPos { get; internal set; }

        /// <summary>
        /// The size of the brush in world space units
        /// </summary>
        public float brushSize { get; internal set; }
        
        /// <summary>
        /// The rotation of the brush in degrees
        /// </summary>
        public float brushRotation { get; internal set; }
        
        /// <summary>
        /// A collection of common RenderTextures that are used during Filter composition
        /// </summary>
        public RTHandleCollection rtHandleCollection { get; private set; }
        
        /// <summary>
        /// A collection of common floating-point values that are used during Filter composition
        /// </summary>
        public Dictionary<string, float> floatProperties { get; private set; }
        
        /// <summary>
        /// A collection of common integer values that are used during Filter composition
        /// </summary>
        public Dictionary<string, int> intProperties { get; private set; }
        
        /// <summary>
        /// A collection of common vector values that are used during Filter composition
        /// </summary>
        public Dictionary<string, Vector4> vectorProperties { get; private set; }

        /// <summary>
        /// The GraphicsFormat that will be used for destination RenderTextures when a FilterStack is evaluated.
        /// This is used for some validation without the need for actual RenderTextures
        /// </summary>
        public GraphicsFormat targetFormat { get; internal set; }
        
        /// <summary>
        /// Constructor
        /// <param name="brushPos">The brush position</param>
        /// <param name="brushSize">The brush size</param>
        /// <param name="brushRotation">The brush rotation</param>
        /// </summary>
        public FilterContext(GraphicsFormat targetFormat, Vector3 brushPos, float brushSize, float brushRotation)
        {
            rtHandleCollection = new RTHandleCollection();
            floatProperties = new Dictionary<string, float>();
            intProperties = new Dictionary<string, int>();
            vectorProperties = new Dictionary<string, Vector4>();

            this.brushPos = brushPos;
            this.brushSize = brushSize;
            this.brushRotation = brushRotation;
            this.targetFormat = targetFormat;
        }

        /// <summary>
        /// Release gathered RenderTexture resources
        /// </summary>
        public void ReleaseRTHandles()
        {
            rtHandleCollection?.ReleaseRTHandles();
        }

        /// <summary>
        /// Dispose method for this class
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Overridable Dispose method for this class. Override this if you create a class that derives from FilterContext
        /// <param name="dispose">Whether or not resources should be disposed</param>
        /// </summary>
        public virtual void Dispose(bool dispose)
        {
            if(m_Disposed) return;

            if(!dispose) return;

            rtHandleCollection?.Dispose(dispose);
            rtHandleCollection = null;
            floatProperties = null;

            m_Disposed = true;
        }

        /// <summary>
        /// Keywords for common RenderTextures and floating-point values that are added to a FilterContext
        /// </summary>
        public static class Keywords
        {
            /// <summary>
            /// Keyword for the Heightmap texture of the associated Terrain instance
            /// </summary>
            public static readonly string Heightmap = "_Heightmap";

            /// <summary>
            /// Keyword for the scale of the Terrain
            /// </summary>
            public static readonly string TerrainScale = "_TerrainScale";
        }
    }
}