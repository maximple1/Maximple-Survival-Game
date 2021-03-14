namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// An enum used for defining which n-dimensional spaces a particular
    /// NoiseType or FractalType implementation supports
    /// </summary>
    [System.Serializable]
    [System.Flags]
    public enum NoiseDimensionFlags
    {
        /// <summary>
        /// No dimensions are supported
        /// </summary>
        None = 0,
        /// <summary>
        /// One-dimensional noise is supported
        /// </summary>
        _1D = ( 1 << 0 ),
        /// <summary>
        /// Two-dimensional noise is supported
        /// </summary>
        _2D = ( 1 << 1 ),
        /// <summary>
        /// Three-dimensional noise is supported
        /// </summary>
        _3D = ( 1 << 2 ),
        /// <summary>
        /// Four-dimensional noise is supported
        /// </summary>
        _4D = ( 1 << 3 ),
    }
}