using UnityEngine;
using VehiclePhysics;

/// <summary>
/// AudioEngineSystem — ГЛАВНЫЙ ДИРИЖЁР всей системы.
/// Единственный MonoBehaviour, который должен висеть на объекте Car Audio.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioEngineSystem : MonoBehaviour
{
    [Header("Engine Preset")]
    [Tooltip("Перетащите сюда пресет двигателя (например VR38DETT)")]
    public AudioEnginePresetSO currentPreset;

    [Header("Debug")]
    [Tooltip("Включить подробное логирование в консоль")]
    public bool enableDebugLogs = true;

    // Модули
    private AudioEnginePhysics physics;
    private AudioEngineLayers layers;           // ← Новый слой генерации звука
    private AudioSource audioSource;

    private float lastLogTime = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;   // 3D звук

        physics = new AudioEnginePhysics();

        // Инициализация физики
        if (currentPreset != null)
        {
            physics.Initialize(currentPreset);
            Debug.Log($"[AudioEngineSystem] Загружен пресет: {currentPreset.EngineName}");
        }
        else
        {
            Debug.LogWarning("[AudioEngineSystem] Preset не назначен. Используются дефолтные значения.");
            physics.InitializeDefault();
        }

        // Создаём слой генерации звука
        layers = new AudioEngineLayers(physics);

        Debug.Log("[AudioEngineSystem] Полностью инициализирован (Фаза 2)");
    }

    private void Update()
    {
        if (physics == null) return;

        VPVehicleController vpp = GetComponentInParent<VPVehicleController>();
        if (vpp == null) return;

        float rpm = vpp.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;
        float throttle = vpp.data.Get(Channel.Input, InputData.Throttle) / 10000f;

        physics.Update(rpm, throttle, 0.7f);

        if (enableDebugLogs && Time.time - lastLogTime > 1f)
        {
            lastLogTime = Time.time;
            Debug.Log($"[Physics] RPM:{physics.CurrentRPM:F0} | Throttle:{physics.CurrentThrottle:F2} | " +
                      $"Firing:{physics.FiringFrequency:F1}Hz | Mech:{physics.MechanicalNoiseLevel:F2} | Stage:{physics.EngineStage}");
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (physics.CurrentRPM < 400f)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0f;
            return;
        }

        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = layers.GetSample();

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
}