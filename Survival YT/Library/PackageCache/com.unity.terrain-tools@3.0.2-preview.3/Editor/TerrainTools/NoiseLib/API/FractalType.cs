using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Struct that provides a definition for a FractalType implementation. Information in this
    /// struct is used during Shader generation and used to determine NoiseType and
    /// FractalType compatibilities.
    /// </summary>
    public struct FractalTypeDescriptor
    {
        /// <summary>
        /// The name to be used for the FractalType
        /// </summary>
        public string name;
        /// <summary>
        /// The Asset path to the .noisehlsltemplate file to be used when generating shaders
        /// for this FractalType variant
        /// </summary>
        public string templatePath;
        /// <summary>
        /// A flag definition the supported number of dimensions that this FractalType implements.
        /// This currently is unused.
        /// </summary>
        public NoiseDimensionFlags supportedDimensions;
        /// <summary>
        /// List of HlslInput structures defining the HLSL function parameter list for this FractalType
        /// </summary>
        public List<HlslInput> inputStructDefinition;
        /// <summary>
        /// Additional include paths to be included in the generated Shader. You can add them to this
        /// list or hardcode them somewhere in the Shader itself.
        /// </summary>
        public List<string> additionalIncludePaths;
        // public List<HlslOutput> outputStructDefinition;
    }

    /// <summary>
    /// Interface used for custom FractalType implementations. This should only be used by
    /// the FractalType< T > abstract class.
    /// </summary>
    public interface IFractalType
    {
        /// <summary>
        /// Returns a descriptor struct defining the FractalType
        /// </summary>
        FractalTypeDescriptor GetDescription();

        /// <summary>
        /// Returns a string that represents the default data state for the FractalType implementation
        /// </summary>
        string GetDefaultSerializedString();

        /// <summary>
        /// Sets up a Material to be used with the FractalType implementation. This function is called
        /// from within NoiseSettings.SetupMaterial( Material mat )
        /// </summary>
        /// <param name="mat"> The Material to setup for use with this FractalType implementation </param>
        /// <param name="serializedString">
        /// The serialized string representing the FractalType data to
        /// be used when setting up the provided Material
        /// </param>
        void SetupMaterial(Material mat, string serializedString);

        /// <summary>
        /// Renders the GUI for the FractalType implementation using the data provided by the serialized string.
        /// </summary>
        /// <param name="serializedString"> A string for the serialized data used by the FractalType implementation </param>
        /// <returns>
        /// A string for the FractalType implementation's serialized data after having gone through possible
        /// changes from user interaction with the GUI
        /// </returns>
        string DoGUI(string serializedString);

        /// <summary>
        /// Converts an object representing the data for a FractalType implementation to a string.
        /// This is later serialized and stored in a NoiseSettings Asset.
        /// </summary>
        /// <param name="target">
        /// The data representation for the FractalType implementation to be converted
        /// into a string
        /// </param>
        /// <returns> The string representation of the provided target object </returns>
        string ToSerializedString(object target);

        /// <summary>
        /// Converts a string representing the data for the FractalType implementation into an
        /// an actual object reference to be used with said FractalType implementation.
        /// </summary>
        /// <param name="serializedString">
        /// A string representing a serialized object that is
        /// used by the FractalType implementation
        /// </param>
        /// <returns> An object deserialized from the provided string </returns>
        object FromSerializedString(string serializedString);
    }

    /// <summary>
    /// A Singleton class used for representing a FractalType implementation. A FractalType class must
    /// inherit from this in order to be considered a valid FractalType and included in the noise
    /// shader generation and available for use with the various noise tools.
    /// </summary>
    public abstract class FractalType<T> : ScriptableSingleton<T>, IFractalType where T : FractalType<T>
    {
        /// <summary>
        /// Returns a descriptor struct defining the FractalType
        /// </summary>
        public abstract FractalTypeDescriptor GetDescription();

        /// <summary>
        /// When overidden, returns a string that represents the default data state for the FractalType implementation
        /// </summary>
        public virtual string GetDefaultSerializedString() { return null; }

        /// <summary>
        /// When overidden, sets up a Material to be used with the FractalType implementation. This function is called
        /// from within NoiseSettings.SetupMaterial( Material mat )
        /// </summary>
        /// <param name="mat"> The Material to setup for use with this FractalType implementation </param>
        /// <param name="serializedString">
        /// The serialized string representing the FractalType data to
        /// be used when setting up the provided Material
        /// </param>
        public virtual void SetupMaterial(Material mat, string serializedString) { }

        /// <summary>
        /// When overidden, renders the GUI for the FractalType implementation using the data provided by the serialized string.
        /// </summary>
        /// <param name="serializedString"> A string for the serialized data used by the FractalType implementation </param>
        /// <returns>
        /// A string for the FractalType implementation's serialized data after having gone through possible
        /// changes from user interaction with the GUI
        /// </returns>
        public virtual string DoGUI(string serializedString) { return null; }
        
        /// <summary>
        /// When overidden, converts an object representing the data for a FractalType implementation to a string.
        /// This is later serialized and stored in a NoiseSettings Asset.
        /// </summary>
        /// <param name="target">
        /// The data representation for the FractalType implementation to be converted into a string
        /// </param>
        /// <returns> The string representation of the provided target object </returns>
        public virtual string ToSerializedString(object target) { return null; }

        /// <summary>
        /// When overidden, converts a string representing the data for the FractalType implementation into an
        /// an actual object reference to be used with said FractalType implementation.
        /// </summary>
        /// <param name="serializedString">
        /// A string representing a serialized object that is used by the FractalType implementation
        /// </param>
        /// <returns> An object deserialized from the provided string </returns>
        public virtual object FromSerializedString(string serializedString) { return null; }
    }
}