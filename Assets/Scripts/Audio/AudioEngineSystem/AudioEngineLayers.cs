using UnityEngine;
using System; // для Math.Tanh

/// <summary>
/// AudioEngineLayers — точное повторение пресета "ESNS Main Sinus.vital"
/// + LIVE-регулировка частоты 1-й гармоники прямо в Play Mode
/// </summary>
public class AudioEngineLayers
{
    private readonly AudioEnginePhysics physics;

    private float phase1 = 0f;   // OSC1
    private float phase2 = 0f;   // OSC2 (+2 semitones)
    private float lfoPhase = 0f;
    private float lfoRate = 0.8f;
    private float lfoRateTimer = 0f;
    private float randomPhase = 0f;

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
        Debug.Log("[AudioEngineLayers] Готов к live-регулировке 1-й гармоники в Play Mode");
    }

    /// <summary>
    /// Новый GetSample с параметрами из AudioEngineSystem (для ползунков в Play Mode)
    /// </summary>
    public float GetSample(float fundamentalMultiplier, float fundamentalOffsetHz)
    {
        if (physics.CurrentRPM < 400f) return 0f;

        // Rand-модуляция LFO (как в Vital)
        lfoRateTimer += 1f / 44100f;
        if (lfoRateTimer > 0.8f)
        {
            lfoRateTimer = 0f;
            lfoRate = 0.8f + (float)random.NextDouble() * 1.2f - 0.6f;
        }

        lfoPhase += lfoRate * Mathf.PI * 2f / 44100f;
        float lfo = Mathf.Sin(lfoPhase) * 0.5f + 0.5f;

        randomPhase += 0.0008f;
        float randMod = Mathf.PerlinNoise(randomPhase * 1.3f, 0f) * 0.3f - 0.15f;

        float baseFreq = physics.FiringFrequency * (1f + lfo * 0.018f + randMod * 0.012f);

        phase1 += baseFreq * Mathf.PI * 2f / 44100f;
        phase2 += baseFreq * 1.122462f * Mathf.PI * 2f / 44100f;

        if (phase1 > Mathf.PI * 2f) phase1 -= Mathf.PI * 2f;
        if (phase2 > Mathf.PI * 2f) phase2 -= Mathf.PI * 2f;

        float sample = 0f;

        for (int i = 0; i < 16; i++)
        {
            float amp = 0.72f / (i + 1f) * (i < 8 ? 1.35f : 0.65f);

            float freq1 = baseFreq * (i + 1) * harmonicDetune[i];
            float freq2 = freq1 * 1.122462f;

            // ← Здесь применяется live-регулировка 1-й гармоники
            if (i == 0)
            {
                freq1 = baseFreq * fundamentalMultiplier * harmonicDetune[0] + fundamentalOffsetHz;
                freq2 = freq1 * 1.122462f;
            }

            float ph1 = phase1 * (i + 1) + harmonicPhaseOffset[i];
            float ph2 = phase2 * (i + 1) + harmonicPhaseOffset[i] * 0.7f;

            sample += Mathf.Sin(ph1) * amp * 0.707f;   // OSC1
            sample += Mathf.Sin(ph2) * amp * 0.191f;   // OSC2
        }

        // Wave Shaping
        sample = (float)Math.Tanh(sample * 2.4f) * 0.82f;

        // Минимальная грязь
        //sample += Mathf.PerlinNoise(phase1 * 28f, 0f) * 0.035f * physics.CurrentLoad;

        sample *= 0.92f;

        return sample;
    }
}