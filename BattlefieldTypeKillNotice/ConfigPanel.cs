using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Duckov.Options.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattlefieldTypeKillNotice
{

    public class ConfigPanel : MonoBehaviour
    {
        public struct KillNoticeConfig
        {
            public float Volume;
            public float TotalScale;
            public bool ShowIcons;
            public int IconType;
            public int IconSize;
            public float HeadshotScale;
            public float IconDuration;
            public int IconSpacing;
            public int XOffset;
            public int YOffset;
            public bool ShowText;
            public int EnemyInfoTextFontSize;
            public int EnemyInfoTextXOffset;
            public int EnemyInfoTextYOffset;
            public int ExpInfoTextFontSize;
            public int ExpInfoTextXOffset;
            public int ExpInfoTextYOffset;
            public bool UseLocalResource;

            public KillNoticeConfig(float volume, float totalScale, bool showIcons, int iconType, int iconSize, float headShotScale, float duration, int iconSpacing, int xOffset, int yOffset, bool showText, int enemyInfoTextFontSize, int enemyInfoTextXOffset, int enemyInfoTextYOffset, int expInfoTextFontSize, int expInfoTextXOffset, int expInfoTextYOffset, bool useLocalResource)
            {
                Volume = volume;
                TotalScale = totalScale;
                ShowIcons = showIcons;
                IconType = iconType;
                IconSize = iconSize;
                HeadshotScale = headShotScale;
                IconDuration = duration;
                IconSpacing = iconSpacing;
                XOffset = xOffset;
                YOffset = yOffset;
                ShowText = showText;
                EnemyInfoTextFontSize = enemyInfoTextFontSize;
                EnemyInfoTextXOffset = enemyInfoTextXOffset;
                EnemyInfoTextYOffset = enemyInfoTextYOffset;
                ExpInfoTextFontSize = expInfoTextFontSize;
                ExpInfoTextXOffset = expInfoTextXOffset;
                ExpInfoTextYOffset = expInfoTextYOffset;
                UseLocalResource = useLocalResource;
            }
        }

        public static ConfigPanel Instance { get; private set; }
        private static ModBehaviour _modInst;
        private static readonly KillNoticeConfig DefaultKillNoticeConfig = new KillNoticeConfig(0.4f, 1f, true, 2, 100, 1.2f, 3f, 100, 0, 0, true, 30, 0, 0, 40, 0, 0, false);

        public static bool ShowWindow = false;
        private Rect windowRect = new Rect(200, 120, 514, 666);
        public static KillNoticeConfig Config = new KillNoticeConfig();

        private string configFilePath;
        private Dictionary<string, string> _localizedText = new Dictionary<string, string>();

        public static void Initialize(ModBehaviour modInst)
        {
            if (Instance != null) return;
            var go = new GameObject("BattlefieldTypeKillNoticeConfigPanel");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ConfigPanel>();
            _modInst = modInst;
            Instance.OnInitializeInternal();
        }

        private void OnInitializeInternal()
        {
            LoadLocalization();
            var directory = Path.Combine(Application.dataPath, "ModConfig", "BattlefieldTypeKillNotice");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            configFilePath = Path.Combine(directory, "config.json");
            LoadConfig();
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    ToggleUI();
                }
            }

            if (ShowWindow)
            {
                if (!Cursor.visible) Cursor.visible = true;
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            }
        }

        private void ToggleUI()
        {
            ShowWindow = !ShowWindow;
            _modInst.OnToggleConfigPanel(ShowWindow);
        }

        private void OnGUI()
        {
            if (!ShowWindow) return;

            GUI.skin.window.fontSize = 14;
            windowRect = GUI.Window(114514, windowRect, DrawWindow, "Config");
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            Config.Volume = DrawFloat(_localizedText["volume"], Config.Volume, 0f, 1f);
            Config.TotalScale = DrawFloat(_localizedText["totalScale"], Config.TotalScale, 0.1f, 5f);
            Config.ShowIcons = GUILayout.Toggle(Config.ShowIcons, _localizedText["showIcons"]);
            GUILayout.Space(8);
            Config.IconType = DrawSingleChoice(_localizedText["iconType"], Config.IconType, new[] { _localizedText["duck1"], _localizedText["duck2"], _localizedText["bfLike"] });
            Config.IconSize = DrawInt(_localizedText["iconSize"], Config.IconSize, 50, 300);
            Config.HeadshotScale = DrawFloat(_localizedText["headshotScale"], Config.HeadshotScale, 1.0f, 2.0f);
            Config.IconDuration = DrawFloat(_localizedText["iconStayTime"], Config.IconDuration, 0.1f, 5f);
            Config.IconSpacing = DrawInt(_localizedText["iconSpacing"], Config.IconSpacing, 0, 300);
            Config.XOffset = DrawInt(_localizedText["xOffset"], Config.XOffset, -1000, 1000);
            Config.YOffset = DrawInt(_localizedText["yOffset"], Config.YOffset, -1000, 1000);
            Config.ShowText = GUILayout.Toggle(Config.ShowText, _localizedText["showText"]);
            GUILayout.Space(8);
            Config.EnemyInfoTextFontSize = DrawInt(_localizedText["enemyInfoFontSize"], Config.EnemyInfoTextFontSize, 10, 50);
            Config.EnemyInfoTextXOffset = DrawInt(_localizedText["enemyInfoXOffset"], Config.EnemyInfoTextXOffset, -1000, 1000);
            Config.EnemyInfoTextYOffset = DrawInt(_localizedText["enemyInfoYOffset"], Config.EnemyInfoTextYOffset, -1000, 1000);
            Config.ExpInfoTextFontSize = DrawInt(_localizedText["expInfoFontSize"], Config.ExpInfoTextFontSize, 20, 70);
            Config.ExpInfoTextXOffset = DrawInt(_localizedText["expInfoXOffset"], Config.ExpInfoTextXOffset, -1000, 1000);
            Config.ExpInfoTextYOffset = DrawInt(_localizedText["expInfoYOffset"], Config.ExpInfoTextYOffset, -1000, 1000);
            Config.UseLocalResource = GUILayout.Toggle(Config.UseLocalResource, _localizedText["useLocalRes"]);
            GUILayout.Space(12);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) { SaveConfig(); }
            if (GUILayout.Button("Load")) { LoadConfig(); }
            if (GUILayout.Button("Default")) { DefaultConfig(); }
            if (GUILayout.Button("Close")) { ToggleUI(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            var e = Event.current;
            if (e != null)
            {
                Vector2 mousePos = e.mousePosition;
                Rect localRect = new Rect(0, 0, windowRect.width, windowRect.height);
                if (localRect.Contains(mousePos))
                {
                    if (e.isMouse || e.isKey)
                    {
                        e.Use();
                    }
                }
            }
        }

        private int DrawInt(string label, int curValue, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            int newValue = Mathf.RoundToInt(GUILayout.HorizontalSlider(curValue, min, max, GUILayout.Width(200)));
            if (newValue != curValue)
            {
                return newValue;
            }
            string input = GUILayout.TextField(curValue.ToString(), GUILayout.Width(50));
            if (int.TryParse(input, out int inputValue))
            {
                inputValue = Mathf.Clamp(inputValue, min, max);
                newValue = inputValue;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);

            return newValue;
        }

        private float DrawFloat(string label, float curValue, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(150));
            var newValue = GUILayout.HorizontalSlider(curValue, min, max);
            GUILayout.Label(curValue.ToString("F2"), GUILayout.Width(40));
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            return newValue;
        }

        public static int DrawSingleChoice(string label, int value, string[] options)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(label);
            value = GUILayout.SelectionGrid(value, options, options.Length);
            GUILayout.EndVertical();
            return value;
        }

        #region Config persistence
        private void SaveConfig()
        {
            try
            {
                var json = JsonConvert.SerializeObject(Config);
                File.WriteAllText(configFilePath, json);
                _modInst.OnConfigChange();
                Debug.Log($"[BattlefieldTypeKillNoticeMod] Config saved to {configFilePath}, current value is {Config}");
            }
            catch (Exception ex)
            {
                Debug.LogError("[BattlefieldTypeKillNoticeMod] SaveConfig failed: " + ex);
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    var json = File.ReadAllText(configFilePath);
                    Config = JsonConvert.DeserializeObject<KillNoticeConfig>(json);
                    Debug.Log($"[BattlefieldTypeKillNoticeMod] Config loaded from {configFilePath}, current value is {Config}");
                }
                else
                {
                    DefaultConfig();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[BattlefieldTypeKillNoticeMod] LoadConfig failed: " + ex);
            }
        }

        private void DefaultConfig()
        {
            Config = DefaultKillNoticeConfig;
        }
        #endregion
        
        #region localization

        private void LoadLocalization()
        {
            string filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "localize.json");

            if (!File.Exists(filePath))
            {
                Debug.LogError($"[BattlefieldTypeKillNoticeMod] Localization file not found at: {filePath}");
                return;
            }

            string jsonText = File.ReadAllText(filePath);
            var allLanguages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonText);

            SystemLanguage sysLang = Application.systemLanguage;
            string culture = CultureInfo.CurrentCulture.Name.ToLower();

            var currentLanguageKey = "chs";
            if (sysLang == SystemLanguage.Chinese || sysLang == SystemLanguage.ChineseSimplified || culture.StartsWith("zh"))
            {
                currentLanguageKey = "chs";
            }
            else if (sysLang == SystemLanguage.English)
            {
                currentLanguageKey = "en";
            }
            else
            {
                Debug.LogWarning($"[BattlefieldTypeKillNoticeMod] Language key '{currentLanguageKey}' not found, fallback to English.");
                currentLanguageKey = "en";
            }

            _localizedText = allLanguages[currentLanguageKey];
        }
        
        #endregion
    }
}