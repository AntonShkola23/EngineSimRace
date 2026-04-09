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
    [Range(0.3f, 1.8f)] public float idleVolume = 0.70f;
    [Range(0.3f, 1.8f)] public float fullThrottleVolume = 1.40f;

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

        // ЕЩЁ НИЖЕ — более басовый и "тяжёлый" звук двигателя
        float baseFrequency = Mathf.Lerp(32f, 320f, rpmFactor);

        float volume = Mathf.Lerp(idleVolume, fullThrottleVolume, currentThrottle);

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += baseFrequency * Mathf.PI * 2f / 44100f;
            if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;

            float sample = 0f;

            // 24 гармоники, сдвинутые ниже
            sample += Mathf.Sin(phase) * 0.52f;   // 1
            sample += Mathf.Sin(phase * 1.98f) * 0.31f;   // 2
            sample += Mathf.Sin(phase * 3.02f) * 0.23f;   // 3
            sample += Mathf.Sin(phase * 4.05f) * 0.18f;   // 4
            sample += Mathf.Sin(phase * 5.12f) * 0.145f;  // 5
            sample += Mathf.Sin(phase * 6.25f) * 0.115f;  // 6
            sample += Mathf.Sin(phase * 7.40f) * 0.095f;  // 7
            sample += Mathf.Sin(phase * 8.60f) * 0.080f;  // 8
            sample += Mathf.Sin(phase * 9.95f) * 0.068f;  // 9
            sample += Mathf.Sin(phase * 11.6f) * 0.058f;  // 10
            sample += Mathf.Sin(phase * 13.4f) * 0.050f;  // 11
            sample += Mathf.Sin(phase * 15.3f) * 0.043f;  // 12
            sample += Mathf.Sin(phase * 17.2f) * 0.038f;  // 13
            sample += Mathf.Sin(phase * 19.1f) * 0.033f;  // 14
            sample += Mathf.Sin(phase * 21.2f) * 0.029f;  // 15
            sample += Mathf.Sin(phase * 23.4f) * 0.026f;  // 16
            sample += Mathf.Sin(phase * 25.7f) * 0.023f;  // 17
            sample += Mathf.Sin(phase * 28.1f) * 0.021f;  // 18
            sample += Mathf.Sin(phase * 30.6f) * 0.019f;  // 19
            sample += Mathf.Sin(phase * 33.3f) * 0.017f;  // 20
            sample += Mathf.Sin(phase * 36.2f) * 0.015f;  // 21
            sample += Mathf.Sin(phase * 39.4f) * 0.013f;  // 22
            sample += Mathf.Sin(phase * 42.8f) * 0.012f;  // 23
            sample += Mathf.Sin(phase * 46.5f) * 0.010f;  // 24

            // Дополнительный низкочастотный "грязный" слой
            sample += Mathf.Sin(phase * 52f) * 0.009f * rpmFactor;
            sample += Mathf.Sin(phase * 59f) * 0.008f * rpmFactor;

            // Механический шум (впуск, клапана, механика)
            noisePhase += 680f * Mathf.PI * 2f / 44100f;
            float noise = Mathf.PerlinNoise(noisePhase * 0.85f, 0f) * 0.08f;
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