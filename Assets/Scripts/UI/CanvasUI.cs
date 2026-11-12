#nullable enable
using System.Collections.Generic;
using PlantsVsZombies.Rendering;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PlantsVsZombies.UI
{
    /// <summary>Retains original artwork and text in native Canvas graphics and native editable controls.</summary>
    /// <remarks>Call BeginFrame and EndFrame around presentation on Unity's main thread.</remarks>
    public static class CanvasUI
    {
        /// <summary>Stores the transform and clip inherited by a nested drawing group.</summary>
        private readonly struct Group
        {
            /// <summary>Gets the parent transform restored when this group ends.</summary>
            internal Matrix4x4 Matrix { get; }
            /// <summary>Gets the parent clipping rectangle.</summary>
            internal Rect Clip { get; }

            /// <summary>Stores the parent drawing state.</summary>
            /// <param name="matrix">The transform before entering a group.</param>
            /// <param name="clip">The clipping rectangle before entering a group.</param>
            internal Group(Matrix4x4 matrix, Rect clip)
            {
                Matrix = matrix;
                Clip = clip;
            }
        }

        /// <summary>Retains one native interactive button between presentation frames.</summary>
        private sealed class ButtonControl
        {
            /// <summary>The transparent native button used for pointer capture and release.</summary>
            internal Button Button = null!;
            /// <summary>Whether the native EventSystem delivered a click since the last presentation.</summary>
            internal bool Clicked;
            /// <summary>The previous screen rectangle identifying this control.</summary>
            internal Rect Identity;
        }

        /// <summary>Retains an editable field and its stable focus identity.</summary>
        private sealed class FieldControl
        {
            /// <summary>The native field responsible for editing, IME, and mobile keyboards.</summary>
            internal InputField Field = null!;
            /// <summary>The field's text graphic.</summary>
            internal Text Text = null!;
            /// <summary>The optional stable focus name assigned by the view.</summary>
            internal string Name = string.Empty;
            /// <summary>The previous untransformed rectangle identifying this field.</summary>
            internal Rect Identity;
            /// <summary>The model value returned by the most recent presentation of this field.</summary>
            internal string LastValue = string.Empty;
        }

        /// <summary>The retained Canvas layers separated by intervening scene renderers.</summary>
        private static readonly List<Canvas> s_layers = new List<Canvas>();
        /// <summary>The retained texture graphics reused by each presentation traversal.</summary>
        private static readonly List<CanvasTextureGraphic> s_images = new List<CanvasTextureGraphic>();
        /// <summary>The retained native text graphics reused by each presentation traversal.</summary>
        private static readonly List<CanvasTextGraphic> s_labels = new List<CanvasTextGraphic>();
        /// <summary>The retained native buttons used by utility screens.</summary>
        private static readonly List<ButtonControl> s_buttons = new List<ButtonControl>();
        /// <summary>The retained native editable fields.</summary>
        private static readonly List<FieldControl> s_fields = new List<FieldControl>();
        /// <summary>The clipping and transform stack for nested original interface groups.</summary>
        private static readonly Stack<Group> s_groups = new Stack<Group>();
        /// <summary>The root screen-space Canvas owned by this interface.</summary>
        private static Canvas? s_canvas;
        /// <summary>The EventSystem created when the scene does not already provide one.</summary>
        private static GameObject? s_ownedEventSystem;
        /// <summary>The default native font shared by label and field layout.</summary>
        private static Font? s_defaultFont;
        /// <summary>The current Canvas layer receiving consecutive interface commands.</summary>
        private static Canvas? s_currentLayer;
        /// <summary>The number of retained layers used by the current frame.</summary>
        private static int s_layerCount;
        /// <summary>The number of retained texture graphics used by the current frame.</summary>
        private static int s_imageCount;
        /// <summary>The number of retained text graphics used by the current frame.</summary>
        private static int s_labelCount;
        /// <summary>The number of retained native buttons used by the current frame.</summary>
        private static int s_buttonCount;
        /// <summary>The number of retained editable fields used by the current frame.</summary>
        private static int s_fieldCount;
        /// <summary>The order of the most recent Canvas submission in the shared painter sequence.</summary>
        private static int s_previousOrder = -2;
        /// <summary>The next sibling slot in the current native Canvas layer.</summary>
        private static int s_siblingIndex;
        /// <summary>The focus identity assigned to the next native editable control.</summary>
        private static string s_nextControlName = string.Empty;
        /// <summary>The focus identity awaiting creation of its native field.</summary>
        private static string? s_requestedFocus;

        /// <summary>Gets the default native font when a view does not supply a font.</summary>
        public static Font DefaultFont => s_defaultFont != null ? s_defaultFont : s_defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        /// <summary>Gets or sets the explicit source-to-screen drawing transform.</summary>
        public static Matrix4x4 Matrix
        {
            get => SpriteDrawState.Matrix;
            set => SpriteDrawState.Matrix = value;
        }
        /// <summary>Gets or sets the inherited texture and text tint.</summary>
        public static Color Color
        {
            get => SpriteDrawState.Color;
            set => SpriteDrawState.Color = value;
        }
        /// <summary>Gets or sets the additional tint applied to text.</summary>
        public static Color ContentColor { get; set; } = UnityEngine.Color.white;
        /// <summary>Gets or sets whether native interactive controls are enabled.</summary>
        public static bool Enabled { get; set; } = true;
        /// <summary>Gets or sets the logical pointer position used by hover artwork and explicit smoke scenarios.</summary>
        public static Vector2 PointerPosition { get; set; }
        /// <summary>Gets whether presentation is currently recording a native Canvas frame.</summary>
        public static bool IsFrameActive { get; private set; }
        /// <summary>Gets the retained texture count for allocation and disposal regression checks.</summary>
        public static int PooledImageCount => s_images.Count;

        /// <summary>Begins a native Canvas presentation frame without sampling input or advancing simulation.</summary>
        public static void BeginFrame()
        {
            EnsureRoot();
            if (s_canvas!.worldCamera == null)
            {
                s_canvas.worldCamera = ObjectSpriteRenderer.SceneCamera;
            }

            s_layerCount = 0;
            s_imageCount = 0;
            s_labelCount = 0;
            s_buttonCount = 0;
            s_fieldCount = 0;
            s_previousOrder = -2;
            s_currentLayer = null;
            s_groups.Clear();
            s_nextControlName = string.Empty;
            ContentColor = UnityEngine.Color.white;
            Enabled = true;
            IsFrameActive = true;
        }

        /// <summary>Hides unused retained objects so removed screens and controls disappear immediately.</summary>
        public static void EndFrame()
        {
            for (var index = s_imageCount; index < s_images.Count; index++)
            {
                s_images[index].gameObject.SetActive(false);
            }

            for (var index = s_labelCount; index < s_labels.Count; index++)
            {
                s_labels[index].gameObject.SetActive(false);
            }

            for (var index = s_buttonCount; index < s_buttons.Count; index++)
            {
                s_buttons[index].Button.gameObject.SetActive(false);
                s_buttons[index].Clicked = false;
            }

            for (var index = s_fieldCount; index < s_fields.Count; index++)
            {
                s_fields[index].Field.DeactivateInputField();
                s_fields[index].Field.gameObject.SetActive(false);
            }

            for (var index = s_layerCount; index < s_layers.Count; index++)
            {
                s_layers[index].gameObject.SetActive(false);
            }

            IsFrameActive = false;
        }

        /// <summary>Displays a texture with the specified native scaling behavior.</summary>
        /// <param name="destination">The original destination rectangle.</param>
        /// <param name="texture">The original artwork.</param>
        /// <param name="scaleMode">Whether to stretch, contain, or crop the artwork.</param>
        /// <param name="alphaBlend">Whether the original call requested alpha blending.</param>
        public static void DrawTexture(Rect destination, Texture? texture, ScaleMode scaleMode = ScaleMode.StretchToFill, bool alphaBlend = true)
        {
            if (texture == null || destination.width <= 0f || destination.height <= 0f)
            {
                return;
            }

            var uv = new Rect(0f, 0f, 1f, 1f);
            var textureAspect = texture.width / (float)texture.height;
            var destinationAspect = destination.width / destination.height;
            if (scaleMode == ScaleMode.ScaleToFit)
            {
                if (textureAspect > destinationAspect)
                {
                    var height = destination.width / textureAspect;
                    destination.y += (destination.height - height) * .5f;
                    destination.height = height;
                }
                else
                {
                    var width = destination.height * textureAspect;
                    destination.x += (destination.width - width) * .5f;
                    destination.width = width;
                }
            }
            else if (scaleMode == ScaleMode.ScaleAndCrop)
            {
                if (textureAspect > destinationAspect)
                {
                    uv.width = destinationAspect / textureAspect;
                    uv.x = (1f - uv.width) * .5f;
                }
                else
                {
                    uv.height = textureAspect / destinationAspect;
                    uv.y = (1f - uv.height) * .5f;
                }
            }

            DrawTextureWithTexCoords(destination, texture, uv, alphaBlend);
        }

        /// <summary>Displays original texture coordinates through a retained native CanvasRenderer.</summary>
        /// <param name="destination">The destination rectangle before transformation.</param>
        /// <param name="texture">The source artwork or atlas.</param>
        /// <param name="uv">The normalized source sampling rectangle.</param>
        /// <param name="alphaBlend">Whether the source call requested alpha blending.</param>
        public static void DrawTextureWithTexCoords(Rect destination, Texture texture, Rect uv, bool alphaBlend = true)
        {
            if (!IsFrameActive || texture == null || destination.width <= 0f || destination.height <= 0f)
            {
                return;
            }

            var layer = NextLayer();
            if (s_imageCount == s_images.Count)
            {
                var imageObject = CreateGraphicObject("Original artwork", layer.transform);
                s_images.Add(imageObject.AddComponent<CanvasTextureGraphic>());
            }

            var graphic = s_images[s_imageCount++];
            PrepareGraphic(graphic.rectTransform, layer.transform, true);
            graphic.Configure(texture, destination, uv, Matrix, Color, SpriteDrawState.Clip);
        }

        /// <summary>Displays text with native Canvas font layout.</summary>
        /// <param name="destination">The original text bounds.</param>
        /// <param name="text">The visible text.</param>
        /// <param name="style">The view's layout and font settings.</param>
        public static void Label(Rect destination, string text, GUIStyle? style)
        {
            if (!IsFrameActive || string.IsNullOrEmpty(text) || destination.width <= 0f || destination.height <= 0f)
            {
                return;
            }

            var layer = NextLayer();
            if (s_labelCount == s_labels.Count)
            {
                var labelObject = CreateGraphicObject("Original text", layer.transform);
                s_labels.Add(labelObject.AddComponent<CanvasTextGraphic>());
            }

            var graphic = s_labels[s_labelCount++];
            PrepareGraphic(graphic.rectTransform, layer.transform, false);
            if (style == null)
            {
                throw new System.ArgumentNullException(nameof(style));
            }

            graphic.Configure(destination, text, style, Matrix, Color * ContentColor * style.normal.textColor, SpriteDrawState.Clip);
        }

        /// <summary>Starts a clipped group whose local origin is the group's top-left corner.</summary>
        /// <param name="bounds">The group's bounds in parent coordinates.</param>
        public static void BeginGroup(Rect bounds)
        {
            s_groups.Push(new Group(Matrix, SpriteDrawState.Clip));
            SpriteDrawState.Clip = Intersect(SpriteDrawState.Clip, TransformBounds(bounds, Matrix));
            Matrix *= Matrix4x4.Translate(new Vector3(bounds.x, bounds.y));
        }

        /// <summary>Restores the parent transform and clipping rectangle.</summary>
        public static void EndGroup()
        {
            if (s_groups.Count == 0)
            {
                return;
            }

            var parent = s_groups.Pop();
            Matrix = parent.Matrix;
            SpriteDrawState.Clip = parent.Clip;
        }

        /// <summary>Displays a transparent native button over the original button artwork.</summary>
        /// <param name="bounds">The original button bounds.</param>
        /// <param name="content">Optional native text content.</param>
        /// <param name="style">The original button's text layout.</param>
        /// <returns>Whether the native EventSystem completed a click since the previous frame.</returns>
        public static bool Button(Rect bounds, GUIContent content, GUIStyle style)
        {
            var layer = NextLayer();
            if (s_buttonCount == s_buttons.Count)
            {
                var buttonObject = CreateGraphicObject("Original button input", layer.transform);
                var graphic = buttonObject.AddComponent<Image>();
                graphic.color = UnityEngine.Color.clear;
                var control = new ButtonControl { Button = buttonObject.AddComponent<Button>() };
                control.Button.targetGraphic = graphic;
                control.Button.transition = Selectable.Transition.None;
                control.Button.onClick.AddListener(() => control.Clicked = true);
                s_buttons.Add(control);
            }

            var button = s_buttons[s_buttonCount++];
            var screenBounds = TransformBounds(bounds, Matrix);
            if (button.Identity != screenBounds)
            {
                button.Clicked = false;
                button.Identity = screenBounds;
            }

            PrepareControl(button.Button.transform as RectTransform, layer.transform, bounds);
            button.Button.interactable = Enabled;
            var clicked = Enabled && button.Clicked;
            button.Clicked = false;
            if (!string.IsNullOrEmpty(content.text))
            {
                Label(bounds, content.text, style);
            }

            return clicked;
        }

        /// <summary>Displays a native field supporting caret movement, IME, selection, and mobile keyboards.</summary>
        /// <param name="bounds">The original editable bounds.</param>
        /// <param name="value">The current external text value.</param>
        /// <param name="style">The native field's font, color, and padding settings.</param>
        /// <returns>The current edited text.</returns>
        public static string TextField(Rect bounds, string value, GUIStyle? style)
        {
            if (style == null)
            {
                throw new System.ArgumentNullException(nameof(style));
            }

            return TextField(bounds, value, 0, style);
        }

        /// <summary>Displays a native field with an optional character limit.</summary>
        /// <param name="bounds">The original editable bounds.</param>
        /// <param name="value">The current external text value.</param>
        /// <param name="maxLength">The character limit, or zero for no limit.</param>
        /// <param name="style">The native field's font, color, and padding settings.</param>
        /// <returns>The current edited text.</returns>
        public static string TextField(Rect bounds, string value, int maxLength, GUIStyle style)
        {
            var layer = NextLayer();
            if (s_fieldCount == s_fields.Count)
            {
                s_fields.Add(CreateField(layer.transform));
            }

            var control = s_fields[s_fieldCount++];
            var identityChanged = control.Name != s_nextControlName || control.Identity != bounds;
            if (identityChanged)
            {
                control.Field.DeactivateInputField();
                control.Field.SetTextWithoutNotify(value);
                control.Name = s_nextControlName;
                control.Identity = bounds;
            }
            else if (control.LastValue != value)
            {
                control.Field.SetTextWithoutNotify(value);
            }

            s_nextControlName = string.Empty;
            PrepareControl(control.Field.transform as RectTransform, layer.transform, bounds);
            control.Field.interactable = Enabled;
            control.Field.characterLimit = Mathf.Max(0, maxLength);
            control.Text.font = style.font != null ? style.font : DefaultFont;
            control.Text.fontSize = Mathf.Max(1, style.fontSize);
            control.Text.fontStyle = style.fontStyle;
            control.Text.alignment = style.alignment;
            control.Text.color = Color * ContentColor * style.normal.textColor;
            var padding = style.padding;
            control.Text.rectTransform.offsetMin = new Vector2(padding.left, padding.bottom);
            control.Text.rectTransform.offsetMax = new Vector2(-padding.right, -padding.top);
            if (!string.IsNullOrEmpty(s_requestedFocus) && control.Name == s_requestedFocus)
            {
                Focus(control.Field);
                s_requestedFocus = null;
            }

            control.LastValue = control.Field.text;
            return control.LastValue;
        }

        /// <summary>Assigns a stable focus identity to the next editable control.</summary>
        /// <param name="name">The view's stable control identity.</param>
        public static void SetNextControlName(string name)
        {
            s_nextControlName = name;
        }

        /// <summary>Transfers native keyboard focus to a named field.</summary>
        /// <param name="name">The stable control identity assigned by the view.</param>
        public static void FocusControl(string name)
        {
            for (var index = 0; index < s_fieldCount; index++)
            {
                var control = s_fields[index];
                if (control.Name == name)
                {
                    Focus(control.Field);
                    s_requestedFocus = null;
                    return;
                }
            }

            s_requestedFocus = name;
        }

        /// <summary>Releases the native interface objects owned by this game lifetime.</summary>
        public static void Dispose()
        {
            if (s_canvas != null)
            {
                s_canvas.gameObject.SetActive(false);
                Destroy(s_canvas.gameObject);
            }

            if (s_ownedEventSystem != null)
            {
                s_ownedEventSystem.SetActive(false);
                Destroy(s_ownedEventSystem);
            }

            s_canvas = null;
            s_ownedEventSystem = null;
            s_currentLayer = null;
            s_layers.Clear();
            s_images.Clear();
            s_labels.Clear();
            s_buttons.Clear();
            s_fields.Clear();
            s_groups.Clear();
            s_requestedFocus = null;
            IsFrameActive = false;
        }

        /// <summary>Intersects two top-left pixel rectangles without creating negative extents.</summary>
        /// <param name="first">The first clipping rectangle.</param>
        /// <param name="second">The second clipping rectangle.</param>
        /// <returns>The accepted pixel intersection.</returns>
        internal static Rect Intersect(Rect first, Rect second)
        {
            var left = Mathf.Max(first.xMin, second.xMin);
            var top = Mathf.Max(first.yMin, second.yMin);
            return new Rect(left, top, Mathf.Max(0f, Mathf.Min(first.xMax, second.xMax) - left), Mathf.Max(0f, Mathf.Min(first.yMax, second.yMax) - top));
        }

        /// <summary>Calculates the axis-aligned screen bounds of an original rectangle.</summary>
        /// <param name="bounds">The original rectangle.</param>
        /// <param name="transform">The original-to-screen transform.</param>
        /// <returns>The rectangle containing all four transformed corners.</returns>
        internal static Rect TransformBounds(Rect bounds, Matrix4x4 transform)
        {
            var first = transform.MultiplyPoint3x4(new Vector3(bounds.xMin, bounds.yMin));
            var second = transform.MultiplyPoint3x4(new Vector3(bounds.xMax, bounds.yMin));
            var third = transform.MultiplyPoint3x4(new Vector3(bounds.xMax, bounds.yMax));
            var fourth = transform.MultiplyPoint3x4(new Vector3(bounds.xMin, bounds.yMax));
            var minimum = Vector3.Min(Vector3.Min(first, second), Vector3.Min(third, fourth));
            var maximum = Vector3.Max(Vector3.Max(first, second), Vector3.Max(third, fourth));
            return Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
        }

        /// <summary>Creates the screen-space Canvas and a native EventSystem when required.</summary>
        private static void EnsureRoot()
        {
            if (s_canvas != null)
            {
                return;
            }

            var root = new GameObject("PvZ Canvas UI", typeof(RectTransform), typeof(Canvas))
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            s_canvas = root.GetComponent<Canvas>();
            s_canvas.renderMode = RenderMode.ScreenSpaceCamera;
            s_canvas.worldCamera = ObjectSpriteRenderer.SceneCamera;
            s_canvas.planeDistance = 1f;
            s_canvas.scaleFactor = 1f;
            s_canvas.referencePixelsPerUnit = 100f;
            if (EventSystem.current == null)
            {
                s_ownedEventSystem = new GameObject("PvZ Native Input", typeof(EventSystem), typeof(CanvasInputModule))
                {
                    hideFlags = HideFlags.DontSave
                };
                s_ownedEventSystem.GetComponent<EventSystem>().sendNavigationEvents = false;
            }
        }

        /// <summary>Gets the retained Canvas layer for the next shared painter-order position.</summary>
        /// <returns>The layer receiving the next native graphic.</returns>
        private static Canvas NextLayer()
        {
            EnsureRoot();
            var order = SpriteDrawState.NextOrder();
            if (s_currentLayer == null || order != s_previousOrder + 1)
            {
                if (s_layerCount == s_layers.Count)
                {
                    var layerObject = CreateGraphicObject("Original interface layer", s_canvas!.transform);
                    var layer = layerObject.AddComponent<Canvas>();
                    layer.overrideSorting = true;
                    layer.worldCamera = ObjectSpriteRenderer.SceneCamera;
                    layerObject.AddComponent<GraphicRaycaster>();
                    s_layers.Add(layer);
                }

                s_currentLayer = s_layers[s_layerCount++];
                if (!s_currentLayer.gameObject.activeSelf)
                {
                    s_currentLayer.gameObject.SetActive(true);
                }

                s_currentLayer.sortingOrder = order;
                LayoutAtTopLeft(s_currentLayer.transform as RectTransform);
                s_siblingIndex = 0;
            }

            s_previousOrder = order;
            return s_currentLayer;
        }

        /// <summary>Creates a retained native graphic object on the reserved presentation layer.</summary>
        /// <param name="name">The name displayed in the Unity hierarchy.</param>
        /// <param name="parent">The owning Canvas layer.</param>
        /// <returns>The newly created native object.</returns>
        private static GameObject CreateGraphicObject(string name, Transform parent)
        {
            var result = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer))
            {
                hideFlags = HideFlags.DontSave,
                layer = 31
            };
            result.transform.SetParent(parent, false);
            LayoutAtTopLeft(result.transform as RectTransform);
            return result;
        }

        /// <summary>Reparents and activates a graphic while preserving its current painter order.</summary>
        /// <param name="transform">The retained graphic transform.</param>
        /// <param name="parent">The layer receiving this frame's submission.</param>
        /// <param name="sizeToScreen">Whether this graphic uses physical screen-sized local bounds.</param>
        private static void PrepareGraphic(RectTransform transform, Transform parent, bool sizeToScreen)
        {
            if (transform.parent != parent)
            {
                transform.SetParent(parent, false);
            }

            if (transform.GetSiblingIndex() != s_siblingIndex)
            {
                transform.SetSiblingIndex(s_siblingIndex);
            }

            s_siblingIndex++;
            if (!transform.gameObject.activeSelf)
            {
                transform.gameObject.SetActive(true);
            }

            if (sizeToScreen)
            {
                var size = new Vector2(Screen.width, Screen.height);
                if (transform.sizeDelta != size)
                {
                    transform.sizeDelta = size;
                }
            }
        }

        /// <summary>Anchors a native transform at the physical screen's top-left corner.</summary>
        /// <param name="transform">The native transform to configure.</param>
        private static void LayoutAtTopLeft(RectTransform? transform)
        {
            if (transform == null)
            {
                return;
            }

            var topLeft = new Vector2(0f, 1f);
            if (transform.anchorMin != topLeft)
            {
                transform.anchorMin = topLeft;
            }

            if (transform.anchorMax != topLeft)
            {
                transform.anchorMax = topLeft;
            }

            if (transform.pivot != topLeft)
            {
                transform.pivot = topLeft;
            }

            if (transform.anchoredPosition != Vector2.zero)
            {
                transform.anchoredPosition = Vector2.zero;
            }

            var size = new Vector2(Screen.width, Screen.height);
            if (transform.sizeDelta != size)
            {
                transform.sizeDelta = size;
            }

            if (transform.localScale != Vector3.one)
            {
                transform.localScale = Vector3.one;
            }

            if (transform.localRotation != Quaternion.identity)
            {
                transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>Transforms a native button or editable field into its screen rectangle.</summary>
        /// <param name="transform">The native control transform.</param>
        /// <param name="parent">The Canvas layer receiving this control.</param>
        /// <param name="bounds">The original control rectangle.</param>
        private static void PrepareControl(RectTransform? transform, Transform parent, Rect bounds)
        {
            if (transform == null)
            {
                return;
            }

            PrepareGraphic(transform, parent, false);
            var origin = Matrix.MultiplyPoint3x4(new Vector3(bounds.x, bounds.y));
            var horizontal = Matrix.MultiplyVector(Vector3.right);
            var vertical = Matrix.MultiplyVector(Vector3.up);
            transform.anchoredPosition = new Vector2(origin.x, -origin.y);
            transform.sizeDelta = bounds.size;
            transform.localScale = new Vector3(horizontal.magnitude, vertical.magnitude, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Atan2(horizontal.y, horizontal.x) * Mathf.Rad2Deg);
        }

        /// <summary>Creates a native field with an invisible input surface and a visible text child.</summary>
        /// <param name="parent">The owning native Canvas layer.</param>
        /// <returns>The retained editable control.</returns>
        private static FieldControl CreateField(Transform parent)
        {
            var fieldObject = CreateGraphicObject("Original text input", parent);
            var inputSurface = fieldObject.AddComponent<Image>();
            inputSurface.color = UnityEngine.Color.clear;
            var textObject = CreateGraphicObject("Editable text", fieldObject.transform);
            var text = textObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            var textTransform = text.rectTransform;
            textTransform.anchorMin = Vector2.zero;
            textTransform.anchorMax = Vector2.one;
            textTransform.offsetMin = Vector2.zero;
            textTransform.offsetMax = Vector2.zero;
            var field = fieldObject.AddComponent<InputField>();
            field.targetGraphic = inputSurface;
            field.textComponent = text;
            field.transition = Selectable.Transition.None;
            field.lineType = InputField.LineType.SingleLine;
            field.customCaretColor = true;
            field.caretColor = UnityEngine.Color.white;
            field.selectionColor = new Color(.2f, .4f, .8f, .5f);
            return new FieldControl { Field = field, Text = text };
        }

        /// <summary>Transfers the native EventSystem's keyboard selection to an editable field.</summary>
        /// <param name="field">The native field receiving keyboard and IME input.</param>
        private static void Focus(InputField field)
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(field.gameObject);
            }

            field.ActivateInputField();
        }

        /// <summary>Destroys an owned Unity object in the appropriate editor or player lifetime.</summary>
        /// <param name="value">The owned native object to release.</param>
        private static void Destroy(Object value)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(value);
            }
            else
            {
                Object.DestroyImmediate(value);
            }
        }
    }
}
