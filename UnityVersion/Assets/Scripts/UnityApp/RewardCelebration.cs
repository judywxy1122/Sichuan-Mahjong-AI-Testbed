using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// The winner's celebration: an animated dollar icon above the Reward
    /// button, a one-shot celebration sound, and the reward video opening —
    /// on macOS it first tries a fullscreen Chrome app window (matching the
    /// Java version), otherwise it falls back to the default browser.
    /// </summary>
    public static class RewardCelebration
    {
        private static readonly List<string> RewardUrls = new List<string>
        {
            "https://www.tiktok.com/@innahbee/video/7527297056680070408",
            "https://www.tiktok.com/@innahbee/video/7516930359687269650",
            "https://www.tiktok.com/@innahbee/video/7519519217574595848",
            "https://www.tiktok.com/@innahbee/video/7333543909315874053",
            "https://www.tiktok.com/@innahbee/video/7519162164683361554",
            "https://www.tiktok.com/@innahbee/video/7379244215131213061",
            "https://www.tiktok.com/@innahbee/video/7416723958856158471",
            "https://www.tiktok.com/@innahbee/video/7299467527459966214",
            "https://www.tiktok.com/@shakira/video/7637621329239264542",
            "https://www.tiktok.com/@shakira/video/7637147185368337694",
            "https://www.tiktok.com/@shakira/video/7653975849384938782",
            "https://www.tiktok.com/@shakira/video/7650978235496271135",
            "https://www.tiktok.com/@shakira/video/7648370860759256350",
            "https://www.tiktok.com/@shakira/video/7645778945740262687",
            "https://www.tiktok.com/@shakira/video/7643914092557634829",
            "https://www.tiktok.com/@shakira/video/7643129251356511502",
            "https://www.tiktok.com/@shakira/video/7642117041159195935",
            "https://www.tiktok.com/@gqspain/video/7650216595243158806"
        };

        private static readonly System.Random rewardRandom = new System.Random();

        public static void OpenRandomReward()
        {
            string url = RewardUrls[rewardRandom.Next(RewardUrls.Count)];
            try
            {
                if (!OpenRewardInChromeFullscreen(url))
                {
                    Application.OpenURL(url);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("Could not open reward video: " + e.Message);
                Application.OpenURL(url);
            }
        }

        private static bool OpenRewardInChromeFullscreen(string url)
        {
            if (Application.platform != RuntimePlatform.OSXEditor
                && Application.platform != RuntimePlatform.OSXPlayer)
            {
                return false;
            }
            try
            {
                Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = "-na \"Google Chrome\" --args --new-window --start-fullscreen "
                                + "\"--app=" + url + "\"",
                    UseShellExecute = false
                });
                if (process == null)
                {
                    return false;
                }
                if (process.WaitForExit(1500))
                {
                    return process.ExitCode == 0;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Animated dollar icon (extracted GIF frames) above the Reward button.</summary>
        public static Image CreateAnimationIcon(Transform parent, TileImageLoader imageLoader, float buttonX,
                                                float buttonY, float buttonWidth)
        {
            if (imageLoader.RewardFrames.Count == 0)
            {
                return null;
            }
            const float size = 54f;
            float x = buttonX + (buttonWidth - size) / 2f;
            float y = Mathf.Max(24f, buttonY - size - 3f);
            Image icon = UiFactory.CreatePanel(parent, "RewardIcon", x, y, size, size, Color.white);
            icon.sprite = imageLoader.RewardFrames[0];
            icon.raycastTarget = false;
            RewardIconAnimator animator = icon.gameObject.AddComponent<RewardIconAnimator>();
            animator.Initialize(icon, imageLoader.RewardFrames);
            return icon;
        }

        private class RewardIconAnimator : MonoBehaviour
        {
            private Image image;
            private List<Sprite> frames;
            private float elapsed;
            private const float FrameSeconds = 0.1f;

            public void Initialize(Image target, List<Sprite> frameSprites)
            {
                image = target;
                frames = frameSprites;
            }

            private void Update()
            {
                if (image == null || frames == null || frames.Count == 0)
                {
                    return;
                }
                elapsed += Time.deltaTime;
                int index = (int)(elapsed / FrameSeconds) % frames.Count;
                image.sprite = frames[index];
            }
        }
    }
}
