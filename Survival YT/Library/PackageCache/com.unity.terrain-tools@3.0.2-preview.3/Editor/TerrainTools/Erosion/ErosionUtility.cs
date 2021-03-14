using UnityEngine;
using System.IO;
using UnityEditor;

namespace Erosion {
    #region ToolTips
    public static class Styles {
        //Common
        public static GUIContent m_TimeDelta = EditorGUIUtility.TrTextContent("Time Interval (dt)", "Specifies the time interval used for each iteration of the simulation. A smaller value will produce a more " +
            "accurate result, but will have a smaller visual impact unless the iteration count is increased.");
        public static GUIContent m_NumIterations = EditorGUIUtility.TrTextContent("Iterations", "Specifies the number of erosion iterations to perform per brush stamp. " +
            "Increasing this value will run the simulation longer and produce more dramatic results at the expense of performance.");
        


        //Hydro 
        public static GUIContent m_HydroErosionControls = EditorGUIUtility.TrTextContent("Hydraulic Erosion Controls");
        public static GUIContent m_AffectHeight = EditorGUIUtility.TrTextContent("Affect Terrain Height", "Toggles whether this brush will affect the terrain height.");
        public static GUIContent m_AddHeight = EditorGUIUtility.TrTextContent("Add Height", "Amount of new height to add before erosion simulation.");
        public static GUIContent m_Invert = EditorGUIUtility.TrTextContent("Invert", "Invert the effect of the erosion simulation");
        public static GUIContent m_HydroLowResIterations = EditorGUIUtility.TrTextContent("Low Res Iterations", "Specifies the number of hydraulic erosion iterations to perform at 1/4 the " +
            "resolution of the brush. Using more low res iterations will optimize the brush performance at the expense of accuracy and detail.");

        public static GUIContent m_IterationBlendScalar = EditorGUIUtility.TrTextContent("Iteration Blend Scalar", "Controls the amount each iteration of the simulation is blended into the result.");
        public static GUIContent m_SimulationScale = EditorGUIUtility.TrTextContent("Simulation Scale", "Controls the world scale of the simulation.");

        public static GUIContent m_ThermalDTScalar = EditorGUIUtility.TrTextContent("dt Scalar", "Multiplier for base time step (dt). Controls the impact of thermal smoothing.");

        public static GUIContent m_WaterLevelScale = EditorGUIUtility.TrTextContent("Water Level Scale", "Debug Value");
        public static GUIContent m_PrecipitationRate = EditorGUIUtility.TrTextContent("Precipitation Rate", "Controls the rate at which water is added to the simulation. " +
            "Making this value higher will increase the effect of water on the simulation, but may lead to instabilities if too high.");
        public static GUIContent m_EvaporationRate = EditorGUIUtility.TrTextContent("Evaporation Rate", "Controls the rate at which water is removed from the simulation. " +
            "Making this value higher will decrease the effect of water on the simulation, but may lead to a net-zero effect if set too high.");
        public static GUIContent m_FlowRate = EditorGUIUtility.TrTextContent("Flow Rate", "Controls the rate at which water flows through the heightfield. " +
            "Setting this higher will result in more dramatic erosion effects, but setting it too high " +
            "may lead to instabilities in the simulation.");

        public static GUIContent m_SedimentScale = EditorGUIUtility.TrTextContent("Sediment Scale", "Debug value");
        public static GUIContent m_SedimentCap = EditorGUIUtility.TrTextContent("Sediment Capacity", "Specifies the maximum density of sediment that can be dissolved.");
        public static GUIContent m_SedimentDissolve = EditorGUIUtility.TrTextContent("Sediment Dissolution Rate", "Specifies the overall rate at which sediment is dissolved off the " +
            "height field, and suspended into the hydraulic flow");
        public static GUIContent m_SedimentDeposit = EditorGUIUtility.TrTextContent("Sediment Deposit Rate", "Specifies the overall rate at which sediment is deposited back into " +
            "the height field and removed from the hydraulic flow");

