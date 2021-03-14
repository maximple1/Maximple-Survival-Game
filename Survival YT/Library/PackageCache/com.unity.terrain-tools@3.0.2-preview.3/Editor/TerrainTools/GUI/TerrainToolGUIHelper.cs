using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Experimental.TerrainAPI;
using Erosion;

public interface IFloatMinMaxSlider {
    float value { get; set; }
    float minValue { get; set; }
    float maxValue { get; set; }

    void DrawInspectorGUI();
}

[Serializable]
public class TerrainFloatMinMaxValue : IFloatMinMaxSlider {
    [SerializeField]
    private bool m_Expanded = false;
    [SerializeField] 
    private float m_Value = 0.0f;
    [SerializeField]
    private float m_MinValue = 0.0f;
    [SerializeField]
    private float m_MaxValue = 1.0f;
    [SerializeField]
    private bool m_shouldClampMin = false;
    [SerializeField]
    private bool m_shouldClampMax = false;
    [SerializeField]
    private float m_MinClampValue = 0.0f;
    [SerializeField]
    private float m_MaxClampValue = 1.0f;
    
    [SerializeField]
    private float m_MouseSensitivity = 1.0f;
    [SerializeField]
    private bool m_WrapValue = false;

    private bool m_EditRange = true;
    private readonly bool m_EditSensitivity = true;

    private readonly GUIContent m_Label;

    public float value
    {
        get => m_Value;
        set
        {
            if(m_WrapValue)
            {
                float difference = m_MaxValue - m_MinValue;

                while(value < m_MinValue)
                {
                    value += difference;
                }

                while(value > m_MaxValue)
                {
                    value -= difference;
                }

                m_Value = value;
            }
            else
            {
                m_Value = Mathf.Clamp(value, m_MinValue, m_MaxValue);
            }
        }
    }

    public float minValue
    {
        get => m_MinValue;
        set
        {
            if (shouldClampMin && value < m_MinClampValue)
            {
                m_MinValue = m_MinClampValue;
            }
            else
            {
                m_MinValue = value;
            }
            if (m_Value < m_MinValue)
            {
                m_Value = m_MinValue;
            }
            if (m_MinValue > m_MaxValue)
            {
                m_MaxValue = m_MinValue;
            }
        }
    }

    public float maxValue
    {
        get => m_MaxValue;
        set
        {
            if (shouldClampMax && value > m_MaxClampValue)
            {
                m_MaxValue = m_MaxClampValue;
            }
            else
            {
                m_MaxValue = value;
            }
            if (m_Value > m_MaxValue)
            {
                m_Value = m_MaxValue;
            }
            if (m_MinValue > m_MaxValue)
            {
                m_MinValue = m_MaxValue;
            }
        }
    }
    public bool shouldClampMin
    {
        get => m_shouldClampMin;
        set
        {
            m_shouldClampMin = value;
            if (m_shouldClampMin)
            {
                minClamp = m_MinClampValue;
            }
        }
    }
    public float minClamp
    {
        get => m_MinClampValue;
        set
        {
            // validate that clamp value is possible
            if (shouldClampMin && shouldClampMax && value > maxClamp)
            {
                throw new ArgumentOutOfRangeException("minClamp", "minimum clamp value must be less than maximum clamp");
            }
            m_MinClampValue = value;
            if (shouldClampMin && m_MinClampValue > minValue)
            {
                minValue = m_MinClampValue;
            }
        }
    }

    public bool shouldClampMax
    {
        get => m_shouldClampMax;
        set
        {
            m_shouldClampMax = value;
            if (m_shouldClampMax)
            {
                maxClamp = m_MaxClampValue;
            }
        }
    }
    public float maxClamp
    {
        get => m_MaxClampValue;
        set
        {
            // validate that clamp value is possible
            if (shouldClampMax && shouldClampMin && value < minClamp)
            {
                throw new ArgumentOutOfRangeException("maxClamp", "maximum clamp value must be greater than minimum clamp");
            }
            m_MaxClampValue = value;
            if (shouldClampMax && m_MaxClampValue < maxValue)
            {
                maxValue = m_MaxClampValue;
            }
        }
    }
    
