using UnityEngine;

/// <summary>
/// AudioEnginePhysics — ЧИСТОЕ ФИЗИЧЕСКОЕ ЯДРО системы (Фаза 1).
/// Здесь рассчитывается вся физика двигателя. Звук здесь НЕ генерируется.
/// </summary>
public class AudioEnginePhysics
{
    // ================================================================
    // 1. БАЗОВЫЕ ХАРАКТЕРИСТИКИ (из пресета)
    // ================================================================
    public int CylinderCount { get; private set; }
    public float DisplacementLiters { get; private set; }
    public bool IsTurbocharged { get; private set; }
    public int EngineStage { get; private set; }     // 0 = stock, 1-5 = тюнинг

    // ================================================================
    // 2. ТЕКУЩИЕ ДИНАМИЧЕСКИЕ ПАРАМЕТРЫ
    // ================================================================
    public float CurrentRPM { get; private set; }
    public float CurrentThrottle { get; private set; }
    public float CurrentLoad { get; private set; }     // 0..1

    // ================================================================
    // 3. 8 КЛЮЧЕВЫХ ФИЗИЧЕСКИХ ЭЛЕМЕНТОВ
    // ================================================================
    public float FiringFrequency { get; private set; }
    public float[] CylinderPhaseOffsets { get; private set; }
    public float IntakePulseStrength { get; private set; }
    public float ExhaustPulseStrength { get; private set; }
    public float MechanicalNoiseLevel { get; private set; }
    public float LoadFactor { get; private set; }
    public float ManifoldResonance { get; private set; }
    public float TurboSpool { get; private set; }

    // Дополнительные параметры для будущего использования в AudioEngineLayers
    public float AirFlowVelocity { get; private set; }
    public float ExhaustGasVelocity { get; private set; }

    private float[] tempPhaseOffsets;

    // ================================================================
    // ИНИЦИАЛИЗАЦИЯ
    // ================================================================

    public void InitializeDefault()
    {
        CylinderCount = 6;
        DisplacementLiters = 3.8f;
        IsTurbocharged = true;
        EngineStage = 0;

        tempPhaseOffsets = new float[CylinderCount];

        Debug.Log("[AudioEnginePhysics] Initialized with DEFAULT values (VR38DETT-like)");
    }

    // ================================================================
    // ОБНОВЛЕНИЕ КАЖДЫЙ КАДР
    // ================================================================
    public void Update(float rpm, float throttle, float load)
    {
        CurrentRPM = Mathf.Clamp(rpm, 0f, 20000f);
        CurrentThrottle = Mathf.Clamp01(throttle);
        CurrentLoad = Mathf.Clamp01(load);

        CalculateFiringFrequency();
        CalculateCylinderPhaseOffsets();
        CalculateIntakeExhaustPulses();
        CalculateMechanicalNoise();
        CalculateLoadFactor();
        CalculateManifoldResonance();
        CalculateTurboSpool();
        CalculateAirAndExhaustFlow();

        // Логирование (раз в секунду)
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($" RPM: {CurrentRPM:F0} | Throttle: {CurrentThrottle:F2} | Load: {CurrentLoad:F2} |");
                      
        }
    }

    // ================================================================
    // РАСЧЁТЫ (улучшенные формулы)
    // ================================================================

    private void CalculateFiringFrequency()
    {
        FiringFrequency = (CurrentRPM / 60f) * (CylinderCount / 2f);
    }

    private void CalculateCylinderPhaseOffsets()
    {
        float step = 360f / CylinderCount;
        for (int i = 0; i < CylinderCount; i++)
        {
            tempPhaseOffsets[i] = i * step;
        }
        CylinderPhaseOffsets = tempPhaseOffsets;
    }

    private void CalculateIntakeExhaustPulses()
    {
        IntakePulseStrength = CurrentThrottle * 1.1f + 0.3f;
        ExhaustPulseStrength = CurrentLoad * 1.35f + (EngineStage * 0.28f);
    }

    private void CalculateMechanicalNoise()
    {
        // Сильнее реагирует на RPM и тюнинг
        MechanicalNoiseLevel = (CurrentRPM / 7500f) * (0.55f + EngineStage * 0.22f);
    }

    private void CalculateLoadFactor()
    {
        LoadFactor = CurrentLoad * (0.4f + CurrentThrottle * 0.6f);
    }

    private void CalculateManifoldResonance()
    {
        ManifoldResonance = Mathf.Lerp(0.35f, 1.25f, CurrentRPM / 7000f) * (1f + EngineStage * 0.3f);
    }

    private void CalculateTurboSpool()
    {
        TurboSpool = IsTurbocharged
            ? Mathf.Clamp01((CurrentRPM - 1650f) / 5100f)
            : 0f;
    }

    private void CalculateAirAndExhaustFlow()
    {
        AirFlowVelocity = CurrentRPM * 0.0009f * (1f + EngineStage * 0.2f);
        ExhaustGasVelocity = CurrentRPM * 0.00145f * (1f + EngineStage * 0.3f);
    }
}