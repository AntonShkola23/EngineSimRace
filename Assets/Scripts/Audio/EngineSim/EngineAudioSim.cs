using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))]
public class EngineAudioSim : MonoBehaviour
{
    [Header("VPP References")]
    public VPVehicleController vehicleController;

    [Header("Engine Simulator Settings")]
    public float minRPM = 900f;
    public float maxRPM = 7800f;

    private AudioSource audioSource;
    private float currentRPM = 0f;
    private float currentThrottle = 0f;

    private float phase = 0f;

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

        // Получаем RPM (делим на 1000, как рекомендует документация VPP CE)
        float rawRPM = vehicleController.data.Get(Channel.Vehicle, VehicleData.EngineRpm);
        currentRPM = rawRPM / 1000f;

        currentThrottle = vehicleController.data.Get(Channel.Input, InputData.Throttle) / 10000f;

        // Простая логика: двигатель "работает", если RPM > 400
        bool engineRunning = currentRPM > 400f;

        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"RPM: {currentRPM:F0} | Throttle: {currentThrottle:F2} | Running: {engineRunning}");
        }

        // Управление AudioSource
        if (engineRunning && !audioSource.isPlaying)
            audioSource.Play();
        else if (!engineRunning && audioSource.isPlaying)
            audioSource.Stop();
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        // Если двигатель не работает — полностью тишина
        if (vehicleController == null || currentRPM < 400f)
        {
            for (int i = 0; i < data.Length; i++)
                data[i] = 0f;
            return;
        }

        float clampedRPM = Mathf.Clamp(currentRPM, minRPM, maxRPM);
        float frequency = Mathf.Lerp(75f, 620f, Mathf.InverseLerp(minRPM, maxRPM, clampedRPM));

        for (int i = 0; i < data.Length; i += channels)
        {
            phase += frequency * Mathf.PI * 2f / 44100f;
            if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;

            float sample = Mathf.Sin(phase) * 0.26f;

            // Вторая гармоника + шум
            sample += Mathf.Sin(phase * 2.05f) * 0.085f;
            sample += Mathf.Sin(phase * 8.7f) * 0.013f * (clampedRPM / maxRPM);

            float volume = Mathf.Lerp(0.55f, 1.45f, currentThrottle);

            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample * volume;
            }
        }
    }
}