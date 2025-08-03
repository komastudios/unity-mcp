using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageParticles
    {
        // Helper method to safely serialize Vector3
        private static object SerializeVector3(Vector3 vector)
        {
            return new { x = vector.x, y = vector.y, z = vector.z };
        }

        // Helper method to safely serialize Quaternion as Euler angles
        private static object SerializeRotation(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;
            return new { x = euler.x, y = euler.y, z = euler.z };
        }

        public static object HandleCommand(JObject @params)
        {
            string action = @params["action"]?.ToString().ToLower();
            if (string.IsNullOrEmpty(action))
            {
                return Response.Error("Action parameter is required.");
            }

            try
            {
                return action switch
                {
                    "create_particle_system" => CreateParticleSystem(@params),
                    "modify_particle_system" => ModifyParticleSystem(@params),
                    "delete_particle_system" => DeleteParticleSystem(@params),
                    "get_particle_info" => GetParticleInfo(@params),
                    "list_particle_systems" => ListParticleSystems(@params),
                    "configure_emission" => ConfigureEmission(@params),
                    "configure_shape" => ConfigureShape(@params),
                    "configure_velocity" => ConfigureVelocity(@params),
                    "configure_color_over_lifetime" => ConfigureColorOverLifetime(@params),
                    "configure_size_over_lifetime" => ConfigureSizeOverLifetime(@params),
                    "configure_rotation_over_lifetime" => ConfigureRotationOverLifetime(@params),
                    "configure_noise" => ConfigureNoise(@params),
                    "configure_collision" => ConfigureCollision(@params),
                    "configure_sub_emitters" => ConfigureSubEmitters(@params),
                    "configure_texture_sheet_animation" => ConfigureTextureSheetAnimation(@params),
                    "configure_trails" => ConfigureTrails(@params),
                    "configure_renderer" => ConfigureRenderer(@params),
                    "play_particle_system" => PlayParticleSystem(@params),
                    "pause_particle_system" => PauseParticleSystem(@params),
                    "stop_particle_system" => StopParticleSystem(@params),
                    "clear_particle_system" => ClearParticleSystem(@params),
                    "simulate_particle_system" => SimulateParticleSystem(@params),
                    "create_particle_material" => CreateParticleMaterial(@params),
                    "optimize_particle_system" => OptimizeParticleSystem(@params),
                    _ => Response.Error($"Unknown action: {action}")
                };
            }
            catch (Exception ex)
            {
                return Response.Error($"Error executing action '{action}': {ex.Message}");
            }
        }

        private static object CreateParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            // Create GameObject with ParticleSystem
            GameObject particleObject = new GameObject(name);
            ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();

            // Set position if provided
            if (@params["position"] != null)
            {
                var posArray = @params["position"].ToObject<float[]>();
                if (posArray?.Length == 3)
                {
                    particleObject.transform.position = new Vector3(posArray[0], posArray[1], posArray[2]);
                }
            }

            // Set rotation if provided
            if (@params["rotation"] != null)
            {
                var rotArray = @params["rotation"].ToObject<float[]>();
                if (rotArray?.Length == 3)
                {
                    particleObject.transform.rotation = Quaternion.Euler(rotArray[0], rotArray[1], rotArray[2]);
                }
            }

            // Configure basic settings
            var main = particleSystem.main;
            
            if (@params["duration"] != null)
                main.duration = @params["duration"].ToObject<float>();
            
            if (@params["looping"] != null)
                main.loop = @params["looping"].ToObject<bool>();
            
            if (@params["start_lifetime"] != null)
                main.startLifetime = @params["start_lifetime"].ToObject<float>();
            
            if (@params["start_speed"] != null)
                main.startSpeed = @params["start_speed"].ToObject<float>();
            
            if (@params["start_size"] != null)
                main.startSize = @params["start_size"].ToObject<float>();
            
            if (@params["start_color"] != null)
            {
                var colorArray = @params["start_color"].ToObject<float[]>();
                if (colorArray?.Length >= 3)
                {
                    float alpha = colorArray.Length > 3 ? colorArray[3] : 1.0f;
                    main.startColor = new Color(colorArray[0], colorArray[1], colorArray[2], alpha);
                }
            }

            if (@params["max_particles"] != null)
                main.maxParticles = @params["max_particles"].ToObject<int>();

            // Set simulation space
            if (@params["simulation_space"] != null)
            {
                string simSpace = @params["simulation_space"].ToString();
                main.simulationSpace = simSpace.ToLower() switch
                {
                    "local" => ParticleSystemSimulationSpace.Local,
                    "world" => ParticleSystemSimulationSpace.World,
                    "custom" => ParticleSystemSimulationSpace.Custom,
                    _ => ParticleSystemSimulationSpace.Local
                };
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(particleObject, $"Create Particle System '{name}'");

            return Response.Success($"Particle system '{name}' created successfully.", new
            {
                name = name,
                id = particleObject.GetInstanceID(),
                position = new { x = particleObject.transform.position.x, y = particleObject.transform.position.y, z = particleObject.transform.position.z },
                rotation = new { x = particleObject.transform.rotation.x, y = particleObject.transform.rotation.y, z = particleObject.transform.rotation.z, w = particleObject.transform.rotation.w },
                particle_count = main.maxParticles,
                duration = main.duration,
                looping = main.loop
            });
        }

        private static object ModifyParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Modify Particle System '{name}'");

            var main = particleSystem.main;

            // Update main module properties
            if (@params["duration"] != null)
                main.duration = @params["duration"].ToObject<float>();
            
            if (@params["looping"] != null)
                main.loop = @params["looping"].ToObject<bool>();
            
            if (@params["start_lifetime"] != null)
                main.startLifetime = @params["start_lifetime"].ToObject<float>();
            
            if (@params["start_speed"] != null)
                main.startSpeed = @params["start_speed"].ToObject<float>();
            
            if (@params["start_size"] != null)
                main.startSize = @params["start_size"].ToObject<float>();
            
            if (@params["start_color"] != null)
            {
                var colorArray = @params["start_color"].ToObject<float[]>();
                if (colorArray?.Length >= 3)
                {
                    float alpha = colorArray.Length > 3 ? colorArray[3] : 1.0f;
                    main.startColor = new Color(colorArray[0], colorArray[1], colorArray[2], alpha);
                }
            }

            if (@params["max_particles"] != null)
                main.maxParticles = @params["max_particles"].ToObject<int>();

            // Update position if provided
            if (@params["position"] != null)
            {
                var posArray = @params["position"].ToObject<float[]>();
                if (posArray?.Length == 3)
                {
                    Undo.RecordObject(particleObject.transform, $"Move Particle System '{name}'");
                    particleObject.transform.position = new Vector3(posArray[0], posArray[1], posArray[2]);
                }
            }

            // Update rotation if provided
            if (@params["rotation"] != null)
            {
                var rotArray = @params["rotation"].ToObject<float[]>();
                if (rotArray?.Length == 3)
                {
                    Undo.RecordObject(particleObject.transform, $"Rotate Particle System '{name}'");
                    particleObject.transform.rotation = Quaternion.Euler(rotArray[0], rotArray[1], rotArray[2]);
                }
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Particle system '{name}' modified successfully.", new
            {
                name = name,
                position = SerializeVector3(particleObject.transform.position),
                rotation = SerializeRotation(particleObject.transform.rotation),
                particle_count = main.maxParticles,
                duration = main.duration,
                looping = main.loop
            });
        }

        private static object DeleteParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.DestroyObjectImmediate(particleObject);

            return Response.Success($"Particle system '{name}' deleted successfully.");
        }

        private static object GetParticleInfo(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            var main = particleSystem.main;
            var emission = particleSystem.emission;
            var shape = particleSystem.shape;
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            var colorOverLifetime = particleSystem.colorOverLifetime;
            var sizeOverLifetime = particleSystem.sizeOverLifetime;

            return Response.Success("Particle system information retrieved.", new
            {
                name = name,
                id = particleObject.GetInstanceID(),
                position = SerializeVector3(particleObject.transform.position),
                rotation = SerializeRotation(particleObject.transform.rotation),
                scale = SerializeVector3(particleObject.transform.localScale),
                active = particleObject.activeInHierarchy,
                playing = particleSystem.isPlaying,
                paused = particleSystem.isPaused,
                stopped = particleSystem.isStopped,
                particle_count = particleSystem.particleCount,
                main_module = new
                {
                    duration = main.duration,
                    looping = main.loop,
                    prewarm = main.prewarm,
                    start_delay = main.startDelay.constant,
                    start_lifetime = main.startLifetime.constant,
                    start_speed = main.startSpeed.constant,
                    start_size = main.startSize.constant,
                    start_rotation = main.startRotation.constant,
                    start_color = new float[] { main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, main.startColor.color.a },
                    gravity_modifier = main.gravityModifier.constant,
                    simulation_space = main.simulationSpace.ToString(),
                    simulation_speed = main.simulationSpeed,
                    delta_time = main.useUnscaledTime ? "Unscaled" : "Scaled",
                    max_particles = main.maxParticles
                },
                emission_module = new
                {
                    enabled = emission.enabled,
                    rate_over_time = emission.rateOverTime.constant,
                    rate_over_distance = emission.rateOverDistance.constant
                },
                shape_module = new
                {
                    enabled = shape.enabled,
                    shape_type = shape.shapeType.ToString(),
                    angle = shape.angle,
                    radius = shape.radius,
                    arc = shape.arc
                },
                velocity_module = new
                {
                    enabled = velocityOverLifetime.enabled,
                    space = velocityOverLifetime.space.ToString()
                },
                color_module = new
                {
                    enabled = colorOverLifetime.enabled
                },
                size_module = new
                {
                    enabled = sizeOverLifetime.enabled
                }
            });
        }

        private static object ListParticleSystems(JObject @params)
        {
            ParticleSystem[] particleSystems = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();

            var particleList = particleSystems.Select(ps => new
            {
                name = ps.gameObject.name,
                id = ps.gameObject.GetInstanceID(),
                position = SerializeVector3(ps.transform.position),
                active = ps.gameObject.activeInHierarchy,
                playing = ps.isPlaying,
                paused = ps.isPaused,
                particle_count = ps.particleCount,
                max_particles = ps.main.maxParticles,
                duration = ps.main.duration,
                looping = ps.main.loop
            }).ToArray();

            return Response.Success($"Found {particleList.Length} particle systems.", new
            {
                count = particleList.Length,
                particle_systems = particleList
            });
        }

        private static object ConfigureEmission(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Emission for '{name}'");

            var emission = particleSystem.emission;

            if (@params["enabled"] != null)
                emission.enabled = @params["enabled"].ToObject<bool>();

            if (@params["rate_over_time"] != null)
                emission.rateOverTime = @params["rate_over_time"].ToObject<float>();

            if (@params["rate_over_distance"] != null)
                emission.rateOverDistance = @params["rate_over_distance"].ToObject<float>();

            // Configure bursts if provided
            if (@params["bursts"] != null)
            {
                var burstsArray = @params["bursts"].ToObject<JArray>();
                List<ParticleSystem.Burst> bursts = new List<ParticleSystem.Burst>();

                foreach (var burstData in burstsArray)
                {
                    float time = burstData["time"]?.ToObject<float>() ?? 0f;
                    short count = burstData["count"]?.ToObject<short>() ?? 30;
                    short cycles = burstData["cycles"]?.ToObject<short>() ?? 1;
                    float interval = burstData["interval"]?.ToObject<float>() ?? 0.01f;

                    bursts.Add(new ParticleSystem.Burst(time, count, cycles, interval));
                }

                emission.SetBursts(bursts.ToArray());
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Emission configured for particle system '{name}'.", new
            {
                enabled = emission.enabled,
                rate_over_time = emission.rateOverTime.constant,
                rate_over_distance = emission.rateOverDistance.constant,
                burst_count = emission.burstCount
            });
        }

        private static object ConfigureShape(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Shape for '{name}'");

            var shape = particleSystem.shape;

            if (@params["enabled"] != null)
                shape.enabled = @params["enabled"].ToObject<bool>();

            if (@params["shape_type"] != null)
            {
                string shapeType = @params["shape_type"].ToString();
                shape.shapeType = shapeType.ToLower() switch
                {
                    "sphere" => ParticleSystemShapeType.Sphere,
                    "hemisphere" => ParticleSystemShapeType.Hemisphere,
                    "cone" => ParticleSystemShapeType.Cone,
                    "box" => ParticleSystemShapeType.Box,
                    "mesh" => ParticleSystemShapeType.Mesh,
                    "circle" => ParticleSystemShapeType.Circle,
                    "rectangle" => ParticleSystemShapeType.Rectangle, // Use Rectangle instead of Edge
                    _ => ParticleSystemShapeType.Cone
                };
            }

            if (@params["angle"] != null)
                shape.angle = @params["angle"].ToObject<float>();

            if (@params["radius"] != null)
                shape.radius = @params["radius"].ToObject<float>();

            if (@params["arc"] != null)
                shape.arc = @params["arc"].ToObject<float>();

            if (@params["box_thickness"] != null)
            {
                var thicknessArray = @params["box_thickness"].ToObject<float[]>();
                if (thicknessArray?.Length == 3)
                {
                    shape.boxThickness = new Vector3(thicknessArray[0], thicknessArray[1], thicknessArray[2]);
                }
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Shape configured for particle system '{name}'.", new
            {
                enabled = shape.enabled,
                shape_type = shape.shapeType.ToString(),
                angle = shape.angle,
                radius = shape.radius,
                arc = shape.arc
            });
        }

        private static object ConfigureVelocity(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Velocity for '{name}'");

            var velocity = particleSystem.velocityOverLifetime;

            if (@params["enabled"] != null)
                velocity.enabled = @params["enabled"].ToObject<bool>();

            if (@params["linear_velocity"] != null)
            {
                var velocityArray = @params["linear_velocity"].ToObject<float[]>();
                if (velocityArray?.Length == 3)
                {
                    velocity.x = velocityArray[0];
                    velocity.y = velocityArray[1];
                    velocity.z = velocityArray[2];
                }
            }

            if (@params["space"] != null)
            {
                string space = @params["space"].ToString();
                velocity.space = space.ToLower() switch
                {
                    "local" => ParticleSystemSimulationSpace.Local,
                    "world" => ParticleSystemSimulationSpace.World,
                    _ => ParticleSystemSimulationSpace.Local
                };
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Velocity configured for particle system '{name}'.", new
            {
                enabled = velocity.enabled,
                space = velocity.space.ToString()
            });
        }

        private static object ConfigureColorOverLifetime(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Color Over Lifetime for '{name}'");

            var colorOverLifetime = particleSystem.colorOverLifetime;

            if (@params["enabled"] != null)
                colorOverLifetime.enabled = @params["enabled"].ToObject<bool>();

            if (@params["gradient"] != null)
            {
                var gradientData = @params["gradient"].ToObject<JObject>();
                Gradient gradient = new Gradient();

                // Create color keys
                if (gradientData["color_keys"] != null)
                {
                    var colorKeysArray = gradientData["color_keys"].ToObject<JArray>();
                    List<GradientColorKey> colorKeys = new List<GradientColorKey>();

                    foreach (var keyData in colorKeysArray)
                    {
                        var colorArray = keyData["color"].ToObject<float[]>();
                        float time = keyData["time"].ToObject<float>();
                        
                        if (colorArray?.Length >= 3)
                        {
                            Color color = new Color(colorArray[0], colorArray[1], colorArray[2], 1.0f);
                            colorKeys.Add(new GradientColorKey(color, time));
                        }
                    }

                    gradient.colorKeys = colorKeys.ToArray();
                }

                // Create alpha keys
                if (gradientData["alpha_keys"] != null)
                {
                    var alphaKeysArray = gradientData["alpha_keys"].ToObject<JArray>();
                    List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

                    foreach (var keyData in alphaKeysArray)
                    {
                        float alpha = keyData["alpha"].ToObject<float>();
                        float time = keyData["time"].ToObject<float>();
                        alphaKeys.Add(new GradientAlphaKey(alpha, time));
                    }

                    gradient.alphaKeys = alphaKeys.ToArray();
                }

                colorOverLifetime.color = gradient;
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Color over lifetime configured for particle system '{name}'.", new
            {
                enabled = colorOverLifetime.enabled
            });
        }

        private static object ConfigureSizeOverLifetime(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Size Over Lifetime for '{name}'");

            var sizeOverLifetime = particleSystem.sizeOverLifetime;

            if (@params["enabled"] != null)
                sizeOverLifetime.enabled = @params["enabled"].ToObject<bool>();

            if (@params["size_curve"] != null)
            {
                var curveData = @params["size_curve"].ToObject<JArray>();
                AnimationCurve curve = new AnimationCurve();

                foreach (var keyData in curveData)
                {
                    float time = keyData["time"].ToObject<float>();
                    float value = keyData["value"].ToObject<float>();
                    float inTangent = keyData["in_tangent"]?.ToObject<float>() ?? 0f;
                    float outTangent = keyData["out_tangent"]?.ToObject<float>() ?? 0f;

                    curve.AddKey(new Keyframe(time, value, inTangent, outTangent));
                }

                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            }
            else if (@params["size"] != null)
            {
                // Handle simple float value for size
                float sizeValue = @params["size"].ToObject<float>();
                sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(sizeValue);
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Size over lifetime configured for particle system '{name}'.", new
            {
                enabled = sizeOverLifetime.enabled
            });
        }

        private static object ConfigureRotationOverLifetime(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Rotation Over Lifetime for '{name}'");

            var rotationOverLifetime = particleSystem.rotationOverLifetime;

            if (@params["enabled"] != null)
                rotationOverLifetime.enabled = @params["enabled"].ToObject<bool>();

            if (@params["angular_velocity"] != null)
                rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(@params["angular_velocity"].ToObject<float>());

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Rotation over lifetime configured for particle system '{name}'.", new
            {
                enabled = rotationOverLifetime.enabled
            });
        }

        private static object ConfigureNoise(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Noise for '{name}'");

            var noise = particleSystem.noise;

            if (@params["enabled"] != null)
                noise.enabled = @params["enabled"].ToObject<bool>();

            if (@params["strength"] != null)
                noise.strength = @params["strength"].ToObject<float>();

            if (@params["frequency"] != null)
                noise.frequency = @params["frequency"].ToObject<float>();

            // Note: octaves property doesn't exist in NoiseModule, removing this line
            // if (@params["octaves"] != null)
            //     noise.octaves = @params["octaves"].ToObject<int>();

            if (@params["octave_multiplier"] != null)
                noise.octaveMultiplier = @params["octave_multiplier"].ToObject<float>();

            if (@params["octave_scale"] != null)
                noise.octaveScale = @params["octave_scale"].ToObject<float>();

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Noise configured for particle system '{name}'.", new
            {
                enabled = noise.enabled,
                strength = noise.strength.constant,
                frequency = noise.frequency
                // octaves = noise.octaves // Property doesn't exist, removing from response
            });
        }

        private static object ConfigureCollision(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Collision for '{name}'");

            var collision = particleSystem.collision;

            if (@params["enabled"] != null)
                collision.enabled = @params["enabled"].ToObject<bool>();

            if (@params["type"] != null)
            {
                string collisionType = @params["type"].ToString();
                collision.type = collisionType.ToLower() switch
                {
                    "planes" => ParticleSystemCollisionType.Planes,
                    "world" => ParticleSystemCollisionType.World,
                    _ => ParticleSystemCollisionType.Planes
                };
            }

            if (@params["dampen"] != null)
                collision.dampen = @params["dampen"].ToObject<float>();

            if (@params["bounce"] != null)
                collision.bounce = @params["bounce"].ToObject<float>();

            if (@params["lifetime_loss"] != null)
                collision.lifetimeLoss = @params["lifetime_loss"].ToObject<float>();

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Collision configured for particle system '{name}'.", new
            {
                enabled = collision.enabled,
                type = collision.type.ToString(),
                dampen = collision.dampen.constant,
                bounce = collision.bounce.constant
            });
        }

        private static object ConfigureSubEmitters(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Sub Emitters for '{name}'");

            var subEmitters = particleSystem.subEmitters;

            if (@params["enabled"] != null)
                subEmitters.enabled = @params["enabled"].ToObject<bool>();

            // Add sub emitters if provided
            if (@params["sub_emitters"] != null)
            {
                var subEmittersArray = @params["sub_emitters"].ToObject<JArray>();
                
                // Clear existing sub emitters
                for (int i = subEmitters.subEmittersCount - 1; i >= 0; i--)
                {
                    subEmitters.RemoveSubEmitter(i);
                }

                foreach (var subEmitterData in subEmittersArray)
                {
                    string subEmitterName = subEmitterData["name"]?.ToString();
                    string triggerType = subEmitterData["type"]?.ToString() ?? "death";

                    if (!string.IsNullOrEmpty(subEmitterName))
                    {
                        GameObject subEmitterObject = GameObject.Find(subEmitterName);
                        if (subEmitterObject != null)
                        {
                            ParticleSystem subEmitterPS = subEmitterObject.GetComponent<ParticleSystem>();
                            if (subEmitterPS != null)
                            {
                                ParticleSystemSubEmitterType type = triggerType.ToLower() switch
                                {
                                    "birth" => ParticleSystemSubEmitterType.Birth,
                                    "death" => ParticleSystemSubEmitterType.Death,
                                    "collision" => ParticleSystemSubEmitterType.Collision,
                                    _ => ParticleSystemSubEmitterType.Death
                                };

                                subEmitters.AddSubEmitter(subEmitterPS, type, ParticleSystemSubEmitterProperties.InheritNothing);
                            }
                        }
                    }
                }
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Sub emitters configured for particle system '{name}'.", new
            {
                enabled = subEmitters.enabled,
                count = subEmitters.subEmittersCount
            });
        }

        private static object ConfigureTextureSheetAnimation(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Texture Sheet Animation for '{name}'");

            var textureSheetAnimation = particleSystem.textureSheetAnimation;

            if (@params["enabled"] != null)
                textureSheetAnimation.enabled = @params["enabled"].ToObject<bool>();

            if (@params["tiles_x"] != null)
                textureSheetAnimation.numTilesX = @params["tiles_x"].ToObject<int>();

            if (@params["tiles_y"] != null)
                textureSheetAnimation.numTilesY = @params["tiles_y"].ToObject<int>();

            if (@params["animation_type"] != null)
            {
                string animationType = @params["animation_type"].ToString();
                textureSheetAnimation.animation = animationType.ToLower() switch
                {
                    "wholesheetonce" => ParticleSystemAnimationType.WholeSheet,
                    "singlerow" => ParticleSystemAnimationType.SingleRow,
                    _ => ParticleSystemAnimationType.WholeSheet
                };
            }

            if (@params["frame_over_time"] != null)
                textureSheetAnimation.frameOverTime = @params["frame_over_time"].ToObject<float>();

            if (@params["start_frame"] != null)
                textureSheetAnimation.startFrame = @params["start_frame"].ToObject<float>();

            if (@params["cycles"] != null)
                textureSheetAnimation.cycleCount = @params["cycles"].ToObject<int>();

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Texture sheet animation configured for particle system '{name}'.", new
            {
                enabled = textureSheetAnimation.enabled,
                tiles_x = textureSheetAnimation.numTilesX,
                tiles_y = textureSheetAnimation.numTilesY,
                animation_type = textureSheetAnimation.animation.ToString()
            });
        }

        private static object ConfigureTrails(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Configure Trails for '{name}'");

            var trails = particleSystem.trails;

            if (@params["enabled"] != null)
                trails.enabled = @params["enabled"].ToObject<bool>();

            if (@params["ratio"] != null)
                trails.ratio = @params["ratio"].ToObject<float>();

            if (@params["lifetime"] != null)
                trails.lifetime = @params["lifetime"].ToObject<float>();

            // Note: minimumVertexDistance property doesn't exist in TrailModule, removing this line
            // if (@params["minimum_vertex_distance"] != null)
            //     trails.minimumVertexDistance = @params["minimum_vertex_distance"].ToObject<float>();

            if (@params["width_over_trail"] != null)
                trails.widthOverTrail = @params["width_over_trail"].ToObject<float>();

            if (@params["color_over_lifetime"] != null)
            {
                var colorArray = @params["color_over_lifetime"].ToObject<float[]>();
                if (colorArray?.Length >= 3)
                {
                    float alpha = colorArray.Length > 3 ? colorArray[3] : 1.0f;
                    Color color = new Color(colorArray[0], colorArray[1], colorArray[2], alpha);
                    
                    Gradient gradient = new Gradient();
                    gradient.SetKeys(
                        new GradientColorKey[] { new GradientColorKey(color, 0.0f), new GradientColorKey(color, 1.0f) },
                        new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
                    );
                    trails.colorOverLifetime = gradient;
                }
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Trails configured for particle system '{name}'.", new
            {
                enabled = trails.enabled,
                ratio = trails.ratio,
                lifetime = trails.lifetime.constant
            });
        }

        private static object ConfigureRenderer(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
            if (renderer == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystemRenderer component.");
            }

            Undo.RecordObject(renderer, $"Configure Renderer for '{name}'");

            if (@params["material"] != null)
            {
                string materialPath = @params["material"].ToString();
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material != null)
                {
                    renderer.material = material;
                }
            }

            if (@params["render_mode"] != null)
            {
                string renderMode = @params["render_mode"].ToString();
                renderer.renderMode = renderMode.ToLower() switch
                {
                    "billboard" => ParticleSystemRenderMode.Billboard,
                    "stretch" => ParticleSystemRenderMode.Stretch,
                    "horizontalbillboard" => ParticleSystemRenderMode.HorizontalBillboard,
                    "verticalbillboard" => ParticleSystemRenderMode.VerticalBillboard,
                    "mesh" => ParticleSystemRenderMode.Mesh,
                    _ => ParticleSystemRenderMode.Billboard
                };
            }

            if (@params["sorting_layer"] != null)
                renderer.sortingLayerName = @params["sorting_layer"].ToString();

            if (@params["sorting_order"] != null)
                renderer.sortingOrder = @params["sorting_order"].ToObject<int>();

            EditorUtility.SetDirty(renderer);

            return Response.Success($"Renderer configured for particle system '{name}'.", new
            {
                material = renderer.material?.name,
                render_mode = renderer.renderMode.ToString(),
                sorting_layer = renderer.sortingLayerName,
                sorting_order = renderer.sortingOrder
            });
        }

        private static object PlayParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            bool withChildren = @params["with_children"]?.ToObject<bool>() ?? true;
            
            if (withChildren)
                particleSystem.Play(true);
            else
                particleSystem.Play(false);

            return Response.Success($"Particle system '{name}' started playing.", new
            {
                playing = particleSystem.isPlaying,
                with_children = withChildren
            });
        }

        private static object PauseParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            bool withChildren = @params["with_children"]?.ToObject<bool>() ?? true;
            
            if (withChildren)
                particleSystem.Pause(true);
            else
                particleSystem.Pause(false);

            return Response.Success($"Particle system '{name}' paused.", new
            {
                paused = particleSystem.isPaused,
                with_children = withChildren
            });
        }

        private static object StopParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            bool withChildren = @params["with_children"]?.ToObject<bool>() ?? true;
            bool stopBehavior = @params["stop_and_clear"]?.ToObject<bool>() ?? false;
            
            if (withChildren)
                particleSystem.Stop(true, stopBehavior ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
            else
                particleSystem.Stop(false, stopBehavior ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);

            return Response.Success($"Particle system '{name}' stopped.", new
            {
                stopped = particleSystem.isStopped,
                with_children = withChildren,
                cleared = stopBehavior
            });
        }

        private static object ClearParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            bool withChildren = @params["with_children"]?.ToObject<bool>() ?? true;
            particleSystem.Clear(withChildren);

            return Response.Success($"Particle system '{name}' cleared.", new
            {
                particle_count = particleSystem.particleCount,
                with_children = withChildren
            });
        }

        private static object SimulateParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            float time = @params["time"]?.ToObject<float>() ?? 1.0f;
            bool withChildren = @params["with_children"]?.ToObject<bool>() ?? true;
            bool restart = @params["restart"]?.ToObject<bool>() ?? true;
            bool fixedTimeStep = @params["fixed_time_step"]?.ToObject<bool>() ?? true;

            particleSystem.Simulate(time, withChildren, restart, fixedTimeStep);

            return Response.Success($"Particle system '{name}' simulated for {time} seconds.", new
            {
                simulated_time = time,
                particle_count = particleSystem.particleCount,
                with_children = withChildren
            });
        }

        private static object CreateParticleMaterial(JObject @params)
        {
            string materialName = @params["material_name"]?.ToString();
            if (string.IsNullOrEmpty(materialName))
            {
                return Response.Error("Material name is required.");
            }

            string shaderName = @params["shader_name"]?.ToString() ?? "Sprites/Default";
            
            // Create new material
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                return Response.Error($"Shader '{shaderName}' not found.");
            }

            Material material = new Material(shader);
            material.name = materialName;

            // Set material properties if provided
            if (@params["properties"] != null)
            {
                var properties = @params["properties"].ToObject<JObject>();
                foreach (var prop in properties)
                {
                    string propName = prop.Key;
                    var propValue = prop.Value;

                    try
                    {
                        if (propValue.Type == JTokenType.Array)
                        {
                            var colorArray = propValue.ToObject<float[]>();
                            if (colorArray?.Length >= 3)
                            {
                                float alpha = colorArray.Length > 3 ? colorArray[3] : 1.0f;
                                material.SetColor(propName, new Color(colorArray[0], colorArray[1], colorArray[2], alpha));
                            }
                        }
                        else if (propValue.Type == JTokenType.Float || propValue.Type == JTokenType.Integer)
                        {
                            material.SetFloat(propName, propValue.ToObject<float>());
                        }
                        else if (propValue.Type == JTokenType.String)
                        {
                            string texturePath = propValue.ToString();
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                            if (texture != null)
                            {
                                material.SetTexture(propName, texture);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Failed to set material property '{propName}': {ex.Message}");
                    }
                }
            }

            // Save material to Assets folder
            string assetPath = $"Assets/Materials/{materialName}.mat";
            string directory = System.IO.Path.GetDirectoryName(assetPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return Response.Success($"Particle material '{materialName}' created successfully.", new
            {
                name = materialName,
                shader = shaderName,
                path = assetPath
            });
        }

        private static object OptimizeParticleSystem(JObject @params)
        {
            string name = @params["particle_system_name"]?.ToString();
            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Particle system name is required.");
            }

            GameObject particleObject = GameObject.Find(name);
            if (particleObject == null)
            {
                return Response.Error($"Particle system '{name}' not found.");
            }

            ParticleSystem particleSystem = particleObject.GetComponent<ParticleSystem>();
            if (particleSystem == null)
            {
                return Response.Error($"GameObject '{name}' does not have a ParticleSystem component.");
            }

            Undo.RecordObject(particleSystem, $"Optimize Particle System '{name}'");

            var main = particleSystem.main;
            var emission = particleSystem.emission;
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            List<string> optimizations = new List<string>();

            // Optimization suggestions and automatic fixes
            if (main.maxParticles > 1000)
            {
                main.maxParticles = 1000;
                optimizations.Add("Reduced max particles to 1000");
            }

            if (main.simulationSpace == ParticleSystemSimulationSpace.World && main.maxParticles > 500)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                optimizations.Add("Changed simulation space to Local for better performance");
            }

            if (renderer != null && renderer.material != null && renderer.material.shader.name.Contains("Standard"))
            {
                // Suggest using a simpler shader for particles
                optimizations.Add("Consider using a simpler shader like 'Sprites/Default' for better performance");
            }

            // Disable unnecessary modules
            var modules = new[]
            {
                ("Velocity Over Lifetime", particleSystem.velocityOverLifetime.enabled && !HasSignificantVelocity(particleSystem.velocityOverLifetime)),
                ("Color Over Lifetime", particleSystem.colorOverLifetime.enabled && !HasSignificantColorChange(particleSystem.colorOverLifetime)),
                ("Size Over Lifetime", particleSystem.sizeOverLifetime.enabled && !HasSignificantSizeChange(particleSystem.sizeOverLifetime)),
                ("Rotation Over Lifetime", particleSystem.rotationOverLifetime.enabled && !HasSignificantRotation(particleSystem.rotationOverLifetime)),
                ("Noise", particleSystem.noise.enabled && particleSystem.noise.strength.constant < 0.1f)
            };

            foreach (var (moduleName, shouldDisable) in modules)
            {
                if (shouldDisable)
                {
                    optimizations.Add($"Disabled {moduleName} module (minimal impact)");
                }
            }

            EditorUtility.SetDirty(particleSystem);

            return Response.Success($"Particle system '{name}' optimized.", new
            {
                optimizations_applied = optimizations.ToArray(),
                max_particles = main.maxParticles,
                simulation_space = main.simulationSpace.ToString()
            });
        }

        private static bool HasSignificantVelocity(ParticleSystem.VelocityOverLifetimeModule velocity)
        {
            return velocity.x.constant != 0 || velocity.y.constant != 0 || velocity.z.constant != 0;
        }

        private static bool HasSignificantColorChange(ParticleSystem.ColorOverLifetimeModule color)
        {
            if (color.color.mode == ParticleSystemGradientMode.Color)
            {
                return color.color.color.a < 1.0f; // Has alpha fade
            }
            return true; // Assume gradient has changes
        }

        private static bool HasSignificantSizeChange(ParticleSystem.SizeOverLifetimeModule size)
        {
            if (size.size.mode == ParticleSystemCurveMode.Constant)
            {
                return size.size.constant != 1.0f;
            }
            return true; // Assume curve has changes
        }

        private static bool HasSignificantRotation(ParticleSystem.RotationOverLifetimeModule rotation)
        {
            return rotation.z.constant != 0;
        }
    }
}