        // Thermal        
        public static GUIContent m_ThermalErosionControls = EditorGUIUtility.TrTextContent("Thermal Erosion Controls");
        public static GUIContent m_DoThermal = EditorGUIUtility.TrTextContent("Thermal Erosion", "Toggles whether the thermal erosion simulation is run. " +
            "Thermal Erosion simulates the crumbling and settling of rock into loose debris.");
        public static GUIContent m_AngleOfRepose = EditorGUIUtility.TrTextContent("Resting Angle", "Specifies the \"Angle of Repose\", or \"Talus Angle\" for the simulated debris. " +
            "Loose debris naturally settles at a specific angle based on its material type and particulate size. Sand, for example naturally settles in piles with " +
            "a ~35 degree slope");
        public static GUIContent m_AngleOfReposeJitter = EditorGUIUtility.TrTextContent("Resting Angle Jitter", "Adds random variation per brush stamp to the resting angle.");
        public static GUIContent m_MatPreset = EditorGUIUtility.TrTextContent("Physical Material Presets", "Value presets for known physical material types");

        //Wind
        public static GUIContent m_WindErosionControls = EditorGUIUtility.TrTextContent("Wind Erosion Controls");
        public static GUIContent m_WindSpeed = EditorGUIUtility.TrTextContent("Wind Speed", "The initial speed of the wind.");
        public static GUIContent m_DiffusionRate = EditorGUIUtility.TrTextContent("Diffusion Rate", "The rate at which suspended particulate is diffused to neighboring cells.");
        public static GUIContent m_Viscosity = EditorGUIUtility.TrTextContent("Viscosity", "The viscosity (thickness) of the fluid being simulated");
        public static GUIContent m_SuspensionRate = EditorGUIUtility.TrTextContent("Particulate Suspension Rate", "The rate at which particulates are removed from the terrain, and suspended in the air");
        public static GUIContent m_DepositionRate = EditorGUIUtility.TrTextContent("Particulate Deposit Rate", "The rate at which particulates are deposited onto the terrain and removed from the air");
        public static GUIContent m_WindSpeedJitter = EditorGUIUtility.TrTextContent("Jitter", "Adds random fluctuation to the wind speed for each brush stamp");
        public static GUIContent m_SlopeFactor = EditorGUIUtility.TrTextContent("Slope Factor", "Controls the degree to which terrain perpendicular to the wind direction is affected by the simulation");
        public static GUIContent m_DragCoefficient = EditorGUIUtility.TrTextContent("Drag", "Controls the degree to which the wind speed will naturally decay");
        public static GUIContent m_ReflectionCoefficient = EditorGUIUtility.TrTextContent("Surface Reflection", "Controls the degree to which wind reflects off of perpendicular surfaces");
        public static GUIContent m_AbrasivenessCoefficient = EditorGUIUtility.TrTextContent("Abrasiveness", "Controls how much suspended particulates will erode terrain");
        public static GUIContent m_AdvectionVelScale = EditorGUIUtility.TrTextContent("Flow Rate", "Controls how fast particulates flow through the wind velocity field");


        // Material
        public static GUIContent m_AffectMaterial = EditorGUIUtility.TrTextContent("Affect Terrain Material", "Toggles whether this brush will affect the terrain material");
        public static GUIContent m_MaterialOpacity = EditorGUIUtility.TrTextContent("Opacity", "Specifies the opacity which the resulting mask will affect the material");
        public static GUIContent m_MaterialSpread = EditorGUIUtility.TrTextContent("Spread", "Controls the spread of the material application.");

        // Advanced Hydro
        public static GUIContent m_GravitationConstant = EditorGUIUtility.TrTextContent("Gravitational Constant", "Specifies the force of gravity on the hydraulic simulation.\n" +
            "The gravitational constant is -9.8 m/s^2 on Earth.");
        public static GUIContent m_RiverbankDissolve = EditorGUIUtility.TrTextContent("Riverbank Dissolution Rate", "A multiplier which controls the sediment dissolution rate where the slope of the height-field is near vertical.");
        public static GUIContent m_RiverbankDeposit = EditorGUIUtility.TrTextContent("Riverbank Deposit Rate", "A multiplier which controls the sediment deposit rate where the slope of the height-field is near vertical.");
        public static GUIContent m_RiverbedDissolve = EditorGUIUtility.TrTextContent("Riverbed Dissolution Rate", "A multiplier which controls the sediment dissolution rate where the slope of the height-field is near horizontal.");
        public static GUIContent m_RiverbedDeposit = EditorGUIUtility.TrTextContent("Riverbed Deposit Rate", "A multiplier which controls the sediment deposit rate where the slope of the height-field is near horizontal.");
    }
    #endregion
}
