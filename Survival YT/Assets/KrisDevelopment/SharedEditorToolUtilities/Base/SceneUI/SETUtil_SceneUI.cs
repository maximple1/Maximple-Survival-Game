////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using U = UnityEngine;
using G = UnityEngine.GUI;
using Gl = UnityEngine.GUILayout;
using SETUtil.Extend;

#if UNITY_EDITOR
using E = UnityEditor;
using EGl = UnityEditor.EditorGUILayout;
#endif


namespace SETUtil.SceneUI
{
	//INTERFACE:
	public interface iGUIElement
	{
		string text { get; set; }
		U.Vector3? position { get; set; }
		U.Rect? rect { get; set; }
		U.Color color { get; set; }
		U.FontStyle fontStyle { get; set; }
		U.GUIStyle guiStyle { get; }

#if UNITY_EDITOR
		void Draw(E.SceneView sceneView);
#endif
	}

	//CLASSES:	
	public abstract class BaseGUIElement
	{
		//public accessors
		public string text { get { return m_text; } set { m_text = value; } }

		public U.Vector3? position { get { return m_position; } set { m_position = value; } }

		public U.Rect? rect { get { return m_rect; } set { m_rect = value; } }

		public U.FontStyle fontStyle { get { return m_fontStyle; } set { m_fontStyle = value; } }

		public U.Color color { get { return m_color; } set { m_color = value; } }

		public virtual U.GUIStyle guiStyle
		{
			get
			{
				U.GUIStyle _style = new U.GUIStyle(U.GUI.skin.label);
				_style.fontStyle = m_fontStyle;
				_style.richText = true;
				return _style;
			}
		}

		//protected:
		protected string m_text = "";
		protected U.Vector3? m_position = null;
		protected U.Rect? m_rect = null;
		protected U.FontStyle m_fontStyle = U.FontStyle.Normal;
		protected U.Color m_color = U.Color.white;

		//CONSTRUCTORS:
		public BaseGUIElement()
		{
			m_text = "";
			m_position = null;
			m_rect = null;
			m_fontStyle = U.FontStyle.Normal;
			m_color = U.Color.white;
		}

		public BaseGUIElement(string text)
		{
			m_text = text;
			m_position = null;
			m_rect = null;
			m_fontStyle = U.FontStyle.Normal;
			m_color = U.Color.white;
		}

		public BaseGUIElement(string text, U.Rect rect, U.Color? color = null)
		{
			m_text = text;
			m_position = null;
			m_rect = rect;
			m_fontStyle = U.FontStyle.Normal;
			m_color = color ?? U.Color.white;
		}

		public BaseGUIElement(string text, U.Rect rect, U.FontStyle fontStyle, U.Color? color = null)
		{
			m_text = text;
			m_position = null;
			m_rect = rect;
			m_fontStyle = fontStyle;
			m_color = color ?? U.Color.white;
		}

		public BaseGUIElement(string text, U.Vector3 position, U.Color? color = null)
		{
			m_text = text;
			m_position = position;
			m_rect = null;
			m_fontStyle = U.FontStyle.Normal;
			m_color = color ?? U.Color.white;
		}

		public BaseGUIElement(string text, U.Vector3 position, U.FontStyle fontStyle, U.Color? color = null)
		{
			m_text = text;
			m_position = position;
			m_rect = null;
			m_fontStyle = fontStyle;
			m_color = color ?? U.Color.white;
		}

		public BaseGUIElement(string text, U.Vector3 position, U.Rect rect, U.FontStyle fontStyle = U.FontStyle.Normal, U.Color? color = null)
		{
			m_text = text;
			m_position = position;
			m_rect = rect;
			m_fontStyle = fontStyle;
			m_color = color ?? U.Color.white;
		}

		//METHODS:
#if UNITY_EDITOR
		public virtual void Draw(E.SceneView sceneView)
		{
			if (m_rect == null)
				m_rect = AutoRect((m_position == null) ? false : true);
		}

		protected U.Rect GetPositionedRect(E.SceneView sceneView)
		{
			U.Vector3 _screenPoint = sceneView.GetScreenPosition(m_position ?? U.Vector3.zero);
			U.Rect _positionedRect = ((U.Rect) m_rect);
			if (m_position != null) {
				_positionedRect.x += _screenPoint.x;
				_positionedRect.y += _screenPoint.y;
			}

			return _positionedRect;
		}
#endif

		public void CenterRect()
		{
			if (m_rect == null)
				return;
			m_rect = new U.Rect(-((U.Rect) m_rect).width / 2, -((U.Rect) m_rect).height / 2, ((U.Rect) m_rect).width, ((U.Rect) m_rect).height);
		}

