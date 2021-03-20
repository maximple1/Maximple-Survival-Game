////////////////////////////////////////
//    Shared Editor Tool Utilities    //
//    by Kris Development             //
////////////////////////////////////////

//License: MIT
//GitLab: https://gitlab.com/KrisDevelopment/SETUtil

using U = UnityEngine;
using G = UnityEngine.GUI;
using Gl = UnityEngine.GUILayout;
using SETUtil.Common.Types;

#if UNITY_EDITOR
using E = UnityEditor;
using EGl = UnityEditor.EditorGUILayout;
#endif

namespace SETUtil.UI
{
	public class NavigationFieldData
	{
		public int border = 0;
		public U.Vector2 scrollView = new U.Vector2(5, 5);
		public U.Rect backupRect = new U.Rect(0, 0, 0, 0);
		public U.Color gridColor = new U.Color(.2f, .2f, .2f, 1f);
		public U.Color backgroundColor = new U.Color(.1f, .1f, .1f, 1f);

		private bool
			m_dragEventStarted = false,
			delayedDragEventInfo = false; //delies info for dragEvent with one frame so it can be captured by other GUI

		private int updateCount = 0;
		private U.Vector2? initialMousePos = null;

		public bool dragEventStarted
		{
			get
			{
				if (m_dragEventStarted)
					return true;
				else
					return delayedDragEventInfo;
			}
		}

		public void DrawBackground()
		{
#if UNITY_EDITOR
			U.Rect rect = new U.Rect(scrollView.x, scrollView.y, backupRect.width, backupRect.height);
			const float SEGMENT_SIZE = 13;
			int
				wideLineIndex = 10,
				loopsHorizontal = (int) U.Mathf.Ceil(rect.width / SEGMENT_SIZE) + 1,
				loopsVertical = (int) U.Mathf.Ceil(rect.height / SEGMENT_SIZE) + 1;
			float addedWidth = 0;

			E.Handles.BeginGUI();
			for (int i = 0; i < loopsVertical; i++) {
				if (i % wideLineIndex == 0)
					addedWidth = 2;
				else addedWidth = 0;
				float loopHeight = loopsVertical * SEGMENT_SIZE;

				U.Vector2 p1 = new U.Vector2(0, rect.y + SEGMENT_SIZE * i);
				p1.y = p1.y % (loopHeight);
				if (p1.y < 0)
					p1.y = loopHeight + p1.y;
				U.Vector2 p2 = new U.Vector2(rect.width, rect.y + SEGMENT_SIZE * i);
				p2.y = p2.y % (loopHeight);
				if (p2.y < 0)
					p2.y = loopHeight + p2.y;

				E.Handles.DrawBezier(p1, p2, p1, p2, gridColor, null, 3 + addedWidth);
			}

			for (int i = 0; i < loopsHorizontal; i++) {
				if (i % wideLineIndex == 0)
					addedWidth = 2;
				else addedWidth = 0;
				float loopWidth = loopsHorizontal * SEGMENT_SIZE;

				U.Vector2 p1 = new U.Vector2(rect.x + SEGMENT_SIZE * i, 0);
				p1.x = p1.x % (loopWidth);
				if (p1.x < 0)
					p1.x = loopWidth + p1.x;

				U.Vector2 p2 = new U.Vector2(rect.x + SEGMENT_SIZE * i, rect.height);
				p2.x = p2.x % (loopWidth);
				if (p2.x < 0)
					p2.x = loopWidth + p2.x;

				E.Handles.DrawBezier(p1, p2, p1, p2, gridColor, null, 3 + addedWidth);
			}

			E.Handles.EndGUI();
#endif
		}

		public bool DragUpdate()
		{
			return DragUpdate(new U.Rect(0, 0, backupRect.width, backupRect.height));
		}

		public bool DragUpdate(U.Rect rect)
		{
			U.Event _current = U.Event.current;

			if (_current.isScrollWheel) {
				const int _speed = 12;
				if (_current.shift)
					scrollView.x -= _current.delta.y * _speed;
				else
					scrollView.y -= _current.delta.y * _speed;
			}

			if (_current.type == U.EventType.MouseDown) {
				if (_current.button == (int) MouseButton.Middle) {
					//on initial press
					initialMousePos = _current.mousePosition;
					_current.Use();
				}
			}

			if (_current.rawType == U.EventType.MouseUp) {
				//on release
				initialMousePos = null;
			}

			if (initialMousePos != null) {
				if (_current.type == U.EventType.MouseDrag) {
					//on drag
					scrollView -= (U.Vector2) initialMousePos - _current.mousePosition;
					initialMousePos = _current.mousePosition;
					m_dragEventStarted = true;
					_current.Use();
				}
			} else
				m_dragEventStarted = false;

			//delayed drag event info
			if (m_dragEventStarted != delayedDragEventInfo) {
				if (updateCount > 0) {
					delayedDragEventInfo = m_dragEventStarted;
					updateCount = 0;
				} else
					updateCount++;
			}

			return m_dragEventStarted; //return if the mouse is currently being dragged
		}

		public void CenterView()
		{
			scrollView = new U.Vector2(5, 5);
		}
	}
}