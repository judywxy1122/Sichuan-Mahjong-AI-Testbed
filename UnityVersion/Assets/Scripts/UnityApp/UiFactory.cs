using UnityEngine;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Code-first uGUI helpers. Everything is positioned in the original
    /// Swing coordinate system: origin at the top-left, y growing downwards.
    /// </summary>
    public static class UiFactory
    {
        private static Font font;
        private static Sprite roundedSprite;

        public static Font GetFont()
        {
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return font;
        }

        /// <summary>Anchors a rect to the top-left and applies Swing-style coordinates.</summary>
        public static void SetRect(RectTransform rt, float x, float y, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(width, height);
        }

        public static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image CreatePanel(Transform parent, string name, float x, float y, float width, float height,
                                        Color color)
        {
            GameObject go = CreateChild(parent, name);
            Image image = go.AddComponent<Image>();
            image.color = color;
            SetRect((RectTransform)go.transform, x, y, width, height);
            return image;
        }

        public static Image CreateRoundedPanel(Transform parent, string name, float x, float y, float width,
                                               float height, Color color)
        {
            Image image = CreatePanel(parent, name, x, y, width, height, color);
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            return image;
        }

        public static Text CreateText(Transform parent, string name, float x, float y, float width, float height,
                                      string content, int size, Color color, bool bold,
                                      TextAnchor anchor = TextAnchor.UpperLeft)
        {
            GameObject go = CreateChild(parent, name);
            Text text = go.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = size;
            text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            text.color = color;
            text.text = content;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            SetRect((RectTransform)go.transform, x, y, width, height);
            return text;
        }

        /// <summary>
        /// Yellow rounded button with a drop shadow and dark edge, close to
        /// the Swing drawActionButton look.
        /// </summary>
        public static Button CreateActionButton(Transform parent, string label, float x, float y, float width,
                                                float height, System.Action onClick)
        {
            return CreateStyledButton(parent, label, x, y, width, height,
                new Color32(255, 242, 78, 255), new Color32(20, 35, 30, 255), new Color32(20, 35, 30, 255), onClick);
        }

        public static Button CreateStyledButton(Transform parent, string label, float x, float y, float width,
                                                float height, Color fill, Color edge, Color textColor,
                                                System.Action onClick)
        {
            GameObject go = CreateChild(parent, "Button " + label);
            SetRect((RectTransform)go.transform, x, y, width, height);

            Image shadow = CreateRoundedPanel(go.transform, "Shadow", 4, 4, width, height,
                new Color(0f, 0f, 0f, 150f / 255f));
            shadow.raycastTarget = false;

            Image edgeImage = CreateRoundedPanel(go.transform, "Edge", 0, 0, width, height, edge);
            Image fillImage = CreateRoundedPanel(go.transform, "Fill", 2, 2, width - 4, height - 4, fill);
            fillImage.raycastTarget = false;

            Text text = CreateText(go.transform, "Label", 0, 0, width, height, label, 15, textColor, true,
                TextAnchor.MiddleCenter);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = edgeImage;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }
            return button;
        }

        /// <summary>Hollow rectangle outline built from four thin images.</summary>
        public static GameObject CreateBorder(Transform parent, string name, float x, float y, float width,
                                              float height, Color color, float thickness)
        {
            GameObject go = CreateChild(parent, name);
            SetRect((RectTransform)go.transform, x, y, width, height);
            CreatePanel(go.transform, "Top", 0, 0, width, thickness, color).raycastTarget = false;
            CreatePanel(go.transform, "Bottom", 0, height - thickness, width, thickness, color).raycastTarget = false;
            CreatePanel(go.transform, "Left", 0, 0, thickness, height, color).raycastTarget = false;
            CreatePanel(go.transform, "Right", width - thickness, 0, thickness, height, color).raycastTarget = false;
            return go;
        }

        /// <summary>White 9-sliced rounded-rect sprite generated once at runtime.</summary>
        public static Sprite GetRoundedSprite()
        {
            if (roundedSprite != null)
            {
                return roundedSprite;
            }
            const int size = 24;
            const int radius = 7;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    bool inside = true;
                    int cx = px < radius ? radius : (px >= size - radius ? size - 1 - radius : px);
                    int cy = py < radius ? radius : (py >= size - radius ? size - 1 - radius : py);
                    if ((px < radius || px >= size - radius) && (py < radius || py >= size - radius))
                    {
                        float dx = px - cx;
                        float dy = py - cy;
                        inside = dx * dx + dy * dy <= radius * radius;
                    }
                    pixels[py * size + px] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            roundedSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius + 1, radius + 1, radius + 1, radius + 1));
            return roundedSprite;
        }

        public static Sprite SpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, Vector4.zero);
        }
    }
}
