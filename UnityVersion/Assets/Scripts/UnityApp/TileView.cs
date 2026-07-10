using SichuanMahjong.Core.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// One rendered tile. The backing Image is the fill color Swing painted
    /// behind each tile (white / light gray on hover / yellow for pungs /
    /// orange for kongs); the child Image carries the tile face sprite.
    /// Interactable tiles (the player's hand) report hover and clicks back to
    /// the game panel, replacing the Swing mouse listeners.
    /// </summary>
    public class TileView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private GamePanelBehaviour panel;
        private Tile tile;
        private Image backgroundImage;
        private Color baseColor;
        private bool interactable;

        public Tile GetTile()
        {
            return tile;
        }

        public static TileView Create(Transform parent, GamePanelBehaviour panel, TileImageLoader imageLoader,
                                      Tile tile, float x, float y, float width, float height, Color background,
                                      bool interactable)
        {
            GameObject go = UiFactory.CreateChild(parent, "Tile " + tile);
            UiFactory.SetRect((RectTransform)go.transform, x, y, width, height);

            TileView view = go.AddComponent<TileView>();
            view.panel = panel;
            view.tile = tile;
            view.baseColor = background;
            view.interactable = interactable;

            view.backgroundImage = go.AddComponent<Image>();
            view.backgroundImage.color = background;
            view.backgroundImage.raycastTarget = interactable;

            Sprite sprite = imageLoader.GetSprite(tile);
            if (sprite != null)
            {
                Image face = UiFactory.CreatePanel(go.transform, "Face", 0, 0, width, height, Color.white);
                face.sprite = sprite;
                face.raycastTarget = false;
            }
            return view;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactable || panel == null)
            {
                return;
            }
            panel.OnTileHovered(this);
            backgroundImage.color = new Color32(192, 192, 192, 255); // Swing Color.LIGHT_GRAY
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!interactable || panel == null)
            {
                return;
            }
            panel.OnTileUnhovered(this);
            backgroundImage.color = baseColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!interactable || panel == null)
            {
                return;
            }
            panel.OnTileClicked(tile);
        }
    }
}
