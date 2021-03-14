using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    [System.Serializable]
    public class NoiseFilter : Filter
    {
        private static readonly GUIContent coordinateLabel = EditorGUIUtility.TrTextContent( "Coordinate Space:", "Edit the Noise associated with this Filter" );
        private static readonly GUIContent worldLabel = EditorGUIUtility.TrTextContent( "World", "Sets the coordinate space for the noise field to world space. World space positions will be used to generate the noise" );
        private static readonly GUIContent localLabel = EditorGUIUtility.TrTextContent( "Local", "Sets the coordinate space for the noise field to local (brush) space. Local (brush) space positions will be used to generate the noise" );
        private static readonly GUIContent worldShortLabel = EditorGUIUtility.TrTextContent( "W", "Sets the coordinate space for the noise field to world space. World space positions will be used to generate the noise" );
        private static readonly GUIContent localShortLabel = EditorGUIUtility.TrTextContent( "L", "Sets the coordinate space for the noise field to local (brush) space. Local (brush) space positions will be used to generate the noise" );
        private static readonly GUIContent heightmapLabel = EditorGUIUtility.TrTextContent( "Use Heightmap", "Use the Heightmap to seed the positions of the Noise Field" );
        private static readonly GUIContent heightmapShortLabel = EditorGUIUtility.TrTextContent( "H", "Use the Heightmap to seed the positions of the Noise Field" );
        private static readonly GUIContent editLabel = EditorGUIUtility.TrTextContent( "Edit", "Edit the Noise associated with this Filter" );
        private static readonly GUIContent editShortLabel = EditorGUIUtility.TrTextContent( "E", "Edit the Noise associated with this Filter" );

        [ SerializeField ]
        private NoiseSettings m_noiseSettings;
        [ SerializeField ]
        private NoiseSettings m_noiseSource;
        [ SerializeField ]
        private bool m_isLocalSpace;
        [ SerializeField ]
        private bool m_useHeightmap;

        private NoiseWindow m_window = null;

        public override string GetDisplayName()
        {
            return "Noise";
        }

        public override string GetToolTip()
        {
            return "Applies noise to the brush mask based on the Noise Settings associated with this filter. To edit the noise, press the \"Edit\" or \"E\" button";
        }

        protected override void OnEval(FilterContext fc, RenderTexture sourceRenderTexture, RenderTexture destinationRenderTexture)
        {
            if( m_noiseSettings == null )
            {
                m_noiseSettings = ScriptableObject.CreateInstance< NoiseSettings >();
            }

            m_noiseSettings.useTextureForPositions = m_useHeightmap;

            if( m_useHeightmap )
            {
                m_noiseSettings.positionTexture = fc.rtHandleCollection[ FilterContext.Keywords.Heightmap ];
            }

            Vector3 brushPosWS = fc.brushPos;
            float brushSize = fc.brushSize;
            float brushRotation = fc.brushRotation;

            // TODO(wyatt): remove magic number and tie it into NoiseSettingsGUI preview size somehow
            float previewSize = 1 / 512f;

            // get proper noise material from current noise settings
            NoiseSettings noiseSettings = m_noiseSettings;
            Material mat = NoiseUtils.GetDefaultBlitMaterial( noiseSettings );

            // setup the noise material with values in noise settings
            noiseSettings.SetupMaterial( mat );

            // convert brushRotation to radians
            brushRotation *= Mathf.PI / 180;

            // change pos and scale so they match the noiseSettings preview
            bool isWorldSpace = false == m_isLocalSpace;
            brushSize = isWorldSpace ? brushSize * previewSize : 1;
            brushPosWS = isWorldSpace ? brushPosWS * previewSize : Vector3.zero;

            // // override noise transform
            Quaternion rotQ             = Quaternion.AngleAxis( -brushRotation, Vector3.up );
            Matrix4x4 translation       = Matrix4x4.Translate( brushPosWS );
            Matrix4x4 rotation          = Matrix4x4.Rotate( rotQ );
            Matrix4x4 scale             = Matrix4x4.Scale( Vector3.one * brushSize );
            Matrix4x4 noiseToWorld      = translation * scale;

            mat.SetMatrix( NoiseSettings.ShaderStrings.transform, noiseSettings.trs * noiseToWorld );

            int pass = NoiseUtils.kNumBlitPasses * NoiseLib.GetNoiseIndex( noiseSettings.domainSettings.noiseTypeName );

            var desc = destinationRenderTexture.descriptor;
            desc.graphicsFormat = NoiseUtils.singleChannelFormat;
            desc.sRGB = false;
            RTHandle rt = RTUtils.GetTempHandle( desc );
            Graphics.Blit( sourceRenderTexture, rt, mat, pass );

            Material blendMat = FilterUtility.blendModesMaterial;
            blendMat.SetTexture("_MainTex", sourceRenderTexture);
            blendMat.SetTexture("_BlendTex", rt);
            Graphics.Blit( sourceRenderTexture, destinationRenderTexture, blendMat, 1 );

            RTUtils.Release( rt );
        }

        protected override void OnDrawGUI(Rect rect, FilterContext filterContext)
        {
            if( m_noiseSettings == null )
            {
                m_noiseSettings = ScriptableObject.CreateInstance< NoiseSettings >();
            }

            GUIContent localLabel = NoiseFilter.localLabel;
            GUIContent worldLabel = NoiseFilter.worldLabel;
            GUIContent heightmapLabel = NoiseFilter.heightmapLabel;
            GUIContent editLabel = NoiseFilter.editLabel;
            
            float editWith = GUI.skin.label.CalcSize( editLabel ).x + 20f;
            Rect editRect = new Rect( rect.xMax - editWith, rect.y, editWith, rect.height );
            
            Rect labelRect = rect;
            labelRect.width = GUI.skin.label.CalcSize( coordinateLabel ).x;

            Rect worldRect = labelRect;
            worldRect.x = labelRect.xMax;
            worldRect.width = GUI.skin.button.CalcSize( worldLabel ).x + 10f;

            Rect localRect = worldRect;
            localRect.x = worldRect.xMax;
            localRect.width = GUI.skin.button.CalcSize( localLabel ).x + 10f;

            Rect heightmapRect = localRect;
            heightmapRect.x = localRect.xMax + 10f;
            heightmapRect.width = GUI.skin.button.CalcSize( heightmapLabel ).x + 10f;

            if( editRect.xMin < heightmapRect.xMax + 10f )
            {
                worldRect.x -= labelRect.width;
                localRect.x -= labelRect.width;
                heightmapRect.x -= labelRect.width;
                labelRect.width = 0;
            }

            editRect.x = Mathf.Max( editRect.x, heightmapRect.xMax + 4f );

            if( editRect.xMax > rect.xMax )
            {
                worldLabel = NoiseFilter.worldShortLabel;
                localLabel = NoiseFilter.localShortLabel;
                heightmapLabel = NoiseFilter.heightmapShortLabel;
                worldRect.width = GUI.skin.label.CalcSize( worldLabel ).x + 10f;
                localRect.width = GUI.skin.label.CalcSize( localLabel ).x + 10f;
                heightmapRect.width = GUI.skin.label.CalcSize( heightmapLabel ).x + 10f;
                localRect.x = worldRect.xMax;
                heightmapRect.x = localRect.xMax + 10f;

                editRect.x = rect.xMax - editWith;
            }

            editRect.x = Mathf.Max( heightmapRect.xMax + 4f, editRect.x );

            if( editRect.xMax > rect.xMax )
            {
                editLabel = editShortLabel;
                editRect.width = GUI.skin.label.CalcSize( editLabel ).x + 10f;
            }

            GUI.Label( labelRect, coordinateLabel );

            if( GUI.Toggle( worldRect, !m_isLocalSpace,  worldLabel, GUI.skin.button ) )
            {
                m_isLocalSpace = false;
            }
            
            if( GUI.Toggle( localRect, m_isLocalSpace,  localLabel, GUI.skin.button ) )
            {
                m_isLocalSpace = true;
            }
            
            m_useHeightmap = GUI.Toggle( heightmapRect, m_useHeightmap, heightmapLabel, GUI.skin.button );

            m_noiseSettings.useTextureForPositions = m_useHeightmap;

            if( GUI.Button( editRect, editLabel ) )
            {
                NoiseWindow wnd = NoiseWindow.Create( m_noiseSettings, m_noiseSource );
                wnd.noiseEditorView.onSettingsChanged += ( noise ) =>
                {
                    m_noiseSettings.Copy( noise );
                };
                wnd.noiseEditorView.onSourceAssetChanged += ( noise ) =>
                {
                    m_noiseSource = noise;
                };
                wnd.onDisableCallback += () =>
                {
                    m_window = null;
                };

                m_window = wnd;
            }
        }

        public override void OnDisable()
        {
            if( m_window != null )
            {
                m_window.Close();
                m_window = null;
            }
        }

        public override List< UnityEngine.Object > GetObjectsToSerialize()
        {
            return new List< UnityEngine.Object >() { m_noiseSettings };
        }
    }
}