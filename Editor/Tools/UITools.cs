using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating UI elements in the Unity Editor
    /// </summary>
    public class CreateUIElementTool : McpToolBase
    {
        public CreateUIElementTool()
        {
            Name = "create_ui_element";
            Description = "Creates a UI element (Canvas, Button, Text, Image, Panel, Slider, Toggle, InputField, Dropdown, ScrollView) with automatic Canvas and EventSystem setup.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            string elementType = parameters["elementType"]?.ToObject<string>();
            string parentPath = parameters["parentPath"]?.ToObject<string>();
            string name = parameters["name"]?.ToObject<string>();

            if (string.IsNullOrEmpty(elementType))
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'elementType' not provided",
                    "validation_error"
                );
            }

            // Ensure Canvas exists for non-Canvas elements
            Canvas canvas = null;
            if (elementType.ToLower() != "canvas")
            {
                canvas = Object.FindObjectOfType<Canvas>();
                if (canvas == null)
                {
                    canvas = CreateNewCanvas();
                }
            }

            // Determine parent transform
            Transform parent = canvas?.transform;
            if (!string.IsNullOrEmpty(parentPath))
            {
                var parentGO = GameObject.Find(parentPath);
                if (parentGO != null) parent = parentGO.transform;
            }

            // Create the UI element
            var resources = new DefaultControls.Resources();
            GameObject element = null;

            switch (elementType.ToLower())
            {
                case "canvas":
                    if (canvas != null)
                    {
                        element = canvas.gameObject;
                    }
                    else
                    {
                        canvas = CreateNewCanvas();
                        element = canvas.gameObject;
                    }
                    break;
                case "button":
                    element = DefaultControls.CreateButton(resources);
                    break;
                case "text":
                    element = DefaultControls.CreateText(resources);
                    break;
                case "image":
                    element = DefaultControls.CreateImage(resources);
                    break;
                case "panel":
                    element = DefaultControls.CreatePanel(resources);
                    break;
                case "slider":
                    element = DefaultControls.CreateSlider(resources);
                    break;
                case "toggle":
                    element = DefaultControls.CreateToggle(resources);
                    break;
                case "inputfield":
                    element = DefaultControls.CreateInputField(resources);
                    break;
                case "dropdown":
                    element = DefaultControls.CreateDropdown(resources);
                    break;
                case "scrollview":
                    element = DefaultControls.CreateScrollView(resources);
                    break;
                default:
                    return McpUnitySocketHandler.CreateErrorResponse(
                        $"Unknown element type '{elementType}'. Supported: Canvas, Button, Text, Image, Panel, Slider, Toggle, InputField, Dropdown, ScrollView",
                        "validation_error"
                    );
            }

            if (element == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"Failed to create UI element of type '{elementType}'",
                    "creation_error"
                );
            }

            // Set parent for non-Canvas elements
            if (parent != null && elementType.ToLower() != "canvas")
            {
                element.transform.SetParent(parent, false);
            }

            // Set custom name if provided
            if (!string.IsNullOrEmpty(name))
            {
                element.name = name;
            }

            Undo.RegisterCreatedObjectUndo(element, $"Create UI {elementType}");

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Created UI element '{element.name}' of type '{elementType}'",
                ["instanceId"] = element.GetInstanceID(),
                ["name"] = element.name,
                ["elementType"] = elementType
            };
        }

        private Canvas CreateNewCanvas()
        {
            var canvasGO = new GameObject("Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");

            // Create EventSystem if needed
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
            }

            return canvas;
        }
    }

    /// <summary>
    /// Tool for modifying UI element properties
    /// </summary>
    public class ModifyUIElementTool : McpToolBase
    {
        public ModifyUIElementTool()
        {
            Name = "modify_ui_element";
            Description = "Modifies UI element properties such as text, fontSize, color, anchoredPosition, sizeDelta, and enabled state.";
            IsAsync = false;
        }

        public override JObject Execute(JObject parameters)
        {
            int? instanceId = parameters["instanceId"]?.ToObject<int?>();
            string objectPath = parameters["objectPath"]?.ToObject<string>();
            JObject properties = parameters["properties"] as JObject;

            // Find the GameObject
            GameObject gameObject = null;
            string identifierInfo = "";

            if (instanceId.HasValue)
            {
                gameObject = EditorUtility.InstanceIDToObject(instanceId.Value) as GameObject;
                identifierInfo = $"instance ID {instanceId.Value}";
            }
            else if (!string.IsNullOrEmpty(objectPath))
            {
                gameObject = GameObject.Find(objectPath);
                identifierInfo = $"path '{objectPath}'";
            }
            else
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Either 'instanceId' or 'objectPath' must be provided",
                    "validation_error"
                );
            }

            if (gameObject == null)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    $"GameObject not found with {identifierInfo}",
                    "not_found_error"
                );
            }

            if (properties == null || properties.Count == 0)
            {
                return McpUnitySocketHandler.CreateErrorResponse(
                    "Required parameter 'properties' not provided or empty",
                    "validation_error"
                );
            }

            // Apply properties
            var rectTransform = gameObject.GetComponent<RectTransform>();

            // text
            if (properties["text"] != null)
            {
                var textComp = gameObject.GetComponent<Text>();
                if (textComp != null)
                {
                    Undo.RecordObject(textComp, "Modify UI Text");
                    textComp.text = properties["text"].ToObject<string>();
                }
            }

            // fontSize
            if (properties["fontSize"] != null)
            {
                var textComp = gameObject.GetComponent<Text>();
                if (textComp != null)
                {
                    Undo.RecordObject(textComp, "Modify UI FontSize");
                    textComp.fontSize = properties["fontSize"].ToObject<int>();
                }
            }

            // color
            if (properties["color"] is JObject colorObj)
            {
                var graphic = gameObject.GetComponent<Graphic>();
                if (graphic != null)
                {
                    Undo.RecordObject(graphic, "Modify UI Color");
                    graphic.color = new Color(
                        colorObj["r"]?.ToObject<float>() ?? graphic.color.r,
                        colorObj["g"]?.ToObject<float>() ?? graphic.color.g,
                        colorObj["b"]?.ToObject<float>() ?? graphic.color.b,
                        colorObj["a"]?.ToObject<float>() ?? graphic.color.a
                    );
                }
            }

            // anchoredPosition
            if (properties["anchoredPosition"] is JObject posObj && rectTransform != null)
            {
                Undo.RecordObject(rectTransform, "Modify UI Position");
                rectTransform.anchoredPosition = new Vector2(
                    posObj["x"]?.ToObject<float>() ?? rectTransform.anchoredPosition.x,
                    posObj["y"]?.ToObject<float>() ?? rectTransform.anchoredPosition.y
                );
            }

            // sizeDelta
            if (properties["sizeDelta"] is JObject sizeObj && rectTransform != null)
            {
                Undo.RecordObject(rectTransform, "Modify UI Size");
                rectTransform.sizeDelta = new Vector2(
                    sizeObj["x"]?.ToObject<float>() ?? rectTransform.sizeDelta.x,
                    sizeObj["y"]?.ToObject<float>() ?? rectTransform.sizeDelta.y
                );
            }

            // enabled
            if (properties["enabled"] != null)
            {
                Undo.RecordObject(gameObject, "Modify UI Enabled");
                gameObject.SetActive(properties["enabled"].ToObject<bool>());
            }

            EditorUtility.SetDirty(gameObject);

            return new JObject
            {
                ["success"] = true,
                ["type"] = "text",
                ["message"] = $"Modified UI element '{gameObject.name}'",
                ["instanceId"] = gameObject.GetInstanceID(),
                ["name"] = gameObject.name
            };
        }
    }
}
