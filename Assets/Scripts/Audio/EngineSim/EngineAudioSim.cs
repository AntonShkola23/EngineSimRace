using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))]
public class EngineAudioSim : MonoBehaviour
{
    [Header("VPP References")]
    public VPVehicleController vehicleController;

    [Header("Engine Settings")]
    [Tooltip("Количество цилиндров (влияет на характер звука)")]
    public int cylinders = 6;

    [Tooltip("Диапазон оборотов двигателя")]
    public float minRPM = 700f;
    public float maxRPM = 7800f;

    [Tooltip("Громкость на холостых и на полном газу")]
    [Range(0.3f, 1.8f)] public float idleVolume = 0.68f;
    [Range(0.3f, 1.8f)] public float fullThrottleVolume = 1.38f;

    private AudioSource audioSource;
    private float currentRPM = 0f;
    private float currentThrottle = 0f;

    private float phase = 0f;
    private float noisePhase = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    void Start()
    {
        if (vehicleController == null)
            vehicleController = GetComponentInParent<VPVehicleController>();

        if (vehicleController == null)
            Debug.LogError("EngineAudioSim: VPVehicleController не найден!");
    }

    void Update()
    {
        if (vehicleController == null) return;

        currentRPM = vehicleController.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;
        currentThrottle = vehicleController.data.Get(Channel.Input, InputData.Throttle) / 10000f;

        bool engineRunning = currentRPM > 400f;

        if (engineRunning && !audioSource.isPlaying)
            audioSource.Play();
        else if (!engineRunning && audioSource.isPlaying)
            audioSource.Stop();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentRPM < 400f)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0f;
            return;
        }

        float rpmFactor = Mathf.InverseLerp(minRPM, maxRPM, currentRPM);

        // ЕЩЁ НИЖЕ основная частота — более басовый и тяжёлый звук
        float baseFrequency = Mathf.Lerp(38f, 380f, rpmFactor);

        float volume = Mathf.Lerp(idleVolume, fullThrottleVolume, currentThrottle);

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += baseFrequency * Mathf.PI * 2f / 44100f;
            if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;

            float sample = 0f;

            // === 24 ГАРМОНИКИ (в 2 раза больше чем раньше) ===
            sample += Mathf.Sin(phase) * 0.50f;   // 1
            sample += Mathf.Sin(phase * 2.00f) * 0.29f;   // 2
            sample += Mathf.Sin(phase * 3.05f) * 0.21f;   // 3
            sample += Mathf.Sin(phase * 4.10f) * 0.16f;   // 4
            sample += Mathf.Sin(phase * 5.20f) * 0.13f;   // 5
            sample += Mathf.Sin(phase * 6.35f) * 0.105f;  // 6
            sample += Mathf.Sin(phase * 7.50f) * 0.085f;  // 7
            sample += Mathf.Sin(phase * 8.70f) * 0.072f;  // 8
            sample += Mathf.Sin(phase * 10.1f) * 0.061f;  // 9
            sample += Mathf.Sin(phase * 11.8f) * 0.052f;  // 10
            sample += Mathf.Sin(phase * 13.6f) * 0.045f;  // 11
            sample += Mathf.Sin(phase * 15.5f) * 0.039f;  // 12
            sample += Mathf.Sin(phase * 17.4f) * 0.034f;  // 13
            sample += Mathf.Sin(phase * 19.4f) * 0.029f;  // 14
            sample += Mathf.Sin(phase * 21.6f) * 0.026f;  // 15
            sample += Mathf.Sin(phase * 23.9f) * 0.023f;  // 16
            sample += Mathf.Sin(phase * 26.3f) * 0.020f;  // 17
            sample += Mathf.Sin(phase * 28.8f) * 0.018f;  // 18
            sample += Mathf.Sin(phase * 31.5f) * 0.016f;  // 19
            sample += Mathf.Sin(phase * 34.4f) * 0.014f;  // 20
            sample += Mathf.Sin(phase * 37.5f) * 0.012f;  // 21
            sample += Mathf.Sin(phase * 40.8f) * 0.011f;  // 22
            sample += Mathf.Sin(phase * 44.3f) * 0.010f;  // 23
            sample += Mathf.Sin(phase * 48.1f) * 0.009f;  // 24

            // Дополнительный слой высоких гармоник (имитация сотен мелких)
            sample += Mathf.Sin(phase * 55f) * 0.008f * rpmFactor;
            sample += Mathf.Sin(phase * 63f) * 0.007f * rpmFactor;

            // Механический шум (впуск + клапана)
            noisePhase += 720f * Mathf.PI * 2f / 44100f;
            float noise = Mathf.PerlinNoise(noisePhase * 0.9f, 0f) * 0.075f;
            sample += noise * rpmFactor;

            // Применяем громкость
            sample *= volume;

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
}