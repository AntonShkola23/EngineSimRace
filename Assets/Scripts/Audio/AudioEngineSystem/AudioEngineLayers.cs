using UnityEngine;
using System; // для Math.Tanh

/// <summary>
/// AudioEngineLayers — ФАЗА 2 (финальная версия на сегодня).
/// Генерирует звук на основе данных из AudioEnginePhysics.
/// </summary>
public class AudioEngineLayers
{
    private readonly AudioEnginePhysics physics;

    private float phase = 0f;
    private float noisePhase1 = 0f;
    private float noisePhase2 = 0f;
    private float noisePhase3 = 0f;
    private float airNoisePhase = 0f;
    private float whiteNoisePhase = 0f;

    private float lfoPhase = 0f;
    private float jitterPhase1 = 0f;
    private float jitterPhase2 = 0f;

    private readonly float[] harmonicDetune = new float[16];
    private readonly float[] harmonicPhaseOffset = new float[16];

    private readonly System.Random random = new System.Random();

    public AudioEngineLayers(AudioEnginePhysics physics)
    {
        this.physics = physics;

        for (int i = 0; i < 16; i++)
        {
            harmonicDetune[i] = 1f + (UnityEngine.Random.value * 0.028f - 0.014f);
            harmonicPhaseOffset[i] = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        Debug.Log("[AudioEngineLayers] Финальная версия Фазы 2 загружена");
    }

    public float GetSample()
    {
        if (physics.CurrentRPM < 400f)
            return 0f;

        // Кривой LFO
        lfoPhase += 7.8f * Mathf.PI * 2f / 44100f;
        jitterPhase1 += 3.1f * Mathf.PI * 2f / 44100f;
        jitterPhase2 += 0.85f * Mathf.PI * 2f / 44100f;

        float mainLFO = Mathf.Sin(lfoPhase);
        float jitter1 = (Mathf.PerlinNoise(jitterPhase1 * 1.1f, 0f) * 2f - 1f);
        float jitter2 = (Mathf.PerlinNoise(jitterPhase2 * 0.4f, jitterPhase2 * 0.7f) * 2f - 1f);

        float lfoRaw = mainLFO + jitter1 * 0.35f + jitter2 * 0.18f;
        float lfoStrength = Mathf.Lerp(1.0f, 0.12f, physics.CurrentRPM / 5500f);
        float lfo = lfoRaw * lfoStrength;

        float baseFreq = physics.FiringFrequency * (1f + lfo * 0.019f);
        float volume = 0.88f * (1f + lfo * 0.065f);

        phase += baseFreq * Mathf.PI * 2f / 44100f;
        if (phase > Mathf.PI * 2f) phase -= Mathf.PI * 2f;

        float sample = 0f;

        // Non-linear harmonics — разные гармоники ведут себя по-разному
        for (int i = 0; i < 16; i++)
        {
            float freq = baseFreq * (i + 1) * harmonicDetune[i];
            float ph = phase * (i + 1) + harmonicPhaseOffset[i];
            float amp = 0.72f / (i + 1f);

            if (i >= 6) amp *= 0.48f;
            if (i >= 10) amp *= 0.25f;

            // Non-linear поведение
            if (i == 2 || i == 3 || i == 5) amp *= (1f + physics.LoadFactor * 0.85f);   // низкие гармоники усиливаются под нагрузкой
            if (i >= 8) amp *= (1f - physics.CurrentLoad * 0.35f);                     // высокие гармоники ослабляются под нагрузкой

            sample += Mathf.Sin(ph) * amp;
        }

        // Wave Shaping
        sample = (float)Math.Tanh(sample * (2.2f + physics.EngineStage * 0.55f)) * 0.81f;

        // Dirty Low Layer
        sample += Mathf.Sin(phase * 26f) * 0.028f * physics.CurrentLoad;
        sample += Mathf.Sin(phase * 32f) * 0.024f * physics.CurrentLoad;

        // Многослойный механический шум
        noisePhase1 += 920f * Mathf.PI * 2f / 44100f;
        noisePhase2 += 1450f * Mathf.PI * 2f / 44100f;
        noisePhase3 += 680f * Mathf.PI * 2f / 44100f;

        float noise1 = Mathf.PerlinNoise(noisePhase1 * 0.78f, 0f) * 0.26f;
        float noise2 = Mathf.PerlinNoise(noisePhase2 * 1.35f, noisePhase2 * 0.45f) * 0.14f;
        float noise3 = Mathf.PerlinNoise(noisePhase3 * 0.55f, noisePhase3 * 1.1f) * 0.09f;
        sample += (noise1 + noise2 + noise3) * physics.MechanicalNoiseLevel;

        // Усиленная грязь от load
        float loadDirt = physics.CurrentLoad * physics.CurrentLoad * 1.5f;
        float heavyGrit = Mathf.PerlinNoise(phase * 35f, 0f) * 0.12f * loadDirt;
        sample += heavyGrit;

        // Воздух на верхах (разряженный шум)
        airNoisePhase += 1850f * Mathf.PI * 2f / 44100f;
        float airNoise = Mathf.PerlinNoise(airNoisePhase * 1.4f, airNoisePhase * 0.6f) * 0.068f;
        sample += airNoise * (physics.CurrentRPM / 2800f);

        // Зафильтрованный белый шум (после ~1800 RPM)
        whiteNoisePhase += 2840f * Mathf.PI * 2f / 44100f;
        float whiteNoise = ((float)random.NextDouble() * 2f - 1f) * 0.045f;
        float whiteNoiseActivation = Mathf.Clamp01((physics.CurrentRPM - 1800f) / 3200f);
        sample += whiteNoise * whiteNoiseActivation;

        sample *= volume;

        return sample;
    }
}