using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.TerrainAPI
{
    /// <summary>
    /// Struct used for defining a NoiseType implementation. Information in this struct
    /// is used in shader generation and for determining compatabilities with
    /// FractalType implementations.
    /// </summary>
    public struct NoiseTypeDescriptor
    {
        /// <summary>
        /// The name of the NoiseType implementation
        /// </summary>
        public string           name;
        /// <summary>
        /// The path to the directory where the generated shader files for this NoiseType
        /// should be written to disk.
        /// </summary>
        public string           outputDir;
        /// <summary>
        /// The file path of the HLSL file containing the implementation for this NoiseType
        /// </summary>
        public string           sourcePath;
        /// <summary>
        /// Flags that represented the dimensions this NoiseType implementation supports
        /// </summary>
        public NoiseDimensionFlags   supportedDimensions;
        /// <summary>
        /// List of HlslInput structs defining the HLSL function parameter list for this
        /// NoiseType implementation. This is used in the noise shader generation. 
        /// </summary>
        public List<HlslInput>  inputStructDefinition;

        // TODO(wyatt): output structs would be useful for things like curl
        //              noise and anything requiring derivatives
        // public List<HlslOutput> outputStructDefinition;
    }

    /// <summary>
    /// Interface for defining a NoiseType implementation. Don't implement directly from this but
    /// instead make your NoiseType implementation inherit from the NoiseType< T > generic abstract class.
    /// </summary>
    public interface INoiseType
    {
        /// <summary>
        /// Returns a description of the NoiseType implementation
        /// </summary>
        NoiseTypeDescriptor     GetDescription();

        /// <summary>
        /// Returns a string representing the default state of the data that this NoiseType
        /// implementation uses.
        /// </summary>
        string                  GetDefaultSerializedString();
        
        /// <summary>
        /// Renders the GUI for the NoiseType implementation using the data provided by the serialized string.
        /// </summary>
        /// <param name="serializedString"> A string for the serialized data used by the NoiseType implementation </param>
        /// <returns>
        /// A string for the NoiseType implementation's serialized data after having gone through possible
        /// changes from user interaction with the GUI
        /// </returns>
        string                  DoGUI(string serializedString);

        /// <summary>
        /// Sets up a Material to be used with the NoiseType implementation. This function is called
        /// from within NoiseSettings.SetupMaterial( Material mat )
        /// </summary>
        /// <param name="mat"> The Material to setup for use with this NoiseType implementation </param>
        /// <param name="serializedString">
        /// The serialized string representing the NoiseType data to
        /// be used when setting up the provided Material
        /// </param>
        void                    SetupMaterial(Material mat, string serializedString);

        /// <summary>
        /// When overidden, converts an object representing the data for a NoiseType implementation to a string.
        /// This is later serialized and stored in a NoiseSettings Asset.
        /// </summary>
        /// <param name="target">
        /// The data representation for the NoiseType implementation to be converted
        /// into a string
        /// </param>
        /// <returns> The string representation of the provided target object </returns>
        string                  ToSerializedString(object target);

        /// <summary>
        /// When overidden, converts a string representing the data for the NoiseType implementation into an
        /// an actual object reference to be used with said NoiseType implementation.
        /// </summary>
        /// <param name="serializedString">
        /// A string representing a serialized object that is
        /// used by the NoiseType implementation
        /// </param>
        /// <returns> An object deserialized from the provided string </returns>
        object                  FromSerializedString(string serializedString);
    }

    /// <summary>
    /// A Singleton class used for representing a NoiseType implementation. A NoiseType class must
    /// inherit from this in order to be considered a valid NoiseType and included in the noise
    /// shader generation and available for use with the various noise tools.
    /// </summary>
    public abstract class NoiseType<T> : ScriptableSingleton<T>, INoiseType where T : NoiseType<T>
    {
        /// <summary>
        /// Returns a descriptor struct defining the NoiseType
        /// </summary>
        public abstract NoiseTypeDescriptor GetDescription();
        
        /// <summary>
        /// When overidden, returns a string that represents the default data state for the NoiseType implementation
        /// </summary>
        public virtual string GetDefaultSerializedString() { return null; }

        /// <summary>
        /// When overidden, sets up a Material to be used with the NoiseType implementation. This function is called
        /// from within NoiseSettings.SetupMaterial( Material mat )
        /// </summary>
        /// <param name="mat"> The Material to setup for use with this NoiseType implementation </param>
        /// <param name="serializedString">
        /// The serialized string representing the NoiseType data to
        /// be used when setting up the provided Material
        /// </param>
        public virtual void SetupMaterial(Material mat, string serializedString) { }

        /// <summary>
        /// When overidden, renders the GUI for the NoiseType implementation using the data provided by the serialized string.
        /// </summary>
        /// <param name="serializedString"> A string for the serialized data used by the NoiseType implementation </param>
        /// <returns>
        /// A string for the NoiseType implementation's serialized data after having gone through possible
        /// changes from user interaction with the GUI
        /// </returns>
        public virtual string DoGUI(string serializedString) { return null; }

        /// <summary>
        /// When overidden, converts an object representing the data for a NoiseType implementation to a string.
        /// This is later serialized and stored in a NoiseSettings Asset.
        /// </summary>
        /// <param name="target">
        /// The data representation for the NoiseType implementation to be converted into a string
        /// </param>
        /// <returns> The string representation of the provided target object </returns>
        public virtual string ToSerializedString(object target) { return null; }

        /// <summary>
        /// When overidden, converts a string representing the data for the NoiseType implementation into an
        /// an actual object reference to be used with said NoiseType implementation.
        /// </summary>
        /// <param name="serializedString">
        /// A string representing a serialized object that is used by the NoiseType implementation
        /// </param>
        /// <returns> An object deserialized from the provided string </returns>
        public virtual object FromSerializedString(string serializedString) { return null; }
    }
}