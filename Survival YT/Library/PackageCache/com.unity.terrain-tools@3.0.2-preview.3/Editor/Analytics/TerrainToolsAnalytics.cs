using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Analytics class for collecting and sending aggregated user data
    /// </summary>
    public static class TerrainToolsAnalytics
    {
        //Event Data
        static bool s_EventRegistered = false;
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.terraintools";
        const string k_EventName = "analytics.uTerrainTools";

        //Brush Analytics Data
        const float k_SignficantThreshold = .01f;

        static BrushAnalyticsData m_Data;
        static List<BrushParameterData> s_ModifiedBrushParameters = new List<BrushParameterData>();
        static List<string> s_UsedBrushShortcut = new List<string>();
        static float s_PaintingDuration;
        static bool s_ParameterChanged;

        [Serializable]
        struct BrushParameterData
        {
            public string name;
            public string value;
        }

        struct BrushAnalyticsData
        {
            public string brush_name;
            public List<string> shortcuts;
            public string[] mask_filters;
            public float strength;
            public float size;
            public float rotation;
            public float spacing;
            public float scatter;
            public float duration;
            public List<BrushParameterData> brush_parameters;
        }

        /// <summary>
        /// Array of BrushParameters used to compare against the most recent brush parameters to check if there
        /// has been a change.
        /// </summary>
        internal static IBrushParameter[] m_OriginalParameters;

        /// <summary>
        /// Interface for iterating over brush parameters of an ambiguous type
        /// </summary>
        public interface IBrushParameter
        {
            System.Type ParameterType();
            string Name { get; set; }
        }

        /// <summary>
        /// Struct containing the name and value associated to an individual brush parameter.
        /// </summary>
        /// <typeparam name="T">The variable type of the brush parameter</typeparam>
        internal struct BrushParameter<T> : IBrushParameter
        {
            public System.Type ParameterType()
            {
                return Value.GetType();
            }
            public T Value { get; set; }
            public string Name { get; set; }
        }

        /// <summary>
        /// Register the AnalyticsEvent for sending data to BigQuery
        /// </summary>
        /// <returns>EventRegisterd boolean identifying if the event was registered correctly</returns>
        static bool EnableAnalytics()
        {
            //Early out if the event has already been registered, returning bool determining 
            //if Editor Analytics are enabled
            if (s_EventRegistered)
                return EditorAnalytics.enabled;

            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour,
                k_MaxNumberOfElements, k_VendorKey);

            if (result == AnalyticsResult.Ok)
                s_EventRegistered = true;

            return s_EventRegistered && EditorAnalytics.enabled;
        }

        /// <summary>
        /// Update the analytics data to be sent when a user starts painting with new parameters/settings
        /// The users time is being tracked while painting
        /// Once the user changes any brush parameters original data is sent and the new data is cached to be compared later
        /// </summary>
        /// <param name="baseBrushSettings">Brush Base class containing common brush parameters</param>
        /// <param name="brushParamFunc">Function returning brush specific parameters</param>
        internal static void UpdateAnalytics(BaseBrushUIGroup baseBrushSettings, Func<TerrainToolsAnalytics.IBrushParameter[]> brushParamFunc)
        {
            if (!EnableAnalytics())
                return;

            s_PaintingDuration += Time.deltaTime;

            if (!s_ParameterChanged)
                return;

            SendAnalytics();
            CompareBrushSettings(brushParamFunc?.Invoke());
            CacheAnalyticsData(baseBrushSettings);
        }

        /// <summary>
        /// Send the EditorAnalytics event and clear data for reuse
        /// </summary>
        static void SendAnalytics()
        {
            if(m_Data.Equals(default(BrushAnalyticsData))) 
                return;

            m_Data.duration = s_PaintingDuration;
            EditorAnalytics.SendEventWithLimit(k_EventName, m_Data);

            //Clear data
            s_ModifiedBrushParameters.Clear();
            s_UsedBrushShortcut.Clear();
            s_ParameterChanged = false;
            s_PaintingDuration = 0;
        }

        /// <summary>
        /// Checks whether the brush parameters have changed between
        /// the original and current state of a brush.
        /// </summary>
        /// <param name="parameters">Array of brushparameter structs which identify the name and value of the brushes
        /// parameters. </param>
        static void CompareBrushSettings(IBrushParameter[] parameters)
        {
            if (parameters == null)
                return;

            for (int i = 0; (i < parameters.Length && i < m_OriginalParameters.Length); i++)
            {
                if (parameters[i].Equals(m_OriginalParameters[i]))
                    continue;

                System.Type type = parameters[i].ParameterType();
                TypeCode typecode = Type.GetTypeCode(type);
                switch (typecode)
                {
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                        {
                            int currentValue = ((BrushParameter<int>)parameters[i]).Value;
                            int originalValue = ((BrushParameter<int>)m_OriginalParameters[i]).Value;
                            CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            break;
                        }

                    case TypeCode.Single:
                        {
                            float currentValue = ((BrushParameter<float>)parameters[i]).Value;
                            float originalValue = ((BrushParameter<float>)m_OriginalParameters[i]).Value;

                            //Check if the user made a significant enough change to the parameter
                            if (CompareSignificance(currentValue, originalValue) == originalValue)
                            {
                                break;
                            }
                            CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            break;
                        }

                    case TypeCode.Boolean:
                        {
                            bool currentValue = ((BrushParameter<bool>)parameters[i]).Value;
                            bool originalValue = ((BrushParameter<bool>)m_OriginalParameters[i]).Value;
                            CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            break;
                        }

                    case TypeCode.String:
                        {
                            string currentValue = ((BrushParameter<string>)parameters[i]).Value;
                            string originalValue = ((BrushParameter<string>)m_OriginalParameters[i]).Value;
                            CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            break;
                        }
                    case TypeCode.Object:
                        {
                            if (type == typeof(Vector3))
                            {
                                Vector3 currentValue = ((BrushParameter<Vector3>)parameters[i]).Value;
                                Vector3 originalValue = ((BrushParameter<Vector3>)m_OriginalParameters[i]).Value;
                                CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            }
                            else if (type == typeof(Vector4))
                            {
                                Vector4 currentValue = ((BrushParameter<Vector4>)parameters[i]).Value;
                                Vector4 originalValue = ((BrushParameter<Vector4>)m_OriginalParameters[i]).Value;
                                CacheChangedParamter(parameters[i].Name, currentValue, originalValue);
                            }
                            else if (type == typeof(Keyframe[]))
                            {
                                Keyframe[] currentValue = ((BrushParameter<Keyframe[]>)parameters[i]).Value;
                                Keyframe[] originalValue = ((BrushParameter<Keyframe[]>)m_OriginalParameters[i]).Value;
                                for (int k = 0; k < currentValue.Length; k++)
                                {
                                    //Check if there's a change between the original and current keyframe values
                                    //Cache the change if there's a difference
                                    if(!currentValue[k].Equals(originalValue[k]))
                                    {
                                        CacheChangedParamter($"{parameters[i].Name}", currentValue[k], originalValue[k]);
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        Debug.LogWarning($"The parameter of type {type} isn't able to be tracked by Analytics");
                        break;
                }
            }

            m_OriginalParameters = parameters;
        }

        static void CacheAnalyticsData(BaseBrushUIGroup brush)
        {
            m_Data.brush_name = brush.brushName;
            m_Data.shortcuts = s_UsedBrushShortcut;
            m_Data.mask_filters = brush.brushMaskFilterStack.filters?.
                Where(x => x.enabled).
                Select(x => x.GetType().Name).ToArray();
            m_Data.strength = CompareSignificance(brush.brushStrength, m_Data.strength);
            m_Data.size = CompareSignificance(brush.brushSize, m_Data.size);
            m_Data.rotation = CompareSignificance(brush.brushRotation, m_Data.rotation);
            m_Data.spacing = CompareSignificance(brush.brushSpacing, m_Data.spacing);
            m_Data.scatter = CompareSignificance(brush.brushScatter, m_Data.scatter);
            m_Data.brush_parameters = s_ModifiedBrushParameters;
        }

        #region Helper Methods
        /// <summary>
        /// Caches the shortcutId on keyRelease to be sent as analytics data if the 
        /// shortcut key hasn't been cached already
        /// </summary>
        /// <param name="shortcutId">ID of the shortcut </param>
        internal static void OnShortcutKeyRelease(string shortcutId)
        {
            if(!s_UsedBrushShortcut.Contains(shortcutId))
                s_UsedBrushShortcut.Add(shortcutId);
        }

        /// <summary>
        /// Flags the parameter changed boolean notifying that the user has changed brush parameters
        /// and the old data needs to be sent while the new data needs to be cached.
        /// </summary>
        internal static void OnParameterChange() => s_ParameterChanged = true;

        /// <summary>
        /// Cache the parameter that has been changed to be sent as analytics data
        /// </summary>
        /// <param name="name">The name of the changed setting Ex: "Brush Strength"</param>
        /// <param name="currentSetting">Setting of the brush before the user starts painting</param>
        /// <param name="originalSetting">Setting of the brush which the user starts painting with</param>
        /// <returns>Returns a boolean indicating if the parameter was changed</returns>
        static void CacheChangedParamter<T>(string name, T currentSetting, T originalSetting)
        {
            s_ModifiedBrushParameters.Add(new BrushParameterData
            {
                name = name,
                value = currentSetting.ToString()
            });
        }

        /// <summary>
        /// Check whether the difference between the initial and current brush parameters
        /// is significant enough to send the data for analyzing. 
        /// </summary>
        /// <param name="currentValue">The latest brush parameter</param>
        /// <param name="originalValue">The starting brush parameter</param>
        /// <returns></returns>
        static float CompareSignificance(float currentValue, float originalValue)
        {
            //Determine if value A has significantly changed from value B
            if (Mathf.Abs(currentValue - originalValue) >= k_SignficantThreshold)
                return currentValue;
            else
                return originalValue;
        }

        #endregion
    }
}

