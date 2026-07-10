using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using SichuanMahjong.Core;
using SichuanMahjong.Core.Algorithm;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SichuanMahjong.UnityApp
{
    /// <summary>
    /// Application entry point. Runs from any scene (no serialized scene
    /// references): it builds the camera/canvas/event system in code, loads
    /// the probability tables off the main thread — the Java Main did this
    /// synchronously before opening the window — and then hands control to
    /// GamePanelBehaviour.
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        private RectTransform gameRoot;
        private GameObject loadingUi;
        private Text loadingText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStart()
        {
            if (UnityEngine.Object.FindObjectOfType<Bootstrap>() != null)
            {
                return;
            }
            GameObject go = new GameObject("MahjongBootstrap");
            DontDestroyOnLoad(go);
            go.AddComponent<Bootstrap>();
        }

        private void Awake()
        {
            ConfigureRuntimeWindow();
            EnsureCamera();
            EnsureEventSystem();
            gameRoot = CreateCanvasAndRoot();
            CreateLoadingUi();
        }

        private static void ConfigureRuntimeWindow()
        {
            Application.targetFrameRate = Config.FPS;
#if !UNITY_EDITOR
            Screen.SetResolution(Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT, false);
#endif
        }

        private void Start()
        {
            StartCoroutine(LoadAndLaunch());
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            go.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private RectTransform CreateCanvasAndRoot()
        {
            GameObject canvasGo = new GameObject("MahjongCanvas", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            DontDestroyOnLoad(canvasGo);

            GameObject rootGo = UiFactory.CreateChild(canvasGo.transform, "GameRoot");
            RectTransform root = (RectTransform)rootGo.transform;
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);
            rootGo.AddComponent<RootScaler>();
            return root;
        }

        private void CreateLoadingUi()
        {
            loadingUi = UiFactory.CreateChild(gameRoot, "Loading");
            UiFactory.SetRect((RectTransform)loadingUi.transform, 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT);
            UiFactory.CreatePanel(loadingUi.transform, "Backdrop", 0, 0, Config.SCREEN_WIDTH, Config.SCREEN_HEIGHT,
                new Color32(20, 45, 32, 255));
            loadingText = UiFactory.CreateText(loadingUi.transform, "LoadingText", 0,
                Config.SCREEN_HEIGHT / 2f - 30f, Config.SCREEN_WIDTH, 60,
                "Loading probability tables...", 26, Color.white, true, TextAnchor.MiddleCenter);
        }

        private IEnumerator LoadAndLaunch()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "probability");
            Exception failure = null;
            Task loadTask = Task.Run(() =>
            {
                try
                {
                    AITableJian.Load(File.ReadLines(Path.Combine(dir, "majiang_ai_jian.txt")));
                    AITableFeng.Load(File.ReadLines(Path.Combine(dir, "majiang_ai_feng.txt")));
                    AITable.Load(File.ReadLines(Path.Combine(dir, "majiang_ai_normal.txt")));
                }
                catch (Exception e)
                {
                    failure = e;
                }
            });

            float started = Time.realtimeSinceStartup;
            while (!loadTask.IsCompleted)
            {
                loadingText.text = "Loading probability tables... "
                                   + (Time.realtimeSinceStartup - started).ToString("F0") + "s";
                yield return null;
            }

            if (failure != null)
            {
                Debug.LogError("Failed to load probability tables: " + failure);
                loadingText.text = "Failed to load probability tables:\n" + failure.Message
                                   + "\nExpected under " + dir;
                yield break;
            }

            Destroy(loadingUi);

            BeepPlayer beeper = gameObject.AddComponent<BeepPlayer>();
            CoreEnv.Beep = beeper.Beep;
            CoreEnv.Println = message => Debug.Log(message);

            GamePanelBehaviour panel = gameObject.AddComponent<GamePanelBehaviour>();
            panel.Initialize(gameRoot, new TileImageLoader(), beeper);
        }

        /// <summary>
        /// Fits the 1920x960 logical space inside the current window while
        /// preserving aspect ratio. Free Aspect resizing in the Unity editor can
        /// otherwise squeeze fixed-coordinate UI until panels overlap or clip.
        /// </summary>
        private class RootScaler : MonoBehaviour
        {
            private void Update()
            {
                RectTransform rt = (RectTransform)transform;
                float scale = Mathf.Min(
                    Screen.width / (float)Config.SCREEN_WIDTH,
                    Screen.height / (float)Config.SCREEN_HEIGHT);
                float scaledWidth = Config.SCREEN_WIDTH * scale;
                float scaledHeight = Config.SCREEN_HEIGHT * scale;
                float offsetX = (Screen.width - scaledWidth) / 2f;
                float offsetY = (Screen.height - scaledHeight) / 2f;

                rt.localScale = new Vector3(scale, scale, 1f);
                rt.anchoredPosition = new Vector2(offsetX, -offsetY);
            }
        }
    }
}