    public float mouseSensitivity {
        get => m_MouseSensitivity;
        set => m_MouseSensitivity = value;
    }

    public bool wrapValue
    {
        get => m_WrapValue;
        set => m_WrapValue = value;
    }

    public TerrainFloatMinMaxValue(GUIContent label, float value, float minValue, float maxValue, bool editRange = true) {
        m_Expanded = false;
        m_Value = value;
        m_MinValue = minValue;
        m_MaxValue = maxValue;
        m_EditRange = editRange;
        m_EditSensitivity = false;
        m_Label = label;
    }

    public TerrainFloatMinMaxValue(GUIContent label, float value, float minValue, float maxValue, bool editRange, float mouseSensitivity) : this(label, value, minValue, maxValue, editRange) {
        m_MouseSensitivity = mouseSensitivity;
        m_EditSensitivity = true;
    }

    public void DrawInspectorGUI() {
        float fieldWidth = EditorGUIUtility.fieldWidth;
        float indentOffset = EditorGUI.indentLevel * 15f;
        Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

		Rect foldoutRect = new Rect(labelRect.xMax - 15, labelRect.y, 15, totalRect.height);
        Rect sliderRect = new Rect(foldoutRect.xMax, foldoutRect.y, totalRect.width - labelRect.width, totalRect.height);
        int rectHeight = 1;
        
        EditorGUI.PrefixLabel(labelRect, m_Label);
        m_Value = EditorGUI.Slider(sliderRect, m_Value, minValue, maxValue);
        if (m_EditRange)
        {
			m_Expanded = GUI.Toggle(foldoutRect, m_Expanded, GUIContent.none, EditorStyles.foldout);
			if (m_Expanded)
            {
                if (m_EditRange)
                {
                    totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    Rect rangeLabelRect = new Rect(sliderRect.x, sliderRect.yMax, sliderRect.width / 2, totalRect.height);
                    Rect minRect = new Rect(totalRect.xMax - 2 * fieldWidth + indentOffset, totalRect.y, fieldWidth, totalRect.height);
                    Rect maxRect = new Rect(totalRect.xMax - fieldWidth, totalRect.y, fieldWidth, totalRect.height);

                    EditorGUI.PrefixLabel(rangeLabelRect, new GUIContent("Range:"));
                    minValue = EditorGUI.FloatField(minRect, minValue);
                    maxValue = EditorGUI.FloatField(maxRect, maxValue);
                }

                if (m_EditSensitivity)
                {
                    totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    Rect sensitivityLabelRect = new Rect(sliderRect.x, totalRect.y, sliderRect.width / 2, totalRect.height);
                    Rect sensitivityValueRect = new Rect(totalRect.xMax - fieldWidth, totalRect.y, fieldWidth, totalRect.height);

                    EditorGUI.PrefixLabel(sensitivityLabelRect, new GUIContent("Mouse Sensitivity:"));
                    m_MouseSensitivity = EditorGUI.FloatField(sensitivityValueRect, m_MouseSensitivity);
                }
            }
        }
        GUILayoutUtility.GetRect(1, rectHeight);
    }
}

public interface IIntMinMaxSlider {
    int value { get; set; }
    int minValue { get; set; }
    int maxValue { get; set; }

    void DrawInspectorGUI();
}

[Serializable]
public class TerrainIntMinMaxValue : IIntMinMaxSlider {
    [SerializeField]
    private bool m_Expanded = false;
    [SerializeField]
    private int m_Value = 0;
    [SerializeField]
    private int m_MinValue = 0;
    [SerializeField]
    private int m_MaxValue = 10;
    
    private GUIContent m_Label;

    public int value {
        get => m_Value;
        set => m_Value = Mathf.Clamp(value, m_MinValue, m_MaxValue);
    }
    public int minValue {
        get => m_MinValue;
        set => m_MinValue = value;
    }
    public int maxValue {
        get => m_MaxValue;
        set => m_MaxValue = value;
    }
    public GUIContent label {
        get => m_Label;
        set => m_Label = value;
    }

    public TerrainIntMinMaxValue(GUIContent label, int value, int minValue, int maxValue) {
        m_Expanded = false;
        m_Value = value;
        m_MinValue = minValue;
        m_MaxValue = maxValue;
        m_Label = label;
    }

