using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using TMPro;
using McpUnity.Unity;
using Newtonsoft.Json.Linq;

namespace McpUnity.Tools
{
    /// <summary>
    /// Tool for creating UI elements in the Unity Editor.
    /// Text elements use TextMeshPro (TextMeshProUGUI) instead of legacy Text.
    /// </summary>
    public class CreateUIElementTool : McpToolBase
    {
        public CreateUIElementTool()
        {
            Name = "create_ui_element";
            Description = "Creates a UI element (Canvas, Button, Text, Image, Panel, Slider, Toggle, InputField, Dropdown, ScrollView) with automatic Canvas and EventSystem setup. Text elements use TextMeshPro.";
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
                    element = CreateTMPButton(resources);
                    break;
                case "text":
                    element = CreateTMPText();
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
                    element = CreateTMPToggle(resources);
                    break;
                case "inputfield":
                    element = CreateTMPInputField();
                    break;
                case "dropdown":
                    element = CreateTMPDropdown();
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
                ["message"] = $"Created UI element '{element.name}' of type '{elementType}' (TextMeshPro)",
                ["instanceId"] = element.GetInstanceID(),
                ["name"] = element.name,
                ["elementType"] = elementType
            };
        }

        private static GameObject CreateTMPText()
        {
            var go = new GameObject("Text (TMP)");
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200f, 50f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "New Text";
            tmp.fontSize = 36f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static GameObject CreateTMPButton(DefaultControls.Resources resources)
        {
            var buttonGO = DefaultControls.CreateButton(resources);

            // Replace legacy Text child with TMP
            var legacyText = buttonGO.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                string originalText = legacyText.text;
                var textGO = legacyText.gameObject;
                Object.DestroyImmediate(legacyText);

                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = originalText;
                tmp.fontSize = 24f;
                tmp.color = new Color(0.196f, 0.196f, 0.196f, 1f);
                tmp.alignment = TextAlignmentOptions.Center;
            }

            return buttonGO;
        }

        private static GameObject CreateTMPToggle(DefaultControls.Resources resources)
        {
            var toggleGO = DefaultControls.CreateToggle(resources);

            // Replace legacy Text child with TMP
            var legacyText = toggleGO.GetComponentInChildren<Text>();
            if (legacyText != null)
            {
                string originalText = legacyText.text;
                var textGO = legacyText.gameObject;
                Object.DestroyImmediate(legacyText);

                var tmp = textGO.AddComponent<TextMeshProUGUI>();
                tmp.text = originalText;
                tmp.fontSize = 20f;
                tmp.color = new Color(0.196f, 0.196f, 0.196f, 1f);
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
            }

            return toggleGO;
        }

        private static GameObject CreateTMPInputField()
        {
            var go = new GameObject("InputField (TMP)");
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 30f);

