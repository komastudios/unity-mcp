using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles Animation System operations including Animation Clips, Animator Controllers, 
    /// Timeline sequences, and animation events.
    /// </summary>
    public static class ManageAnimation
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
                    case "create_clip":
                        return CreateAnimationClip(@params);
                    case "create_controller":
                        return CreateAnimatorController(@params);
                    case "modify_controller":
                        return ModifyAnimatorController(@params);
                    case "create_timeline":
                        return CreateTimelineAsset(@params);
                    case "modify_timeline":
                        return ModifyTimelineAsset(@params);
                    case "add_animation_event":
                        return AddAnimationEvent(@params);
                    case "set_animator_parameter":
                        return SetAnimatorParameter(@params);
                    case "play_animation":
                        return PlayAnimation(@params);
                    case "record_animation":
                        return RecordAnimation(@params);
                    case "get_animation_info":
                        return GetAnimationInfo(@params);
                    case "create_state":
                        return CreateAnimatorState(@params);
                    case "create_transition":
                        return CreateStateTransition(@params);
                    case "modify_curve":
                        return ModifyAnimationCurve(@params);
                    default:
                        return Response.Error($"Unknown animation action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageAnimation] Action '{action}' failed: {e}");
                return Response.Error($"Internal error processing animation action '{action}': {e.Message}");
            }
        }

        private static object CreateAnimationClip(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/Animations";
            float length = @params["length"]?.ToObject<float>() ?? 1.0f;
            bool looping = @params["looping"]?.ToObject<bool>() ?? false;

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Animation clip name is required.");
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

                // Create animation clip
                AnimationClip clip = new AnimationClip();
                clip.name = name;
                clip.frameRate = 60f;
                
                // Set looping
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = looping;
                AnimationUtility.SetAnimationClipSettings(clip, settings);

                // Add basic curves if specified
                JArray curves = @params["curves"] as JArray;
                if (curves != null)
                {
                    foreach (JObject curveData in curves)
                    {
                        AddCurveToClip(clip, curveData);
                    }
                }

                string fullPath = $"{path}/{name}.anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"Animation clip '{name}' created successfully.", new
                {
                    name = clip.name,
                    path = fullPath,
                    length = clip.length,
                    frameRate = clip.frameRate,
                    looping = settings.loopTime
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create animation clip: {e.Message}");
            }
        }

        private static object CreateAnimatorController(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/Animators";

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Animator controller name is required.");
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

                // Create animator controller
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{path}/{name}.controller");
                
                // Add default parameters if specified
                JArray parameters = @params["parameters"] as JArray;
                if (parameters != null)
                {
                    foreach (JObject paramData in parameters)
                    {
                        AddParameterToController(controller, paramData);
                    }
                }

                // Add default layers if specified
                JArray layers = @params["layers"] as JArray;
                if (layers != null)
                {
                    foreach (JObject layerData in layers)
                    {
                        AddLayerToController(controller, layerData);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"Animator controller '{name}' created successfully.", new
                {
                    name = controller.name,
                    path = AssetDatabase.GetAssetPath(controller),
                    layers = controller.layers.Select(l => l.name).ToArray(),
                    parameters = controller.parameters.Select(p => new { p.name, p.type }).ToArray()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create animator controller: {e.Message}");
            }
        }

        private static object ModifyAnimatorController(JObject @params)
        {
            string controllerPath = @params["controller_path"]?.ToString();
            if (string.IsNullOrEmpty(controllerPath))
            {
                return Response.Error("Controller path is required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return Response.Error($"Animator controller not found at path: {controllerPath}");
            }

            try
            {
                // Add parameters
                JArray addParameters = @params["add_parameters"] as JArray;
                if (addParameters != null)
                {
                    foreach (JObject paramData in addParameters)
                    {
                        AddParameterToController(controller, paramData);
                    }
                }

                // Remove parameters
                JArray removeParameters = @params["remove_parameters"] as JArray;
                if (removeParameters != null)
                {
                    foreach (string paramName in removeParameters)
                    {
                        // Find parameter index by name
                        for (int i = 0; i < controller.parameters.Length; i++)
                        {
                            if (controller.parameters[i].name == paramName)
                            {
                                controller.RemoveParameter(i);
                                break;
                            }
                        }
                    }
                }

                // Add layers
                JArray addLayers = @params["add_layers"] as JArray;
                if (addLayers != null)
                {
                    foreach (JObject layerData in addLayers)
                    {
                        AddLayerToController(controller, layerData);
                    }
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();

                return Response.Success("Animator controller modified successfully.", new
                {
                    name = controller.name,
                    path = AssetDatabase.GetAssetPath(controller),
                    layers = controller.layers.Select(l => l.name).ToArray(),
                    parameters = controller.parameters.Select(p => new { p.name, p.type }).ToArray()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify animator controller: {e.Message}");
            }
        }

        private static object CreateTimelineAsset(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/Timeline";
            float duration = @params["duration"]?.ToObject<float>() ?? 10.0f;

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Timeline asset name is required.");
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

                // Create timeline asset
                TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = name;
                timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
                timeline.fixedDuration = duration;

                string fullPath = $"{path}/{name}.playable";
                AssetDatabase.CreateAsset(timeline, fullPath);

                // Add tracks if specified
                JArray tracks = @params["tracks"] as JArray;
                if (tracks != null)
                {
                    foreach (JObject trackData in tracks)
                    {
                        AddTrackToTimeline(timeline, trackData);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"Timeline asset '{name}' created successfully.", new
                {
                    name = timeline.name,
                    path = fullPath,
                    duration = timeline.duration,
                    trackCount = timeline.GetRootTracks().Count()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create timeline asset: {e.Message}");
            }
        }

        private static object ModifyTimelineAsset(JObject @params)
        {
            string timelinePath = @params["timeline_path"]?.ToString();
            if (string.IsNullOrEmpty(timelinePath))
            {
                return Response.Error("Timeline path is required.");
            }

            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            if (timeline == null)
            {
                return Response.Error($"Timeline asset not found at path: {timelinePath}");
            }

            try
            {
                // Modify duration
                if (@params["duration"] != null)
                {
                    timeline.fixedDuration = @params["duration"].ToObject<float>();
                }

                // Add tracks
                JArray addTracks = @params["add_tracks"] as JArray;
                if (addTracks != null)
                {
                    foreach (JObject trackData in addTracks)
                    {
                        AddTrackToTimeline(timeline, trackData);
                    }
                }

                EditorUtility.SetDirty(timeline);
                AssetDatabase.SaveAssets();

                return Response.Success("Timeline asset modified successfully.", new
                {
                    name = timeline.name,
                    path = AssetDatabase.GetAssetPath(timeline),
                    duration = timeline.duration,
                    trackCount = timeline.GetRootTracks().Count()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify timeline asset: {e.Message}");
            }
        }

        private static object AddAnimationEvent(JObject @params)
        {
            string clipPath = @params["clip_path"]?.ToString();
            float time = @params["time"]?.ToObject<float>() ?? 0f;
            string functionName = @params["function_name"]?.ToString();

            if (string.IsNullOrEmpty(clipPath) || string.IsNullOrEmpty(functionName))
            {
                return Response.Error("Clip path and function name are required.");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return Response.Error($"Animation clip not found at path: {clipPath}");
            }

            try
            {
                AnimationEvent animEvent = new AnimationEvent();
                animEvent.time = time;
                animEvent.functionName = functionName;
                
                // Add parameters if specified
                if (@params["string_parameter"] != null)
                    animEvent.stringParameter = @params["string_parameter"].ToString();
                if (@params["float_parameter"] != null)
                    animEvent.floatParameter = @params["float_parameter"].ToObject<float>();
                if (@params["int_parameter"] != null)
                    animEvent.intParameter = @params["int_parameter"].ToObject<int>();

                AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
                Array.Resize(ref events, events.Length + 1);
                events[events.Length - 1] = animEvent;
                AnimationUtility.SetAnimationEvents(clip, events);

                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                return Response.Success($"Animation event added to clip '{clip.name}'.", new
                {
                    clipName = clip.name,
                    eventTime = time,
                    functionName = functionName,
                    totalEvents = events.Length
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to add animation event: {e.Message}");
            }
        }

        private static object SetAnimatorParameter(JObject @params)
        {
            string targetName = @params["target"]?.ToString();
            string parameterName = @params["parameter_name"]?.ToString();
            JToken value = @params["value"];

            if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(parameterName))
            {
                return Response.Error("Target GameObject and parameter name are required.");
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                return Response.Error($"GameObject '{targetName}' not found.");
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return Response.Error($"GameObject '{targetName}' does not have an Animator component.");
            }

            try
            {
                // Set parameter based on type
                AnimatorControllerParameter param = null;
                if (animator.runtimeAnimatorController != null)
                {
                    param = animator.parameters.FirstOrDefault(p => p.name == parameterName);
                }

                if (param == null)
                {
                    return Response.Error($"Parameter '{parameterName}' not found in animator controller.");
                }

                switch (param.type)
                {
                    case AnimatorControllerParameterType.Bool:
                        animator.SetBool(parameterName, value.ToObject<bool>());
                        break;
                    case AnimatorControllerParameterType.Float:
                        animator.SetFloat(parameterName, value.ToObject<float>());
                        break;
                    case AnimatorControllerParameterType.Int:
                        animator.SetInteger(parameterName, value.ToObject<int>());
                        break;
                    case AnimatorControllerParameterType.Trigger:
                        if (value.ToObject<bool>())
                            animator.SetTrigger(parameterName);
                        else
                            animator.ResetTrigger(parameterName);
                        break;
                }

                return Response.Success($"Animator parameter '{parameterName}' set successfully.", new
                {
                    target = targetName,
                    parameter = parameterName,
                    value = value,
                    type = param.type.ToString()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set animator parameter: {e.Message}");
            }
        }

        private static object PlayAnimation(JObject @params)
        {
            string targetName = @params["target"]?.ToString();
            string stateName = @params["state_name"]?.ToString();
            int layer = @params["layer"]?.ToObject<int>() ?? 0;
            float normalizedTime = @params["normalized_time"]?.ToObject<float>() ?? 0f;

            if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(stateName))
            {
                return Response.Error("Target GameObject and state name are required.");
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                return Response.Error($"GameObject '{targetName}' not found.");
            }

            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
            {
                return Response.Error($"GameObject '{targetName}' does not have an Animator component.");
            }

            try
            {
                animator.Play(stateName, layer, normalizedTime);

                return Response.Success($"Animation '{stateName}' started on '{targetName}'.", new
                {
                    target = targetName,
                    stateName = stateName,
                    layer = layer,
                    normalizedTime = normalizedTime
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to play animation: {e.Message}");
            }
        }

        private static object RecordAnimation(JObject @params)
        {
            string targetName = @params["target"]?.ToString();
            string clipName = @params["clip_name"]?.ToString();
            string savePath = @params["save_path"]?.ToString() ?? "Assets/Animations";

            if (string.IsNullOrEmpty(targetName) || string.IsNullOrEmpty(clipName))
            {
                return Response.Error("Target GameObject and clip name are required.");
            }

            GameObject target = GameObject.Find(targetName);
            if (target == null)
            {
                return Response.Error($"GameObject '{targetName}' not found.");
            }

            try
            {
                // Start animation recording
                AnimationMode.StartAnimationMode();
                
                // Create new animation clip
                AnimationClip clip = new AnimationClip();
                clip.name = clipName;

                // Record current state as keyframe at time 0
                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(target, clip, 0f);
                AnimationMode.EndSampling();

                string fullPath = $"{savePath}/{clipName}.anim";
                AssetDatabase.CreateAsset(clip, fullPath);
                AssetDatabase.SaveAssets();

                AnimationMode.StopAnimationMode();

                return Response.Success($"Animation recording started for '{clipName}'.", new
                {
                    target = targetName,
                    clipName = clipName,
                    path = fullPath,
                    isRecording = AnimationMode.InAnimationMode()
                });
            }
            catch (Exception e)
            {
                AnimationMode.StopAnimationMode();
                return Response.Error($"Failed to start animation recording: {e.Message}");
            }
        }

        private static object GetAnimationInfo(JObject @params)
        {
            string targetName = @params["target"]?.ToString();
            string clipPath = @params["clip_path"]?.ToString();

            try
            {
                if (!string.IsNullOrEmpty(targetName))
                {
                    // Get info from GameObject's Animator
                    GameObject target = GameObject.Find(targetName);
                    if (target == null)
                    {
                        return Response.Error($"GameObject '{targetName}' not found.");
                    }

                    Animator animator = target.GetComponent<Animator>();
                    if (animator == null)
                    {
                        return Response.Error($"GameObject '{targetName}' does not have an Animator component.");
                    }

                    var currentState = animator.GetCurrentAnimatorStateInfo(0);
                    return Response.Success($"Animation info for '{targetName}'.", new
                    {
                        target = targetName,
                        hasController = animator.runtimeAnimatorController != null,
                        controllerName = animator.runtimeAnimatorController?.name,
                        currentState = new
                        {
                            fullPathHash = currentState.fullPathHash,
                            normalizedTime = currentState.normalizedTime,
                            length = currentState.length,
                            speed = currentState.speed
                        },
                        parameters = animator.parameters?.Select(p => new { p.name, p.type, p.defaultBool, p.defaultFloat, p.defaultInt }).ToArray()
                    });
                }
                else if (!string.IsNullOrEmpty(clipPath))
                {
                    // Get info from Animation Clip asset
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip == null)
                    {
                        return Response.Error($"Animation clip not found at path: {clipPath}");
                    }

                    var events = AnimationUtility.GetAnimationEvents(clip);
                    var settings = AnimationUtility.GetAnimationClipSettings(clip);

                    return Response.Success($"Animation clip info for '{clip.name}'.", new
                    {
                        name = clip.name,
                        path = clipPath,
                        length = clip.length,
                        frameRate = clip.frameRate,
                        looping = settings.loopTime,
                        events = events.Select(e => new { e.time, e.functionName, e.stringParameter, e.floatParameter, e.intParameter }).ToArray(),
                        curves = AnimationUtility.GetCurveBindings(clip).Select(b => new { b.propertyName, b.path, b.type }).ToArray()
                    });
                }
                else
                {
                    return Response.Error("Either target GameObject name or clip path is required.");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get animation info: {e.Message}");
            }
        }

        private static object CreateAnimatorState(JObject @params)
        {
            string controllerPath = @params["controller_path"]?.ToString();
            string stateName = @params["state_name"]?.ToString();
            string clipPath = @params["clip_path"]?.ToString();
            int layerIndex = @params["layer_index"]?.ToObject<int>() ?? 0;

            if (string.IsNullOrEmpty(controllerPath) || string.IsNullOrEmpty(stateName))
            {
                return Response.Error("Controller path and state name are required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return Response.Error($"Animator controller not found at path: {controllerPath}");
            }

            try
            {
                AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;
                AnimatorState state = stateMachine.AddState(stateName);

                if (!string.IsNullOrEmpty(clipPath))
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (clip != null)
                    {
                        state.motion = clip;
                    }
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();

                return Response.Success($"Animator state '{stateName}' created successfully.", new
                {
                    stateName = state.name,
                    layerIndex = layerIndex,
                    hasMotion = state.motion != null,
                    motionName = state.motion?.name
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create animator state: {e.Message}");
            }
        }

        private static object CreateStateTransition(JObject @params)
        {
            string controllerPath = @params["controller_path"]?.ToString();
            string fromState = @params["from_state"]?.ToString();
            string toState = @params["to_state"]?.ToString();
            int layerIndex = @params["layer_index"]?.ToObject<int>() ?? 0;

            if (string.IsNullOrEmpty(controllerPath) || string.IsNullOrEmpty(fromState) || string.IsNullOrEmpty(toState))
            {
                return Response.Error("Controller path, from state, and to state are required.");
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                return Response.Error($"Animator controller not found at path: {controllerPath}");
            }

            try
            {
                AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;
                
                AnimatorState fromStateObj = stateMachine.states.FirstOrDefault(s => s.state.name == fromState).state;
                AnimatorState toStateObj = stateMachine.states.FirstOrDefault(s => s.state.name == toState).state;

                if (fromStateObj == null)
                {
                    return Response.Error($"From state '{fromState}' not found.");
                }
                if (toStateObj == null)
                {
                    return Response.Error($"To state '{toState}' not found.");
                }

                AnimatorStateTransition transition = fromStateObj.AddTransition(toStateObj);
                
                // Set transition properties if specified
                if (@params["duration"] != null)
                    transition.duration = @params["duration"].ToObject<float>();
                if (@params["has_exit_time"] != null)
                    transition.hasExitTime = @params["has_exit_time"].ToObject<bool>();
                if (@params["exit_time"] != null)
                    transition.exitTime = @params["exit_time"].ToObject<float>();

                // Add conditions if specified
                JArray conditions = @params["conditions"] as JArray;
                if (conditions != null)
                {
                    foreach (JObject conditionData in conditions)
                    {
                        string parameter = conditionData["parameter"]?.ToString();
                        string mode = conditionData["mode"]?.ToString();
                        JToken threshold = conditionData["threshold"];

                        if (!string.IsNullOrEmpty(parameter) && !string.IsNullOrEmpty(mode))
                        {
                            AnimatorConditionMode conditionMode = (AnimatorConditionMode)Enum.Parse(typeof(AnimatorConditionMode), mode);
                            float thresholdValue = threshold?.ToObject<float>() ?? 0f;
                            transition.AddCondition(conditionMode, thresholdValue, parameter);
                        }
                    }
                }

                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssets();

                return Response.Success($"Transition created from '{fromState}' to '{toState}'.", new
                {
                    fromState = fromState,
                    toState = toState,
                    duration = transition.duration,
                    hasExitTime = transition.hasExitTime,
                    conditionCount = transition.conditions.Length
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create state transition: {e.Message}");
            }
        }

        private static object ModifyAnimationCurve(JObject @params)
        {
            string clipPath = @params["clip_path"]?.ToString();
            string propertyName = @params["property_name"]?.ToString();
            string targetPath = @params["target_path"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(clipPath) || string.IsNullOrEmpty(propertyName))
            {
                return Response.Error("Clip path and property name are required.");
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                return Response.Error($"Animation clip not found at path: {clipPath}");
            }

            try
            {
                // Create or modify animation curve
                AnimationCurve curve = new AnimationCurve();
                
                // Add keyframes if specified
                JArray keyframes = @params["keyframes"] as JArray;
                if (keyframes != null)
                {
                    foreach (JObject keyframeData in keyframes)
                    {
                        float time = keyframeData["time"]?.ToObject<float>() ?? 0f;
                        float value = keyframeData["value"]?.ToObject<float>() ?? 0f;
                        curve.AddKey(time, value);
                    }
                }

                // Set the curve on the clip
                Type componentType = typeof(Transform); // Default to Transform
                if (@params["component_type"] != null)
                {
                    string componentTypeName = @params["component_type"].ToString();
                    componentType = Type.GetType($"UnityEngine.{componentTypeName}, UnityEngine") ?? typeof(Transform);
                }

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(targetPath, componentType, propertyName);
                AnimationUtility.SetEditorCurve(clip, binding, curve);

                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssets();

                return Response.Success($"Animation curve modified for property '{propertyName}'.", new
                {
                    clipName = clip.name,
                    propertyName = propertyName,
                    targetPath = targetPath,
                    keyframeCount = curve.keys.Length
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify animation curve: {e.Message}");
            }
        }

        // Helper methods
        private static void AddCurveToClip(AnimationClip clip, JObject curveData)
        {
            string propertyName = curveData["property"]?.ToString();
            string targetPath = curveData["target_path"]?.ToString() ?? "";
            JArray keyframes = curveData["keyframes"] as JArray;

            if (string.IsNullOrEmpty(propertyName) || keyframes == null) return;

            AnimationCurve curve = new AnimationCurve();
            foreach (JObject keyframe in keyframes)
            {
                float time = keyframe["time"]?.ToObject<float>() ?? 0f;
                float value = keyframe["value"]?.ToObject<float>() ?? 0f;
                curve.AddKey(time, value);
            }

            Type componentType = typeof(Transform);
            if (curveData["component_type"] != null)
            {
                string componentTypeName = curveData["component_type"].ToString();
                componentType = Type.GetType($"UnityEngine.{componentTypeName}, UnityEngine") ?? typeof(Transform);
            }

            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(targetPath, componentType, propertyName);
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        private static void AddParameterToController(AnimatorController controller, JObject paramData)
        {
            string paramName = paramData["name"]?.ToString();
            string paramType = paramData["type"]?.ToString();

            if (string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(paramType)) return;

            AnimatorControllerParameterType type = (AnimatorControllerParameterType)Enum.Parse(typeof(AnimatorControllerParameterType), paramType);
            controller.AddParameter(paramName, type);

            // Set default value if specified
            AnimatorControllerParameter param = controller.parameters.LastOrDefault(p => p.name == paramName);
            if (param != null)
            {
                switch (type)
                {
                    case AnimatorControllerParameterType.Bool:
                        if (paramData["default_value"] != null)
                            param.defaultBool = paramData["default_value"].ToObject<bool>();
                        break;
                    case AnimatorControllerParameterType.Float:
                        if (paramData["default_value"] != null)
                            param.defaultFloat = paramData["default_value"].ToObject<float>();
                        break;
                    case AnimatorControllerParameterType.Int:
                        if (paramData["default_value"] != null)
                            param.defaultInt = paramData["default_value"].ToObject<int>();
                        break;
                }
            }
        }

        private static void AddLayerToController(AnimatorController controller, JObject layerData)
        {
            string layerName = layerData["name"]?.ToString();
            if (string.IsNullOrEmpty(layerName)) return;

            AnimatorControllerLayer layer = new AnimatorControllerLayer();
            layer.name = layerName;
            layer.defaultWeight = layerData["weight"]?.ToObject<float>() ?? 1f;
            layer.stateMachine = new AnimatorStateMachine();
            layer.stateMachine.name = layerName;

            controller.AddLayer(layer);
        }

        private static void AddTrackToTimeline(TimelineAsset timeline, JObject trackData)
        {
            string trackType = trackData["type"]?.ToString();
            string trackName = trackData["name"]?.ToString();

            if (string.IsNullOrEmpty(trackType)) return;

            TrackAsset track = null;
            switch (trackType.ToLower())
            {
                case "animation":
                    track = timeline.CreateTrack<AnimationTrack>(null, trackName);
                    break;
                case "audio":
                    track = timeline.CreateTrack<AudioTrack>(null, trackName);
                    break;
                case "activation":
                    track = timeline.CreateTrack<ActivationTrack>(null, trackName);
                    break;
            }

            if (track != null && !string.IsNullOrEmpty(trackName))
            {
                track.name = trackName;
            }
        }
    }
}