    public void DrawInspectorGUI() {
        float fieldWidth = EditorGUIUtility.fieldWidth;
        float indentOffset = EditorGUI.indentLevel * 15f;
        Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

        Rect foldoutRect = new Rect(labelRect.xMax - 8, labelRect.y, 8, totalRect.height);
        Rect sliderRect = new Rect(foldoutRect.xMax, labelRect.y, totalRect.width - labelRect.width, totalRect.height);

        EditorGUI.PrefixLabel(labelRect, m_Label);
        m_Value = EditorGUI.IntSlider(sliderRect, m_Value, minValue, maxValue);

		m_Expanded = GUI.Toggle(foldoutRect, m_Expanded, GUIContent.none, EditorStyles.foldout);
		if (m_Expanded) {
            totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
            Rect rangeLabelRect = new Rect(sliderRect.x, sliderRect.yMax, sliderRect.width / 2, totalRect.height);
            Rect minRect = new Rect(totalRect.xMax - 2 * fieldWidth + indentOffset, totalRect.y, fieldWidth, totalRect.height);
            Rect maxRect = new Rect(totalRect.xMax - fieldWidth, totalRect.y, fieldWidth, totalRect.height);

            EditorGUI.PrefixLabel(rangeLabelRect, new GUIContent("Range:"));
            m_MinValue = EditorGUI.IntField(minRect, m_MinValue);
            m_MaxValue = EditorGUI.IntField(maxRect, m_MaxValue);
        }
        GUILayoutUtility.GetRect(1, 2);
    }
}

public static class TerrainToolGUIHelper
{
    public static GUILayoutOption dontExpandWidth = GUILayout.ExpandWidth(false);

    public static GUIStyle toolbarNormalStyle = null;
    public static GUIStyle toolbarActiveStyle = null;
    public static GUIStyle leftToolbarStyle = null;
    public static GUIStyle midToolbarStyle = null;
    public static GUIStyle midToolbarActiveStyle = null;
    public static GUIStyle rightToolbarStyle = null;

    static TerrainToolGUIHelper() {
        toolbarNormalStyle = new GUIStyle("ToolbarButton");
        toolbarActiveStyle = new GUIStyle("ToolbarButton");
        toolbarActiveStyle.normal.background = toolbarNormalStyle.hover.background;
        leftToolbarStyle = new GUIStyle("CommandLeft");
        midToolbarStyle = new GUIStyle("CommandMid");
        midToolbarActiveStyle = new GUIStyle("CommandMid");
        midToolbarActiveStyle.normal.background = midToolbarStyle.active.background;
        rightToolbarStyle = new GUIStyle("CommandRight");
    }

    public static GUIStyle GetToolbarToggleStyle(bool isToggled)
    {
        return isToggled ? toolbarActiveStyle : toolbarNormalStyle;
    }

    public static bool DrawToggleHeaderFoldout(GUIContent title, bool state, ref bool enabled)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMin += 32f;
        labelRect.xMax -= 20f;

        var foldoutRect = backgroundRect;
        foldoutRect.xMin += 13f;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;

        var toggleRect = foldoutRect;
        toggleRect.x = foldoutRect.xMax + 4f;

		// Background rect should be full-width
		backgroundRect.xMin = 16f * EditorGUI.indentLevel;
		backgroundRect.xMin = 0;

		backgroundRect.width += 4f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Active checkbox
        state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

