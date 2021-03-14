using UnityEngine;
using System;

namespace Erosion {
    [Serializable]
    public class HydraulicErosionSettings {
        [SerializeField]
        public int m_AddHeightAmt;

        [SerializeField]
        public TerrainFloatMinMaxValue m_HydroTimeDelta = new TerrainFloatMinMaxValue(Erosion.Styles.m_TimeDelta, 0.3f, 0.0f, 0.1f);
        [SerializeField]
        public TerrainIntMinMaxValue m_HydroIterations = new TerrainIntMinMaxValue(Erosion.Styles.m_NumIterations, 100, 1, 500);
        [SerializeField]
        public int m_HydroLowResIterations;
        [SerializeField]
        public float m_GravitationalConstant;

        [SerializeField]
        public TerrainFloatMinMaxValue m_SimScale = new TerrainFloatMinMaxValue(Erosion.Styles.m_SimulationScale, 1.0f, 0.0f, 100.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_IterationBlendScalar = new TerrainFloatMinMaxValue(Erosion.Styles.m_IterationBlendScalar, 1.0f, -1.0f, 1.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_PrecipRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_PrecipitationRate, 0.4f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_EvaporationRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_EvaporationRate, 0.4f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_FlowRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_FlowRate, 0.4f, 0.0f, 10.0f);

        [SerializeField]
        public float m_SedimentScale;
        [SerializeField]
        public float m_WaterLevelScale;

        [SerializeField]
        public TerrainFloatMinMaxValue m_SedimentCapacity = new TerrainFloatMinMaxValue(Erosion.Styles.m_SedimentCap, 0.42f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_SedimentDissolveRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_SedimentDissolve, 0.51f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_SedimentDepositRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_SedimentDeposit, 0.4f, 0.0f, 10.0f);

        [SerializeField]
        public TerrainFloatMinMaxValue m_RiverBankDissolveRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_RiverbankDissolve, 2.0f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_RiverBankDepositRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_RiverbankDeposit, 8.0f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_RiverBedDissolveRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_RiverbedDissolve, 8.0f, 0.0f, 10.0f);
        [SerializeField]
        public TerrainFloatMinMaxValue m_RiverBedDepositRate = new TerrainFloatMinMaxValue(Erosion.Styles.m_RiverbedDeposit, 2.0f, 0.0f, 10.0f);

        [SerializeField]
        public bool m_DoThermal;
        [SerializeField]
        public int m_ThermalIterations;
        [SerializeField]
        public float m_ThermalTimeDelta;
        [SerializeField]
        public int m_ThermalReposeAngle;
        [SerializeField]
        public Vector2 m_AngleOfRepose;

        [SerializeField]
        public TerrainFloatMinMaxValue m_MaterialSpread = new TerrainFloatMinMaxValue(Erosion.Styles.m_MaterialSpread, 0.5f, 0.0f, 1.0f);
        [SerializeField]
        public bool m_AffectHeight;
        [SerializeField]
        public bool m_AffectMaterial;

        public enum MaskSource {
            Sediment = 0,
            HeightDiff = 1,
            WaterFlux = 2,
            WaterLevel = 3,
            WaterSpeed = 4
        }

        [SerializeField]
        public MaskSource m_MaskSourceSelection = MaskSource.Sediment;

        [SerializeField]
        public float m_MaterialOpacity;

        public HydraulicErosionSettings() { Reset(); }

        public void Reset() {
            m_AddHeightAmt = 25;

            m_AngleOfRepose = new Vector2(35.0f, 35.0f);

            m_HydroTimeDelta.value = 0.05f;
            m_HydroTimeDelta.minValue = 0.0f;
            m_HydroTimeDelta.maxValue = 0.1f;

            m_HydroLowResIterations = 120;

            m_HydroIterations.value = 100;
            m_HydroIterations.minValue = 1;
            m_HydroIterations.maxValue = 500;

            m_GravitationalConstant = -9.8f;

            m_SimScale.value = 25.0f;
            m_SimScale.minValue = 0.00001f;
            m_SimScale.maxValue = 100.0f;

            m_IterationBlendScalar.value = 1.0f;
            m_IterationBlendScalar.minValue = -1.0f;
            m_IterationBlendScalar.maxValue = 1.0f;

            m_PrecipRate.value = 0.4f;
            m_PrecipRate.minValue = 0.0f;
            m_PrecipRate.maxValue = 1.0f;

            m_EvaporationRate.value = 0.4f;
            m_EvaporationRate.minValue = 0.0f;
            m_EvaporationRate.maxValue = 1.0f;

            m_FlowRate.value = 0.5f;
            m_FlowRate.minValue = 0.0f;
            m_FlowRate.maxValue = 1.0f;

            m_SedimentCapacity.value = 0.42f;
            m_SedimentCapacity.minValue = 0.0f;
            m_SedimentCapacity.maxValue = 1.0f;
            
            m_SedimentDissolveRate.value = 0.51f;
            m_SedimentDissolveRate.minValue = 0.0f;
            m_SedimentDissolveRate.maxValue = 1.0f;

            m_SedimentDepositRate.value = 0.40f;
            m_SedimentDepositRate.minValue = 0.0f;
            m_SedimentDepositRate.maxValue = 1.0f;

            m_RiverBankDissolveRate.value = 5.0f;
            m_RiverBankDissolveRate.minValue = 0.0f;
            m_RiverBankDissolveRate.maxValue = 10.0f;

            m_RiverBankDepositRate.value = 1.0f;
            m_RiverBankDepositRate.minValue = 0.0f;
            m_RiverBankDepositRate.maxValue = 10.0f;

            m_RiverBedDissolveRate.value = 1.0f;
            m_RiverBedDissolveRate.minValue = 0.0f;
            m_RiverBedDissolveRate.maxValue = 10.0f;

            m_RiverBedDepositRate.value = 5.0f;
            m_RiverBedDepositRate.minValue = 0.0f;
            m_RiverBedDepositRate.maxValue = 10.0f;

            m_DoThermal = true;
            m_ThermalIterations = 3;
            m_ThermalTimeDelta = 0.005f;
            m_ThermalReposeAngle = 85;

            m_AffectHeight = true;
            m_AffectMaterial = false;

            m_MaterialSpread.value = 0.5f;
            m_MaterialSpread.minValue = 0.0f;
            m_MaterialSpread.maxValue = 1.0f;

            m_MaskSourceSelection = 0;
            m_MaterialOpacity = 1.0f;
            m_SedimentScale = 1.0f;
            m_WaterLevelScale = 1.0f;
        }
    }
}