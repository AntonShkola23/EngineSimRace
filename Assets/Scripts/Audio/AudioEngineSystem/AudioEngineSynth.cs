using System;
using UnityEngine;
using VehiclePhysics;

/// <summary>
/// EngineAudioVitalSim — новый чистый скрипт.
/// Точное повторение пресета "ESNS Main Sinus.vital" + данные из VPP.
/// Без старых наработок.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioEngineSynth : MonoBehaviour
{
    [Header("VPP References")]
    public VPVehicleController vehicleController;

    [Header("Engine Settings")]
    public float minRPM = 700f;
    public float maxRPM = 7800f;

    private AudioSource audioSource;

    // Фазы осцилляторов
    private float phase1 = 0f;   // OSC1
    private float phase2 = 0f;   // OSC2 (+2 semitones)

    // LFO Growing Oscillations
    private float lfoPhase = 0f;
    private float lfoRate = 0.8f;        // базовая скорость
    private float lfoRateTimer = 0f;

    // Random modulation
    private float randomPhase = 0f;

    // Unison 8 voices simulation
    private readonly float[] unisonDetune = new float[8];
    private readonly float[] unisonPhaseOffset = new float[8];

    private readonly System.Random random = new System.Random();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D звук
    }

    private void Start()
    {
        if (vehicleController == null)
            vehicleController = GetComponentInParent<VPVehicleController>();

        // Инициализация unison
        for (int i = 0; i < 8; i++)
        {
            unisonDetune[i] = 1f + (UnityEngine.Random.value * 0.035f - 0.0175f);
            unisonPhaseOffset[i] = UnityEngine.Random.value * Mathf.PI * 2f;
        }
    }

    private void Update()
    {
        if (vehicleController == null) return;

        float currentRPM = vehicleController.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;

        if (currentRPM > 400f && !audioSource.isPlaying)
            audioSource.Play();
        else if (currentRPM < 400f && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float currentRPM = 0f;
        if (vehicleController != null)
            currentRPM = vehicleController.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;

        if (currentRPM < 400f)
        {
            for (int i = 0; i < data.Length; i++) data[i] = 0f;
            return;
        }

        // Rand-модуляция скорости LFO (как в Vital)
        lfoRateTimer += 1f / 44100f;
        if (lfoRateTimer > 0.8f)
        {
            lfoRateTimer = 0f;
            lfoRate = 0.8f + (float)random.NextDouble() * 1.2f - 0.6f;
        }

        lfoPhase += lfoRate * Mathf.PI * 2f / 44100f;
        float lfo = Mathf.Sin(lfoPhase) * 0.5f + 0.5f; // Growing Oscillations

        randomPhase += 0.00085f;
        float randMod = Mathf.PerlinNoise(randomPhase * 1.35f, 0f) * 0.32f - 0.16f;

        float baseFreq = currentRPM * 0.5f * (1f + lfo * 0.022f + randMod * 0.015f); // базовая частота от RPM

        phase1 += baseFreq * Mathf.PI * 2f / 44100f;
        phase2 += baseFreq * 1.122462f * Mathf.PI * 2f / 44100f; // +2 semitones

        if (phase1 > Mathf.PI * 2f) phase1 -= Mathf.PI * 2f;
        if (phase2 > Mathf.PI * 2f) phase2 -= Mathf.PI * 2f;

        float sample = 0f;

        // OSC1 + OSC2 с 8-voice unison
        for (int i = 0; i < 16; i++) // 16 гармоник для точного заполнения спектра
        {
            float amp = 0.72f / (i + 1f);

            // Unison 8 voices для OSC1
            for (int u = 0; u < 8; u++)
            {
                float freq1 = baseFreq * (i + 1) * unisonDetune[u];
                float ph1 = phase1 * (i + 1) + unisonPhaseOffset[u];
                sample += Mathf.Sin(ph1) * amp * 0.707f; // уровень OSC1
            }

            // Unison 8 voices для OSC2
            for (int u = 0; u < 8; u++)
            {
                float freq2 = baseFreq * 1.122462f * (i + 1) * unisonDetune[u];
                float ph2 = phase2 * (i + 1) + unisonPhaseOffset[u] * 0.7f;
                sample += Mathf.Sin(ph2) * amp * 0.191f; // уровень OSC2
            }
        }

        // Wave Shaping (как в пресете)
        sample = (float)Math.Tanh(sample * 2.4f) * 0.82f;

        // Финальная громкость
        sample *= 0.92f;

        for (int i = 0; i < data.Length; i += channels)
        {
            for (int c = 0; c < channels; c++)
            {
                data[i + c] = sample;
            }
        }
    }
}