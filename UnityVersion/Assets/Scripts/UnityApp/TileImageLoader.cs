using System.Collections.Generic;
using System.IO;
using SichuanMahjong.Core.Model;
using UnityEngine;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Loads tile faces, the table background photo and the reward animation
    /// frames from StreamingAssets. Textures are loaded through raw PNG/JPG
    /// bytes so no editor import settings are required.
    /// </summary>
    public class TileImageLoader
    {
        private readonly Dictionary<TileTypeEnum, Dictionary<int, Sprite>> images =
            new Dictionary<TileTypeEnum, Dictionary<int, Sprite>>();

        public Sprite Background { get; private set; }
        public List<Sprite> RewardFrames { get; } = new List<Sprite>();

        public TileImageLoader()
        {
            foreach (TileTypeEnum type in new[] { TileTypeEnum.B, TileTypeEnum.C, TileTypeEnum.D })
            {
                Dictionary<int, Sprite> typeImages = new Dictionary<int, Sprite>();
                for (int number = 1; number <= 9; number++)
                {
                    string path = Path.Combine(Application.streamingAssetsPath, "img", type.GetEnglish(),
                        "0" + number + ".png");
                    Sprite sprite = LoadSprite(path);
                    if (sprite != null)
                    {
                        typeImages[number] = sprite;
                    }
                }
                images[type] = typeImages;
            }

            Background = LoadSprite(Path.Combine(Application.streamingAssetsPath, Config.BACKGROUND_IMAGE_PATH));

            for (int frame = 0; ; frame++)
            {
                string path = Path.Combine(Application.streamingAssetsPath, "img", "reward",
                    "dollar_frame_" + frame.ToString("00") + ".png");
                if (!File.Exists(path))
                {
                    break;
                }
                Sprite sprite = LoadSprite(path);
                if (sprite == null)
                {
                    break;
                }
                RewardFrames.Add(sprite);
            }
        }

        public Sprite GetSprite(Tile tile)
        {
            if (images.TryGetValue(tile.GetTileType(), out Dictionary<int, Sprite> typeImages)
                && typeImages.TryGetValue(tile.GetNumber(), out Sprite sprite))
            {
                return sprite;
            }
            return null;
        }

        private static Sprite LoadSprite(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Missing image: " + path);
                    return null;
                }
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Debug.LogWarning("Could not decode image: " + path);
                    return null;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                return UiFactory.SpriteFromTexture(texture);
            }
            catch (IOException e)
            {
                Debug.LogWarning("Could not load image " + path + ": " + e.Message);
                return null;
            }
        }
    }
}
