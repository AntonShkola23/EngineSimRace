using CrankcaseAudio.Unity;
using UnityEngine;
using VehiclePhysics;

/// <summary>
/// Feeds Vehicle Physics Pro telemetry into Crankcase REV (REVEnginePlayer).
/// Attach to a child of the vehicle (or any object under VPVehicleController)
/// together with AudioSource + REVEnginePlayer.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(REVEnginePlayer))]
public class VppRevBridge : MonoBehaviour
{
    [Header("VPP")]
    [Tooltip("If empty, resolved from parents at runtime.")]
    public VPVehicleController vehicle;

    [Header("REV Model")]
    [Tooltip("Engine model (.bytes). If empty, loads Resources path below.")]
    public TextAsset modelData;

    [Tooltip("Used only when modelData is not assigned. Path under Resources without extension.")]
    public string resourcesModelPath = "engines/48000/Camaro_SS";

    [Header("RPM mapping (VPP absolute → REV 0..1)")]
    [Tooltip("RPM treated as 0.0 for REV (idle).")]
    public float idleRpm = 800f;

    [Tooltip("RPM treated as 1.0 for REV (limiter / redline of the model).")]
    public float maxRpm = 6500f;

    [Header("Output")]
    [Range(0f, 4f)] public float volume = 1f;
    [Range(0.1f, 2f)] public float pitch = 1f;

    [Header("Behaviour")]
    [Tooltip("Play AudioSource + StartEngine when RPM is above this.")]
    public float runningRpmThreshold = 200f;

    [Tooltip("Log mapped values ~once per second.")]
    public bool debugLogs = true;

    REVEnginePlayer rev;
    AudioSource audioSource;
    int lastGear = 1;
    bool modelLoaded;

    void Awake()
    {
        rev = GetComponent<REVEnginePlayer>();
        audioSource = GetComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        // 3D positional engine; set 0 for full 2D if preferred
        if (audioSource.spatialBlend < 0.01f)
            audioSource.spatialBlend = 1f;

        if (vehicle == null)
            vehicle = GetComponentInParent<VPVehicleController>();
    }

    void Start()
    {
        if (vehicle == null)
        {
            Debug.LogError("[VppRevBridge] VPVehicleController not found. Assign vehicle or place under a VPP vehicle.");
            enabled = false;
            return;
        }

        EnsureModelLoaded();
        rev.Volume = volume;
        rev.Pitch = pitch;
    }

    void EnsureModelLoaded()
    {
        if (modelLoaded || rev.State != REVEnginePlayer.eState.UnInitialized)
        {
            modelLoaded = true;
            return;
        }

        byte[] bytes = null;

        if (modelData != null)
        {
            // Prefer explicit assignment; also set public field so REVEnginePlayer.Start can use it if order differs
            rev.modelData = modelData;
            bytes = modelData.bytes;
        }
        else if (!string.IsNullOrEmpty(resourcesModelPath))
        {
            var asset = Resources.Load<TextAsset>(resourcesModelPath);
            if (asset == null)
            {
                Debug.LogError("[VppRevBridge] Model not found at Resources/" + resourcesModelPath);
                enabled = false;
                return;
            }
            rev.modelData = asset;
            bytes = asset.bytes;
        }

        if (bytes == null || bytes.Length == 0)
        {
            Debug.LogError("[VppRevBridge] No REV model data.");
            enabled = false;
            return;
        }

        // If Start already ran on REVEnginePlayer with modelData set, state may already be Initialized/Running.
        if (rev.State == REVEnginePlayer.eState.UnInitialized)
        {
            rev.LoadModelFileData(bytes);
            rev.StartEngine();
        }

        modelLoaded = true;
        Debug.Log("[VppRevBridge] REV model loaded. Version=" + REVEnginePlayer.VERSION);
    }

    void Update()
    {
        if (vehicle == null || rev == null)
            return;

        if (rev.State == REVEnginePlayer.eState.UnInitialized)
            EnsureModelLoaded();

        if (rev.State == REVEnginePlayer.eState.UnInitialized)
            return;

        float rpmAbs = vehicle.data.Get(Channel.Vehicle, VehicleData.EngineRpm) / 1000f;
        float throttle = vehicle.data.Get(Channel.Input, InputData.Throttle) / 10000f;
        float speedMs = vehicle.data.Get(Channel.Vehicle, VehicleData.Speed) / 1000f;
        float speedKph = Mathf.Max(0f, speedMs * 3.6f);

        int gearId = vehicle.data.Get(Channel.Vehicle, VehicleData.GearboxGear);
        // REV: gears 1..6 only (no N/R). Hold last forward gear in N/R.
        if (gearId >= 1)
            lastGear = Mathf.Clamp(gearId, 1, 6);

        float rpmNorm = Mathf.InverseLerp(idleRpm, maxRpm, rpmAbs);
        rpmNorm = Mathf.Clamp01(rpmNorm);

        rev.Throttle = Mathf.Clamp01(throttle);
        rev.Rpm = rpmNorm;
        rev.Velocity = speedKph;
        rev.Gear = lastGear;
        rev.Volume = volume;
        rev.Pitch = pitch;

        bool running = rpmAbs > runningRpmThreshold;
        if (running)
        {
            if (rev.State == REVEnginePlayer.eState.Paused || rev.State == REVEnginePlayer.eState.Initialized)
                rev.StartEngine();

            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        if (debugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log(
                $"[VppRevBridge] RPM={rpmAbs:F0} ({rpmNorm:F2}) thr={throttle:F2} " +
                $"gear={lastGear} (raw {gearId}) v={speedKph:F0} kph state={rev.State}");
        }
    }
}
