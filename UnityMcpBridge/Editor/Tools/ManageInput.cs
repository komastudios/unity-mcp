using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityMcpBridge.Editor.Helpers;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
#endif

namespace UnityMcpBridge.Editor.Tools
{
    /// <summary>
    /// Handles Input System operations including Input Actions, Control schemes, 
    /// and Device handling for both Legacy and New Input System.
    /// </summary>
    public static class ManageInput
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
                    case "create_input_actions":
                        return CreateInputActions(@params);
                    case "modify_input_actions":
                        return ModifyInputActions(@params);
                    case "get_input_value":
                        return GetInputValue(@params);
                    case "simulate_input":
                        return SimulateInput(@params);
                    case "get_connected_devices":
                        return GetConnectedDevices(@params);
                    case "create_control_scheme":
                        return CreateControlScheme(@params);
                    case "set_input_settings":
                        return SetInputSettings(@params);
                    case "get_input_info":
                        return GetInputInfo(@params);
                    case "bind_input_action":
                        return BindInputAction(@params);
                    case "create_input_component":
                        return CreateInputComponent(@params);
                    default:
                        return Response.Error($"Unknown input action: '{action}'.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ManageInput] Action '{action}' failed: {e}");
                return Response.Error($"Internal error processing input action '{action}': {e.Message}");
            }
        }

        private static object CreateInputActions(JObject @params)
        {
            string name = @params["name"]?.ToString();
            string path = @params["path"]?.ToString() ?? "Assets/InputActions";

            if (string.IsNullOrEmpty(name))
            {
                return Response.Error("Input actions name is required.");
            }

            try
            {
#if ENABLE_INPUT_SYSTEM
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

                InputActionAsset inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
                inputActions.name = name;

                // Create default action map if specified
                if (@params["action_maps"] != null)
                {
                    JArray actionMaps = @params["action_maps"] as JArray;
                    foreach (JObject mapData in actionMaps)
                    {
                        string mapName = mapData["name"]?.ToString() ?? "Default";
                        var actionMap = inputActions.AddActionMap(mapName);

                        // Add actions to the map
                        if (mapData["actions"] != null)
                        {
                            JArray actions = mapData["actions"] as JArray;
                            foreach (JObject actionData in actions)
                            {
                                string actionName = actionData["name"]?.ToString();
                                string actionType = actionData["type"]?.ToString() ?? "Button";
                                
                                if (!string.IsNullOrEmpty(actionName))
                                {
                                    InputActionType inputActionType = (InputActionType)Enum.Parse(typeof(InputActionType), actionType);
                                    var inputAction = actionMap.AddAction(actionName, inputActionType);

                                    // Add bindings if specified
                                    if (actionData["bindings"] != null)
                                    {
                                        JArray bindings = actionData["bindings"] as JArray;
                                        foreach (string binding in bindings)
                                        {
                                            inputAction.AddBinding(binding);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                string fullPath = $"{path}/{name}.inputactions";
                AssetDatabase.CreateAsset(inputActions, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"Input actions '{name}' created successfully.", new
                {
                    name = inputActions.name,
                    path = fullPath,
                    actionMaps = inputActions.actionMaps.Count(),
                    totalActions = inputActions.actionMaps.SelectMany(map => map.actions).Count()
                });
#else
                return Response.Error("New Input System is not enabled. Enable it in Project Settings > Player > Configuration.");
#endif
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create input actions: {e.Message}");
            }
        }

        private static object ModifyInputActions(JObject @params)
        {
            string assetPath = @params["asset_path"]?.ToString();
            if (string.IsNullOrEmpty(assetPath))
            {
                return Response.Error("Input actions asset path is required.");
            }

#if ENABLE_INPUT_SYSTEM
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (inputActions == null)
            {
                return Response.Error($"Input actions asset not found at path: {assetPath}");
            }

            try
            {
                // Add new action map
                if (@params["add_action_map"] != null)
                {
                    JObject mapData = @params["add_action_map"] as JObject;
                    string mapName = mapData["name"]?.ToString();
                    if (!string.IsNullOrEmpty(mapName))
                    {
                        var actionMap = inputActions.AddActionMap(mapName);
                        
                        // Add actions to the new map
                        if (mapData["actions"] != null)
                        {
                            JArray actions = mapData["actions"] as JArray;
                            foreach (JObject actionData in actions)
                            {
                                string actionName = actionData["name"]?.ToString();
                                string actionType = actionData["type"]?.ToString() ?? "Button";
                                
                                if (!string.IsNullOrEmpty(actionName))
                                {
                                    InputActionType inputActionType = (InputActionType)Enum.Parse(typeof(InputActionType), actionType);
                                    var inputAction = actionMap.AddAction(actionName, inputActionType);

                                    // Add bindings
                                    if (actionData["bindings"] != null)
                                    {
                                        JArray bindings = actionData["bindings"] as JArray;
                                        foreach (string binding in bindings)
                                        {
                                            inputAction.AddBinding(binding);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Modify existing action
                if (@params["modify_action"] != null)
                {
                    JObject actionData = @params["modify_action"] as JObject;
                    string mapName = actionData["map_name"]?.ToString();
                    string actionName = actionData["action_name"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(mapName) && !string.IsNullOrEmpty(actionName))
                    {
                        var actionMap = inputActions.FindActionMap(mapName);
                        if (actionMap != null)
                        {
                            var action = actionMap.FindAction(actionName);
                            if (action != null)
                            {
                                // Add new binding
                                if (actionData["add_binding"] != null)
                                {
                                    string binding = actionData["add_binding"].ToString();
                                    action.AddBinding(binding);
                                }
                            }
                        }
                    }
                }

                EditorUtility.SetDirty(inputActions);
                AssetDatabase.SaveAssets();

                return Response.Success($"Input actions '{inputActions.name}' modified successfully.", new
                {
                    name = inputActions.name,
                    path = assetPath,
                    actionMaps = inputActions.actionMaps.Count(),
                    totalActions = inputActions.actionMaps.SelectMany(map => map.actions).Count()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify input actions: {e.Message}");
            }
#else
            return Response.Error("New Input System is not enabled. Enable it in Project Settings > Player > Configuration.");
#endif
        }

        private static object GetInputValue(JObject @params)
        {
            string inputName = @params["input_name"]?.ToString();
            string inputType = @params["input_type"]?.ToString()?.ToLower() ?? "legacy";

            if (string.IsNullOrEmpty(inputName))
            {
                return Response.Error("Input name is required.");
            }

            try
            {
                if (inputType == "legacy")
                {
                    // Legacy Input System
                    string axisType = @params["axis_type"]?.ToString()?.ToLower() ?? "axis";
                    
                    if (axisType == "button")
                    {
                        bool buttonDown = Input.GetButtonDown(inputName);
                        bool button = Input.GetButton(inputName);
                        bool buttonUp = Input.GetButtonUp(inputName);

                        return Response.Success($"Legacy button input '{inputName}' values.", new
                        {
                            inputName = inputName,
                            inputType = "legacy_button",
                            buttonDown = buttonDown,
                            button = button,
                            buttonUp = buttonUp
                        });
                    }
                    else if (axisType == "key")
                    {
                        KeyCode keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), inputName);
                        bool keyDown = Input.GetKeyDown(keyCode);
                        bool key = Input.GetKey(keyCode);
                        bool keyUp = Input.GetKeyUp(keyCode);

                        return Response.Success($"Legacy key input '{inputName}' values.", new
                        {
                            inputName = inputName,
                            inputType = "legacy_key",
                            keyDown = keyDown,
                            key = key,
                            keyUp = keyUp
                        });
                    }
                    else
                    {
                        float axisValue = Input.GetAxis(inputName);
                        float axisRaw = Input.GetAxisRaw(inputName);

                        return Response.Success($"Legacy axis input '{inputName}' values.", new
                        {
                            inputName = inputName,
                            inputType = "legacy_axis",
                            axisValue = axisValue,
                            axisRaw = axisRaw
                        });
                    }
                }
#if ENABLE_INPUT_SYSTEM
                else if (inputType == "new")
                {
                    // New Input System
                    string actionPath = @params["action_path"]?.ToString();
                    if (string.IsNullOrEmpty(actionPath))
                    {
                        return Response.Error("Action path is required for new input system.");
                    }

                    var action = InputSystem.actions.FindAction(actionPath);
                    if (action != null)
                    {
                        bool triggered = action.triggered;
                        bool performed = action.WasPerformedThisFrame();
                        bool pressed = action.IsPressed();
                        var value = action.ReadValueAsObject();

                        return Response.Success($"New input action '{actionPath}' values.", new
                        {
                            inputName = inputName,
                            actionPath = actionPath,
                            inputType = "new_input_system",
                            triggered = triggered,
                            performed = performed,
                            pressed = pressed,
                            value = value?.ToString() ?? "null"
                        });
                    }
                    else
                    {
                        return Response.Error($"Input action not found: {actionPath}");
                    }
                }
#endif
                else
                {
                    return Response.Error($"Unknown input type: {inputType}");
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get input value: {e.Message}");
            }
        }

        private static object SimulateInput(JObject @params)
        {
            string inputType = @params["input_type"]?.ToString()?.ToLower() ?? "legacy";
            
            try
            {
#if ENABLE_INPUT_SYSTEM
                if (inputType == "new")
                {
                    string deviceType = @params["device_type"]?.ToString()?.ToLower() ?? "keyboard";
                    
                    if (deviceType == "keyboard")
                    {
                        string key = @params["key"]?.ToString();
                        if (!string.IsNullOrEmpty(key))
                        {
                            Key keyEnum = (Key)Enum.Parse(typeof(Key), key);
                            var keyboard = InputSystem.AddDevice<Keyboard>();
                            
                            using (StateEvent.From(keyboard, out var eventPtr))
                            {
                                keyboard[keyEnum].WriteValueIntoEvent(1.0f, eventPtr);
                                InputSystem.QueueEvent(eventPtr);
                            }

                            return Response.Success($"Simulated keyboard input: {key}", new
                            {
                                deviceType = "keyboard",
                                key = key,
                                simulated = true
                            });
                        }
                    }
                    else if (deviceType == "mouse")
                    {
                        if (@params["position"] != null)
                        {
                            JObject position = @params["position"] as JObject;
                            Vector2 mousePos = new Vector2(
                                position["x"]?.ToObject<float>() ?? 0f,
                                position["y"]?.ToObject<float>() ?? 0f
                            );

                            var mouse = InputSystem.AddDevice<Mouse>();
                            InputSystem.QueueStateEvent(mouse, new MouseState { position = mousePos });

                            return Response.Success($"Simulated mouse position", new
                            {
                                deviceType = "mouse",
                                position = new { x = mousePos.x, y = mousePos.y },
                                simulated = true
                            });
                        }
                    }

                    return Response.Error("Invalid simulation parameters for new input system.");
                }
                else
#endif
                {
                    // Legacy input simulation is limited in editor
                    return Response.Success("Legacy input simulation requested (limited in editor mode).", new
                    {
                        inputType = "legacy",
                        simulated = false,
                        reason = "Legacy input simulation requires runtime"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to simulate input: {e.Message}");
            }
        }

        private static object GetConnectedDevices(JObject @params)
        {
            try
            {
#if ENABLE_INPUT_SYSTEM
                var devices = InputSystem.devices;
                var deviceInfo = devices.Select(device => new
                {
                    name = device.name,
                    displayName = device.displayName,
                    deviceClass = device.GetType().Name,
                    enabled = device.enabled,
                    canRunInBackground = device.canRunInBackground,
                    description = new
                    {
                        interfaceName = device.description.interfaceName,
                        product = device.description.product,
                        manufacturer = device.description.manufacturer
                    }
                }).ToArray();

                return Response.Success("Connected input devices retrieved.", new
                {
                    deviceCount = devices.Count,
                    devices = deviceInfo,
                    inputSystemEnabled = true
                });
#else
                // Legacy input system device detection is limited
                var legacyDevices = new List<object>();
                
                // Check for basic input devices
                legacyDevices.Add(new { name = "Keyboard", type = "Keyboard", available = true });
                legacyDevices.Add(new { name = "Mouse", type = "Mouse", available = true });
                
                // Check for joysticks
                string[] joystickNames = Input.GetJoystickNames();
                for (int i = 0; i < joystickNames.Length; i++)
                {
                    if (!string.IsNullOrEmpty(joystickNames[i]))
                    {
                        legacyDevices.Add(new { name = joystickNames[i], type = "Joystick", index = i });
                    }
                }

                return Response.Success("Legacy input devices detected.", new
                {
                    deviceCount = legacyDevices.Count,
                    devices = legacyDevices.ToArray(),
                    inputSystemEnabled = false
                });
#endif
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get connected devices: {e.Message}");
            }
        }

        private static object CreateControlScheme(JObject @params)
        {
#if ENABLE_INPUT_SYSTEM
            string assetPath = @params["asset_path"]?.ToString();
            string schemeName = @params["scheme_name"]?.ToString();

            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(schemeName))
            {
                return Response.Error("Asset path and scheme name are required.");
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (inputActions == null)
            {
                return Response.Error($"Input actions asset not found at path: {assetPath}");
            }

            try
            {
                var controlScheme = new InputControlScheme(schemeName);
                
                // Add device requirements
                if (@params["devices"] != null)
                {
                    JArray devices = @params["devices"] as JArray;
                    var deviceRequirements = new List<InputControlScheme.DeviceRequirement>();
                    
                    foreach (string deviceName in devices)
                    {
                        deviceRequirements.Add(new InputControlScheme.DeviceRequirement
                        {
                            controlPath = $"<{deviceName}>",
                            isOptional = false
                        });
                    }
                    
                    controlScheme = controlScheme.WithDeviceRequirements(deviceRequirements.ToArray());
                }

                // Add the control scheme to the input actions
                var schemes = inputActions.controlSchemes.ToList();
                schemes.Add(controlScheme);
                inputActions.controlSchemes = schemes.ToArray();

                EditorUtility.SetDirty(inputActions);
                AssetDatabase.SaveAssets();

                return Response.Success($"Control scheme '{schemeName}' created successfully.", new
                {
                    schemeName = schemeName,
                    assetPath = assetPath,
                    deviceCount = controlScheme.deviceRequirements.Count()
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create control scheme: {e.Message}");
            }
#else
            return Response.Error("New Input System is not enabled. Control schemes require the new Input System.");
#endif
        }

        private static object SetInputSettings(JObject @params)
        {
            try
            {
                // Legacy Input Manager settings
                if (@params["legacy_settings"] != null)
                {
                    JObject legacySettings = @params["legacy_settings"] as JObject;
                    
                    // Note: Modifying Input Manager settings programmatically is complex
                    // This would typically require SerializedObject manipulation
                    Debug.Log("Legacy input settings modification requested (requires SerializedObject manipulation)");
                }

#if ENABLE_INPUT_SYSTEM
                // New Input System settings
                if (@params["new_input_settings"] != null)
                {
                    JObject newSettings = @params["new_input_settings"] as JObject;
                    
                    if (newSettings["update_mode"] != null)
                    {
                        string updateMode = newSettings["update_mode"].ToString();
                        InputSystem.settings.updateMode = (InputSettings.UpdateMode)Enum.Parse(typeof(InputSettings.UpdateMode), updateMode);
                    }
                    
                    if (newSettings["compensate_for_screen_orientation"] != null)
                    {
                        InputSystem.settings.compensateForScreenOrientation = newSettings["compensate_for_screen_orientation"].ToObject<bool>();
                    }
                    
                    if (newSettings["filter_noise_on_current"] != null)
                    {
                        InputSystem.settings.filterNoiseOnCurrent = newSettings["filter_noise_on_current"].ToObject<bool>();
                    }
                }
#endif

                return Response.Success("Input settings updated successfully.", new
                {
#if ENABLE_INPUT_SYSTEM
                    newInputSystemEnabled = true,
                    updateMode = InputSystem.settings.updateMode.ToString(),
                    compensateForScreenOrientation = InputSystem.settings.compensateForScreenOrientation,
                    filterNoiseOnCurrent = InputSystem.settings.filterNoiseOnCurrent
#else
                    newInputSystemEnabled = false
#endif
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set input settings: {e.Message}");
            }
        }

        private static object GetInputInfo(JObject @params)
        {
            try
            {
                var info = new
                {
#if ENABLE_INPUT_SYSTEM
                    newInputSystemEnabled = true,
                    inputSystemVersion = InputSystem.version.ToString(),
                    updateMode = InputSystem.settings.updateMode.ToString(),
                    deviceCount = InputSystem.devices.Count,
                    activeDevices = InputSystem.devices.Where(d => d.enabled).Count(),
#else
                    newInputSystemEnabled = false,
#endif
                    legacyInputEnabled = true,
                    mousePosition = new { x = Input.mousePosition.x, y = Input.mousePosition.y, z = Input.mousePosition.z },
                    joystickCount = Input.GetJoystickNames().Length,
                    joystickNames = Input.GetJoystickNames()
                };

                return Response.Success("Input system information retrieved.", info);
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get input info: {e.Message}");
            }
        }

        private static object BindInputAction(JObject @params)
        {
#if ENABLE_INPUT_SYSTEM
            string assetPath = @params["asset_path"]?.ToString();
            string mapName = @params["map_name"]?.ToString();
            string actionName = @params["action_name"]?.ToString();
            string binding = @params["binding"]?.ToString();

            if (string.IsNullOrEmpty(assetPath) || string.IsNullOrEmpty(mapName) || 
                string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(binding))
            {
                return Response.Error("Asset path, map name, action name, and binding are required.");
            }

            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
            if (inputActions == null)
            {
                return Response.Error($"Input actions asset not found at path: {assetPath}");
            }

            try
            {
                var actionMap = inputActions.FindActionMap(mapName);
                if (actionMap == null)
                {
                    return Response.Error($"Action map '{mapName}' not found.");
                }

                var action = actionMap.FindAction(actionName);
                if (action == null)
                {
                    return Response.Error($"Action '{actionName}' not found in map '{mapName}'.");
                }

                // Add the binding
                action.AddBinding(binding);

                EditorUtility.SetDirty(inputActions);
                AssetDatabase.SaveAssets();

                return Response.Success($"Binding '{binding}' added to action '{actionName}'.", new
                {
                    assetPath = assetPath,
                    mapName = mapName,
                    actionName = actionName,
                    binding = binding,
                    totalBindings = action.bindings.Count
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to bind input action: {e.Message}");
            }
#else
            return Response.Error("New Input System is not enabled. Input action binding requires the new Input System.");
#endif
        }

        private static object CreateInputComponent(JObject @params)
        {
            string gameObjectName = @params["gameobject_name"]?.ToString();
            string componentType = @params["component_type"]?.ToString()?.ToLower() ?? "player_input";

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
#if ENABLE_INPUT_SYSTEM
                if (componentType == "player_input")
                {
                    PlayerInput playerInput = targetObject.GetComponent<PlayerInput>();
                    if (playerInput == null)
                    {
                        playerInput = targetObject.AddComponent<PlayerInput>();
                    }

                    // Set input actions asset if specified
                    if (@params["input_actions_path"] != null)
                    {
                        string actionsPath = @params["input_actions_path"].ToString();
                        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(actionsPath);
                        if (actions != null)
                        {
                            playerInput.actions = actions;
                        }
                    }

                    // Set behavior
                    if (@params["behavior"] != null)
                    {
                        string behavior = @params["behavior"].ToString();
                        playerInput.notificationBehavior = (PlayerNotifications)Enum.Parse(typeof(PlayerNotifications), behavior);
                    }

                    EditorUtility.SetDirty(targetObject);

                    return Response.Success($"PlayerInput component added to '{gameObjectName}'.", new
                    {
                        gameObjectName = gameObjectName,
                        componentType = "PlayerInput",
                        actionsAsset = playerInput.actions?.name ?? "None",
                        notificationBehavior = playerInput.notificationBehavior.ToString()
                    });
                }
                else
                {
                    return Response.Error($"Unknown input component type: {componentType}");
                }
#else
                return Response.Error("New Input System is not enabled. Input components require the new Input System.");
#endif
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create input component: {e.Message}");
            }
        }
    }
}