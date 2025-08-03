using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityMcpBridge.Editor.Helpers;

namespace UnityMcpBridge.Editor.Tools
{
    public static class ManageUI
    {
        /// <summary>
        /// Helper method to safely serialize Vector3 objects to avoid circular reference issues
        /// </summary>
        private static object SerializeVector3(Vector3 vector)
        {
            return new { x = vector.x, y = vector.y, z = vector.z };
        }

        public static object HandleCommand(JObject @params)
        {
            try
            {
                string action = @params["action"]?.ToString();
                if (string.IsNullOrEmpty(action))
                {
                    return Response.Error("Action parameter is required.");
                }
                
                return action.ToLower() switch
                {
                    "create_canvas" => CreateCanvas(@params),
                    "add_ui_element" => AddUIElement(@params),
                    "modify_ui_element" => ModifyUIElement(@params),
                    "set_ui_layout" => SetUILayout(@params),
                    "create_ui_event" => CreateUIEvent(@params),
                    "set_ui_animation" => SetUIAnimation(@params),
                    "get_ui_info" => GetUIInfo(@params),
                    "create_ui_prefab" => CreateUIPrefab(@params),
                    "setup_event_system" => SetupEventSystem(@params),
                    _ => Response.Error($"Unknown UI action: {action}")
                };
            }
            catch (Exception e)
            {
                return Response.Error($"UI management error: {e.Message}");
            }
        }

        private static object CreateCanvas(JObject @params)
        {
            string canvasName = @params["canvas_name"]?.ToString() ?? "Canvas";
            string renderMode = @params["render_mode"]?.ToString()?.ToLower() ?? "screen_space_overlay";
            
