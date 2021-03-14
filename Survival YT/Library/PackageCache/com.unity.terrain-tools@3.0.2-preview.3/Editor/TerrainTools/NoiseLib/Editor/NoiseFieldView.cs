using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.Experimental.TerrainAPI
{
    internal class NoiseFieldView : IMGUIContainer
    {
        private SerializedObject m_serializedNoise;

        private RenderTexture m_previewRT;
        private Image m_image;

        private bool m_mouseDrag;

        private SerializedProperty m_translation;
        private SerializedProperty m_rotation;
        private SerializedProperty m_scale;

        public NoiseFieldView( SerializedObject serializedNoise )
        {
            m_serializedNoise = serializedNoise;

            m_image = new Image()
            {
                name = "noise-window__preview-container-texture--unity-image",
                style =
                {
                    position = Position.Absolute
                }
            };

            m_image.RegisterCallback< MouseDownEvent >( OnMouseDown );
            m_image.RegisterCallback< MouseMoveEvent >( OnMouseDrag );
            m_image.RegisterCallback< MouseUpEvent >( OnMouseUp );
            m_image.RegisterCallback< WheelEvent >( OnScrollWheel );
            m_image.RegisterCallback< MouseOutEvent >( ( evt ) => { m_mouseDrag = false; } );
            m_image.RegisterCallback< IMGUIEvent >( ( evt ) => { MarkDirtyRepaint(); } );

            RegisterCallback< GeometryChangedEvent >( ResizePreview );

            Add( m_image );

            onGUIHandler += UpdateTexture;

            m_translation = serializedNoise.FindProperty( "transformSettings.translation" );
            m_rotation = serializedNoise.FindProperty( "transformSettings.rotation" );
            m_scale = serializedNoise.FindProperty( "transformSettings.scale" );

            this.Bind( serializedNoise );
        }

        private void OnMouseDown( MouseDownEvent evt )
        {
            if( evt.button != 0 )
            {
                return;
            }

            if( m_mouseDrag )
            {
                evt.StopImmediatePropagation();
                return;
            }

            m_mouseDrag = true;
        }

        private void OnMouseDrag( MouseMoveEvent evt )
        {
            if( !m_mouseDrag || evt.button != 0 )
            {
                return;
            }

            m_serializedNoise.ApplyModifiedProperties();
            m_serializedNoise.Update();
            {
                Rect rect = m_image.worldBound;

                Vector3 t = m_translation.vector3Value;
                Vector3 r = m_rotation.vector3Value;
                Vector3 s = m_scale.vector3Value;

                Vector2 previewDims = new Vector2(rect.width, rect.height);
                Vector2 abs = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z));

                // change noise offset panning icon
                Vector2 sign = new Vector2(-Mathf.Sign(s.x), Mathf.Sign(s.z));
                Vector2 delta = evt.mouseDelta / previewDims * abs * sign;
                Vector3 d3 = new Vector3(delta.x, 0, delta.y);

                d3 = Quaternion.Euler( r ) * d3;

                t += d3;
                
                m_translation.vector3Value = t;

                evt.StopPropagation();
            }
            m_serializedNoise.ApplyModifiedProperties();
            m_serializedNoise.Update();
        }

        private void OnMouseUp( MouseUpEvent evt )
        {
            if( evt.button != 0 )
            {
                return;
            }

            m_mouseDrag = false;
        }

        private void OnScrollWheel( WheelEvent evt )
        {
            if( m_mouseDrag )
            {
                evt.StopImmediatePropagation();
                return;
            }

            m_serializedNoise.ApplyModifiedProperties();
            m_serializedNoise.Update();
            {
                Vector3 s = m_scale.vector3Value;
                Vector2 abs = new Vector2(Mathf.Abs(s.x), Mathf.Abs(s.z));

                abs += Vector2.one * .001f;
                    
                float scroll = evt.delta.y;

                s.x += abs.x * scroll * .05f;
                s.z += abs.y * scroll * .05f;
                
                m_scale.vector3Value = s;
            }
            m_serializedNoise.ApplyModifiedProperties();
            m_serializedNoise.Update();
            
            evt.StopPropagation();
        }

        private void UpdateTexture()
        {
            // create preview RT here and keep until the next Repaint
            if( m_previewRT != null )
            {
                RenderTexture.ReleaseTemporary( m_previewRT );
            }

            NoiseSettings noiseSettings = m_serializedNoise.targetObject as NoiseSettings;

            m_previewRT = RenderTexture.GetTemporary(512, 512, 0, NoiseUtils.previewFormat);
            RenderTexture tempRT = RenderTexture.GetTemporary(512, 512, 0, NoiseUtils.singleChannelFormat);

            RenderTexture prevActive = RenderTexture.active;
            
            NoiseUtils.Blit2D(noiseSettings, tempRT);

            NoiseUtils.BlitPreview2D(tempRT, m_previewRT);

            RenderTexture.active = prevActive;

            RenderTexture.ReleaseTemporary(tempRT);

            m_image.image = m_previewRT;
        }

        private void ResizePreview( GeometryChangedEvent evt )
        {
            Rect newRect = evt.newRect;

            if( newRect.width < newRect.height )
            {
                newRect.height = newRect.width;
            }
            else
            {
                newRect.width = newRect.height;
            }

            m_image.style.width = newRect.width;
            m_image.style.height = newRect.height;

            m_image.transform.position = new Vector3( localBound.width / 2 - newRect.width / 2, localBound.height / 2 - newRect.height / 2, 0 );

            if(m_previewRT != null)
            {
                RenderTexture.ReleaseTemporary(m_previewRT);
            }
        }

        public void Close()
        {
            if(m_previewRT != null)
            {
                RenderTexture.ReleaseTemporary(m_previewRT);
            }
        }
    }
}
