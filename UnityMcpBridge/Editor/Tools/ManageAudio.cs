using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles Audio System operations including AudioSource, AudioClip, Audio Mixer, 
    /// and 3D spatial audio settings.
    /// </summary>
    public static class ManageAudio
    {
        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString().ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            try
            {
                switch (action)
                {
                    case "add_audio_source":
                        return AddAudioSource(@params);
                    case "modify_audio_source":
                        return ModifyAudioSource(@params);
                    case "play_audio":
                        return PlayAudio(@params);
                    case "stop_audio":
                        return StopAudio(@params);
                    case "pause_audio":
                        return PauseAudio(@params);
                    case "create_audio_mixer":
                        return CreateAudioMixer(@params);
                    case "modify_audio_mixer":
                        return ModifyAudioMixer(@params);
                    case "set_3d_audio_settings":
                        return Set3DAudioSettings(@params);
                    case "get_audio_info":
                        return GetAudioInfo(@params);
                    case "import_audio_clip":
                        return ImportAudioClip(@params);
                    case "create_audio_listener":
                        return CreateAudioListener(@params);
                    case "set_audio_reverb_zone":
                        return SetAudioReverbZone(@params);
                    default:
                        return Response.Error($"Unknown audio action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageAudio] Action '{action}' failed: {e}");
                return Response.Error($"Internal error processing audio action '{action}': {e.Message}");
            }
        }

        private static object AddAudioSource(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                AudioSource audioSource = targetObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = targetObject.AddComponent<AudioSource>();
                }

                // Set audio clip if specified
                if (@params["audio_clip_path"] != null)
                {
                    string clipPath = @params["audio_clip_path"].ToString();
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                    if (clip != null)
                        audioSource.clip = clip;
                    else
                        Debug.LogWarning($"Audio clip not found at path: {clipPath}");
                }

                // Set basic properties
                if (@params["volume"] != null)
                    audioSource.volume = Mathf.Clamp01(@params["volume"].ToObject<float>());
                if (@params["pitch"] != null)
                    audioSource.pitch = @params["pitch"].ToObject<float>();
                if (@params["loop"] != null)
                    audioSource.loop = @params["loop"].ToObject<bool>();
                if (@params["play_on_awake"] != null)
                    audioSource.playOnAwake = @params["play_on_awake"].ToObject<bool>();

                // Set 3D audio properties
                if (@params["spatial_blend"] != null)
                    audioSource.spatialBlend = Mathf.Clamp01(@params["spatial_blend"].ToObject<float>());
                if (@params["min_distance"] != null)
                    audioSource.minDistance = @params["min_distance"].ToObject<float>();
                if (@params["max_distance"] != null)
                    audioSource.maxDistance = @params["max_distance"].ToObject<float>();

                // Set rolloff mode
                if (@params["rolloff_mode"] != null)
                {
                    string rolloffMode = @params["rolloff_mode"].ToString();
                    audioSource.rolloffMode = (AudioRolloffMode)Enum.Parse(typeof(AudioRolloffMode), rolloffMode);
                }

                // Set audio mixer group
                if (@params["mixer_group_path"] != null)
                {
                    string mixerGroupPath = @params["mixer_group_path"].ToString();
                    AudioMixerGroup mixerGroup = AssetDatabase.LoadAssetAtPath<AudioMixerGroup>(mixerGroupPath);
                    if (mixerGroup != null)
                        audioSource.outputAudioMixerGroup = mixerGroup;
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"AudioSource added/modified on '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    clipName = audioSource.clip?.name ?? "None",
                    volume = audioSource.volume,
                    pitch = audioSource.pitch,
                    loop = audioSource.loop,
                    playOnAwake = audioSource.playOnAwake,
                    spatialBlend = audioSource.spatialBlend,
                    minDistance = audioSource.minDistance,
                    maxDistance = audioSource.maxDistance,
                    rolloffMode = audioSource.rolloffMode.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add audio source: {e.Message}");
            }
        }

        private static object ModifyAudioSource(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have an AudioSource component.");
            }

            try
            {
                // Modify properties
                if (@params["volume"] != null)
                    audioSource.volume = Mathf.Clamp01(@params["volume"].ToObject<float>());
                if (@params["pitch"] != null)
                    audioSource.pitch = @params["pitch"].ToObject<float>();
                if (@params["loop"] != null)
                    audioSource.loop = @params["loop"].ToObject<bool>();
                if (@params["spatial_blend"] != null)
                    audioSource.spatialBlend = Mathf.Clamp01(@params["spatial_blend"].ToObject<float>());
                if (@params["min_distance"] != null)
                    audioSource.minDistance = @params["min_distance"].ToObject<float>();
                if (@params["max_distance"] != null)
                    audioSource.maxDistance = @params["max_distance"].ToObject<float>();

                // Change audio clip
                if (@params["audio_clip_path"] != null)
                {
                    string clipPath = @params["audio_clip_path"].ToString();
                    if (clipPath == "None" || string.IsNullOrEmpty(clipPath))
                    {
                        audioSource.clip = null;
                    }
                    else
                    {
                        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
                        if (clip != null)
                            audioSource.clip = clip;
                    }
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"AudioSource on '{gameObjectName}' modified successfully.", new
                {
                    gameObjectName = gameObjectName,
                    clipName = audioSource.clip?.name ?? "None",
                    volume = audioSource.volume,
                    pitch = audioSource.pitch,
                    loop = audioSource.loop,
                    spatialBlend = audioSource.spatialBlend
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify audio source: {e.Message}");
            }
        }

        private static object PlayAudio(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have an AudioSource component.");
            }

            try
            {
                if (Application.isPlaying)
                {
                    float delay = @params["delay"]?.ToObject<float>() ?? 0f;
                    
                    if (delay > 0f)
                        audioSource.PlayDelayed(delay);
                    else
                        audioSource.Play();

                    return Response.Success($"Audio playing on '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        clipName = audioSource.clip?.name ?? "None",
                        isPlaying = audioSource.isPlaying,
                        delay = delay
                    });
                }
                else
                {
                    return Response.Success($"Audio would play on '{gameObjectName}' (requires Play Mode).", new
                    {
                        gameObjectName = gameObjectName,
                        clipName = audioSource.clip?.name ?? "None",
                        isPlaying = false,
                        reason = "Not in Play Mode"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to play audio: {e.Message}");
            }
        }

        private static object StopAudio(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have an AudioSource component.");
            }

            try
            {
                if (Application.isPlaying)
                {
                    audioSource.Stop();
                    return Response.Success($"Audio stopped on '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        isPlaying = audioSource.isPlaying
                    });
                }
                else
                {
                    return Response.Success($"Audio would stop on '{gameObjectName}' (requires Play Mode).", new
                    {
                        gameObjectName = gameObjectName,
                        reason = "Not in Play Mode"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to stop audio: {e.Message}");
            }
        }

        private static object PauseAudio(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have an AudioSource component.");
            }

            try
            {
                if (Application.isPlaying)
                {
                    bool unpause = @params["unpause"]?.ToObject<bool>() ?? false;
                    
                    if (unpause)
                        audioSource.UnPause();
                    else
                        audioSource.Pause();

                    return Response.Success($"Audio {(unpause ? "unpaused" : "paused")} on '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        isPlaying = audioSource.isPlaying,
                        action = unpause ? "unpaused" : "paused"
                    });
                }
                else
                {
                    return Response.Success($"Audio would be paused on '{gameObjectName}' (requires Play Mode).", new
                    {
                        gameObjectName = gameObjectName,
                        reason = "Not in Play Mode"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to pause audio: {e.Message}");
            }
        }

        private static object CreateAudioMixer(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/AudioMixers";

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Audio mixer name is required.");
            }

            try
            {
                // Ensure directory exists
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string[] folders = path.Split('/');
                    string currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }

                string fullPath = $"{path}/{name}.mixer";
                
                // Check if mixer already exists
                AudioMixer existingMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(fullPath);
                if (existingMixer != null)
                {
                    return Response.Error($"Audio mixer already exists at '{fullPath}'.");
                }

                // AudioMixer cannot be created programmatically in Unity
                // This is a limitation of Unity's API - AudioMixer must be created through the Editor
                return Response.Error("AudioMixer creation requires Unity Editor menu. Use Assets > Create > Audio Mixer instead.");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create audio mixer: {e.Message}");
            }
        }

        private static object ModifyAudioMixer(JObject @params)
        {
            string mixerPath = @params["mixer_path"]?.ToString();
            if (string.IsNullOrEmpty(mixerPath))
            {
                return Response.Error("Audio mixer path is required.");
            }

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
            if (mixer == null)
            {
                return Response.Error($"Audio mixer not found at path: {mixerPath}");
            }

            try
            {
                // Set mixer parameters
                if (@params["parameters"] != null)
                {
                    JObject parameters = @params["parameters"] as JObject;
                    foreach (var param in parameters)
                    {
                        string paramName = param.Key;
                        float paramValue = param.Value.ToObject<float>();
                        mixer.SetFloat(paramName, paramValue);
                    }
                }

                EditorUtility.SetDirty(mixer);
                AssetDatabase.SaveAssets();

                return Response.Success($"Audio mixer '{mixer.name}' modified successfully.", new
                {
                    name = mixer.name,
                    path = mixerPath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify audio mixer: {e.Message}");
            }
        }

        private static object Set3DAudioSettings(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            AudioSource audioSource = targetObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' does not have an AudioSource component.");
            }

            try
            {
                // Set 3D audio properties
                if (@params["spatial_blend"] != null)
                    audioSource.spatialBlend = Mathf.Clamp01(@params["spatial_blend"].ToObject<float>());
                
                if (@params["doppler_level"] != null)
                    audioSource.dopplerLevel = @params["doppler_level"].ToObject<float>();
                
                if (@params["spread"] != null)
                    audioSource.spread = Mathf.Clamp(@params["spread"].ToObject<float>(), 0f, 360f);
                
                if (@params["rolloff_mode"] != null)
                {
                    string rolloffMode = @params["rolloff_mode"].ToString();
                    audioSource.rolloffMode = (AudioRolloffMode)Enum.Parse(typeof(AudioRolloffMode), rolloffMode);
                }
                
                if (@params["min_distance"] != null)
                    audioSource.minDistance = @params["min_distance"].ToObject<float>();
                
                if (@params["max_distance"] != null)
                    audioSource.maxDistance = @params["max_distance"].ToObject<float>();

                // Set volume rolloff curve if specified
                if (@params["volume_rolloff"] != null)
                {
                    JArray rolloffPoints = @params["volume_rolloff"] as JArray;
                    if (rolloffPoints.Count >= 2)
                    {
                        Keyframe[] keyframes = new Keyframe[rolloffPoints.Count];
                        for (int i = 0; i < rolloffPoints.Count; i++)
                        {
                            JObject point = rolloffPoints[i] as JObject;
                            keyframes[i] = new Keyframe(
                                point["time"]?.ToObject<float>() ?? 0f,
                                point["value"]?.ToObject<float>() ?? 1f
                            );
                        }
                        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(keyframes));
                    }
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"3D audio settings applied to '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    spatialBlend = audioSource.spatialBlend,
                    dopplerLevel = audioSource.dopplerLevel,
                    spread = audioSource.spread,
                    rolloffMode = audioSource.rolloffMode.ToString(),
                    minDistance = audioSource.minDistance,
                    maxDistance = audioSource.maxDistance
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set 3D audio settings: {e.Message}");
            }
        }

        private static object GetAudioInfo(JObject @params)
        {
            try
            {
                string gameObjectName = @params["gameobject_name"]?.ToString();
                
                if (!string.IsNullOrEmpty(gameObjectName))
                {
                    GameObject targetObject = GameObject.Find(gameObjectName);
                    if (targetObject == null)
                    {
                        return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
                    }

                    AudioSource audioSource = targetObject.GetComponent<AudioSource>();
                    AudioListener audioListener = targetObject.GetComponent<AudioListener>();

                    return Response.Success($"Audio info for '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        hasAudioSource = audioSource != null,
                        hasAudioListener = audioListener != null,
                        audioSource = audioSource != null ? new
                        {
                            clipName = audioSource.clip?.name ?? "None",
                            volume = audioSource.volume,
                            pitch = audioSource.pitch,
                            loop = audioSource.loop,
                            isPlaying = audioSource.isPlaying,
                            spatialBlend = audioSource.spatialBlend,
                            minDistance = audioSource.minDistance,
                            maxDistance = audioSource.maxDistance,
                            rolloffMode = audioSource.rolloffMode.ToString()
                        } : null
                    });
                }
                else
                {
                    // Return global audio settings
                    return Response.Success("Global audio settings.", new
                    {
                        globalVolume = AudioListener.volume,
                        sampleRate = AudioSettings.outputSampleRate
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get audio info: {e.Message}");
            }
        }

        private static object ImportAudioClip(JObject @params)
        {
            string sourcePath = @params["source_path"]?.ToString();
            string targetPath = @params["target_path"]?.ToString();

            if (string.IsNullOrEmpty(sourcePath))
            {
                return Response.Error("Source path is required.");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = "Assets/Audio/" + System.IO.Path.GetFileName(sourcePath);
            }

            try
            {
                // Ensure target directory exists
                string targetDir = System.IO.Path.GetDirectoryName(targetPath);
                if (!AssetDatabase.IsValidFolder(targetDir))
                {
                    string[] folders = targetDir.Split('/');
                    string currentPath = folders[0];
                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newPath = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newPath;
                    }
                }

                // Copy file to project
                if (System.IO.File.Exists(sourcePath))
                {
                    System.IO.File.Copy(sourcePath, targetPath, true);
                    AssetDatabase.ImportAsset(targetPath);

                    // Configure import settings if specified
                    AudioImporter importer = AssetImporter.GetAtPath(targetPath) as AudioImporter;
                    if (importer != null)
                    {
                        if (@params["force_to_mono"] != null)
                            importer.forceToMono = @params["force_to_mono"].ToObject<bool>();
                        
                        if (@params["load_in_background"] != null)
                            importer.loadInBackground = @params["load_in_background"].ToObject<bool>();
                        
                        // Note: preloadAudioData is now per-platform setting in SampleSettings
                        // This is a simplified version for backwards compatibility
                        if (@params["preload_audio_data"] != null)
                        {
                            var sampleSettings = importer.defaultSampleSettings;
                            sampleSettings.loadType = @params["preload_audio_data"].ToObject<bool>() 
                                ? AudioClipLoadType.DecompressOnLoad 
                                : AudioClipLoadType.CompressedInMemory;
                            importer.defaultSampleSettings = sampleSettings;
                        }

                        importer.SaveAndReimport();
                    }

                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(targetPath);
                    
                    return Response.Success($"Audio clip imported successfully.", new
                    {
                        name = clip.name,
                        path = targetPath,
                        length = clip.length,
                        frequency = clip.frequency,
                        channels = clip.channels
                    });
                }
                else
                {
                    return Response.Error($"Source file not found: {sourcePath}");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to import audio clip: {e.Message}");
            }
        }

        private static object CreateAudioListener(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                // Check if there's already an AudioListener in the scene
                AudioListener existingListener = GameObject.FindFirstObjectByType<AudioListener>();
                if (existingListener != null && existingListener.gameObject != targetObject)
                {
                    bool removeExisting = @params["remove_existing"]?.ToObject<bool>() ?? false;
                    if (removeExisting)
                    {
                        UnityEngine.Object.DestroyImmediate(existingListener);
                    }
                    else
                    {
                        return Response.Error($"AudioListener already exists on '{existingListener.gameObject.name}'. Set 'remove_existing' to true to replace it.");
                    }
                }

                AudioListener audioListener = targetObject.GetComponent<AudioListener>();
                if (audioListener == null)
                {
                    audioListener = targetObject.AddComponent<AudioListener>();
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"AudioListener added to '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create audio listener: {e.Message}");
            }
        }

        private static object SetAudioReverbZone(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            if (string.IsNullOrEmpty(gameObjectName))
            {
                return Response.Error("GameObject name is required.");
            }

            GameObject targetObject = GameObject.Find(gameObjectName);
            if (targetObject == null)
            {
                return Response.Error($"GameObject '{gameObjectName}' not found in scene.");
            }

            try
            {
                AudioReverbZone reverbZone = targetObject.GetComponent<AudioReverbZone>();
                if (reverbZone == null)
                {
                    reverbZone = targetObject.AddComponent<AudioReverbZone>();
                }

                // Set reverb zone properties
                if (@params["min_distance"] != null)
                    reverbZone.minDistance = @params["min_distance"].ToObject<float>();
                if (@params["max_distance"] != null)
                    reverbZone.maxDistance = @params["max_distance"].ToObject<float>();
                
                if (@params["reverb_preset"] != null)
                {
                    string presetName = @params["reverb_preset"].ToString();
                    reverbZone.reverbPreset = (AudioReverbPreset)Enum.Parse(typeof(AudioReverbPreset), presetName);
                }

                // Set custom reverb parameters if preset is User
                if (reverbZone.reverbPreset == AudioReverbPreset.User)
                {
                    if (@params["room"] != null)
                        reverbZone.room = @params["room"].ToObject<int>();
                    if (@params["room_hf"] != null)
                        reverbZone.roomHF = @params["room_hf"].ToObject<int>();
                    if (@params["decay_time"] != null)
                        reverbZone.decayTime = @params["decay_time"].ToObject<float>();
                    if (@params["decay_hf_ratio"] != null)
                        reverbZone.decayHFRatio = @params["decay_hf_ratio"].ToObject<float>();
                    if (@params["reflections"] != null)
                        reverbZone.reflections = @params["reflections"].ToObject<int>();
                    if (@params["reflections_delay"] != null)
                        reverbZone.reflectionsDelay = @params["reflections_delay"].ToObject<float>();
                    if (@params["reverb"] != null)
                        reverbZone.reverb = @params["reverb"].ToObject<int>();
                    if (@params["reverb_delay"] != null)
                        reverbZone.reverbDelay = @params["reverb_delay"].ToObject<float>();
                    if (@params["diffusion"] != null)
                        reverbZone.diffusion = @params["diffusion"].ToObject<float>();
                    if (@params["density"] != null)
                        reverbZone.density = @params["density"].ToObject<float>();
                    if (@params["hf_reference"] != null)
                        reverbZone.HFReference = @params["hf_reference"].ToObject<float>();
                    if (@params["room_lf"] != null)
                        reverbZone.roomLF = @params["room_lf"].ToObject<int>();
                    if (@params["lf_reference"] != null)
                        reverbZone.LFReference = @params["lf_reference"].ToObject<float>();
                }

                EditorUtility.SetDirty(targetObject);

                return Response.Success($"AudioReverbZone added/modified on '{gameObjectName}'.", new
                {
                    gameObjectName = gameObjectName,
                    minDistance = reverbZone.minDistance,
                    maxDistance = reverbZone.maxDistance,
                    reverbPreset = reverbZone.reverbPreset.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set audio reverb zone: {e.Message}");
            }
        }
    }
}