		protected U.Rect AutoRect(bool center = true)
		{
			//automatically determine the rect size and prepare it for eventual screen positioning
			U.Vector2
				_size2f = guiStyle.CalcSize(new U.GUIContent(m_text)),
				_offset2f = U.Vector2.zero;

			if (center)
				_offset2f -= _size2f / 2;

			return new U.Rect(_offset2f.x, _offset2f.y, _size2f.x, _size2f.y);
		}
	}

	//GUI Elements:
	public class GUILabel : BaseGUIElement, iGUIElement
	{
		//CONSTRUCTORS:
		public GUILabel(string text)
			: base(text)
		{
		}

		public GUILabel(string text, U.Rect rect, U.Color? color = null)
			: base(text, rect, color)
		{
		}

		public GUILabel(string text, U.Rect rect, U.FontStyle fontStyle, U.Color? color = null)
			: base(text, rect, fontStyle, color)
		{
		}

		public GUILabel(string text, U.Vector3 position, U.Color? color = null)
			: base(text, position, color)
		{
		}

		public GUILabel(string text, U.Vector3 position, U.FontStyle fontStyle, U.Color? color = null)
			: base(text, position, fontStyle, color)
		{
		}

		public GUILabel(string text, U.Vector3 position, U.Rect rect, U.FontStyle fontStyle = U.FontStyle.Normal, U.Color? color = null)
			: base(text, position, rect, fontStyle, color)
		{
		}

		//METHODS:
#if UNITY_EDITOR
		public override void Draw(E.SceneView sceneView)
		{
			base.Draw(sceneView);
			EditorUtil.BeginColorPocket(m_color);
			G.Label(GetPositionedRect(sceneView), m_text, guiStyle);
			EditorUtil.EndColorPocket();
		}
#endif
	}

	public class GUIButton : BaseGUIElement, iGUIElement
	{
		public override U.GUIStyle guiStyle
		{
			get
			{
				U.GUIStyle _style = new U.GUIStyle(U.GUI.skin.button);
				_style.fontStyle = m_fontStyle;
				_style.richText = true;
				return _style;
			}
		}

		public System.Action onClick = null;

		//CONSTRUCTORS:
		public GUIButton(string text, System.Action onClick)
			: base(text)
		{
			this.onClick = onClick;
		}

		public GUIButton(string text, U.Rect rect, System.Action onClick, U.Color? color = null)
			: base(text, rect, color)
		{
			this.onClick = onClick;
		}

		public GUIButton(string text, U.Rect rect, System.Action onClick, U.FontStyle fontStyle, U.Color? color = null)
			: base(text, rect, fontStyle, color)
		{
			this.onClick = onClick;
		}

		public GUIButton(string text, U.Vector3 position, System.Action onClick, U.Color? color = null)
			: base(text, position, color)
		{
			this.onClick = onClick;
		}

		public GUIButton(string text, U.Vector3 position, System.Action onClick, U.FontStyle fontStyle, U.Color? color = null)
			: base(text, position, fontStyle, color)
		{
			this.onClick = onClick;
		}

		public GUIButton(string text, U.Vector3 position, U.Rect rect, System.Action onClick, U.FontStyle fontStyle = U.FontStyle.Normal, U.Color? color = null)
			: base(text, position, rect, fontStyle, color)
		{
			this.onClick = onClick;
		}


		//METHODS:
#if UNITY_EDITOR
		public override void Draw(E.SceneView sceneView)
		{
			base.Draw(sceneView);
			EditorUtil.BeginColorPocket(m_color);
			if (G.Button(GetPositionedRect(sceneView), m_text, guiStyle))
				if (onClick != null) {
					onClick();
					onClick = null;
					EditorUtil.UnsubGUIDelegate();
				}

			EditorUtil.EndColorPocket();
		}
#endif
	}

	public class GUIImage : BaseGUIElement, iGUIElement
	{
		public U.Texture2D image;

		//CONSTRUCTORS:
		public GUIImage(U.Texture2D image)
		{
			this.image = image;
		}

		public GUIImage(U.Rect rect, U.Texture2D image, U.Color? color = null)
			: base("", rect, color)
		{
			this.image = image;
		}

		public GUIImage(U.Vector3 position, U.Texture2D image, U.Color? color = null)
			: base("", position, color)
		{
			this.image = image;
		}

		public GUIImage(U.Vector3 position, U.Rect rect, U.Texture2D image, U.Color? color = null)
			: base("", position, rect)
		{
			this.image = image;
			m_color = color ?? U.Color.white;
		}


		//METHODS:
#if UNITY_EDITOR
		public override void Draw(E.SceneView sceneView)
		{
			base.Draw(sceneView);
			EditorUtil.BeginColorPocket(m_color);
			G.DrawTexture(GetPositionedRect(sceneView), image);
			EditorUtil.EndColorPocket();
		}
#endif
	}
}