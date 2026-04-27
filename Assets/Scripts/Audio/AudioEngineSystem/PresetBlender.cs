using UnityEngine;

/// <summary>
/// PresetBlender — плавный переход между тремя пресетами: Idle → Low → Med
/// </summary>
public class PresetBlender
{
    public EngineRangePreset idlePreset;
    public EngineRangePreset lowPreset;
    public EngineRangePreset medPreset;

    // Зоны переходов
    [Header("Idle → Low")]
    public float idleToLowStart = 1700f;
    public float idleToLowEnd = 2900f;

    [Header("Low → Med")]
    public float lowToMedStart = 3800f;
    public float lowToMedEnd = 4800f;

    public PresetBlender(EngineRangePreset idle, EngineRangePreset low, EngineRangePreset med)
    {
        idlePreset = idle;
        lowPreset = low;
        medPreset = med;
    }

    /// <summary>
    /// Возвращает вес для Low пресета (0..1)
    /// </summary>
    public float GetLowWeight(float rpm)
    {
        if (rpm <= idleToLowStart) return 0f;
        if (rpm >= idleToLowEnd) return 1f;
        float t = Mathf.InverseLerp(idleToLowStart, idleToLowEnd, rpm);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    /// <summary>
    /// Возвращает вес для Med пресета (0..1)
    /// </summary>
    public float GetMedWeight(float rpm)
    {
        if (rpm <= lowToMedStart) return 0f;
        if (rpm >= lowToMedEnd) return 1f;
        float t = Mathf.InverseLerp(lowToMedStart, lowToMedEnd, rpm);
        return Mathf.SmoothStep(0f, 1f, t);
    }

    /// <summary>
    /// Универсальный Lerp между тремя пресетами
    /// </summary>
    public float LerpParam(float idleValue, float lowValue, float medValue, float currentRPM)
    {
        float lowWeight = GetLowWeight(currentRPM);
        float medWeight = GetMedWeight(currentRPM);

        // Сначала blend Idle → Low, потом Low → Med
        float valueLow = Mathf.Lerp(idleValue, lowValue, lowWeight);
        return Mathf.Lerp(valueLow, medValue, medWeight);
    }
}