            var image = go.AddComponent<Image>();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            // Text Area
            var textAreaGO = new GameObject("Text Area");
            var textAreaRT = textAreaGO.AddComponent<RectTransform>();
            textAreaGO.AddComponent<RectMask2D>();
            textAreaGO.transform.SetParent(go.transform, false);
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10f, 6f);
            textAreaRT.offsetMax = new Vector2(-10f, -7f);

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            var placeholderRT = placeholderGO.AddComponent<RectTransform>();
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;

            var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.text = "Enter text...";
            placeholder.fontSize = 14f;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.color = new Color(0.196f, 0.196f, 0.196f, 0.5f);
            placeholder.alignment = TextAlignmentOptions.MidlineLeft;

            // Text
            var textGO = new GameObject("Text");
            var textRT = textGO.AddComponent<RectTransform>();
            textGO.transform.SetParent(textAreaGO.transform, false);
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 14f;
            text.color = new Color(0.196f, 0.196f, 0.196f, 1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;

            var inputField = go.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRT;
            inputField.textComponent = text;
            inputField.placeholder = placeholder;
            inputField.fontAsset = text.font;

            return go;
        }

        private static GameObject CreateTMPDropdown()
        {
            var go = new GameObject("Dropdown (TMP)");
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160f, 30f);

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            image.type = Image.Type.Sliced;

            // Label
            var labelGO = new GameObject("Label");
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelGO.transform.SetParent(go.transform, false);
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(10f, 6f);
            labelRT.offsetMax = new Vector2(-25f, -7f);

            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "Option A";
            label.fontSize = 14f;
            label.color = new Color(0.196f, 0.196f, 0.196f, 1f);
            label.alignment = TextAlignmentOptions.MidlineLeft;

            // Arrow
            var arrowGO = new GameObject("Arrow");
            var arrowRT = arrowGO.AddComponent<RectTransform>();
            arrowGO.AddComponent<Image>();
            arrowGO.transform.SetParent(go.transform, false);
            arrowRT.anchorMin = new Vector2(1f, 0.5f);
            arrowRT.anchorMax = new Vector2(1f, 0.5f);
            arrowRT.sizeDelta = new Vector2(20f, 20f);
            arrowRT.anchoredPosition = new Vector2(-15f, 0f);

            // Template (hidden by default)
            var templateGO = new GameObject("Template");
            var templateRT = templateGO.AddComponent<RectTransform>();
            templateGO.AddComponent<Image>();
            var scrollRect = templateGO.AddComponent<ScrollRect>();
            templateGO.transform.SetParent(go.transform, false);
            templateRT.anchorMin = new Vector2(0f, 0f);
            templateRT.anchorMax = new Vector2(1f, 0f);
            templateRT.pivot = new Vector2(0.5f, 1f);
            templateRT.anchoredPosition = Vector2.zero;
            templateRT.sizeDelta = new Vector2(0f, 150f);

            // Content
            var contentGO = new GameObject("Content");
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentGO.transform.SetParent(templateGO.transform, false);
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 28f);

            scrollRect.content = contentRT;

            // Item
            var itemGO = new GameObject("Item");
            var itemRT = itemGO.AddComponent<RectTransform>();
            var itemToggle = itemGO.AddComponent<Toggle>();
            itemGO.transform.SetParent(contentGO.transform, false);
            itemRT.anchorMin = new Vector2(0f, 0.5f);
            itemRT.anchorMax = new Vector2(1f, 0.5f);
            itemRT.sizeDelta = new Vector2(0f, 20f);

            // Item Background
            var itemBgGO = new GameObject("Item Background");
            var itemBgRT = itemBgGO.AddComponent<RectTransform>();
            itemBgGO.AddComponent<Image>();
            itemBgGO.transform.SetParent(itemGO.transform, false);
            itemBgRT.anchorMin = Vector2.zero;
            itemBgRT.anchorMax = Vector2.one;
            itemBgRT.sizeDelta = Vector2.zero;

            // Item Checkmark
            var checkmarkGO = new GameObject("Item Checkmark");
            var checkmarkRT = checkmarkGO.AddComponent<RectTransform>();
            checkmarkGO.AddComponent<Image>();
            checkmarkGO.transform.SetParent(itemGO.transform, false);
            checkmarkRT.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRT.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRT.sizeDelta = new Vector2(20f, 20f);
            checkmarkRT.anchoredPosition = new Vector2(10f, 0f);

            itemToggle.graphic = checkmarkGO.GetComponent<Image>();

            // Item Label
            var itemLabelGO = new GameObject("Item Label");
            var itemLabelRT = itemLabelGO.AddComponent<RectTransform>();
            itemLabelGO.transform.SetParent(itemGO.transform, false);
            itemLabelRT.anchorMin = Vector2.zero;
            itemLabelRT.anchorMax = Vector2.one;
            itemLabelRT.offsetMin = new Vector2(20f, 1f);
            itemLabelRT.offsetMax = new Vector2(-10f, -2f);

            var itemLabel = itemLabelGO.AddComponent<TextMeshProUGUI>();
            itemLabel.text = "Option A";
            itemLabel.fontSize = 14f;
            itemLabel.color = new Color(0.196f, 0.196f, 0.196f, 1f);
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;

            templateGO.SetActive(false);

            var dropdown = go.AddComponent<TMP_Dropdown>();
            dropdown.template = templateRT;
            dropdown.captionText = label;
            dropdown.itemText = itemLabel;
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "Option A", "Option B", "Option C" });

            return go;
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
    /// Tool for modifying UI element properties.
    /// Supports both TextMeshPro (TMP_Text) components.
    /// </summary>
    public class ModifyUIElementTool : McpToolBase
    {
        public ModifyUIElementTool()
        {
            Name = "modify_ui_element";
            Description = "Modifies UI element properties such as text, fontSize, color, anchoredPosition, sizeDelta, and enabled state. Supports TextMeshPro text components.";
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
                var tmpText = gameObject.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    Undo.RecordObject(tmpText, "Modify UI Text");
                    tmpText.text = properties["text"].ToObject<string>();
                }
            }

            // fontSize
            if (properties["fontSize"] != null)
            {
                var tmpText = gameObject.GetComponent<TMP_Text>();
                if (tmpText != null)
                {
                    Undo.RecordObject(tmpText, "Modify UI FontSize");
                    tmpText.fontSize = properties["fontSize"].ToObject<float>();
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
