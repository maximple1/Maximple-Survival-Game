using System.Collections.Generic;
using System.Globalization;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Enum for determining the type of a given HLSL value during shader generation
    /// </summary>
    public enum HlslValueType
    {
        Float = 0,
        Float2,
        Float3,
        Float4,
    }

    /// <summary>
    /// Representation for an HLSL float
    /// </summary>
    public struct HlslFloat
    {
        /// <summary>
        /// The value for the HLSL float
        /// </summary>
        public float val;

        /// <summary>
        /// The constructor for an HlslFloat
        /// </summary>
        /// <param name="val"> The GPU value for this HlslFloat </param>
        public HlslFloat(float val)
        {
            this.val = val;
        }
    }

    /// <summary>
    // Representation for an HLSL float2
    /// </summary>
    public struct HlslFloat2
    {
        /// <summary>
        /// The x-compenent for the HLSL float2
        /// </summary>
        public float x;
        
        /// <summary>
        /// The y-compenent for the HLSL float2
        /// </summary>
        public float y;
        
        /// <summary>
        /// The constructor for an HlslFloat2
        /// </summary>
        /// <param name="x"> The GPU value of the x-component to be used for this HlslFloat2 </param>
        /// <param name="y"> The GPU value of the y-component to be used for this HlslFloat2 </param>
        public HlslFloat2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }

    /// <summary>
    /// Representation for an HLSL float3
    /// </summary>
    public struct HlslFloat3
    {
        /// <summary>
        /// The x-compenent for the HLSL float3
        /// </summary>
        public float x;
        
        /// <summary>
        /// The y-compenent for the HLSL float3
        /// </summary>
        public float y;
        
        /// <summary>
        /// The z-compenent for the HLSL float3
        /// </summary>
        public float z;
        
        /// <summary>
        /// The constructor for an HlslFloat3
        /// </summary>
        /// <param name="x"> The GPU value of the x-component to be used for this HlslFloat3 </param>
        /// <param name="y"> The GPU value of the y-component to be used for this HlslFloat3 </param>
        /// <param name="z"> The GPU value of the z-component to be used for this HlslFloat3 </param>
        public HlslFloat3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

    /// <summary>
    /// Representation for an HLSL float4
    /// </summary>
    public struct HlslFloat4
    {
        /// <summary>
        /// The x-compenent for the HLSL float4
        /// </summary>
        public float x;

        /// <summary>
        /// The y-compenent for the HLSL float4
        /// </summary>
        public float y;
        
        /// <summary>
        /// The z-compenent for the HLSL float4
        /// </summary>
        public float z;
        
        /// <summary>
        /// The w-compenent for the HLSL float4
        /// </summary>
        public float w;

        /// <summary>
        /// The constructor for an HlslFloat4
        /// </summary>
        /// <param name="x"> The GPU value of the x-component to be used for this HlslFloat4 </param>
        /// <param name="y"> The GPU value of the y-component to be used for this HlslFloat4 </param>
        /// <param name="z"> The GPU value of the z-component to be used for this HlslFloat4 </param>
        /// <param name="w"> The GPU value of the w-component to be used for this HlslFloat4 </param>
        public HlslFloat4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    /// <summary>
    /// Representation for an HLSL function input parameter
    /// </summary>
    public struct HlslInput
    {
        /// <summary>
        /// The name of this HLSL function input parameter
        /// </summary>
        public string           name;

        /// <summary>
        /// Returns the value type for the variable this HlslInput represents
        /// </summary>
        public HlslValueType    valueType
        {
            get;
            private set;
        }

        private HlslFloat       m_floatValue;
        /// <summary>
        /// Returns the HLSL float value. Sets the HLSL float value and value
        /// type to HlslValueType.Float
        /// </summary>
        public HlslFloat        floatValue
        {
            get { return m_floatValue; }
            set
            {
                valueType = HlslValueType.Float;
                m_floatValue = value;
            }
        }

        private HlslFloat2 m_float2Value;
        /// <summary>
        /// Returns the HLSL float2 value. Sets the HLSL float value and value
        /// type to HlslValueType.Float2
        /// </summary>
        public HlslFloat2 float2Value
        {
            get { return m_float2Value; }
            set
            {
                valueType = HlslValueType.Float2;
                m_float2Value = value;
            }
        }

        private HlslFloat3 m_float3Value;
        /// <summary>
        /// Returns the HLSL float3 value. Sets the HLSL float value and value
        /// type to HlslValueType.Float3
        /// </summary>
        public HlslFloat3 float3Value
        {
            get { return m_float3Value; }
            set
            {
                valueType = HlslValueType.Float3;
                m_float3Value = value;
            }
        }

        private HlslFloat4 m_float4Value;
        /// <summary>
        /// Returns the HLSL float4 value. Sets the HLSL float value and value
        /// type to HlslValueType.Float4
        /// </summary>
        public HlslFloat4 float4Value
        {
            get { return m_float4Value; }
            set
            {
                valueType = HlslValueType.Float4;
                m_float4Value = value;
            }
        }
        
        /// <summary>
        /// Returns the string representation of the HlslValueType for this struct
        /// </summary>
        public string GetHlslValueTypeString()
        {
            switch(valueType)
            {
                case HlslValueType.Float:
                    return "float";
                case HlslValueType.Float2:
                    return "float2";
                case HlslValueType.Float3:
                    return "float3";
                case HlslValueType.Float4:
                    return "float4";
            }

            return "unsupported_type";
        }

        
        /// <summary>
        /// Returns the formatted HLSL string for the default value declaration for this struct's HlslValueType
        /// </summary>
        public string GetDefaultValueString()
        {
            string valueTypeString = GetHlslValueTypeString();
            string constructedValueString = null;

            switch(valueType)
            {
                case HlslValueType.Float:
                    constructedValueString = floatValue.val.ToString( CultureInfo.InvariantCulture );
                    break;
                case HlslValueType.Float2:
                    constructedValueString = string.Format( CultureInfo.InvariantCulture,
                                                            "{0}({1})", valueTypeString,
                                                                float2Value.x.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float2Value.y.ToString( CultureInfo.InvariantCulture ) );
                    break;
                case HlslValueType.Float3:
                    constructedValueString = string.Format( CultureInfo.InvariantCulture,
                                                            "{0}({1})", valueTypeString,
                                                                float3Value.x.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float3Value.y.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float3Value.z.ToString( CultureInfo.InvariantCulture ) );
                    break;
                case HlslValueType.Float4:
                    constructedValueString = string.Format( CultureInfo.InvariantCulture,
                                                            "{0}({1})", valueTypeString,
                                                                float4Value.x.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float4Value.y.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float4Value.z.ToString( CultureInfo.InvariantCulture ) + ", " +
                                                                float4Value.w.ToString( CultureInfo.InvariantCulture ) );
                    break;
                default:
                    return "unsupported_type()"; 
            }

            return constructedValueString;
        }
    }

    // public struct HlslOutput
    // {
    //     public string name;
    //     public HlslValueType valueType;

    //     public HlslOutput(string name, HlslValueType valueType)
    //     {
    //         this.name = name;
    //         this.valueType = valueType;
    //     }
    // }

    // public struct HlslStructDescriptor
    // {
    //     public List<HlslInput> members;
    // }

    // public struct HlslFunctionDescriptor
    // {
    //     public List<HlslInput> inputs;
    //     public List<HlslOutput> outputs;
    // }
}