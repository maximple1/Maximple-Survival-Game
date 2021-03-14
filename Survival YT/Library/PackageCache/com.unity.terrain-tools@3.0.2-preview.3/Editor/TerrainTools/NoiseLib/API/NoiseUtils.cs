using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// A utility class for rendering noise defined by NoiseSettings into various Texture types.
    /// </summary>
    public static class NoiseUtils
    {
        // private static readonly int kAllSlices = -1;

        /// <summary>
        /// Number of passes that the builtin noise blit shader contains. The passes include
        /// one for blitting 2D noise and one for blitting 3D noise.
        /// </summary>
        public static readonly int kNumBlitPasses = 2;

        private static bool supportsCopyTexture3D
        {
            get { return (SystemInfo.copyTextureSupport & CopyTextureSupport.Copy3D) != 0; }
        }

        private static bool supportsCopyTextureRTToTexture
        {
            get { return (SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) != 0; }
        }

        /// <summary>
        /// Format to use when rendering a Noise field into a single channel RenderTexture
        /// </summary>
        public static GraphicsFormat singleChannelFormat =>
            SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, FormatUsage.Render) &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 &&
            SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2
            // return Terrain height format because values will be packed and unpacked on GPU using Terrain height functions    
            ? GraphicsFormat.R16_SFloat : Terrain.heightmapFormat;


        /// <summary>
        /// Format to use when rendering a preview of a Noise field into a RenderTexture with NoiseUtils.BlitPreview functions
        /// </summary>
        public static GraphicsFormat previewFormat => GraphicsFormat.R8G8B8A8_UNorm;

        static NoiseUtils()
        {
            
        }

        /*=========================================================================

            Material Getter Functions

        =========================================================================*/

        private static Material s_defaultPreviewMaterial;

        private static Material GetDefaultPreviewMaterial()
        {
            if(s_defaultPreviewMaterial == null)
            {
                s_defaultPreviewMaterial = new Material(Shader.Find("Hidden/TerrainTools/Noise/Preview"));
            }

            return s_defaultPreviewMaterial;
        }

        /// <summary>
        /// Returns a Material reference to the default blit material for the given Type of FractalType.
        /// </summary>
        /// <remarks> Usage is: Material mat = GetDefaultBlitMaterial( typeof(FbmFractalType) ); </remarks>
        /// <returns> A reference to the default blit Material for the specified Type of FractalType </return>
        /// <param name="fractalType"> The Type for a given FractalType </param>
        public static Material GetDefaultBlitMaterial(Type fractalType)
        {
            return NoiseLib.GetGeneratedMaterial(typeof(NoiseBlitShaderGenerator), fractalType);
        }

        /// <summary>
        /// Returns a Material reference to the default blit material for the given NoiseSettings object.
        /// </summary>
        /// <remarks>
        /// Internally, this uses noise.domainSettings.fractalTypeName to get it's FractalType
        /// </remarks>
        /// <returns> A reference to the default blit Material for the specified NoiseSettings instance </returns>
        public static Material GetDefaultBlitMaterial(NoiseSettings noise)
        {
            IFractalType fractal = NoiseLib.GetFractalTypeInstance( noise.domainSettings.fractalTypeName );
            
            if(fractal == null)
            {
                return null;
            }

            return GetDefaultBlitMaterial( fractal.GetType() );
        }

        /*=========================================================================

            Blit Noise using Preview shader

        =========================================================================*/

        /// <summary>
        /// Blits the source RenderTexture into the destination RenderTexture using the default Preview Blit Material.
        /// This is the blit Material that is used when rendering the NoiseSettings Preview.
        /// </summary>
        /// <param name = "source"> The source RenderTexture used in the Blit operation </param>
        /// <param name = "destination">
        /// The destination RenderTexture in the Blit operation
        /// This will have the noise preview shader logic applied to it.
        /// </param>
        public static void BlitPreview2D(RenderTexture source, RenderTexture destination)
        {
            BlitPreview2D(source, destination, GetDefaultPreviewMaterial());
        }

        /// <summary>
        /// Blits the source RenderTexture into the destination RenderTexture using the specified Material.
        /// </summary>
        /// <param name = "source"> The source RenderTexture used in the Blit operation </param>
        /// <param name = "destination">
        /// The destination RenderTexture in the Blit operation
        /// This will have the noise preview shader logic applied to it.
        /// </param>
        /// <param name="mat"> The material to be used in the blit operation </param>
        public static void BlitPreview2D(RenderTexture src, RenderTexture dest, Material mat)
        {
            if(mat == null)
            {
                Debug.LogError("NoiseUtils::BlitPreview2D: Provided preview material is NULL");
                
                Graphics.Blit(src, dest);

                return;
            }

            Graphics.Blit(src, dest, mat, 0);
        }

        /*=========================================================================

            Blit raw noise data into texture

        =========================================================================*/

        /// <summary>
        /// Blits 2D noise defined by the given NoiseSettings instance into the destination RenderTexture.
        /// </summary>
        /// <param name = "noise"> An instance of NoiseSettings defining the type of noise to render </param>
        /// <param name = "dest"> The destination RenderTexture that the noise will be rendered into. </param>
        public static void Blit2D(NoiseSettings noise, RenderTexture dest)
        {
            Material mat = GetDefaultBlitMaterial( noise );
            
            if( mat == null )
            {
                return;
            }

            Blit2D( noise, dest, mat );
        }

        /// <summary>
        /// Blits 2D noise defined by the given NoiseSettings instance into the destination RenderTexture
        /// using the provided Material.
        /// </summary>
        /// <param name = "noise"> An instance of NoiseSettings defining the type of noise to render </param>
        /// <param name = "dest"> The destination RenderTexture that the noise will be rendered into. </param>
        /// <param name = "mat"> The Material to be used for rendering the noise </param>
        public static void Blit2D(NoiseSettings noise, RenderTexture dest, Material mat)
        {
            int pass = NoiseLib.GetNoiseIndex( noise.domainSettings.noiseTypeName );

            INTERNAL_Blit2D( noise, dest, mat, pass * kNumBlitPasses + 0 );
        }

        private static void INTERNAL_Blit2D(NoiseSettings noise, RenderTexture dest, Material mat, int pass)
        {
            noise.SetupMaterial( mat );

            var tempRT = RenderTexture.GetTemporary(dest.descriptor);
            var prev = RenderTexture.active;

            RenderTexture.active = tempRT; // keep this
            Graphics.Blit(tempRT, mat, pass);
            Graphics.Blit(tempRT, dest);

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(tempRT);
        }

        /// <summary>
        /// Blits 3D noise defined by the given NoiseSettings instance into the destination RenderTexture.
        /// </summary>
        /// <param name = "noise"> An instance of NoiseSettings defining the type of noise to render </param>
        /// <param name = "dest"> The destination RenderTexture that the noise will be rendered into. </param>
        public static void Blit3D(NoiseSettings noise, RenderTexture dest)
        {
            throw new NotImplementedException("NoiseUtils::Blit3D: Function not implemented yet");

            // Debug.Assert(dest.dimension == UnityEngine.Rendering.TextureDimension.Tex3D,
            //              "NoiseUtils::Blit3D: Provided RenderTexture is not a 3D texture. You have to manually create it as a volume");
            
            // Material mat = GetDefaultBlitMaterial(noise);
            
            // if(mat == null)
            // {
            //     return;
            // }

            // int pass = NoiseLib.GetNoiseIndex(noise.domainSettings.noiseTypeName);

            // RenderTexture prev = RenderTexture.active;

            // Graphics.SetRenderTarget(dest, 0, CubemapFace.Unknown, kAllSlices);

            // // Graphics.Blit( dest, mat,  )

            // RenderTexture.active = prev;
        }

        /// <summary>
        /// Bakes 2D noise defined by the given NoiseSettings instance into a Texture2D instance and returns
        /// a reference to it.
        /// </summary>
        /// <param name = "noise"> An instance of NoiseSettings defining the type of noise to bake </param>
        /// <param name = "width"> The width of the baked Texture2D </param>
        /// <param name = "height"> The height of the baked Texture2D </param>
        /// <param name = "format"> The GraphicsFormat for the baked Texture2D. In most cases, you will want to use GraphicsFormat.R16_UNorm </param>
        /// <param name = "flags"> TextureCreation flags for the baked Texture2D </param>
        /// <returns> A reference to the baked Texture2D instance </returns>
        public static Texture2D BakeToTexture2D(NoiseSettings noise, int width, int height,
                                                GraphicsFormat format = GraphicsFormat.R16_UNorm,
                                                TextureCreationFlags flags = TextureCreationFlags.None)
        {
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 0, GraphicsFormat.R16_UNorm);
            Texture2D texture = new Texture2D( width, height, format, flags );

            Blit2D(noise, rt);

            RenderTexture.active = rt;

            bool mipChain = ( (int)flags & (int)TextureCreationFlags.MipChain ) != 0;
            texture.ReadPixels( new Rect( 0, 0, width, height ), 0, 0, mipChain );

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary( rt );

            return texture;
        }

        /// <summary>
        /// Bakes 3D noise defined by the given NoiseSettings instance into a Texture3D instance and returns
        /// a reference to it.
        /// </summary>
        /// <param name = "noise"> An instance of NoiseSettings defining the type of noise to bake </param>
        /// <param name = "width"> The width of the baked Texture3D </param>
        /// <param name = "height"> The height of the baked Texture3D </param>
        /// <param name = "depth"> The depth of the baked Texture3D </param>
        /// <param name = "format"> The GraphicsFormat for the baked Texture3D. In most cases, you will want to use GraphicsFormat.R16_UNorm </param>
        /// <param name = "flags"> TextureCreation flags for the baked Texture3D. </param>
        /// <returns> A reference to the baked Texture3D instance </returns>
        /// <remarks>
        /// Be careful when specifying TextureCreation flags. If you specify that mipmaps should be generated for
        /// a Texture3D, that will use a lot more memory than if you were generating mipmaps for a Texture2D.
        /// </remarks>
        public static Texture3D BakeToTexture3D(NoiseSettings noise, int width, int height, int depth,
                                                GraphicsFormat format = GraphicsFormat.R16_UNorm,
                                                TextureCreationFlags flags = TextureCreationFlags.None)
        {
            Material mat = GetDefaultBlitMaterial( noise );
            
            if(mat == null)
            {
                return null;
            }

            RenderTexture sliceRT = RenderTexture.GetTemporary(width, height, 0, GraphicsFormat.R16_UNorm);
            Texture2D slice2D = new Texture2D(width, height, format, flags);

            Color[] colors = new Color[width * height * depth];

            noise.SetupMaterial(mat);
            int pass = NoiseLib.GetNoiseIndex(noise);

            RenderTexture.active = sliceRT;

            List<Color[]> sliceColors = new List<Color[]>(depth);

            for(int i = 0; i < depth; ++i)
            {
                float uvy = ((float)i + 0.5f) / depth;
                mat.SetFloat("_UVY", uvy);

                Graphics.Blit(null, sliceRT, mat, pass * kNumBlitPasses + 1);

                slice2D.ReadPixels(new Rect(0, 0, width, height), 0, 0);

                sliceColors.Add(slice2D.GetPixels(0, 0, width, height));
            }

            int pixPerSlice = width * height;

            for(int sliceID = 0; sliceID < sliceColors.Count; ++sliceID)
            {
                for(int pixelID = 0; pixelID < sliceColors[sliceID].Length; ++pixelID)
                {
                    int pixel = (pixPerSlice * sliceID) + pixelID;
                    colors[pixel] = sliceColors[sliceID][pixelID];
                }
            }

            bool mipChain = ( (int)flags & (int)TextureCreationFlags.MipChain ) != 0;

            Texture3D texture = new Texture3D(width, height, depth, format, flags);

            texture.SetPixels(colors);
            texture.Apply(mipChain);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary( sliceRT );

            return texture;
        }
    }
}