using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    public class NoiseEditorView : BindableElement
    {
        internal static class Styles
        {
            public static GUIContent title = EditorGUIUtility.TrTextContent("Edit Noise");
            
            // uss and class names
            public static readonly string noiseWindowName = "noise-window";
            public static readonly string settingsContainerName = "noise-window__settings-container";
            public static readonly string objectFieldContainer = "noise-window__noise-asset-field-container";
            public static readonly string noiseAssetFieldName = "noise-window__noise-asset-field";
            public static readonly string settingsScrollViewName = "noise-window__settings-scrollview";
            public static readonly string noisePreviewTextureName = "noise-window__preview-container-texture";
            public static readonly string noisePreviewLabelName = "noise-window__preview-texture-label";
            public static readonly string noisePreviewContainerName = "noise-window__preview-container";
            public static readonly string noiseGUIContainerName = "noise-window__noise-gui-container";
            public static readonly string saveButtonsContainer = "noise-window__file-panel-container";
            public static readonly string filePanelContainer = "noise-window__file-panel-container";
            public static readonly string filePanelButton = "noise-window__file-panel-button";
            public static readonly string resetButtonName = "noise-window__file-panel-button-reset";
            public static readonly string saveAsButtonName = "noise-window__file-panel-button-saveas";
            public static readonly string applyButtonName = "noise-window__file-panel-button-apply";
            public static readonly string exportContainer = "noise-window__export-container";
            public static readonly string foldoutContainer = "noise-window__foldout-container";
            public static readonly string exportHeader = "noise-window__export-header";
            public static readonly string exportType = "noise-window__export-type";
            public static readonly string exportDims2D = "noise-window__export-dimensions";
            public static readonly string exportDims3D = "noise-window__export-dimensions";
            public static readonly string exportFormat = "noise-window__export-format";
            public static readonly string exportHeaderLabel = "noise-window__export-header-label";
            public static readonly string exportSettings = "noise-window__export-settings";
            public static readonly string exportButton = "noise-window__export-button";
            public static readonly string flexArea = "noise-window__flex-area";
            public static readonly string flexHalf = "noise-window__flex-half";
            public static readonly string flexThird = "noise-window__flex-third";
            
            // labels, tooltips, etc
            public static readonly string noiseAssetFieldLabel = "Noise Settings Asset";
            public static readonly string noiseAssetFieldTooltip = "The source NoiseSettings Asset that serves as the base " +
                                                                   "state of the NoiseSettings that is being edited and previewed. " +
                                                                   "This is what the settings are reverted to if an Asset is provided";
            public static readonly string resetTooltip = "Resets the NoiseSettings the to default settings for the NoiseSettings Asset type";
            public static readonly string revertTooltip = "Reverts the NoiseSettings the to the settings of the specified source NoiseSettings Asset";
            public static readonly string saveasTooltip = "Open a Save File dialogue that allows you to specify a new file that the current state of the NoiseSettings Asset will be saved to";
            public static readonly string applyTooltip = "Apply the current settings to the source NoiseSettings Asset";
            public static readonly string exportTooltip = "Toggle the view for settings that allow you to export the generated Noise to a 2D or 3D Texture and save that to disk";
            public static readonly string previewLabel = "Noise Field Preview:";
            public static readonly string previewLabelTooltip = "Noise Field Preview:";

            public static readonly string reset = "Reset";
            public static readonly string revert = "Revert";
        }

        public enum ExportTextureType
        {
            Texture2D = 0,
            Texture3D,
        }

        [ SerializeField ] private NoiseSettings m_noiseSourceAsset;
        [ SerializeField ] private NoiseSettings m_noiseUpdateTarget;
        [ SerializeField ] private NoiseSettings m_noiseProfileIfNull;
        [ SerializeField ] private SerializedObject m_serializedNoiseProfile;

        public NoiseSettings noiseUpdateTarget
        {
            get { return m_noiseUpdateTarget; }
        }

        public NoiseSettings noiseSourceAsset
        {
            get
            {
                return m_noiseSourceAsset;
            }

            set
            {
                if( value == m_noiseSourceAsset )
                {
                    return;
                }

                m_noiseSourceAsset = value;

                INTERNAL_OnSourceProfileChanged( value );
            }
        }
        
        private NoiseSettingsGUI m_noiseGUI;
        private NoiseFieldView m_noiseFieldView;
        private VisualElement m_settingsContainer;

        private VisualElement filePanelContainer;
        private ObjectField objectField;
        private Button saveAsButton;
        private Button revertButton;
        private Button applyButton;

        private VisualElement m_exportContainer;
        private VisualElement m_exportSettings;
        private PopupField< ExportTextureType > m_exportType;
        private Vector2IntField m_exportDims2D;
        private Vector3IntField m_exportDims3D;
        private PopupField< GraphicsFormat > m_exportFormat;
        private VisualElement m_exportButton;

        public NoiseEditorView( NoiseSettings _noiseUpdateTarget_ = null, NoiseSettings _sourceAsset_ = null )
        {
            // create temp noisesettings asset and the IMGUI view for this window
            m_noiseUpdateTarget = _noiseUpdateTarget_ == null ? ScriptableObject.CreateInstance< NoiseSettings >() : _noiseUpdateTarget_;
            m_serializedNoiseProfile = new SerializedObject( m_noiseUpdateTarget );
            m_noiseGUI = new NoiseSettingsGUI();
            m_noiseGUI.Init( m_noiseUpdateTarget );

            m_noiseSourceAsset = _sourceAsset_;

            var stylesheet = EditorGUIUtility.isProSkin ?
                Resources.Load< StyleSheet >( "Styles/Noise_Dark" ) :
                Resources.Load< StyleSheet >( "Styles/Noise_Light" );
            
            var settingsScrollView = new ScrollView()
            {
                name = Styles.settingsScrollViewName
            };

            ///////////////////////////////////////////////////////////////////////////////
            // settings buttons
            ///////////////////////////////////////////////////////////////////////////////
            
            var noiseGUIContainer = new IMGUIContainer()
            {
                name = Styles.noiseGUIContainerName
            };
            noiseGUIContainer. onGUIHandler = () =>
            {
                EditorGUI.BeginChangeCheck();
                {
                    m_noiseGUI.OnGUI( NoiseSettingsGUIFlags.All & ( ~NoiseSettingsGUIFlags.Preview ) );
                }
                bool changed = EditorGUI.EndChangeCheck();

                if( changed )
                {
                    INTERNAL_OnSettingsChanged();
                }
            };
            settingsScrollView.Add( noiseGUIContainer );

            ///////////////////////////////////////////////////////////////////////////////
            // settings buttons
            ///////////////////////////////////////////////////////////////////////////////
            
            filePanelContainer = new VisualElement()
            {
                name = Styles.saveButtonsContainer,
                style =
                {
                    flexDirection = FlexDirection.Row
                }
            };
            filePanelContainer.AddToClassList( Styles.filePanelContainer );

            saveAsButton = new Button( SaveAsCallback )
            {
                name = Styles.saveAsButtonName,
                text = "Save As",
                tooltip = Styles.saveasTooltip
            };
            saveAsButton.AddToClassList( Styles.filePanelButton );
            
            revertButton = new Button( ResetRevertCallback )
            {
                name = Styles.resetButtonName,
                text = "Reset",
                tooltip = Styles.resetTooltip
            };
            revertButton.AddToClassList( Styles.filePanelButton );
            
            applyButton = new Button( () => { Undo.RecordObject( m_noiseSourceAsset, "NoiseWindow - Apply Settings" ); m_noiseSourceAsset.CopySerialized( m_noiseUpdateTarget ); } )
            {
                name = Styles.applyButtonName,
                text = "Apply",
                tooltip = Styles.applyTooltip
            };
            applyButton.AddToClassList( Styles.filePanelButton );
            applyButton.AddToClassList( Styles.filePanelButton );

            ///////////////////////////////////////////////////////////////////////////////
            // noise settings object field
            ///////////////////////////////////////////////////////////////////////////////
            
            var objectFieldContainer = new VisualElement()
            {
                name = Styles.objectFieldContainer
            };
            objectFieldContainer.AddToClassList( Styles.objectFieldContainer );

            objectField = new ObjectField()
            {
                name = Styles.noiseAssetFieldName,
                allowSceneObjects = false,
                objectType = typeof( NoiseSettings ),
                label = Styles.noiseAssetFieldLabel,
                tooltip = Styles.noiseAssetFieldTooltip//,
                // viewDataKey = Styles.noiseAssetFieldName
            };
            objectField.AddToClassList( Styles.noiseAssetFieldName );
            objectField.RegisterCallback< ChangeEvent< UnityEngine.Object > >( OnSourceProfileChanged );

            objectFieldContainer.Add( objectField );

            ///////////////////////////////////////////////////////////////////////////////
            // export settings
            ///////////////////////////////////////////////////////////////////////////////
            
            var flexArea = new VisualElement()
            {
                name = Styles.flexArea
            };
            flexArea.AddToClassList( Styles.flexArea );

            var exportContainer = new VisualElement()
            {
                name = Styles.exportContainer
            };
            exportContainer.AddToClassList( Styles.exportContainer );

            var exportHeader = new Foldout()
            {
                name = Styles.exportHeader,
                text = "Export Settings",
                tooltip = Styles.exportTooltip,
                viewDataKey = Styles.exportHeader
            };
            exportHeader.RegisterCallback< ChangeEvent< bool > >( 
                ( evt ) =>
                {
                    if( evt.newValue )
                    {
                        m_exportContainer.Add( m_exportSettings );
                        m_exportContainer.Add( m_exportButton );
                    }
                    else
                    {
                        m_exportContainer.Remove( m_exportSettings );
                        m_exportContainer.Remove( m_exportButton );
                    }
                }
             );
             exportHeader.AddToClassList( Styles.foldoutContainer );

            var exportSettings = CreateExportSettingsView();

            var exportButton = new Button(
                () =>
                {
                    if( m_exportType.value == ExportTextureType.Texture2D )
                    {
                        Export2D();
                    }
                    else if( m_exportType.value == ExportTextureType.Texture3D )
                    {
                        Export3D();
                    }
                }
            )
            {
                name = Styles.exportButton,
                text = "Export To Texture"
            };
            exportButton.AddToClassList( Styles.exportButton );

            m_exportButton = exportButton;
            exportContainer.Add( exportHeader );
            // exportContainer.Add( exportSettings );
            // exportContainer.Add( exportButton );

            m_exportContainer = exportContainer;
            exportHeader.value = false;

            // container for the settings panel
            var settingsContainer = new VisualElement()
            {
                name = Styles.settingsContainerName
            };
            settingsContainer.AddToClassList( Styles.settingsContainerName );
            settingsContainer.Add( objectFieldContainer );
            settingsContainer.Add( filePanelContainer );
            settingsContainer.Add( settingsScrollView );
            settingsContainer.Add( flexArea ); // add this so the export stuff stays at the bottom of the settings container
            settingsContainer.Add( exportContainer );
            settingsContainer.Bind( m_serializedNoiseProfile );

            ///////////////////////////////////////////////////////////////////////////////
            // settings buttons
            ///////////////////////////////////////////////////////////////////////////////
            
            var previewContainer = new VisualElement()
            {
                name = Styles.noisePreviewContainerName
            };
            previewContainer.AddToClassList( Styles.noisePreviewContainerName );

            var previewLabel = new Label()
            {
                name = Styles.noisePreviewLabelName,
                text = Styles.previewLabel,
                tooltip = Styles.previewLabelTooltip
            };
            previewLabel.AddToClassList( Styles.noisePreviewLabelName );
            previewContainer.Add( previewLabel );

            m_noiseFieldView = new NoiseFieldView( m_serializedNoiseProfile )
            {
                name = Styles.noisePreviewTextureName
            };
            m_noiseFieldView.onGUIHandler += () =>
            {
                INTERNAL_OnSettingsChanged();
            };
            m_noiseFieldView.AddToClassList( Styles.noisePreviewTextureName );
            previewContainer.Add( m_noiseFieldView );

            ///////////////////////////////////////////////////////////////////////////////
            // wrap it all up
            ///////////////////////////////////////////////////////////////////////////////
            
            styleSheets.Add( stylesheet );
            AddToClassList( Styles.noiseWindowName );
            Add( settingsContainer );
            Add( previewContainer );

            this.Bind( m_serializedNoiseProfile );

            m_settingsContainer = settingsContainer;

            INTERNAL_OnSourceProfileChanged( _sourceAsset_ );

            this.viewDataKey = Styles.noiseWindowName;
        }

        private VisualElement CreateExportSettingsView()
        {
            var settingsContainer = new VisualElement()
            {
                name = Styles.exportSettings
            };
            settingsContainer.AddToClassList( Styles.exportSettings );

            var exportTypes = new List< ExportTextureType >()
            {
                ExportTextureType.Texture2D,
                ExportTextureType.Texture3D
            };

            var exportType = new PopupField< ExportTextureType >( exportTypes, exportTypes[ 0 ] )
            {
                name = Styles.exportType,
                label = "Type"
            };
            exportType.RegisterCallback< ChangeEvent< ExportTextureType > >(
                ( evt ) =>
                {
                    if( evt.newValue == ExportTextureType.Texture2D )
                    {
                        m_exportSettings.Remove( m_exportDims3D );
                        m_exportSettings.Insert( 1, m_exportDims2D );
                    }
                    else if( evt.newValue == ExportTextureType.Texture3D )
                    {
                        m_exportSettings.Remove( m_exportDims2D );
                        m_exportSettings.Insert( 1, m_exportDims3D );
                    }
                }
            );

            var dimensionsField2D = new Vector2IntField()
            {
                name = Styles.exportDims2D,
                label = "Dimensions",
                value = new Vector2Int( 512, 512 )
            };
            var dimensionsField3D = new Vector3IntField()
            {
                name = Styles.exportDims3D,
                label = "Dimensions",
                value = new Vector3Int( 64, 64, 64 )
            };

            var m_listOfFormats = new List< GraphicsFormat >()
            {
                // GraphicsFormat.R8_UNorm,
                // GraphicsFormat.R8_SNorm,
                GraphicsFormat.R16_UNorm,
                // GraphicsFormat.R16_SNorm,
                GraphicsFormat.R16_SFloat,
                // GraphicsFormat.R32_SFloat,
            };

            var exportFormat = new PopupField< GraphicsFormat >( m_listOfFormats, GraphicsFormat.R16_UNorm )
            {
                name = Styles.exportFormat,
                label = "Format"
            };
            
            settingsContainer.Add( exportType );
            settingsContainer.Add( dimensionsField2D );
            settingsContainer.Add( exportFormat );

            m_exportSettings = settingsContainer;
            m_exportType = exportType;
            m_exportDims2D = dimensionsField2D;
            m_exportDims3D = dimensionsField3D;
            m_exportFormat = exportFormat;

            exportType.value = ExportTextureType.Texture2D;

            return settingsContainer;
        }

        private void Export2D()
        {
            Texture2D texture = null;

            var textureDims = m_exportDims2D.value;
            var textureFormat = m_exportFormat.value;

            try
            {
                string path = EditorUtility.SaveFilePanel("Export Noise To Texture2D",
                                                       Application.dataPath,
                                                       "New Noise Texture2D.png",
                                                       "png");

                if( string.IsNullOrEmpty( path ) )
                {
                    return;
                }

                if(!path.StartsWith(Application.dataPath))
                {
                    Debug.LogError("You must specificy a path in your project's Assets folder to export a Noise Texture");
                }

                if(!string.IsNullOrEmpty(path) )
                {
                    EditorUtility.DisplayProgressBar("Exporting Noise to Texture2D", "Making some noise...", 0.1f);

                    texture = NoiseUtils.BakeToTexture2D( m_noiseUpdateTarget, textureDims.x, textureDims.y, textureFormat, TextureCreationFlags.None );

                    byte[] bytes = ImageConversion.EncodeToPNG(texture);

                    System.IO.File.WriteAllBytes(path, bytes);

                    Texture2D.DestroyImmediate(texture);
                    texture = null;

                    string assetPath = path.Remove(0, Application.dataPath.Length - "Assets".Length);

                    AssetDatabase.Refresh();

                    EditorUtility.ClearProgressBar();

                    texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                    EditorGUIUtility.PingObject(texture);
                }
            }
            catch(Exception e)
            {
                Debug.LogError( e );

                if(texture != null)
                {
                    Texture2D.DestroyImmediate( texture );
                }

                Debug.Log("Exception caught");

                EditorUtility.ClearProgressBar();
            }
        }

        private void Export3D()
        {
            Texture3D texture = null;

            var textureDims = m_exportDims3D.value;
            var textureFormat = m_exportFormat.value;

            try
            {
                string path = EditorUtility.SaveFilePanel( "Export Noise To Texture3D",
                                                           Application.dataPath,
                                                           "New Noise Texture3D.asset",
                                                           "asset" );

                if( string.IsNullOrEmpty( path ) )
                {
                    return;
                }

                if( !path.StartsWith( Application.dataPath ) )
                {
                    Debug.LogError("You must specificy a path in your project's Assets folder to export a Noise Texture");
                }

                if(!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    EditorUtility.DisplayProgressBar("Exporting Noise to Texture3D", "Making some noise...", 0.1f);

                    texture = NoiseUtils.BakeToTexture3D( m_noiseUpdateTarget, textureDims.x, textureDims.y, textureDims.z, textureFormat, TextureCreationFlags.None );
                    
                    AssetDatabase.CreateAsset(texture, path.Remove(0, Application.dataPath.Length - "Assets".Length));
                    
                    AssetDatabase.Refresh();

                    EditorUtility.ClearProgressBar();

                    EditorGUIUtility.PingObject(texture);
                }
            }
            catch(Exception e)
            {
                Debug.LogError( e );

                if(texture != null)
                {
                    Texture2D.DestroyImmediate( texture );
                }

                EditorUtility.ClearProgressBar();
            }
        }


        private void SaveAsCallback()
        {
            string path = EditorUtility.SaveFilePanel( "Save Noise Settings",
                                                       Application.dataPath,
                                                       "New Noise Settings.asset",
                                                       "asset" );
            // saving to project's asset folder
            if( path.StartsWith( Application.dataPath ) )
            {
                // TODO(wyatt): need to check if this works with different locales/languages. folder might not be
                //              called "Assets" in non-English Editor builds
                string assetPath = path.Substring( Application.dataPath.Length - 6 );
                // settingsProfile = NoiseSettings.CreateAsset(assetPath, noiseSettings);
                var asset = NoiseSettingsFactory.CreateAsset( assetPath );
                asset.CopySerialized( m_noiseUpdateTarget );
            }
            // saving asset somewhere else. why? dunno!
            else if( !string.IsNullOrEmpty( path ) )
            {
                Debug.LogError( "Invalid path specified for creation of new Noise Settings asset. Must be a valid path within the current Unity project's Assets folder/data path." );
            }
        }

        private void ResetRevertCallback()
        {
            Undo.RecordObject( m_noiseUpdateTarget, "NoiseWindow - Reset or Revert Settings" ); 

            if( m_noiseSourceAsset == null )
            {
                m_noiseUpdateTarget.Reset();
            }
            else
            {
                m_noiseUpdateTarget.Copy( m_noiseSourceAsset );
            }
        }

        private void INTERNAL_OnSettingsChanged()
        {
            onSettingsChanged?.Invoke( m_noiseUpdateTarget );
        }

        private void INTERNAL_OnSourceProfileChanged( NoiseSettings sourceProfile )
        {
            if( sourceProfile == null )
            {
                revertButton.text = Styles.reset;
                revertButton.tooltip = Styles.resetTooltip;

                filePanelContainer.Clear();
                filePanelContainer.Add( revertButton );
                filePanelContainer.Add( saveAsButton );

                revertButton.RemoveFromClassList( Styles.flexThird );
                saveAsButton.RemoveFromClassList( Styles.flexThird );
                
                revertButton.AddToClassList( Styles.flexHalf );
                saveAsButton.AddToClassList( Styles.flexHalf );
            }
            else
            {
                revertButton.text = Styles.revert;
                revertButton.tooltip = Styles.revertTooltip;

                filePanelContainer.Clear();
                filePanelContainer.Add( revertButton );
                filePanelContainer.Add( applyButton );
                filePanelContainer.Add( saveAsButton );

                revertButton.RemoveFromClassList( Styles.flexHalf );
                saveAsButton.RemoveFromClassList( Styles.flexHalf );
                
                revertButton.AddToClassList( Styles.flexThird );
                saveAsButton.AddToClassList( Styles.flexThird );
            }
            
            // Undo.RegisterCompleteObjectUndo( this, "NoiseSettings object changed" );

            if( sourceProfile != null && m_noiseSourceAsset != sourceProfile )
            {
                m_noiseUpdateTarget.Copy( sourceProfile );
            }
            else
            {
                // should revert to the NULL asset settings
            }

            objectField.value = sourceProfile;

            INTERNAL_OnSettingsChanged();
            onSourceAssetChanged?.Invoke( sourceProfile );

            m_noiseSourceAsset = sourceProfile;
        }

        private void OnSourceProfileChanged( ChangeEvent< UnityEngine.Object > evt )
        {
            if( evt.newValue == null )
            {
                INTERNAL_OnSourceProfileChanged( null );

                return;
            }

            var settings = ( NoiseSettings )evt.newValue;

            if( settings == m_noiseSourceAsset )
            {
                return;
            }

            INTERNAL_OnSourceProfileChanged( settings );
        }

        public void OnClose()
        {
            m_noiseFieldView?.Close();
        }

        public event Action< NoiseSettings > onSettingsChanged;
        public event Action< NoiseSettings > onSourceAssetChanged;
    }
}