using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public List<Sprite> AiAvatars { get; } = new List<Sprite>();
        public Sprite PlayerAvatar { get; private set; }

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
            LoadAvatars();

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

        private void LoadAvatars()
        {
            string avatarDirectory = ResolveAvatarDirectory();
            if (string.IsNullOrEmpty(avatarDirectory))
            {
                Debug.LogWarning("Missing avatar directory.");
                return;
            }

            List<string> aiPaths = Directory.GetFiles(avatarDirectory, "ai_*.png").OrderBy(_ => Random.value).ToList();
            foreach (string path in aiPaths.Take(3))
            {
                Sprite sprite = LoadCroppedSprite(path, Config.AI_AVATAR_WIDTH / (float)Config.AI_AVATAR_HEIGHT);
                if (sprite != null)
                {
                    AiAvatars.Add(sprite);
                }
            }

            List<string> playerPaths = Directory.GetFiles(avatarDirectory, "player_*.png")
                .OrderBy(_ => Random.value)
                .ToList();
            if (playerPaths.Count > 0)
            {
                PlayerAvatar = LoadCroppedSprite(playerPaths[0],
                    Config.PLAYER_AVATAR_WIDTH / (float)Config.PLAYER_AVATAR_HEIGHT);
            }
        }

        private static string ResolveAvatarDirectory()
        {
            string streamingPath = Path.Combine(Application.streamingAssetsPath, Config.AVATAR_IMAGE_PATH);
            if (Directory.Exists(streamingPath))
            {
                return streamingPath;
            }

            string repoRootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..",
                Config.AVATAR_IMAGE_PATH));
            if (Directory.Exists(repoRootPath))
            {
                return repoRootPath;
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

        private static Sprite LoadCroppedSprite(string path, float targetAspect)
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

                float sourceAspect = texture.width / (float)texture.height;
                Rect crop;
                if (sourceAspect > targetAspect)
                {
                    float width = texture.height * targetAspect;
                    crop = new Rect((texture.width - width) / 2f, 0f, width, texture.height);
                }
                else
                {
                    float height = texture.width / targetAspect;
                    crop = new Rect(0f, Mathf.Max(0f, texture.height - height) / 2f, texture.width, height);
                }

                return Sprite.Create(texture, crop, new Vector2(0.5f, 0.5f), 100f, 0,
                    SpriteMeshType.FullRect, Vector4.zero);
            }
            catch (IOException e)
            {
                Debug.LogWarning("Could not load image " + path + ": " + e.Message);
                return null;
            }
        }
    }
}