        // Enabled toggle
        enabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, EditorStyles.toggle);

        var e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (toggleRect.Contains(e.mousePosition))
            {
                enabled = !enabled;
                e.Use();
            }
            else if (backgroundRect.Contains(e.mousePosition))
            {
                state = !state;
                e.Use();
            }
        }

        return state;
    }

	public static bool DrawToggleHeaderFoldout(GUIContent title, bool state, ref bool enabled, float padding)
	{
		var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

		var labelRect = backgroundRect;
		labelRect.xMin += 32f;
		labelRect.xMax -= 20f;

		var foldoutRect = backgroundRect;
		foldoutRect.xMin += padding;
		foldoutRect.y += 1f;
		foldoutRect.width = 13f;
		foldoutRect.height = 13f;

		var toggleRect = foldoutRect;
		toggleRect.x = foldoutRect.xMax + 4f;

		// Background rect should be full-width
		backgroundRect.xMin = padding;
		backgroundRect.xMin = 0;

		backgroundRect.width += 4f;

		// Background
		float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
		EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

		// Title
		EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

		// Active checkbox
		state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

		// Enabled toggle
		enabled = GUI.Toggle(toggleRect, enabled, GUIContent.none, EditorStyles.toggle);

		var e = Event.current;

		if (e.type == EventType.MouseDown && e.button == 0)
		{
			if (toggleRect.Contains(e.mousePosition))
			{
				enabled = !enabled;
				e.Use();
			}
			else if (backgroundRect.Contains(e.mousePosition))
			{
				state = !state;
				e.Use();
			}
		}

		return state;
	}

    public static bool DrawHeaderFoldout(GUIContent title, bool state)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMin += 16f;
        labelRect.xMax -= 20f;

        var foldoutRect = backgroundRect;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;


        // Background rect should be full-width
        backgroundRect.xMin = 0;
        backgroundRect.width += 4f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Active checkbox
        state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

        var e = Event.current; 

        if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
        {
            state = !state;
            e.Use();
        }

        return state;
    }

	public static bool DrawSimpleFoldout(GUIContent label, bool state, int indentLevel = 0, float width = 10f)
	{
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(indentLevel * 15);
		state = GUILayout.Toggle(state, GUIContent.none, EditorStyles.foldout, GUILayout.Width(width));
		GUILayout.Label(label);
		EditorGUILayout.EndHorizontal();

		return state;
	}

    public static bool DrawHeaderFoldoutForErosion(GUIContent title, bool state, ResetTool resetMethod)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMin += 16f;
        labelRect.xMax -= 20f;

        var foldoutRect = backgroundRect;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;


        // Background rect should be full-width
        backgroundRect.xMin = 0;
        backgroundRect.width += 4f;

        var gearIconRect = new Rect();
        gearIconRect.y = backgroundRect.y;
        gearIconRect.x = backgroundRect.width - 30f;
        gearIconRect.width = 16f;
        gearIconRect.height = 16f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Active checkbox
        state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

        bool reset = false;
        //icon
        reset = GUI.Toggle(gearIconRect, reset, EditorGUIUtility.IconContent("_Popup"), EditorStyles.label);

        var e = Event.current;

        if (reset)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset"), false, () => { resetMethod(); });
            menu.ShowAsContext();
            e.Use();
        }
        else if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
        {
            state = !state;
            e.Use();
        }

        if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 1)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset"), false, () => { resetMethod(); });
            menu.ShowAsContext();
            e.Use();
        }

        return state;
    }

    public static bool DrawHeaderFoldoutForBrush(GUIContent title, bool state, ResetBrush resetMethod)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 17f);

        var labelRect = backgroundRect;
        labelRect.xMin += 16f;
        labelRect.xMax -= 20f;

        var foldoutRect = backgroundRect;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;


        // Background rect should be full-width
        backgroundRect.xMin = 0;
        backgroundRect.width += 4f;

        var gearIconRect = new Rect();
        gearIconRect.y = backgroundRect.y;
        gearIconRect.x = backgroundRect.width - 30f;
        gearIconRect.width = 16f;
        gearIconRect.height = 16f;

        // Background
        float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
        EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

        // Title
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        // Active checkbox
        state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

        bool reset = false;
        //icon
        reset = GUI.Toggle(gearIconRect, reset, EditorGUIUtility.IconContent("_Popup"), EditorStyles.label);

        var e = Event.current;

        if (reset)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset"), false, () => { resetMethod(); });
            menu.ShowAsContext();
            e.Use();
        }
        else if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
        {
            state = !state;
            e.Use();
        }

        if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 1)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Reset"), false, () => { resetMethod(); });
            menu.ShowAsContext();
            e.Use();
        }

        return state;
    }

    public static bool DrawToggleFoldout(SerializedProperty prop, GUIContent title, Action func, bool toggled)
    {
        bool state = prop.isExpanded;
        state = DrawToggleHeaderFoldout(title, state, ref toggled);

        if (state)
        {
            EditorGUI.indentLevel++;
            if (func != null)
            {
                func();
            }
            --EditorGUI.indentLevel;
        }

        prop.isExpanded = state;

        return toggled;
    }

    public static bool DrawFoldout(bool expanded, GUIContent title, Action func)
    {
        bool state = expanded;
        state = DrawHeaderFoldout(title, state);

        if (state)
        {
            EditorGUI.indentLevel++;
            if (func != null)
            {
                func();
            }
            EditorGUI.indentLevel--;
        }

        return state;
    }

    public static void DrawFoldout(SerializedProperty prop, GUIContent title, Action func)
    {
        prop.isExpanded = DrawFoldout(prop.isExpanded, title, func);
    }

    private static Rect GetToolbarRect(GUIContent[] toolbarContent, params GUILayoutOption[] options)
    {
        Debug.Assert(toolbarContent.Length > 0);

        Rect maxRect = EditorGUILayout.GetControlRect(false, 0f);
        Rect totalRect = new Rect(maxRect.xMin, maxRect.yMin, 0f, 0f);
        Vector2 buttonPos = new Vector2(maxRect.xMin, maxRect.yMin);
        GUIStyle skin = GetToolbarToggleStyle(false);
        //bool newLine = true;
        int linecount = 1;

        Vector2 buttonSize = skin.CalcSize(toolbarContent[0]);


        for (int i = 0; i < toolbarContent.Length; ++i) {
            buttonSize = skin.CalcSize(toolbarContent[i]);

            if (buttonPos.x + buttonSize.x > maxRect.xMax) {
                buttonPos.x = maxRect.xMin;
                buttonPos.y += buttonSize.y;
                linecount++;
            } else {
                totalRect.xMax = Mathf.Max(buttonPos.x + buttonSize.x, totalRect.xMax);
            }

            buttonPos.x += buttonSize.x;
        }

        totalRect.height = buttonSize.y * linecount;

        return totalRect;
    }

    public static int HorizontalFlagToolbar(GUIContent[] toolbarContent, int[] enumValues, int selection, params GUILayoutOption[] options)
    {
        Rect toolbarRect = GetToolbarRect(toolbarContent, options);

        // GUI.Box(totalRect, GUIContent.none);

        GUILayoutUtility.GetRect(toolbarRect.width, toolbarRect.height / 4);
        // GUI.Box(totalRect, GUIContent.none);
        // Rect maxRect = EditorGUILayout.GetControlRect(false, totalRect.height);

        Vector2 buttonPos = new Vector2(toolbarRect.xMin, toolbarRect.yMin);

        for (int i = 0; i < toolbarContent.Length; ++i) {
            int enumVal = enumValues[i];
            bool wasActive = (selection & enumVal) == enumVal && enumVal != 0;
            GUIStyle skin = GetToolbarToggleStyle(wasActive);
            Vector2 buttonSize = skin.CalcSize(toolbarContent[i]);

            if (buttonPos.x + buttonSize.x > toolbarRect.xMax) {
                buttonPos.x = toolbarRect.xMin;
                buttonPos.y += buttonSize.y;
            }

            Rect buttonRect = new Rect(buttonPos.x, buttonPos.y, buttonSize.x, buttonSize.y);

            if (GUI.Button(buttonRect, toolbarContent[i], skin)) {
                if (enumVal == 0) {
                    selection = enumVal;
                } else if (enumVal == ~0) {
                    selection = wasActive ? ~enumVal : enumVal;
                } else {
                    selection = wasActive ? (selection & ~enumVal) : (selection | enumVal);
                }
            }

            buttonPos.x += buttonSize.x;
        }

        return selection;
    }

    // assumes that an enum value of 0 = None and ~0 = Everything
    private static int OLDHorizontalFlagToolbar(GUIContent[] toolbarContent, int[] enumValues, int selection, params GUILayoutOption[] options)
    {
        // TODO(wyatt): Change to use EditorGUIUtility.GetFlowLayoutedRects instead of Begin/EndHorizontal
        Rect widthRect = GUILayoutUtility.GetRect(Screen.width, 17f);
        // GetToolbarRect(true, toolbarContent, options);
        // GUI.Box(widthRect, GUIContent.none);
        Vector2 currPos = widthRect.position;
        Rect totalRect = widthRect;
        bool newLine = true;
        //int skinID = 0; // left = 0, 1 = mid, 2 = right

        for (int i = 0; i < toolbarContent.Length; ++i) {
            int enumVal = enumValues[i];
            bool wasActive = (selection & enumVal) == enumVal && enumVal != 0;
            GUIStyle skin = GetToolbarToggleStyle(wasActive);
            Vector2 size = skin.CalcSize(toolbarContent[i]);
            Rect buttonRect = new Rect(currPos.x, currPos.y, size.x, size.y);

            currPos.x += size.x;

            totalRect.yMax = Mathf.Max(currPos.y + size.y, totalRect.yMax);

            if (currPos.x + size.x > widthRect.xMax) {
                currPos.x = widthRect.xMin;
                currPos.y += size.y;
                newLine = true;
            }

            if (newLine) {
                // reserve a rect for the line
                Rect reservedRect = GUILayoutUtility.GetRect(widthRect.width, size.y);
                // GUI.Box(reservedRect, GUIContent.none);
                newLine = false;
            }

            if (GUI.Button(buttonRect, toolbarContent[i], skin)) {
                if (enumVal == 0) {
                    selection = enumVal;
                } else if (enumVal == ~0) {
                    selection = wasActive ? ~enumVal : enumVal;
                } else {
                    selection = wasActive ? (selection & ~enumVal) : (selection | enumVal);
                }
            }
        }

        return selection;
    }

    public static int MinMaxSliderInt(GUIContent label, int value, ref int minValue, ref int maxValue) {
        float fieldWidth = EditorGUIUtility.fieldWidth;
        float indentOffset = EditorGUI.indentLevel * 15f;
        Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

        Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, totalRect.width - labelRect.width - 2 * fieldWidth - 4, totalRect.height);

        Rect minLabelRect = new Rect(sliderRect.xMax + 4 - indentOffset, labelRect.y, fieldWidth, totalRect.height);
        Rect minRect = new Rect(minLabelRect.xMax, labelRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

        Rect maxRect = new Rect(minRect.xMax - indentOffset, sliderRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

        EditorGUI.PrefixLabel(labelRect, label);
        value = EditorGUI.IntSlider(sliderRect, value, minValue, maxValue);
        EditorGUI.PrefixLabel(minLabelRect, new GUIContent("Range:"));
        minValue = EditorGUI.IntField(minRect, minValue);
        maxValue = EditorGUI.IntField(maxRect, maxValue);

        return value;
    }

    public static float MinMaxSlider(GUIContent label, float value, ref float minValue, ref float maxValue) {
        float fieldWidth = EditorGUIUtility.fieldWidth;
        float indentOffset = EditorGUI.indentLevel * 15f;
        Rect totalRect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
        Rect labelRect = new Rect(totalRect.x, totalRect.y, EditorGUIUtility.labelWidth - indentOffset, totalRect.height);

        Rect sliderRect = new Rect(labelRect.xMax, labelRect.y, totalRect.width - labelRect.width - 2 * fieldWidth - 4, totalRect.height);

        Rect minLabelRect = new Rect(sliderRect.xMax + 4 - indentOffset, labelRect.y, fieldWidth, totalRect.height);
        Rect minRect = new Rect(minLabelRect.xMax, labelRect.y, fieldWidth / 2 + indentOffset, totalRect.height);
        
        Rect maxRect = new Rect(minRect.xMax - indentOffset, sliderRect.y, fieldWidth / 2 + indentOffset, totalRect.height);

        EditorGUI.PrefixLabel(labelRect, label);
        value = EditorGUI.Slider(sliderRect, value, minValue, maxValue);
        EditorGUI.PrefixLabel(minLabelRect, new GUIContent("Range:"));
        minValue = EditorGUI.FloatField(minRect, minValue);
        maxValue = EditorGUI.FloatField(maxRect, maxValue);

        return value;
    }
}