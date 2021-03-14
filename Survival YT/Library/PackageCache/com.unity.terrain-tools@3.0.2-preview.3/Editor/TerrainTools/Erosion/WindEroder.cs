using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;
using System;
using System.Collections.Generic;

namespace Erosion {

    [Serializable]
    public class WindEroder : ITerrainEroder {

        #region Resources
        //we store all our compute shaders in a dictionary
        Dictionary<string, ComputeShader> m_ComputeShaders = new Dictionary<string, ComputeShader>();
        ComputeShader GetComputeShader(string name) {
            ComputeShader s = null;
            try {
                s = m_ComputeShaders[name];
            } catch {
                s = (ComputeShader)Resources.Load(name);
                if (s != null) {
                    m_ComputeShaders[name] = s;
                }
            }
            return s;
        }

        #endregion

 
        #region Simulation Params
        [SerializeField]
        public TerrainFloatMinMaxValue m_WindSpeed = new TerrainFloatMinMaxValue(Erosion.Styles.m_WindSpeed, 1.7f, 0.0f, 10.0f);
        [SerializeField]
        public float m_WindSpeedJitter = 0.0f;
        [SerializeField]
        private TerrainFloatMinMaxValue m_dt = new TerrainFloatMinMaxValue(Erosion.Styles.m_TimeDelta, 0.006f, 0.001f, 0.01f);

        public Vector4 m_WindVel = Vector4.zero; //TODO: sloppy..

        [SerializeField]
        private TerrainIntMinMaxValue m_Iterations = new TerrainIntMinMaxValue(Erosion.Styles.m_NumIterations, 12, 1, 100);