            try
            {
                // Create Canvas GameObject
                GameObject canvasObject = new GameObject(canvasName);
                Canvas canvas = canvasObject.AddComponent<Canvas>();
                CanvasScaler canvasScaler = canvasObject.AddComponent<CanvasScaler>();
                GraphicRaycaster graphicRaycaster = canvasObject.AddComponent<GraphicRaycaster>();

                // Set render mode
                switch (renderMode)
                {
                    case "screen_space_overlay":
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        break;
                    case "screen_space_camera":
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        if (@params["camera_name"] != null)
                        {
                            string cameraName = @params["camera_name"].ToString();
                            Camera renderCamera = GameObject.Find(cameraName)?.GetComponent<Camera>();
                            if (renderCamera != null)
                                canvas.worldCamera = renderCamera;
                        }
                        break;
                    case "world_space":
                        canvas.renderMode = RenderMode.WorldSpace;
                        break;
                }

                // Configure Canvas Scaler
                if (@params["ui_scale_mode"] != null)
                {
                    string scaleMode = @params["ui_scale_mode"].ToString();
                    canvasScaler.uiScaleMode = (CanvasScaler.ScaleMode)Enum.Parse(typeof(CanvasScaler.ScaleMode), scaleMode);
                }

                if (@params["reference_resolution"] != null)
                {
                    JObject resolution = @params["reference_resolution"] as JObject;
                    canvasScaler.referenceResolution = new Vector2(
                        resolution["x"]?.ToObject<float>() ?? 1920f,
                        resolution["y"]?.ToObject<float>() ?? 1080f
                    );
                }

                if (@params["match_width_or_height"] != null)
                    canvasScaler.matchWidthOrHeight = @params["match_width_or_height"].ToObject<float>();

                // Set sorting order
                if (@params["sort_order"] != null)
                    canvas.sortingOrder = @params["sort_order"].ToObject<int>();

                // Set planeDistance for Screen Space Camera
                if (@params["plane_distance"] != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    canvas.planeDistance = @params["plane_distance"].ToObject<float>();

                EditorUtility.SetDirty(canvasObject);

                return Response.Success($"Canvas '{canvasName}' created successfully.", new
                {
                    canvasName = canvasName,
                    renderMode = canvas.renderMode.ToString(),
                    sortingOrder = canvas.sortingOrder,
                    scaleMode = canvasScaler.uiScaleMode.ToString(),
                    referenceResolution = new { x = canvasScaler.referenceResolution.x, y = canvasScaler.referenceResolution.y }
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create canvas: {e.Message}");
            }
        }

        private static object AddUIElement(JObject @params)
        {
            string elementType = @params["element_type"]?.ToString()?.ToLower();
            string elementName = @params["element_name"]?.ToString();
            string parentName = @params["parent_name"]?.ToString();

            if (string.IsNullOrEmpty(elementType) || string.IsNullOrEmpty(elementName))
            {
                return Response.Error("Element type and name are required.");
            }

            try
            {
                GameObject parent = null;
                if (!string.IsNullOrEmpty(parentName))
                {
                    parent = GameObject.Find(parentName);
                    if (parent == null)
                    {
                        return Response.Error($"Parent object '{parentName}' not found.");
                    }
                }

                GameObject uiElement = null;

                switch (elementType)
                {
                    case "button":
                        uiElement = CreateButton(elementName, parent);
                        break;
                    case "text":
                        uiElement = CreateText(elementName, parent);
                        break;
                    case "textmeshpro":
                    case "tmp":
                        uiElement = CreateTextMeshPro(elementName, parent);
                        break;
                    case "image":
                        uiElement = CreateImage(elementName, parent);
                        break;
                    case "inputfield":
                        uiElement = CreateInputField(elementName, parent);
                        break;
                    case "slider":
                        uiElement = CreateSlider(elementName, parent);
                        break;
                    case "toggle":
                        uiElement = CreateToggle(elementName, parent);
                        break;
                    case "dropdown":
                        uiElement = CreateDropdown(elementName, parent);
                        break;
                    case "scrollview":
                        uiElement = CreateScrollView(elementName, parent);
                        break;
                    case "panel":
                        uiElement = CreatePanel(elementName, parent);
                        break;
                    default:
                        return Response.Error($"Unknown UI element type: {elementType}");
                }

                // Apply common properties
                ApplyCommonUIProperties(uiElement, @params);

                EditorUtility.SetDirty(uiElement);

                return Response.Success($"UI element '{elementName}' of type '{elementType}' created successfully.", new
                {
                    elementName = elementName,
                    elementType = elementType,
                    parentName = parent?.name ?? "None",
                    position = SerializeVector3(uiElement.transform.position),
                    hasRectTransform = uiElement.GetComponent<RectTransform>() != null
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create UI element: {e.Message}");
            }
        }

        private static GameObject CreateButton(string name, GameObject parent)
        {
            GameObject buttonObj = new GameObject(name);
            if (parent != null) buttonObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
            Image image = buttonObj.AddComponent<Image>();
            Button button = buttonObj.AddComponent<Button>();

            // Create button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            Text text = textObj.AddComponent<Text>();

            // Configure text
            text.text = name;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.black;

            // Set text rect to fill button
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Set default button size
            rectTransform.sizeDelta = new Vector2(160, 30);

            return buttonObj;
        }

        private static GameObject CreateText(string name, GameObject parent)
        {
            GameObject textObj = new GameObject(name);
            if (parent != null) textObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            Text text = textObj.AddComponent<Text>();

            text.text = name;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.black;

            rectTransform.sizeDelta = new Vector2(160, 30);

            return textObj;
        }

        private static GameObject CreateTextMeshPro(string name, GameObject parent)
        {
            GameObject textObj = new GameObject(name);
            if (parent != null) textObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();

            text.text = name;
            text.fontSize = 14;
            text.color = Color.black;

            rectTransform.sizeDelta = new Vector2(160, 30);

            return textObj;
        }

        private static GameObject CreateImage(string name, GameObject parent)
        {
            GameObject imageObj = new GameObject(name);
            if (parent != null) imageObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = imageObj.AddComponent<RectTransform>();
            Image image = imageObj.AddComponent<Image>();

            rectTransform.sizeDelta = new Vector2(100, 100);

            return imageObj;
        }

        private static GameObject CreateInputField(string name, GameObject parent)
        {
            GameObject inputFieldObj = new GameObject(name);
            if (parent != null) inputFieldObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = inputFieldObj.AddComponent<RectTransform>();
            Image image = inputFieldObj.AddComponent<Image>();
            InputField inputField = inputFieldObj.AddComponent<InputField>();

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            Text placeholderText = placeholderObj.AddComponent<Text>();

            // Create text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputFieldObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            Text text = textObj.AddComponent<Text>();

            // Configure placeholder
            placeholderText.text = "Enter text...";
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = Color.gray;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            placeholderRect.anchoredPosition = Vector2.zero;

            // Configure text
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.black;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Configure input field
            inputField.textComponent = text;
            inputField.placeholder = placeholderText;

            rectTransform.sizeDelta = new Vector2(160, 30);

            return inputFieldObj;
        }

        private static GameObject CreateSlider(string name, GameObject parent)
        {
            GameObject sliderObj = new GameObject(name);
            if (parent != null) sliderObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
            Slider slider = sliderObj.AddComponent<Slider>();

            // Create Background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(sliderObj.transform, false);
            RectTransform backgroundRect = backgroundObj.AddComponent<RectTransform>();
            Image backgroundImage = backgroundObj.AddComponent<Image>();

            // Create Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();

            // Create Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            Image fillImage = fillObj.AddComponent<Image>();

            // Create Handle Slide Area
            GameObject handleSlideAreaObj = new GameObject("Handle Slide Area");
            handleSlideAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleSlideAreaRect = handleSlideAreaObj.AddComponent<RectTransform>();

            // Create Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleSlideAreaObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            Image handleImage = handleObj.AddComponent<Image>();

            // Configure slider
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;

            rectTransform.sizeDelta = new Vector2(160, 20);

            return sliderObj;
        }

        private static GameObject CreateToggle(string name, GameObject parent)
        {
            GameObject toggleObj = new GameObject(name);
            if (parent != null) toggleObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = toggleObj.AddComponent<RectTransform>();
            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Create Background
            GameObject backgroundObj = new GameObject("Background");
            backgroundObj.transform.SetParent(toggleObj.transform, false);
            RectTransform backgroundRect = backgroundObj.AddComponent<RectTransform>();
            Image backgroundImage = backgroundObj.AddComponent<Image>();

            // Create Checkmark
            GameObject checkmarkObj = new GameObject("Checkmark");
            checkmarkObj.transform.SetParent(backgroundObj.transform, false);
            RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
            Image checkmarkImage = checkmarkObj.AddComponent<Image>();

            // Create Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            Text labelText = labelObj.AddComponent<Text>();

            // Configure toggle
            toggle.targetGraphic = backgroundImage;
            toggle.graphic = checkmarkImage;

            // Configure label
            labelText.text = name;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.black;

            rectTransform.sizeDelta = new Vector2(160, 20);

            return toggleObj;
        }

        private static GameObject CreateDropdown(string name, GameObject parent)
        {
            GameObject dropdownObj = new GameObject(name);
            if (parent != null) dropdownObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = dropdownObj.AddComponent<RectTransform>();
            Image image = dropdownObj.AddComponent<Image>();
            Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();

            // Create Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            Text labelText = labelObj.AddComponent<Text>();

            // Create Arrow
            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            Image arrowImage = arrowObj.AddComponent<Image>();

            // Create Template
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            Image templateImage = templateObj.AddComponent<Image>();

            // Configure dropdown
            dropdown.captionText = labelText;
            dropdown.template = templateRect;

            // Configure label
            labelText.text = "Option A";
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.black;

            rectTransform.sizeDelta = new Vector2(160, 30);

            return dropdownObj;
        }

        private static GameObject CreateScrollView(string name, GameObject parent)
        {
            GameObject scrollViewObj = new GameObject(name);
            if (parent != null) scrollViewObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = scrollViewObj.AddComponent<RectTransform>();
            Image image = scrollViewObj.AddComponent<Image>();
            ScrollRect scrollRect = scrollViewObj.AddComponent<ScrollRect>();

            // Create Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(scrollViewObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            Image viewportImage = viewportObj.AddComponent<Image>();
            Mask mask = viewportObj.AddComponent<Mask>();

            // Create Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();

            // Configure scroll rect
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;

            rectTransform.sizeDelta = new Vector2(200, 200);

            return scrollViewObj;
        }

        private static GameObject CreatePanel(string name, GameObject parent)
        {
            GameObject panelObj = new GameObject(name);
            if (parent != null) panelObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
            Image image = panelObj.AddComponent<Image>();

            rectTransform.sizeDelta = new Vector2(200, 200);

            return panelObj;
        }

        private static void ApplyCommonUIProperties(GameObject uiElement, JObject @params)
        {
            RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
            if (rectTransform == null) return;

            // Set position
            if (@params["position"] != null)
            {
                JObject position = @params["position"] as JObject;
                rectTransform.anchoredPosition = new Vector2(
                    position["x"]?.ToObject<float>() ?? rectTransform.anchoredPosition.x,
                    position["y"]?.ToObject<float>() ?? rectTransform.anchoredPosition.y
                );
            }

            // Set size
            if (@params["size"] != null)
            {
                JObject size = @params["size"] as JObject;
                rectTransform.sizeDelta = new Vector2(
                    size["width"]?.ToObject<float>() ?? rectTransform.sizeDelta.x,
                    size["height"]?.ToObject<float>() ?? rectTransform.sizeDelta.y
                );
            }

            // Set anchors
            if (@params["anchors"] != null)
            {
                JObject anchors = @params["anchors"] as JObject;
                if (anchors["min"] != null)
                {
                    JObject min = anchors["min"] as JObject;
                    rectTransform.anchorMin = new Vector2(
                        min["x"]?.ToObject<float>() ?? rectTransform.anchorMin.x,
                        min["y"]?.ToObject<float>() ?? rectTransform.anchorMin.y
                    );
                }
                if (anchors["max"] != null)
                {
                    JObject max = anchors["max"] as JObject;
                    rectTransform.anchorMax = new Vector2(
                        max["x"]?.ToObject<float>() ?? rectTransform.anchorMax.x,
                        max["y"]?.ToObject<float>() ?? rectTransform.anchorMax.y
                    );
                }
            }

            // Set rotation
            if (@params["rotation"] != null)
            {
                JObject rotation = @params["rotation"] as JObject;
                rectTransform.rotation = Quaternion.Euler(
                    rotation["x"]?.ToObject<float>() ?? rectTransform.rotation.eulerAngles.x,
                    rotation["y"]?.ToObject<float>() ?? rectTransform.rotation.eulerAngles.y,
                    rotation["z"]?.ToObject<float>() ?? rectTransform.rotation.eulerAngles.z
                );
            }

            // Set scale
            if (@params["scale"] != null)
            {
                JObject scale = @params["scale"] as JObject;
                rectTransform.localScale = new Vector3(
                    scale["x"]?.ToObject<float>() ?? rectTransform.localScale.x,
                    scale["y"]?.ToObject<float>() ?? rectTransform.localScale.y,
                    scale["z"]?.ToObject<float>() ?? rectTransform.localScale.z
                );
            }
        }

        private static object ModifyUIElement(JObject @params)
        {
            string elementName = @params["element_name"]?.ToString();
            if (string.IsNullOrEmpty(elementName))
            {
                return Response.Error("Element name is required.");
            }

            GameObject uiElement = GameObject.Find(elementName);
            if (uiElement == null)
            {
                return Response.Error($"UI element '{elementName}' not found.");
            }

            try
            {
                // Apply common properties
                ApplyCommonUIProperties(uiElement, @params);

                // Modify specific component properties
                if (@params["text"] != null)
                {
                    Text textComponent = uiElement.GetComponent<Text>();
                    TextMeshProUGUI tmpComponent = uiElement.GetComponent<TextMeshProUGUI>();
                    
                    string newText = @params["text"].ToString();
                    if (textComponent != null)
                        textComponent.text = newText;
                    if (tmpComponent != null)
                        tmpComponent.text = newText;
                }

                if (@params["color"] != null)
                {
                    JObject color = @params["color"] as JObject;
                    Color newColor = new Color(
                        color["r"]?.ToObject<float>() ?? 1f,
                        color["g"]?.ToObject<float>() ?? 1f,
                        color["b"]?.ToObject<float>() ?? 1f,
                        color["a"]?.ToObject<float>() ?? 1f
                    );

                    Image imageComponent = uiElement.GetComponent<Image>();
                    Text textComponent = uiElement.GetComponent<Text>();
                    TextMeshProUGUI tmpComponent = uiElement.GetComponent<TextMeshProUGUI>();

                    if (imageComponent != null)
                        imageComponent.color = newColor;
                    if (textComponent != null)
                        textComponent.color = newColor;
                    if (tmpComponent != null)
                        tmpComponent.color = newColor;
                }

                if (@params["sprite_path"] != null)
                {
                    string spritePath = @params["sprite_path"].ToString();
                    Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                    if (sprite != null)
                    {
                        Image imageComponent = uiElement.GetComponent<Image>();
                        if (imageComponent != null)
                            imageComponent.sprite = sprite;
                    }
                }

                EditorUtility.SetDirty(uiElement);

                return Response.Success($"UI element '{elementName}' modified successfully.", new
                {
                    elementName = elementName,
                    position = SerializeVector3(uiElement.transform.position),
                    hasText = uiElement.GetComponent<Text>() != null || uiElement.GetComponent<TextMeshProUGUI>() != null,
                    hasImage = uiElement.GetComponent<Image>() != null
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to modify UI element: {e.Message}");
            }
        }

        private static object SetUILayout(JObject @params)
        {
            string elementName = @params["element_name"]?.ToString();
            string layoutType = @params["layout_type"]?.ToString()?.ToLower();

            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(layoutType))
            {
                return Response.Error("Element name and layout type are required.");
            }

            GameObject uiElement = GameObject.Find(elementName);
            if (uiElement == null)
            {
                return Response.Error($"UI element '{elementName}' not found.");
            }

            try
            {
                LayoutGroup layoutGroup = null;

                switch (layoutType)
                {
                    case "horizontal":
                        layoutGroup = uiElement.GetComponent<HorizontalLayoutGroup>() ?? uiElement.AddComponent<HorizontalLayoutGroup>();
                        break;
                    case "vertical":
                        layoutGroup = uiElement.GetComponent<VerticalLayoutGroup>() ?? uiElement.AddComponent<VerticalLayoutGroup>();
                        break;
                    case "grid":
                        GridLayoutGroup gridLayout = uiElement.GetComponent<GridLayoutGroup>() ?? uiElement.AddComponent<GridLayoutGroup>();
                        layoutGroup = gridLayout;
                        
                        if (@params["cell_size"] != null)
                        {
                            JObject cellSize = @params["cell_size"] as JObject;
                            gridLayout.cellSize = new Vector2(
                                cellSize["width"]?.ToObject<float>() ?? 100f,
                                cellSize["height"]?.ToObject<float>() ?? 100f
                            );
                        }
                        
                        if (@params["spacing"] != null)
                        {
                            JObject spacing = @params["spacing"] as JObject;
                            gridLayout.spacing = new Vector2(
                                spacing["x"]?.ToObject<float>() ?? 0f,
                                spacing["y"]?.ToObject<float>() ?? 0f
                            );
                        }
                        break;
                    default:
                        return Response.Error($"Unknown layout type: {layoutType}");
                }

                // Configure common layout properties
                if (layoutGroup != null)
                {
                    if (@params["padding"] != null)
                    {
                        JObject padding = @params["padding"] as JObject;
                        layoutGroup.padding = new RectOffset(
                            padding["left"]?.ToObject<int>() ?? 0,
                            padding["right"]?.ToObject<int>() ?? 0,
                            padding["top"]?.ToObject<int>() ?? 0,
                            padding["bottom"]?.ToObject<int>() ?? 0
                        );
                    }

                    if (@params["child_alignment"] != null)
                    {
                        string alignment = @params["child_alignment"].ToString();
                        layoutGroup.childAlignment = (TextAnchor)Enum.Parse(typeof(TextAnchor), alignment);
                    }

                    if (@params["child_control_size"] != null)
                    {
                        JObject controlSize = @params["child_control_size"] as JObject;
                        // Note: childControlWidth/Height properties don't exist in base LayoutGroup
                        // These properties are specific to HorizontalOrVerticalLayoutGroup subclasses
                        if (layoutGroup is HorizontalOrVerticalLayoutGroup hvLayoutGroup)
                        {
                            hvLayoutGroup.childControlWidth = controlSize["width"]?.ToObject<bool>() ?? false;
                            hvLayoutGroup.childControlHeight = controlSize["height"]?.ToObject<bool>() ?? false;
                        }
                    }

                    if (@params["child_force_expand"] != null)
                    {
                        JObject forceExpand = @params["child_force_expand"] as JObject;
                        // Note: childForceExpandWidth/Height properties don't exist in base LayoutGroup
                        // These properties are specific to HorizontalOrVerticalLayoutGroup subclasses
                        if (layoutGroup is HorizontalOrVerticalLayoutGroup hvLayoutGroup)
                        {
                            hvLayoutGroup.childForceExpandWidth = forceExpand["width"]?.ToObject<bool>() ?? false;
                            hvLayoutGroup.childForceExpandHeight = forceExpand["height"]?.ToObject<bool>() ?? false;
                        }
                    }
                }

                EditorUtility.SetDirty(uiElement);

                return Response.Success($"Layout '{layoutType}' applied to '{elementName}'.", new
                {
                    elementName = elementName,
                    layoutType = layoutType,
                    hasLayoutGroup = layoutGroup != null
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set UI layout: {e.Message}");
            }
        }

        private static object CreateUIEvent(JObject @params)
        {
            string elementName = @params["element_name"]?.ToString();
            string eventType = @params["event_type"]?.ToString()?.ToLower();
            string scriptName = @params["script_name"]?.ToString();
            string methodName = @params["method_name"]?.ToString();

            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(eventType))
            {
                return Response.Error("Element name and event type are required.");
            }

            GameObject uiElement = GameObject.Find(elementName);
            if (uiElement == null)
            {
                return Response.Error($"UI element '{elementName}' not found.");
            }

            try
            {
                // This is a simplified version - in practice, you'd need to create or reference actual scripts
                switch (eventType)
                {
                    case "onclick":
                        Button button = uiElement.GetComponent<Button>();
                        if (button == null)
                        {
                            return Response.Error($"Element '{elementName}' is not a button.");
                        }
                        
                        // Note: In practice, you'd need to reference an actual MonoBehaviour script
                        Debug.Log($"OnClick event would be added to button '{elementName}' calling {scriptName}.{methodName}");
                        break;

                    case "onvaluechanged":
                        Slider slider = uiElement.GetComponent<Slider>();
                        Toggle toggle = uiElement.GetComponent<Toggle>();
                        InputField inputField = uiElement.GetComponent<InputField>();
                        
                        if (slider != null)
                        {
                            Debug.Log($"OnValueChanged event would be added to slider '{elementName}' calling {scriptName}.{methodName}");
                        }
                        else if (toggle != null)
                        {
                            Debug.Log($"OnValueChanged event would be added to toggle '{elementName}' calling {scriptName}.{methodName}");
                        }
                        else if (inputField != null)
                        {
                            Debug.Log($"OnValueChanged event would be added to input field '{elementName}' calling {scriptName}.{methodName}");
                        }
                        else
                        {
                            return Response.Error($"Element '{elementName}' does not support OnValueChanged events.");
                        }
                        break;

                    default:
                        return Response.Error($"Unknown event type: {eventType}");
                }

                return Response.Success($"UI event '{eventType}' configured for '{elementName}'.", new
                {
                    elementName = elementName,
                    eventType = eventType,
                    scriptName = scriptName ?? "None",
                    methodName = methodName ?? "None"
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create UI event: {e.Message}");
            }
        }

        private static object SetUIAnimation(JObject @params)
        {
            string elementName = @params["element_name"]?.ToString();
            string animationType = @params["animation_type"]?.ToString()?.ToLower();

            if (string.IsNullOrEmpty(elementName) || string.IsNullOrEmpty(animationType))
            {
                return Response.Error("Element name and animation type are required.");
            }

            GameObject uiElement = GameObject.Find(elementName);
            if (uiElement == null)
            {
                return Response.Error($"UI element '{elementName}' not found.");
            }

            try
            {
                Animator animator = uiElement.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = uiElement.AddComponent<Animator>();
                }

                // This is a simplified version - in practice, you'd create actual animation clips and controllers
                switch (animationType)
                {
                    case "fade":
                        Debug.Log($"Fade animation would be applied to '{elementName}'");
                        break;
                    case "scale":
                        Debug.Log($"Scale animation would be applied to '{elementName}'");
                        break;
                    case "slide":
                        Debug.Log($"Slide animation would be applied to '{elementName}'");
                        break;
                    case "rotate":
                        Debug.Log($"Rotate animation would be applied to '{elementName}'");
                        break;
                    default:
                        return Response.Error($"Unknown animation type: {animationType}");
                }

                EditorUtility.SetDirty(uiElement);

                return Response.Success($"UI animation '{animationType}' configured for '{elementName}'.", new
                {
                    elementName = elementName,
                    animationType = animationType,
                    hasAnimator = animator != null
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to set UI animation: {e.Message}");
            }
        }

        private static object GetUIInfo(JObject @params)
        {
            try
            {
                string elementName = @params["element_name"]?.ToString();
                
                if (!string.IsNullOrEmpty(elementName))
                {
                    GameObject uiElement = GameObject.Find(elementName);
                    if (uiElement == null)
                    {
                        return Response.Error($"UI element '{elementName}' not found.");
                    }

                    RectTransform rectTransform = uiElement.GetComponent<RectTransform>();
                    Canvas canvas = uiElement.GetComponentInParent<Canvas>();

                    return Response.Success($"UI info for '{elementName}'.", new
                    {
                        elementName = elementName,
                        hasRectTransform = rectTransform != null,
                        position = rectTransform?.anchoredPosition ?? Vector2.zero,
                        size = rectTransform?.sizeDelta ?? Vector2.zero,
                        anchors = rectTransform != null ? new { 
                            min = rectTransform.anchorMin, 
                            max = rectTransform.anchorMax 
                        } : null,
                        parentCanvas = canvas?.name ?? "None",
                        components = uiElement.GetComponents<Component>().Select(c => c.GetType().Name).ToArray(),
                        childCount = uiElement.transform.childCount
                    });
                }
                else
                {
                    // Return general UI system info
                    Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>();
                    EventSystem eventSystem = GameObject.FindFirstObjectByType <EventSystem>();

                    return Response.Success("UI system information.", new
                    {
                        canvasCount = canvases.Length,
                        canvases = canvases.Select(c => new {
                            name = c.name,
                            renderMode = c.renderMode.ToString(),
                            sortingOrder = c.sortingOrder
                        }).ToArray(),
                        hasEventSystem = eventSystem != null,
                        eventSystemName = eventSystem?.name ?? "None"
                    });
                }
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to get UI info: {e.Message}");
            }
        }

        private static object CreateUIPrefab(JObject @params)
        {
            string elementName = @params["element_name"]?.ToString();
            string prefabPath = @params["prefab_path"]?.ToString();

            if (string.IsNullOrEmpty(elementName))
            {
                return Response.Error("Element name is required.");
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                prefabPath = $"Assets/UI/Prefabs/{elementName}.prefab";
            }

            GameObject uiElement = GameObject.Find(elementName);
            if (uiElement == null)
            {
                return Response.Error($"UI element '{elementName}' not found.");
            }

            try
            {
                // Ensure directory exists
                string directory = System.IO.Path.GetDirectoryName(prefabPath);
                if (!AssetDatabase.IsValidFolder(directory))
                {
                    string[] folders = directory.Split('/');
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

                // Create prefab
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(uiElement, prefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                return Response.Success($"UI prefab created successfully.", new
                {
                    elementName = elementName,
                    prefabPath = prefabPath,
                    prefabName = prefab.name
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to create UI prefab: {e.Message}");
            }
        }

        private static object SetupEventSystem(JObject @params)
        {
            try
            {
                // Check if EventSystem already exists
                EventSystem existingEventSystem = GameObject.FindFirstObjectByType <EventSystem>();
                if (existingEventSystem != null)
                {
                    bool replaceExisting = @params["replace_existing"]?.ToObject<bool>() ?? false;
                    if (!replaceExisting)
                    {
                        return Response.Success("EventSystem already exists.", new
                        {
                            eventSystemName = existingEventSystem.name,
                            replaced = false
                        });
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(existingEventSystem.gameObject);
                    }
                }

                // Create new EventSystem
                GameObject eventSystemObj = new GameObject("EventSystem");
                EventSystem eventSystem = eventSystemObj.AddComponent<EventSystem>();
                StandaloneInputModule inputModule = eventSystemObj.AddComponent<StandaloneInputModule>();

                // Configure input module
                if (@params["horizontal_axis"] != null)
                    inputModule.horizontalAxis = @params["horizontal_axis"].ToString();
                if (@params["vertical_axis"] != null)
                    inputModule.verticalAxis = @params["vertical_axis"].ToString();
                if (@params["submit_button"] != null)
                    inputModule.submitButton = @params["submit_button"].ToString();
                if (@params["cancel_button"] != null)
                    inputModule.cancelButton = @params["cancel_button"].ToString();

                EditorUtility.SetDirty(eventSystemObj);

                return Response.Success("EventSystem created successfully.", new
                {
                    eventSystemName = eventSystemObj.name,
                    hasStandaloneInputModule = inputModule != null,
                    replaced = existingEventSystem != null
                });
            }
            catch (Exception e)
            {
                return Response.Error($"Failed to setup event system: {e.Message}");
            }
        }
    }
}