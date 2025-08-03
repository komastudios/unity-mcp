using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using System.IO;
using UnityMcpBridge.Editor.Helpers;
using Newtonsoft.Json.Linq;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageLighting
    {
        public static object HandleCommand(JObject commandData)
        {
            try
            {
                if (commandData["action"] == null)
                {
                    return Response.Error("Missing 'action' parameter");
                }

                string action = commandData["action"].ToString();

                switch (action)
                {
                    case "create_light":
                        return CreateLight(commandData);
                    case "modify_light":
                        return ModifyLight(commandData);
                    case "delete_light":
                        return DeleteLight(commandData);
                    case "setup_lighting":
                        return SetupLighting(commandData);
                    case "bake_lightmaps":
                        return BakeLightmaps(commandData);
                    case "create_material":
                        return CreateMaterial(commandData);
                    case "modify_material":
                        return ModifyMaterial(commandData);
                    case "setup_post_processing":
                        return SetupPostProcessing(commandData);
                    case "configure_render_pipeline":
                        return ConfigureRenderPipeline(commandData);
                    case "get_lighting_info":
                        return GetLightingInfo(commandData);
                    case "create_reflection_probe":
                        return CreateReflectionProbe(commandData);
                    case "setup_light_probe_group":
                        return SetupLightProbeGroup(commandData);
                    default:
                        return Response.Error($"Unknown action: {action}");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Error in ManageLighting: {e.Message}");
            }
        }

        private static object CreateLight(JObject commandData)
        {
            try
            {
                string lightName = commandData["light_name"]?.ToString() ?? "New Light";
                string lightType = commandData["light_type"]?.ToString() ?? "directional";
                
                GameObject lightObject = new GameObject(lightName);
                Light lightComponent = lightObject.AddComponent<Light>();

                // Set light type
                switch (lightType.ToLower())
                {
                    case "directional":
                        lightComponent.type = LightType.Directional;
                        break;
                    case "point":
                        lightComponent.type = LightType.Point;
                        break;
                    case "spot":
                        lightComponent.type = LightType.Spot;
                        break;
                    case "area":
                        lightComponent.type = LightType.Rectangle;
                        break;
                }

                // Set position
                if (commandData["position"] != null)
                {
                    var posObj = commandData["position"] as JObject;
                    if (posObj != null)
                    {
                        Vector3 position = new Vector3(
                            posObj["x"]?.ToObject<float>() ?? 0f,
                            posObj["y"]?.ToObject<float>() ?? 0f,
                            posObj["z"]?.ToObject<float>() ?? 0f
                        );
                        lightObject.transform.position = position;
                    }
                }

                // Set rotation
                if (commandData["rotation"] != null)
                {
                    var rotObj = commandData["rotation"] as JObject;
                    if (rotObj != null)
                    {
                        Vector3 rotation = new Vector3(
                            rotObj["x"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.x,
                            rotObj["y"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.y,
                            rotObj["z"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.z
                        );
                        lightObject.transform.rotation = Quaternion.Euler(rotation);
                    }
                }

                // Set light properties
                if (commandData["intensity"] != null)
                    lightComponent.intensity = commandData["intensity"].ToObject<float>();

                if (commandData["color"] != null)
                {
                    var colorObj = commandData["color"] as JObject;
                    if (colorObj != null)
                    {
                        Color color = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 1f,
                            colorObj["g"]?.ToObject<float>() ?? 1f,
                            colorObj["b"]?.ToObject<float>() ?? 1f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        lightComponent.color = color;
                    }
                }

                if (commandData["range"] != null)
                    lightComponent.range = commandData["range"].ToObject<float>();

                if (commandData["spot_angle"] != null)
                    lightComponent.spotAngle = commandData["spot_angle"].ToObject<float>();

                if (commandData["shadows"] != null)
                {
                    string shadowType = commandData["shadows"].ToString().ToLower();
                    switch (shadowType)
                    {
                        case "none":
                            lightComponent.shadows = LightShadows.None;
                            break;
                        case "hard":
                            lightComponent.shadows = LightShadows.Hard;
                            break;
                        case "soft":
                            lightComponent.shadows = LightShadows.Soft;
                            break;
                    }
                }

                // Set parent if specified
                if (commandData["parent_name"] != null)
                {
                    string parentName = commandData["parent_name"].ToString();
                    GameObject parent = GameObject.Find(parentName);
                    if (parent != null)
                    {
                        lightObject.transform.SetParent(parent.transform);
                    }
                }

                // Register undo
                Undo.RegisterCreatedObjectUndo(lightObject, "Create Light");

                var lightInfo = GetLightInfo(lightComponent);

                return Response.Success("Light created successfully", new Dictionary<string, object>
                {
                    ["light_info"] = lightInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create light: {e.Message}");
            }
        }

        private static object ModifyLight(JObject commandData)
        {
            try
            {
                if (commandData["light_name"] == null)
                {
                    return Response.Error("Missing 'light_name' parameter");
                }

                string lightName = commandData["light_name"].ToString();
                GameObject lightObject = GameObject.Find(lightName);
                if (lightObject == null)
                {
                    return Response.Error($"Light '{lightName}' not found");
                }

                Light lightComponent = lightObject.GetComponent<Light>();
                if (lightComponent == null)
                {
                    return Response.Error($"GameObject '{lightName}' does not have a Light component");
                }

                Undo.RecordObject(lightComponent, "Modify Light");
                Undo.RecordObject(lightObject.transform, "Modify Light Transform");

                // Modify properties
                if (commandData["intensity"] != null)
                    lightComponent.intensity = commandData["intensity"].ToObject<float>();

                if (commandData["color"] != null)
                {
                    var colorObj = commandData["color"] as JObject;
                    if (colorObj != null)
                    {
                        Color color = new Color(
                            colorObj["r"]?.ToObject<float>() ?? lightComponent.color.r,
                            colorObj["g"]?.ToObject<float>() ?? lightComponent.color.g,
                            colorObj["b"]?.ToObject<float>() ?? lightComponent.color.b,
                            colorObj["a"]?.ToObject<float>() ?? lightComponent.color.a
                        );
                        lightComponent.color = color;
                    }
                }

                if (commandData["range"] != null)
                    lightComponent.range = commandData["range"].ToObject<float>();

                if (commandData["spot_angle"] != null)
                    lightComponent.spotAngle = commandData["spot_angle"].ToObject<float>();

                if (commandData["position"] != null)
                {
                    var posObj = commandData["position"] as JObject;
                    if (posObj != null)
                    {
                        Vector3 position = new Vector3(
                            posObj["x"]?.ToObject<float>() ?? lightObject.transform.position.x,
                            posObj["y"]?.ToObject<float>() ?? lightObject.transform.position.y,
                            posObj["z"]?.ToObject<float>() ?? lightObject.transform.position.z
                        );
                        lightObject.transform.position = position;
                    }
                }

                if (commandData["rotation"] != null)
                {
                    var rotObj = commandData["rotation"] as JObject;
                    if (rotObj != null)
                    {
                        Vector3 rotation = new Vector3(
                            rotObj["x"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.x,
                            rotObj["y"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.y,
                            rotObj["z"]?.ToObject<float>() ?? lightObject.transform.rotation.eulerAngles.z
                        );
                        lightObject.transform.rotation = Quaternion.Euler(rotation);
                    }
                }

                var lightInfo = GetLightInfo(lightComponent);

                return Response.Success("Light modified successfully", new Dictionary<string, object>
                {
                    ["light_info"] = lightInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify light: {e.Message}");
            }
        }

        private static object DeleteLight(JObject commandData)
        {
            try
            {
                if (commandData["light_name"] == null)
                {
                    return Response.Error("Missing 'light_name' parameter");
                }

                string lightName = commandData["light_name"].ToString();
                GameObject lightObject = GameObject.Find(lightName);

                if (lightObject == null)
                {
                    return Response.Error($"Light '{lightName}' not found");
                }

                Light lightComponent = lightObject.GetComponent<Light>();
                if (lightComponent == null)
                {
                    return Response.Error($"GameObject '{lightName}' does not have a Light component");
                }

                Undo.DestroyObjectImmediate(lightObject);

                return Response.Success($"Light '{lightName}' deleted successfully");
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to delete light: {e.Message}");
            }
        }

        private static object SetupLighting(JObject commandData)
        {
            try
            {
                // Configure lighting settings
                if (commandData["ambient_mode"] != null)
                {
                    string ambientMode = commandData["ambient_mode"].ToString().ToLower();
                    switch (ambientMode)
                    {
                        case "skybox":
                            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
                            break;
                        case "trilight":
                            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                            break;
                        case "flat":
                            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                            break;
                    }
                }

                if (commandData["ambient_color"] != null)
                {
                    var colorObj = commandData["ambient_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color ambientColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 1f,
                            colorObj["g"]?.ToObject<float>() ?? 1f,
                            colorObj["b"]?.ToObject<float>() ?? 1f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        RenderSettings.ambientLight = ambientColor;
                    }
                }

                if (commandData["ambient_intensity"] != null)
                    RenderSettings.ambientIntensity = commandData["ambient_intensity"].ToObject<float>();

                if (commandData["skybox_material"] != null)
                {
                    string skyboxPath = commandData["skybox_material"].ToString();
                    Material skyboxMaterial = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);
                    if (skyboxMaterial != null)
                    {
                        RenderSettings.skybox = skyboxMaterial;
                    }
                }

                if (commandData["fog_enabled"] != null)
                    RenderSettings.fog = commandData["fog_enabled"].ToObject<bool>();

                if (commandData["fog_color"] != null)
                {
                    var colorObj = commandData["fog_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color fogColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 1f,
                            colorObj["g"]?.ToObject<float>() ?? 1f,
                            colorObj["b"]?.ToObject<float>() ?? 1f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        RenderSettings.fogColor = fogColor;
                    }
                }

                if (commandData["fog_mode"] != null)
                {
                    string fogMode = commandData["fog_mode"].ToString().ToLower();
                    switch (fogMode)
                    {
                        case "linear":
                            RenderSettings.fogMode = FogMode.Linear;
                            break;
                        case "exponential":
                            RenderSettings.fogMode = FogMode.Exponential;
                            break;
                        case "exponentialsquared":
                            RenderSettings.fogMode = FogMode.ExponentialSquared;
                            break;
                    }
                }

                if (commandData["fog_start_distance"] != null)
                    RenderSettings.fogStartDistance = commandData["fog_start_distance"].ToObject<float>();

                if (commandData["fog_end_distance"] != null)
                    RenderSettings.fogEndDistance = commandData["fog_end_distance"].ToObject<float>();

                if (commandData["fog_density"] != null)
                    RenderSettings.fogDensity = commandData["fog_density"].ToObject<float>();

                return Response.Success("Lighting settings configured successfully", new Dictionary<string, object>
                {
                    ["lighting_settings"] = GetCurrentLightingSettings()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to setup lighting: {e.Message}");
            }
        }

        private static object BakeLightmaps(JObject commandData)
        {
            try
            {
                // Configure lightmapping settings
                if (commandData["lightmap_resolution"] != null)
                    LightmapEditorSettings.realtimeResolution = commandData["lightmap_resolution"].ToObject<float>();

                if (commandData["lightmap_padding"] != null)
                    LightmapEditorSettings.padding = commandData["lightmap_padding"].ToObject<int>();

                if (commandData["lightmap_size"] != null)
                    LightmapEditorSettings.maxAtlasSize = commandData["lightmap_size"].ToObject<int>();

                if (commandData["ambient_occlusion"] != null)
                    LightmapEditorSettings.enableAmbientOcclusion = commandData["ambient_occlusion"].ToObject<bool>();

                if (commandData["directional_mode"] != null)
                {
                    string directionalMode = commandData["directional_mode"].ToString().ToLower();
                    switch (directionalMode)
                    {
                        case "non_directional":
                            LightmapEditorSettings.lightmapsMode = LightmapsMode.NonDirectional;
                            break;
                        case "combined_directional":
                            LightmapEditorSettings.lightmapsMode = LightmapsMode.CombinedDirectional;
                            break;
                    }
                }

                // Start baking
                bool async = commandData["async"]?.ToObject<bool>() ?? true;
                
                if (async)
                {
                    Lightmapping.BakeAsync();
                }
                else
                {
                    Lightmapping.Bake();
                }

                return Response.Success("Lightmap baking started", new Dictionary<string, object>
                {
                    ["baking_async"] = async,
                    ["lightmap_settings"] = GetCurrentLightingSettings()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to bake lightmaps: {e.Message}");
            }
        }

        private static object CreateMaterial(JObject commandData)
        {
            try
            {
                if (commandData["material_name"] == null)
                {
                    return Response.Error("Missing 'material_name' parameter");
                }

                string materialName = commandData["material_name"].ToString();
                string shaderName = commandData["shader_name"]?.ToString() ?? "Universal Render Pipeline/Lit";
                string savePath = commandData["save_path"]?.ToString() ?? $"Assets/Materials/{materialName}.mat";

                // Ensure directory exists
                string directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Shader shader = Shader.Find(shaderName);
                if (shader == null)
                {
                    return Response.Error($"Shader '{shaderName}' not found");
                }

                Material material = new Material(shader);
                material.name = materialName;

                // Set material properties
                if (commandData["albedo_color"] != null)
                {
                    var colorObj = commandData["albedo_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color albedoColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 1f,
                            colorObj["g"]?.ToObject<float>() ?? 1f,
                            colorObj["b"]?.ToObject<float>() ?? 1f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        material.SetColor("_BaseColor", albedoColor);
                    }
                }

                if (commandData["metallic"] != null)
                    material.SetFloat("_Metallic", commandData["metallic"].ToObject<float>());

                if (commandData["smoothness"] != null)
                    material.SetFloat("_Smoothness", commandData["smoothness"].ToObject<float>());

                if (commandData["emission_color"] != null)
                {
                    var colorObj = commandData["emission_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color emissionColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 0f,
                            colorObj["g"]?.ToObject<float>() ?? 0f,
                            colorObj["b"]?.ToObject<float>() ?? 0f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        material.SetColor("_EmissionColor", emissionColor);
                        material.EnableKeyword("_EMISSION");
                    }
                }

                if (commandData["albedo_texture"] != null)
                {
                    string texturePath = commandData["albedo_texture"].ToString();
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    if (texture != null)
                    {
                        material.SetTexture("_BaseMap", texture);
                    }
                }

                // Save material
                AssetDatabase.CreateAsset(material, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var materialInfo = GetMaterialInfo(material);

                return Response.Success("Material created successfully", new Dictionary<string, object>
                {
                    ["material_info"] = materialInfo,
                    ["material_path"] = savePath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create material: {e.Message}");
            }
        }

        private static object ModifyMaterial(JObject commandData)
        {
            try
            {
                if (commandData["material_path"] == null)
                {
                    return Response.Error("Missing 'material_path' parameter");
                }

                string materialPath = commandData["material_path"].ToString();
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

                if (material == null)
                {
                    return Response.Error($"Material not found at path: {materialPath}");
                }

                Undo.RecordObject(material, "Modify Material");

                // Modify material properties
                if (commandData["albedo_color"] != null)
                {
                    var colorObj = commandData["albedo_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color albedoColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 1f,
                            colorObj["g"]?.ToObject<float>() ?? 1f,
                            colorObj["b"]?.ToObject<float>() ?? 1f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        material.SetColor("_BaseColor", albedoColor);
                    }
                }

                if (commandData["metallic"] != null)
                    material.SetFloat("_Metallic", commandData["metallic"].ToObject<float>());

                if (commandData["smoothness"] != null)
                    material.SetFloat("_Smoothness", commandData["smoothness"].ToObject<float>());

                if (commandData["emission_color"] != null)
                {
                    var colorObj = commandData["emission_color"] as JObject;
                    if (colorObj != null)
                    {
                        Color emissionColor = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 0f,
                            colorObj["g"]?.ToObject<float>() ?? 0f,
                            colorObj["b"]?.ToObject<float>() ?? 0f,
                            colorObj["a"]?.ToObject<float>() ?? 1f
                        );
                        material.SetColor("_EmissionColor", emissionColor);
                        if (emissionColor != Color.black)
                        {
                            material.EnableKeyword("_EMISSION");
                        }
                        else
                        {
                            material.DisableKeyword("_EMISSION");
                        }
                    }
                }

                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();

                var materialInfo = GetMaterialInfo(material);

                return Response.Success("Material modified successfully", new Dictionary<string, object>
                {
                    ["material_info"] = materialInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify material: {e.Message}");
            }
        }

        private static object SetupPostProcessing(JObject commandData)
        {
            try
            {
                // Find or create post-process volume
                string volumeName = commandData.ContainsKey("volume_name") ? commandData["volume_name"].ToString() : "Global Volume";
                
                GameObject volumeObject = GameObject.Find(volumeName);
                if (volumeObject == null)
                {
                    volumeObject = new GameObject(volumeName);
                    Undo.RegisterCreatedObjectUndo(volumeObject, "Create Post Process Volume");
                }

                Volume volume = volumeObject.GetComponent<Volume>();
                if (volume == null)
                {
                    volume = volumeObject.AddComponent<Volume>();
                }

                // Configure volume
                if (commandData.ContainsKey("is_global"))
                    volume.isGlobal = Convert.ToBoolean(commandData["is_global"]);

                if (commandData.ContainsKey("priority"))
                    volume.priority = Convert.ToSingle(commandData["priority"]);

                // Create or load volume profile
                string profilePath = commandData.ContainsKey("profile_path") ? 
                    commandData["profile_path"].ToString() : 
                    $"Assets/Settings/{volumeName}_Profile.asset";
                    
                string volumePath = AssetDatabase.GetAssetPath(volumeObject);
                if (string.IsNullOrEmpty(volumePath))
                {
                    volumePath = $"Scene/{volumeName}";
                }

                if (volume.profile == null)
                {
                    string directory = Path.GetDirectoryName(profilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, profilePath);
                    volume.profile = profile;
                }

                // Configure post-processing effects
                if (commandData.ContainsKey("bloom_enabled"))
                {
                    bool bloomEnabled = Convert.ToBoolean(commandData["bloom_enabled"]);
                    if (bloomEnabled)
                    {
                        if (!volume.profile.Has<UnityEngine.Rendering.Universal.Bloom>())
                        {
                            var bloom = volume.profile.Add<UnityEngine.Rendering.Universal.Bloom>();
                            bloom.active = true;
                        }
                    }
                }

                if (commandData.ContainsKey("color_adjustments_enabled"))
                {
                    bool colorAdjustmentsEnabled = Convert.ToBoolean(commandData["color_adjustments_enabled"]);
                    if (colorAdjustmentsEnabled)
                    {
                        if (!volume.profile.Has<UnityEngine.Rendering.Universal.ColorAdjustments>())
                        {
                            var colorAdjustments = volume.profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
                            colorAdjustments.active = true;
                        }
                    }
                }

                EditorUtility.SetDirty(volume.profile);
                AssetDatabase.SaveAssets();

                return Response.Success("Post-processing setup completed", new Dictionary<string, object>
                {
                    ["volume_name"] = volumeName,
                    ["volume_path"] = volumePath,
                    ["profile_path"] = profilePath
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to setup post-processing: {e.Message}");
            }
        }

        private static object ConfigureRenderPipeline(JObject commandData)
        {
            try
            {
                var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (renderPipelineAsset == null)
                {
                    return Response.Error("Universal Render Pipeline asset not found");
                }

                // Note: Many URP settings are not directly accessible via script
                // This is a simplified implementation
                
                var pipelineInfo = new JObject
                {
                    ["pipeline_type"] = "Universal Render Pipeline",
                    ["asset_name"] = renderPipelineAsset.name,
                    ["asset_path"] = AssetDatabase.GetAssetPath(renderPipelineAsset)
                };

                return Response.Success("Render pipeline information retrieved", new JObject
                {
                    ["pipeline_info"] = pipelineInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to configure render pipeline: {e.Message}");
            }
        }

        private static object GetLightingInfo(JObject commandData)
        {
            try
            {
                var lightingInfo = new JObject();

                if (commandData.ContainsKey("light_name"))
                {
                    string lightName = commandData["light_name"].ToString();
                    GameObject lightObject = GameObject.Find(lightName);
                    if (lightObject != null)
                    {
                        Light lightComponent = lightObject.GetComponent<Light>();
                        if (lightComponent != null)
                        {
                            lightingInfo["light_info"] = JObject.FromObject(GetLightInfo(lightComponent));
                        }
                    }
                }
                else
                {
                    // Get all lights in scene
                    Light[] allLights = UnityEngine.Object.FindObjectsOfType<Light>();
                    var lightsInfo = new JArray();
                    
                    foreach (Light light in allLights)
                    {
                        lightsInfo.Add(JObject.FromObject(GetLightInfo(light)));
                    }
                    
                    lightingInfo["all_lights"] = lightsInfo;
                    lightingInfo["lighting_settings"] = JObject.FromObject(GetCurrentLightingSettings());
                    lightingInfo["lightmap_settings"] = JObject.FromObject(GetLightmapSettings());
                }

                return Response.Success("Lighting information retrieved", lightingInfo);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get lighting info: {e.Message}");
            }
        }

        private static object CreateReflectionProbe(JObject commandData)
        {
            try
            {
                string probeName = commandData.ContainsKey("probe_name") ? commandData["probe_name"].ToString() : "Reflection Probe";
                
                GameObject probeObject = new GameObject(probeName);
                ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();

                // Set position
                if (commandData.ContainsKey("position"))
                {
                    var posDict = commandData["position"] as JObject;
                    if (posDict != null)
                    {
                        Vector3 position = new Vector3(
                            posDict.GetValue("x")?.ToObject<float>() ?? 0f,
                            posDict.GetValue("y")?.ToObject<float>() ?? 0f,
                            posDict.GetValue("z")?.ToObject<float>() ?? 0f
                        );
                        probeObject.transform.position = position;
                    }
                }

                // Set size
                if (commandData.ContainsKey("size"))
                {
                    var sizeDict = commandData["size"] as JObject;
                    if (sizeDict != null)
                    {
                        Vector3 size = new Vector3(
                            sizeDict.GetValue("x")?.ToObject<float>() ?? 10f,
                            sizeDict.GetValue("y")?.ToObject<float>() ?? 10f,
                            sizeDict.GetValue("z")?.ToObject<float>() ?? 10f
                        );
                        probe.size = size;
                    }
                }

                if (commandData.ContainsKey("resolution"))
                {
                    int resolution = commandData["resolution"].ToObject<int>();
                    probe.resolution = resolution;
                }

                if (commandData.ContainsKey("intensity"))
                    probe.intensity = commandData["intensity"].ToObject<float>();

                Undo.RegisterCreatedObjectUndo(probeObject, "Create Reflection Probe");

                var probeInfo = new JObject
                {
                    ["name"] = probeName,
                    ["position"] = new JObject
                    {
                        ["x"] = probeObject.transform.position.x,
                        ["y"] = probeObject.transform.position.y,
                        ["z"] = probeObject.transform.position.z
                    },
                    ["size"] = new JObject
                    {
                        ["x"] = probe.size.x,
                        ["y"] = probe.size.y,
                        ["z"] = probe.size.z
                    },
                    ["resolution"] = probe.resolution,
                    ["intensity"] = probe.intensity
                };

                return Response.Success("Reflection probe created successfully", new JObject
                {
                    ["probe_info"] = probeInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create reflection probe: {e.Message}");
            }
        }

        private static object SetupLightProbeGroup(JObject commandData)
        {
            try
            {
                string groupName = commandData.ContainsKey("group_name") ? commandData["group_name"].ToString() : "Light Probe Group";
                
                GameObject probeGroupObject = new GameObject(groupName);
                LightProbeGroup probeGroup = probeGroupObject.AddComponent<LightProbeGroup>();

                // Set position
                if (commandData.ContainsKey("position"))
                {
                    var posDict = commandData["position"] as JObject;
                    if (posDict != null)
                    {
                        Vector3 position = new Vector3(
                            posDict.GetValue("x")?.ToObject<float>() ?? 0f,
                            posDict.GetValue("y")?.ToObject<float>() ?? 0f,
                            posDict.GetValue("z")?.ToObject<float>() ?? 0f
                        );
                        probeGroupObject.transform.position = position;
                    }
                }

                // Create default probe positions
                List<Vector3> probePositions = new List<Vector3>();
                
                if (commandData.ContainsKey("probe_positions"))
                {
                    var positionsArray = commandData["probe_positions"] as JArray;
                    if (positionsArray != null)
                    {
                        foreach (var posObj in positionsArray)
                        {
                            var posDict = posObj as JObject;
                            if (posDict != null)
                            {
                                Vector3 probePos = new Vector3(
                                    posDict.GetValue("x")?.ToObject<float>() ?? 0f,
                                    posDict.GetValue("y")?.ToObject<float>() ?? 0f,
                                    posDict.GetValue("z")?.ToObject<float>() ?? 0f
                                );
                                probePositions.Add(probePos);
                            }
                        }
                    }
                }
                else
                {
                    // Create default 3x3x3 grid
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = 0; y <= 2; y++)
                        {
                            for (int z = -1; z <= 1; z++)
                            {
                                probePositions.Add(new Vector3(x * 2f, y * 2f, z * 2f));
                            }
                        }
                    }
                }

                probeGroup.probePositions = probePositions.ToArray();

                Undo.RegisterCreatedObjectUndo(probeGroupObject, "Create Light Probe Group");

                var groupInfo = new JObject
                {
                    ["name"] = groupName,
                    ["position"] = new JObject
                    {
                        ["x"] = probeGroupObject.transform.position.x,
                        ["y"] = probeGroupObject.transform.position.y,
                        ["z"] = probeGroupObject.transform.position.z
                    },
                    ["probe_count"] = probeGroup.probePositions.Length
                };

                return Response.Success("Light probe group created successfully", new JObject
                {
                    ["group_info"] = groupInfo
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create light probe group: {e.Message}");
            }
        }

        // Helper methods
        private static Dictionary<string, object> GetLightInfo(Light light)
        {
            return new Dictionary<string, object>
            {
                ["name"] = light.gameObject.name,
                ["type"] = light.type.ToString(),
                ["intensity"] = light.intensity,
                ["color"] = new Dictionary<string, object>
                {
                    ["r"] = light.color.r,
                    ["g"] = light.color.g,
                    ["b"] = light.color.b,
                    ["a"] = light.color.a
                },
                ["range"] = light.range,
                ["spot_angle"] = light.spotAngle,
                ["shadows"] = light.shadows.ToString(),
                ["position"] = new Dictionary<string, object>
                {
                    ["x"] = light.transform.position.x,
                    ["y"] = light.transform.position.y,
                    ["z"] = light.transform.position.z
                },
                ["rotation"] = new Dictionary<string, object>
                {
                    ["x"] = light.transform.rotation.eulerAngles.x,
                    ["y"] = light.transform.rotation.eulerAngles.y,
                    ["z"] = light.transform.rotation.eulerAngles.z
                }
            };
        }

        private static Dictionary<string, object> GetCurrentLightingSettings()
        {
            return new Dictionary<string, object>
            {
                ["ambient_mode"] = RenderSettings.ambientMode.ToString(),
                ["ambient_color"] = new Dictionary<string, object>
                {
                    ["r"] = RenderSettings.ambientLight.r,
                    ["g"] = RenderSettings.ambientLight.g,
                    ["b"] = RenderSettings.ambientLight.b,
                    ["a"] = RenderSettings.ambientLight.a
                },
                ["ambient_intensity"] = RenderSettings.ambientIntensity,
                ["fog_enabled"] = RenderSettings.fog,
                ["fog_color"] = new Dictionary<string, object>
                {
                    ["r"] = RenderSettings.fogColor.r,
                    ["g"] = RenderSettings.fogColor.g,
                    ["b"] = RenderSettings.fogColor.b,
                    ["a"] = RenderSettings.fogColor.a
                },
                ["fog_mode"] = RenderSettings.fogMode.ToString(),
                ["fog_start_distance"] = RenderSettings.fogStartDistance,
                ["fog_end_distance"] = RenderSettings.fogEndDistance,
                ["fog_density"] = RenderSettings.fogDensity,
                ["skybox_material"] = RenderSettings.skybox != null ? AssetDatabase.GetAssetPath(RenderSettings.skybox) : null
            };
        }

        private static Dictionary<string, object> GetLightmapSettings()
        {
            return new Dictionary<string, object>
            {
                ["resolution"] = LightmapEditorSettings.realtimeResolution,
                ["padding"] = LightmapEditorSettings.padding,
                ["max_atlas_size"] = LightmapEditorSettings.maxAtlasSize,
                ["ambient_occlusion"] = LightmapEditorSettings.enableAmbientOcclusion,
                ["lightmaps_mode"] = LightmapEditorSettings.lightmapsMode.ToString(),
                ["is_baking"] = Lightmapping.isRunning
            };
        }

        private static Dictionary<string, object> GetMaterialInfo(Material material)
        {
            var materialInfo = new Dictionary<string, object>
            {
                ["name"] = material.name,
                ["shader"] = material.shader.name
            };

            // Get common properties if they exist
            if (material.HasProperty("_BaseColor"))
            {
                Color baseColor = material.GetColor("_BaseColor");
                materialInfo["albedo_color"] = new Dictionary<string, object>
                {
                    ["r"] = baseColor.r,
                    ["g"] = baseColor.g,
                    ["b"] = baseColor.b,
                    ["a"] = baseColor.a
                };
            }

            if (material.HasProperty("_Metallic"))
                materialInfo["metallic"] = material.GetFloat("_Metallic");

            if (material.HasProperty("_Smoothness"))
                materialInfo["smoothness"] = material.GetFloat("_Smoothness");

            if (material.HasProperty("_EmissionColor"))
            {
                Color emissionColor = material.GetColor("_EmissionColor");
                materialInfo["emission_color"] = new Dictionary<string, object>
                {
                    ["r"] = emissionColor.r,
                    ["g"] = emissionColor.g,
                    ["b"] = emissionColor.b,
                    ["a"] = emissionColor.a
                };
            }

            return materialInfo;
        }
    }
}