        [SerializeField]
        //private Vector3 m_SimulationScale = new Vector3(1.0f, 0.0f, 100.0f);
        private TerrainFloatMinMaxValue m_SimulationScale = new TerrainFloatMinMaxValue(Erosion.Styles.m_SimulationScale, 10.0f, 0.0f, 100.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_DiffusionRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_DiffusionRate, 0.0001f, 0.0f, 0.01f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_Viscosity = new TerrainFloatMinMaxValue(Erosion.Styles.m_Viscosity, 0.00075f, 0.0f, 0.01f);
        [SerializeField]
        private int m_ProjectionSteps = 2;
        [SerializeField]
        private int m_DiffuseSteps = 1;
        [SerializeField]
        private TerrainFloatMinMaxValue m_AdvectionVelScale = new TerrainFloatMinMaxValue(Erosion.Styles.m_AdvectionVelScale, 10.0f, 0.0f, 25.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_SuspensionRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_SuspensionRate, 22.0f, 0.0f, 25.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_DepositionRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_DepositionRate, 10.0f, 0.0f, 25.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_SlopeFactor = new TerrainFloatMinMaxValue(Erosion.Styles.m_SlopeFactor, 1.0f, 0.0f, 10.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_DragCoefficient = new TerrainFloatMinMaxValue(Erosion.Styles.m_DragCoefficient, 0.5f, 0.0f, 10.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_ReflectionCoefficient = new TerrainFloatMinMaxValue(Erosion.Styles.m_ReflectionCoefficient, 23.0f, 0.0f, 50.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_AbrasivenessCoefficient = new TerrainFloatMinMaxValue(Erosion.Styles.m_AbrasivenessCoefficient, 0.0f, 0.0f, 10.0f);
        [SerializeField]
        private TerrainFloatMinMaxValue m_ThermalTimeDelta = new TerrainFloatMinMaxValue(Erosion.Styles.m_ThermalDTScalar, 0.004f, 0.0f, 0.1f);
        [SerializeField]
        private int m_ThermalIterations = 3;
        [SerializeField]
        private float m_AngleOfRepose = 45.0f;

        #region Analytics Property Access
        public int ProjectionSteps => m_ProjectionSteps;
        public int DiffuseSteps => m_DiffuseSteps;
        public int ThermalIterations => m_ThermalIterations;
        public float AngleOfRepose => m_AngleOfRepose;
        public TerrainFloatMinMaxValue TimeInterval => m_dt;
        public TerrainIntMinMaxValue Iterations => m_Iterations; 
        public TerrainFloatMinMaxValue SimulationScale => m_SimulationScale; 
        public TerrainFloatMinMaxValue DiffusionRate => m_DiffusionRate;
        public TerrainFloatMinMaxValue Viscosity => m_Viscosity;
        public TerrainFloatMinMaxValue AdvectionVelScale => m_AdvectionVelScale;
        public TerrainFloatMinMaxValue SuspensionRate => m_SuspensionRate;
        public TerrainFloatMinMaxValue DepositionRate => m_DepositionRate;
        public TerrainFloatMinMaxValue SlopeFactor => m_SlopeFactor;
        public TerrainFloatMinMaxValue DragCoefficient => m_DragCoefficient;
        public TerrainFloatMinMaxValue ReflectionCoefficient => m_ReflectionCoefficient;
        public TerrainFloatMinMaxValue AbrasivenessCoefficient => m_AbrasivenessCoefficient;
        public TerrainFloatMinMaxValue ThermalTimeDelta => m_ThermalTimeDelta;
        #endregion

        public WindEroder() { ResetSettings(); }

        //[SerializeField]
        //private Texture2D m_WindVelocityTex = null;
        #endregion

        public Dictionary<string, RenderTexture> inputTextures { get; set; } = new Dictionary<string, RenderTexture>();
        public Dictionary<string, RenderTexture> outputTextures { get; private set; } = new Dictionary<string, RenderTexture>();

        public void OnEnable() { }

        private void ResetOutputs(int width, int height) {
            foreach (var rt in outputTextures) {
                RenderTexture.ReleaseTemporary(rt.Value);
            }
            outputTextures.Clear();

            //TODO: clean this up
            RenderTexture heightRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            heightRT.enableRandomWrite = true;
            outputTextures["Height"] = heightRT;

            RenderTexture windVelRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            heightRT.enableRandomWrite = true;
            outputTextures["Wind Velocity"] = windVelRT;

            RenderTexture divergenceRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            divergenceRT.enableRandomWrite = true;
            outputTextures["Divergence"] = divergenceRT;

            RenderTexture thermalSedimentRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            thermalSedimentRT.enableRandomWrite = true;
            outputTextures["Thermal Sediment"] = thermalSedimentRT;

            RenderTexture sedimentRT = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            sedimentRT.enableRandomWrite = true;
            outputTextures["Sediment"] = sedimentRT;
        }

        public void ErodeHeightmap(Vector3 terrainDimension, Rect domainRect, Vector2 texelSize, bool invertEffect = false) {
            ResetOutputs((int)domainRect.width, (int)domainRect.height);
            ErodeHelper(terrainDimension, domainRect, texelSize, invertEffect, false);
        }

        private void ErodeHelper(Vector3 terrainScale, Rect domainRect, Vector2 texelSize, bool invertEffect, bool lowRes) {
            RenderTexture tmpRT = UnityEngine.RenderTexture.active;

            //this one is mandatory
            if (!inputTextures.ContainsKey("Height")) {
                throw (new Exception("No input heightfield specified!"));
            }

            #region Find Compute Kernels
            ComputeShader advectionCS = GetComputeShader("Advection");
            ComputeShader projectionCS = GetComputeShader("Projection");
            ComputeShader diffusionCS = GetComputeShader("Diffusion");
            ComputeShader utilityCS = GetComputeShader("ImageUtility");
            ComputeShader aeolianCS = GetComputeShader("Aeolian");
            ComputeShader thermalCS = GetComputeShader("Thermal");

            int advectKernelIdx = advectionCS.FindKernel("Advect");
            int divergenceKernelIdx = projectionCS.FindKernel("Divergence");
            int gradientSubtractKernelIdx = projectionCS.FindKernel("GradientSubtract");
            int diffuseKernelIdx = diffusionCS.FindKernel("Diffuse");
            int remapKernelIdx = utilityCS.FindKernel("RemapValues");
            int addConstantIdx = utilityCS.FindKernel("AddConstant");
            int applyDragKernelIdx = aeolianCS.FindKernel("ApplyHeightfieldDrag");
            int erodeKernelIdx = aeolianCS.FindKernel("WindSedimentErode");
            int thermalKernelIdx = thermalCS.FindKernel("ThermalErosion");

            int[] numWorkGroups = { 1, 1, 1 };
            #endregion

            int xRes = (int)inputTextures["Height"].width;
            int yRes = (int)inputTextures["Height"].height;

            #region Create Render Textures
            RenderTexture heightmapRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            RenderTexture heightmapPrevRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            RenderTexture collisionRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            RenderTexture sedimentPrevRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            RenderTexture sedimentRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            RenderTexture windVelRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            RenderTexture windVelPrevRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            RenderTexture divergenceRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            RenderTexture thermalSedimentRT = RenderTexture.GetTemporary(xRes, yRes, 0, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);

            heightmapRT.enableRandomWrite = true;
            heightmapPrevRT.enableRandomWrite = true;
            collisionRT.enableRandomWrite = true;
            sedimentRT.enableRandomWrite = true;
            sedimentPrevRT.enableRandomWrite = true;
            windVelRT.enableRandomWrite = true;
            windVelPrevRT.enableRandomWrite = true;
            divergenceRT.enableRandomWrite = true;
            thermalSedimentRT.enableRandomWrite = true;
            #endregion


            #region Setup Input Textures
            //clear the render textures
            Graphics.Blit(Texture2D.blackTexture, sedimentRT);
            Graphics.Blit(Texture2D.blackTexture, collisionRT);
            Graphics.Blit(Texture2D.blackTexture, windVelRT);
            Graphics.Blit(Texture2D.blackTexture, windVelPrevRT);

            Graphics.Blit(inputTextures["Height"], heightmapPrevRT);
            Graphics.Blit(inputTextures["Height"], heightmapRT);
            #endregion

            //precompute some values on the CPU (these become uniform constants in the shader)
            float dx = (float)texelSize.x * m_SimulationScale.value;
            float dy = (float)texelSize.y * m_SimulationScale.value;

            float dxy = Mathf.Sqrt(dx * dx + dy * dy);
            Vector4 dxdy = new Vector4(dx, dy, 1.0f / dx, 1.0f / dy); //TODO: make this the same for all compute shaders

            advectionCS.SetFloat("dt", m_dt.value);
            advectionCS.SetFloat("velScale", m_AdvectionVelScale.value);
            advectionCS.SetVector("dxdy", dxdy);
            advectionCS.SetVector("DomainRes", new Vector4((float)xRes, (float)yRes, 1.0f / (float)xRes, 1.0f / (float)yRes));

            diffusionCS.SetFloat("dt", m_dt.value);

            projectionCS.SetVector("dxdy", dxdy);

            aeolianCS.SetFloat("dt", m_dt.value);
            aeolianCS.SetFloat("SuspensionRate", m_SuspensionRate.value);
            aeolianCS.SetFloat("DepositionRate", m_DepositionRate.value);
            aeolianCS.SetFloat("SlopeFactor", m_SlopeFactor.value);
            aeolianCS.SetFloat("DragCoefficient", m_DragCoefficient.value);
            aeolianCS.SetFloat("ReflectionCoefficient", m_ReflectionCoefficient.value);
            aeolianCS.SetFloat("AbrasivenessCoefficient", m_AbrasivenessCoefficient.value * 1000.0f);
            aeolianCS.SetVector("DomainDim", new Vector4((float)xRes, (float)yRes, 0.0f, 0.0f));
            aeolianCS.SetVector("terrainScale", new Vector4(terrainScale.x, terrainScale.y, terrainScale.z, 0.0f));
            aeolianCS.SetVector("dxdy", dxdy);

            //use full tile res here?
            diffusionCS.SetVector("texDim", new Vector4((float)inputTextures["Height"].width, (float)inputTextures["Height"].height, 0.0f, 0.0f));

            #region Fluid Simulation Loop
            for (int i = 0; i < m_Iterations.value; i++) {

                #region Velocity Step

                utilityCS.SetTexture(addConstantIdx, "OutputTex", windVelPrevRT);
                utilityCS.SetVector("Constant", m_WindVel);
                utilityCS.Dispatch(addConstantIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                //Apply drag from heightfield
                aeolianCS.SetTexture(applyDragKernelIdx, "InHeightMap", heightmapPrevRT);
                aeolianCS.SetTexture(applyDragKernelIdx, "WindVel", windVelPrevRT);
                aeolianCS.SetTexture(applyDragKernelIdx, "OutWindVel", windVelRT);
                aeolianCS.Dispatch(applyDragKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                //Diffuse Velocity
                diffusionCS.SetFloat("diff", m_Viscosity.value);
                for (int j = 0; j < m_DiffuseSteps; j++) {
                    diffusionCS.SetTexture(diffuseKernelIdx, "InputTex", windVelRT);
                    diffusionCS.SetTexture(diffuseKernelIdx, "OutputTex", windVelPrevRT);

                    diffusionCS.Dispatch(diffuseKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);
                    Graphics.Blit(windVelPrevRT, windVelRT);
                }

                //Project Velocity
                for (int j = 0; j < m_ProjectionSteps; j++) {
                    projectionCS.SetTexture(divergenceKernelIdx, "VelocityTex2D", windVelRT);
                    projectionCS.SetTexture(divergenceKernelIdx, "DivergenceTex2D", divergenceRT);
                    projectionCS.Dispatch(divergenceKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                    projectionCS.SetTexture(gradientSubtractKernelIdx, "PressureTex2D", divergenceRT);
                    projectionCS.SetTexture(gradientSubtractKernelIdx, "VelocityTex2D", windVelRT);
                    projectionCS.SetTexture(gradientSubtractKernelIdx, "VelocityOutTex2D", windVelPrevRT);
                    projectionCS.Dispatch(gradientSubtractKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                    Graphics.Blit(windVelPrevRT, windVelRT);
                }

                //Advect velocity along previous iteration's velocity field
                advectionCS.SetTexture(advectKernelIdx, "InputTex", windVelRT);
                advectionCS.SetTexture(advectKernelIdx, "OutputTex", windVelPrevRT);
                advectionCS.SetTexture(advectKernelIdx, "VelocityTex", windVelRT);

                advectionCS.Dispatch(advectKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                Graphics.Blit(windVelPrevRT, windVelRT);
                #endregion

                #region Density Step
                //Diffuse Sediment
                diffusionCS.SetFloat("diff", m_DiffusionRate.value);
                for (int j = 0; j < m_DiffuseSteps; j++) {
                    diffusionCS.SetTexture(diffuseKernelIdx, "InputTex", sedimentRT);
                    diffusionCS.SetTexture(diffuseKernelIdx, "OutputTex", sedimentPrevRT);

                    diffusionCS.Dispatch(diffuseKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);
                    Graphics.Blit(sedimentPrevRT, sedimentRT);
                }

                //Advect Sediment
                advectionCS.SetTexture(advectKernelIdx, "InputTexFloat", sedimentRT);
                advectionCS.SetTexture(advectKernelIdx, "OutputTexFloat", sedimentPrevRT);
                advectionCS.SetTexture(advectKernelIdx, "VelocityTex", windVelRT);

                advectionCS.Dispatch(advectKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                #endregion


                #region Erosion Step

                // Erode Sediment (pick sediment up off the heightmap and store in sediment RT)
                aeolianCS.SetTexture(erodeKernelIdx, "InHeightMap", heightmapPrevRT);
                aeolianCS.SetTexture(erodeKernelIdx, "InSediment", sedimentPrevRT);
                aeolianCS.SetTexture(erodeKernelIdx, "WindVel", windVelRT);
                aeolianCS.SetTexture(erodeKernelIdx, "OutSediment", sedimentRT);
                aeolianCS.SetTexture(erodeKernelIdx, "OutHeightMap", heightmapRT);
                aeolianCS.Dispatch(erodeKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);

                #endregion

                #region Thermal / Diffusion
                thermalCS.SetFloat("dt", m_ThermalTimeDelta.value * m_dt.value);
                thermalCS.SetFloat("InvDiagMag", 1.0f / Mathf.Sqrt(dx * dx + dy * dy));
                thermalCS.SetVector("dxdy", new Vector4(dx, dy, 1.0f / dx, 1.0f / dy));
                thermalCS.SetVector("terrainDim", new Vector4(terrainScale.x, terrainScale.y, terrainScale.z));
                thermalCS.SetVector("texDim", new Vector4((float)xRes, (float)yRes, 0.0f, 0.0f));

                thermalCS.SetTexture(thermalKernelIdx, "TerrainHeightPrev", heightmapPrevRT);
                thermalCS.SetTexture(thermalKernelIdx, "TerrainHeight", heightmapRT);
                thermalCS.SetTexture(thermalKernelIdx, "Sediment", thermalSedimentRT);
                thermalCS.SetTexture(thermalKernelIdx, "ReposeMask", collisionRT); //TODO
                thermalCS.SetTexture(thermalKernelIdx, "Collision", collisionRT);
                thermalCS.SetTexture(thermalKernelIdx, "Hardness", collisionRT); //TODO

                Graphics.Blit(heightmapRT, heightmapPrevRT);
                for (int j = 0; j < m_ThermalIterations; j++) {
                    Vector2 m = new Vector2(Mathf.Tan(m_AngleOfRepose * Mathf.Deg2Rad), Mathf.Tan(m_AngleOfRepose * Mathf.Deg2Rad));
                    thermalCS.SetVector("angleOfRepose", new Vector4(m.x, m.y, 0.0f, 0.0f));

                    thermalCS.Dispatch(thermalKernelIdx, xRes / numWorkGroups[0], yRes / numWorkGroups[1], numWorkGroups[2]);
                    Graphics.Blit(heightmapRT, heightmapPrevRT);
                }
                #endregion

                //swap buffers for next iteration
                //Graphics.Blit(heightmapRT, heightmapPrevRT);
                Graphics.Blit(sedimentRT, sedimentPrevRT);

            }
            #endregion

            Graphics.Blit(heightmapRT, outputTextures["Height"]);
            Graphics.Blit(windVelRT, outputTextures["Wind Velocity"]);
            Graphics.Blit(divergenceRT, outputTextures["Divergence"]);
            Graphics.Blit(thermalSedimentRT, outputTextures["Thermal Sediment"]);
            Graphics.Blit(sedimentRT, outputTextures["Sediment"]);

            RenderTexture.ReleaseTemporary(heightmapRT);
            RenderTexture.ReleaseTemporary(heightmapPrevRT);
            RenderTexture.ReleaseTemporary(collisionRT);
            RenderTexture.ReleaseTemporary(sedimentRT);
            RenderTexture.ReleaseTemporary(sedimentPrevRT);
            RenderTexture.ReleaseTemporary(windVelRT);
            RenderTexture.ReleaseTemporary(windVelPrevRT);
            RenderTexture.ReleaseTemporary(divergenceRT);
            RenderTexture.ReleaseTemporary(thermalSedimentRT);

            UnityEngine.RenderTexture.active = tmpRT;
        }

        bool m_ShowControls = true;
        bool m_ShowAdvancedUI = false;
        bool m_ShowThermalUI = false;
        public void OnInspectorGUI() {

            m_ShowControls = TerrainToolGUIHelper.DrawHeaderFoldoutForErosion(Erosion.Styles.m_WindErosionControls, m_ShowControls, ResetSettings);
           
            if (m_ShowControls) {
                EditorGUILayout.BeginVertical("GroupBox");
                m_SimulationScale.DrawInspectorGUI();
                m_WindSpeed.DrawInspectorGUI();
                //m_WindSpeedJitter = (float)EditorGUILayout.IntSlider(Erosion.Styles.m_WindSpeedJitter, (int)m_WindSpeedJitter, 0, 100);

                EditorGUI.indentLevel++;
                m_ShowAdvancedUI = TerrainToolGUIHelper.DrawSimpleFoldout(new GUIContent("Advanced"), m_ShowAdvancedUI);
                if (m_ShowAdvancedUI) {
                    m_dt.DrawInspectorGUI();
                    m_Iterations.DrawInspectorGUI();

                    m_SuspensionRate.DrawInspectorGUI();
                    m_DepositionRate.DrawInspectorGUI();
                    m_SlopeFactor.DrawInspectorGUI();

                    m_AdvectionVelScale.DrawInspectorGUI();
                    m_DragCoefficient.DrawInspectorGUI();
                    m_ReflectionCoefficient.DrawInspectorGUI();
                    m_DiffusionRate.DrawInspectorGUI();
                    m_AbrasivenessCoefficient.DrawInspectorGUI();

                    m_Viscosity.DrawInspectorGUI();
					//m_DiffuseSteps = EditorGUILayout.IntSlider("Diffusion Steps", m_DiffuseSteps, 0, 20);
					//m_ProjectionSteps = EditorGUILayout.IntSlider("Projection Steps", m_ProjectionSteps, 0, 20);
					EditorGUI.indentLevel++;
					m_ShowThermalUI = TerrainToolGUIHelper.DrawSimpleFoldout(new GUIContent("Thermal Smoothing"), m_ShowThermalUI, 1);
                    if (m_ShowThermalUI) {
                        m_ThermalIterations = EditorGUILayout.IntSlider("# Iterations", m_ThermalIterations, 0, 100);
                        m_ThermalTimeDelta.DrawInspectorGUI();
                        m_AngleOfRepose = EditorGUILayout.Slider(Erosion.Styles.m_AngleOfRepose, m_AngleOfRepose, 0.0f, 89.0f);
                    }
                }
                EditorGUILayout.EndVertical();
            }
        }
        public void ResetSettings()
        {
            m_WindSpeed.value = 100.0f;
            m_WindSpeed.minValue = 0.0f;
            m_WindSpeed.maxValue = 500.0f;
            
            m_WindSpeedJitter = 0.0f;

            m_dt.value = 0.001f;
            m_dt.minValue = 0.00001f;
            m_dt.maxValue = 0.05f;
           
            m_WindVel = Vector4.zero; //ToDo: User specified? Flow map?

            m_Iterations.value = 3;
            m_Iterations.minValue = 1;
            m_Iterations.maxValue = 10;

            m_SimulationScale.value = 10.0f;
            m_SimulationScale.minValue = 0.0f;
            m_SimulationScale.maxValue = 100.0f;

            m_DiffusionRate.value = 0.1f;
            m_DiffusionRate.minValue = 0.0f;
            m_DiffusionRate.maxValue = 0.5f;

            m_Viscosity.value = 0.15f;
            m_Viscosity.minValue = 0.0f;
            m_Viscosity.maxValue = 0.5f;
           
            m_ProjectionSteps = 2;

            m_DiffuseSteps = 2;

            m_AdvectionVelScale.value = 10.0f;
            m_AdvectionVelScale.value = 10.0f;
            m_AdvectionVelScale.minValue = 0.0f;
            m_AdvectionVelScale.maxValue = 25.0f;

            m_SuspensionRate.value = 100.0f;
            m_SuspensionRate.minValue = 0.0f;
            m_SuspensionRate.maxValue = 200.0f;

            m_DepositionRate.value = 25.0f;
            m_DepositionRate.minValue = 0.0f;
            m_DepositionRate.maxValue = 200.0f;

            m_SlopeFactor.value = 1.0f;
            m_SlopeFactor.minValue = 0.5f;
            m_SlopeFactor.maxValue = 4.0f;

            m_DragCoefficient.value = 0.5f;
            m_DragCoefficient.minValue = 0.0f;
            m_DragCoefficient.maxValue = 10.0f;

            m_ReflectionCoefficient.value = 5.0f;
            m_ReflectionCoefficient.minValue = 0.0f;
            m_ReflectionCoefficient.maxValue = 10.0f;

            m_AbrasivenessCoefficient.value = 0.0f;
            m_AbrasivenessCoefficient.minValue = 0.0f;
            m_AbrasivenessCoefficient.maxValue = 10.0f;

            m_ThermalTimeDelta.value = 2.0f;
            m_ThermalTimeDelta.minValue = 0.0f;
            m_ThermalTimeDelta.maxValue = 10.0f;

            m_ThermalIterations = 2;
            m_AngleOfRepose = 5.0f;
        }
    
    }
}