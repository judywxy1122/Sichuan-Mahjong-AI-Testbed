using System.Collections.Generic;
using SichuanMahjong.Core;
using SichuanMahjong.Core.Gameplay;
using SichuanMahjong.Core.Model;
using UnityEngine;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Modeless "winning hand" panel — port of GamePanel.showWinningHandDialog.
    /// The concealed tiles are arranged into groups by WinningHandArranger and
    /// rendered in a horizontally scrollable row next to the melds.
    /// </summary>
    public static class WinningHandDialog
    {
        private const float DialogWidth = 980f;
        private const float DialogHeight = 190f;
        private const float ViewportX = 16f;
        private const float ViewportY = 48f;
        private const float ScrollbarHeight = 14f;
        private const float ScrollbarGap = 5f;

        public static GameObject Show(Transform parent, TileImageLoader imageLoader, Game game)
        {
            Player winner = game.GetWinner();
            if (winner == null)
            {
                return null;
            }

            GameObject dialog = UiFactory.CreateChild(parent, "WinningHandDialog");
            float x = (Config.SCREEN_WIDTH - DialogWidth) / 2f;
            float y = (Config.SCREEN_HEIGHT - DialogHeight) / 2f;
            UiFactory.SetRect((RectTransform)dialog.transform, x, y, DialogWidth, DialogHeight);

            Image backdrop = dialog.AddComponent<Image>();
            backdrop.color = new Color32(35, 70, 45, 255);

            UiFactory.CreateText(dialog.transform, "Title", 16, 12, DialogWidth - 120, 28,
                winner.GetName() + " wins", 20, new Color32(255, 245, 120, 255), true);

            UiFactory.CreateActionButton(dialog.transform, "Close", DialogWidth - 92, 10, 80, 30,
                () => Object.Destroy(dialog));

            // Horizontally scrollable tile row.
            float viewportWidth = DialogWidth - ViewportX * 2f;
            float viewportHeight = DialogHeight - 62f - ScrollbarHeight - ScrollbarGap;
            GameObject viewport = UiFactory.CreateChild(dialog.transform, "Viewport");
            UiFactory.SetRect((RectTransform)viewport.transform, ViewportX, ViewportY, viewportWidth,
                viewportHeight);
            viewport.AddComponent<RectMask2D>();
            Image viewportImage = viewport.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0f);

            GameObject content = UiFactory.CreateChild(viewport.transform, "Content");
            RectTransform contentRt = (RectTransform)content.transform;

            float cursorX = 0f;
            cursorX = AddSectionLabel(content.transform, "Winning shape", cursorX);
            List<List<Tile>> concealedGroups = WinningHandArranger.ArrangeConcealedWinningGroups(
                winner, game.GetWinningTile());
            if (concealedGroups.Count == 0)
            {
                cursorX = AddTileGroup(content.transform, imageLoader, winner.GetHand().ToList(), cursorX);
                if (game.GetWinningTile() != null)
                {
                    cursorX = AddTileGroup(content.transform, imageLoader,
                        new List<Tile> { game.GetWinningTile() }, cursorX);
                }
            }
            else
            {
                foreach (List<Tile> group in concealedGroups)
                {
                    cursorX = AddTileGroup(content.transform, imageLoader, group, cursorX);
                }
            }

            List<Group> melds = winner.GetHand().GetPungKong();
            if (melds.Count > 0)
            {
                cursorX = AddSectionLabel(content.transform, "Melds", cursorX);
                foreach (Group group in melds)
                {
                    cursorX = AddTileGroup(content.transform, imageLoader, group.ToList(), cursorX);
                }
            }

            cursorX += 24f;
            UiFactory.SetRect(contentRt, 0, 0, cursorX, viewportHeight);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;

            Scrollbar scrollbar = CreateHorizontalScrollbar(dialog.transform, ViewportX,
                ViewportY + viewportHeight + ScrollbarGap, viewportWidth, ScrollbarHeight);
            scrollRect.horizontalScrollbar = scrollbar;
            scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.horizontalScrollbarSpacing = 0f;

            return dialog;
        }

        private static float AddSectionLabel(Transform parent, string text, float cursorX)
        {
            float width = text.Length * 9f + 12f;
            UiFactory.CreateText(parent, "Section " + text, cursorX + 10f, 0, width,
                DialogHeight - 62f - ScrollbarHeight - ScrollbarGap, text, 15,
                Color.white, true, TextAnchor.MiddleLeft);
            return cursorX + 10f + width + 2f;
        }

        private static float AddTileGroup(Transform parent, TileImageLoader imageLoader, List<Tile> tiles,
                                          float cursorX)
        {
            const float tileWidth = 52f;
            const float tileHeight = 73f;
            const float pad = 4f;
            float groupWidth = tiles.Count * (tileWidth + 2f) + pad * 2f;
            float groupHeight = tileHeight + pad * 2f;
            float groupY = (DialogHeight - 62f - groupHeight - 2f) / 2f;

            GameObject group = UiFactory.CreateChild(parent, "TileGroup");
            UiFactory.SetRect((RectTransform)group.transform, cursorX + 8f, groupY, groupWidth + 2f, groupHeight + 2f);
            UiFactory.CreateBorder(group.transform, "GroupBorder", 0, 0, groupWidth + 2f, groupHeight + 2f,
                new Color32(255, 245, 120, 255), 1f);
            UiFactory.CreatePanel(group.transform, "GroupBack", 1, 1, groupWidth, groupHeight,
                new Color32(45, 86, 56, 255));

            for (int i = 0; i < tiles.Count; i++)
            {
                float tileX = 1f + pad + i * (tileWidth + 2f);
                UiFactory.CreateBorder(group.transform, "TileBorder", tileX - 1f, pad, tileWidth + 2f,
                    tileHeight + 2f, Color.white, 1f);
                Image face = UiFactory.CreatePanel(group.transform, "TileFace", tileX, pad + 1f, tileWidth,
                    tileHeight, Color.white);
                Sprite sprite = imageLoader.GetSprite(tiles[i]);
                if (sprite != null)
                {
                    face.sprite = sprite;
                }
                face.raycastTarget = false;
            }

            return cursorX + 8f + groupWidth + 2f;
        }

        private static Scrollbar CreateHorizontalScrollbar(Transform parent, float x, float y, float width,
                                                            float height)
        {
            GameObject scrollbarObject = UiFactory.CreateChild(parent, "HorizontalScrollbar");
            UiFactory.SetRect((RectTransform)scrollbarObject.transform, x, y, width, height);

            Image track = scrollbarObject.AddComponent<Image>();
            track.color = new Color32(225, 225, 225, 255);

            GameObject slidingArea = UiFactory.CreateChild(scrollbarObject.transform, "Sliding Area");
            RectTransform slidingRt = (RectTransform)slidingArea.transform;
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(2f, 2f);
            slidingRt.offsetMax = new Vector2(-2f, -2f);

            GameObject handle = UiFactory.CreateChild(slidingArea.transform, "Handle");
            RectTransform handleRt = (RectTransform)handle.transform;
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0.25f, 1f);
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;

            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color32(175, 175, 175, 255);

            Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRt;
            scrollbar.direction = Scrollbar.Direction.LeftToRight;
            return scrollbar;
        }
    }
}
