using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// An EditorWindow that enables the editing and previewing of NoiseSettings Assets
    /// </summary>
    public class NoiseWindow : EditorWindow
    {
        /*=========================================================================================
            
            Statics

        ==========================================================================================*/
        
        private static List< NoiseWindow > s_openNoiseWindows = new List<NoiseWindow>();

        /// <summary>
        /// Create a NoiseWindow with no source asset to load from
        /// </summary>
        [ MenuItem( "Window/Terrain/Edit Noise" ) ]
        public static NoiseWindow Create()
        {
            NoiseSettings noise = ScriptableObject.CreateInstance< NoiseSettings >(); 
            return Create( noise );
        }

        /// <summary>
        /// Create a NoiseWindow that applies changes to a provided NoiseAsset and loads from a provided source Asset
        /// </summary>
        public static NoiseWindow Create( NoiseSettings noise, NoiseSettings sourceAsset = null )
        {
            NoiseWindow wnd = null;

            // check to see if a window with the same context exists already
            foreach( var w in s_openNoiseWindows )
            {
                if( w.noiseEditorView != null && w.noiseEditorView.noiseUpdateTarget == noise )
                {
                    wnd = w;

                    break;
                }
            }

            if( null == wnd )
            {
                wnd = ScriptableObject.CreateInstance< NoiseWindow >();
                wnd.titleContent = EditorGUIUtility.TrTextContent( "Noise Editor" );

                var view = new NoiseEditorView( noise, sourceAsset );
                wnd.rootVisualElement.Clear();
                wnd.rootVisualElement.Add( view );
                wnd.noiseEditorView = view;
                
                wnd.m_noiseAsset = noise;

                wnd.minSize = new Vector2( 550, 300 );

                wnd.rootVisualElement.Bind( new SerializedObject( wnd.m_noiseAsset ) );
                wnd.rootVisualElement.viewDataKey = "NoiseWindow";
            }

            wnd.Show();
            wnd.Focus();

            return wnd;
        }

        /*=========================================================================================
            
            NoiseWindow

        ==========================================================================================*/
        
        private NoiseSettings m_noiseAsset;
        
        public NoiseEditorView noiseEditorView
        {
            get; private set;
        }

        void OnEnable()
        {
            s_openNoiseWindows.Add( this );
        }

        void OnDisable()
        {
            s_openNoiseWindows.Remove( this );

            onDisableCallback?.Invoke();

            noiseEditorView?.OnClose();
        }

        public event Action onDisableCallback;
    }
}