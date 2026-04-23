using UnityEngine;
using VehiclePhysics;

[RequireComponent(typeof(AudioSource))]
public class AudioEngineSystem : MonoBehaviour
{
    [Header("Engine Preset")]
    public AudioEnginePresetSO currentPreset;

    [Header("Debug")]
    public bool DebugLogs = true;

    [Header("Layer Control")]
    public bool MainHarmonics = true;

    public bool AdditionalLayers = true;                    // ← выключает все добавочные слои сразу
    public bool LowBodyLayer = true;
    public bool MechanicalNoise = true;
    public bool WhiteNoise = true;

    [Header("Layer Volumes")]
    [Range(0f, 2f)] public float LayersMaster= 1f;   // ← мастер-ползунок
    [Range(0f, 2f)] public float lowBody = 1f;
    [Range(0f, 2f)] public float mechanicalNoise = 1f;
    [Range(0f, 2f)] public float whiteNoise = 1f;
    

    private AudioEnginePhysics physics;
    private AudioEngineLayers layers;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;

        physics = new AudioEnginePhysics();

        if (currentPreset != null)
            physics.Initialize(currentPreset);
        else
            physics.InitializeDefault();

        layers = new AudioEngineLayers(physics);
    }

    private void Update()
    {
        if (physics == null) return;

        VPVehicleController vpp = GetComponentInParent<VPVehicleController>();
        if (vpp == null) return;

        float rpm = vpp.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;
        float throttle = vpp.data.Get(Channel.Input, InputData.Throttle) / 10000f;
        float load = throttle;

        physics.Update(rpm, throttle, load); // передача данных в physics

        if (rpm > 400f && !audioSource.isPlaying)
            audioSource.Play();
        else if (rpm < 400f && audioSource.isPlaying)
            audioSource.Stop();
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (layers == null) return;

        for (int i = 0; i < data.Length; i += channels)
        {
            float sample = layers.GetSample(
                MainHarmonics,
                AdditionalLayers,
                LowBodyLayer,
                MechanicalNoise,
                WhiteNoise,
                lowBody,
                mechanicalNoise,
                whiteNoise,
                LayersMaster); // запрос в AEL на звук

            for (int c = 0; c < channels; c++)
                data[i + c] = sample;
        }
    }
}