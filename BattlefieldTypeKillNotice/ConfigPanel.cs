using System;
using System.Collections.Generic;
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
        // 单例
        public static ConfigPanel Instance { get; private set; }
        private static ModBehaviour _modInst;
        private static readonly KillNoticeConfig DefaultKillNoticeConfig = new KillNoticeConfig(0.4f, 1f, true, 2, 100, 1.2f, 3f, 100, 0, 0, true, 30, 0, 0, 40, 0, 0, false);

        // 界面状态
        public static bool ShowWindow = false;
        private Rect windowRect = new Rect(200, 120, 514, 666);
        public static KillNoticeConfig Config = new KillNoticeConfig();

        // 保存路径
        private string configFilePath;

        // 初始化入口：建议由你的 mod loader 在合适时机调用
        public static void Initialize(ModBehaviour modInst)
        {
            if (Instance != null) return;
            // 尝试创建 GameObject（大多数注入会在主线程上）
            var go = new GameObject("BattlefieldTypeKillNoticeConfigPanel");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<ConfigPanel>();
            _modInst = modInst;
            Instance.OnInitializeInternal();
        }

        private void OnInitializeInternal()
        {
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
            // 切换 UI（热键可来自 _config，也可以写死）
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    ToggleUI();
                }
            }

            // 如果 UI 展示时，让鼠标可见并尝试解锁；否则恢复
            if (ShowWindow)
            {
                if (!Cursor.visible) Cursor.visible = true;
                if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // 恢复为默认（谨慎：很多游戏会在每帧根据自己逻辑设置 Cursor）
                // 这里我们只在不显示 UI 时尝试把鼠标隐藏（你也可以注释掉下面两行）
                // Cursor.visible = false;
                // Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void ToggleUI()
        {
            ShowWindow = !ShowWindow;
            _modInst.OnToggleConfigPanel(ShowWindow);
        }

        // IMGUI 绘制
        private void OnGUI()
        {
            if (!ShowWindow) return;

            // 窗口样式：你可以设置 GUISkin 以更像 Editor
            GUI.skin.window.fontSize = 14;

            // 窗口
            windowRect = GUI.Window(114514, windowRect, DrawWindow, "Config");
            // 若想把窗口限制在屏幕内，可在这里做简单修正
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            Config.Volume = DrawFloat("音量", Config.Volume, 0f, 1f);
            Config.TotalScale = DrawFloat("总体缩放", Config.TotalScale, 0.1f, 5f);
            Config.ShowIcons = GUILayout.Toggle(Config.ShowIcons, "是否显示图标");
            GUILayout.Space(8);
            Config.IconType = DrawSingleChoice("图标类型", Config.IconType, new[] { "鸭鸭1", "鸭鸭2", "战地风" });
            Config.IconSize = DrawInt("图标大小", Config.IconSize, 50, 300);
            Config.HeadshotScale = DrawFloat("爆头缩放", Config.HeadshotScale, 1.0f, 2.0f);
            Config.IconDuration = DrawFloat("图标滞留时长", Config.IconDuration, 0.1f, 5f);
            Config.IconSpacing = DrawInt("图标之间间隔", Config.IconSpacing, 0, 300);
            Config.XOffset = DrawInt("横向偏移", Config.XOffset, -1000, 1000);
            Config.YOffset = DrawInt("纵向偏移", Config.YOffset, -1000, 1000);
            Config.ShowText = GUILayout.Toggle(Config.ShowText, "是否显示文本");
            GUILayout.Space(8);
            Config.EnemyInfoTextFontSize = DrawInt("敌人文本字号", Config.EnemyInfoTextFontSize, 10, 50);
            Config.EnemyInfoTextXOffset = DrawInt("敌人文本横向偏移", Config.EnemyInfoTextXOffset, -1000, 1000);
            Config.EnemyInfoTextYOffset = DrawInt("敌人文本纵向偏移", Config.EnemyInfoTextYOffset, -1000, 1000);
            Config.ExpInfoTextFontSize = DrawInt("经验文本字号", Config.ExpInfoTextFontSize, 20, 70);
            Config.ExpInfoTextXOffset = DrawInt("经验文本横向偏移", Config.ExpInfoTextXOffset, -1000, 1000);
            Config.ExpInfoTextYOffset = DrawInt("经验文本纵向偏移", Config.ExpInfoTextYOffset, -1000, 1000);
            Config.UseLocalResource = GUILayout.Toggle(Config.UseLocalResource, "是否使用本地替换资源(需要重启游戏)");
            GUILayout.Space(12);
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save")) { SaveConfig(); }
            if (GUILayout.Button("Load")) { LoadConfig(); }
            if (GUILayout.Button("Default")) { DefaultConfig(); }
            if (GUILayout.Button("Close")) { ToggleUI(); }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            // 窗口拖动
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            // --- 事件拦截（很关键）：在窗口区域内把事件标记为已用，避免透传到游戏
            var e = Event.current;
            if (e != null)
            {
                Vector2 mousePos = e.mousePosition;
                // 注意：Event.current.mousePosition 在 Window 的 Draw 中是相对窗口左上角的，
                // 所以我们判断使用局部坐标：如果在当前窗口内部就 Use()
                Rect localRect = new Rect(0, 0, windowRect.width, windowRect.height);
                if (localRect.Contains(mousePos))
                {
                    // 吞掉鼠标点击和拖动事件
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
            // 滑动条（float -> int）
            int newValue = Mathf.RoundToInt(GUILayout.HorizontalSlider(curValue, min, max, GUILayout.Width(200)));
            if (newValue != curValue)
            {
                return newValue;
            }
            // 数字输入框
            string input = GUILayout.TextField(curValue.ToString(), GUILayout.Width(50));
            // 尝试解析输入值
            if (int.TryParse(input, out int inputValue))
            {
                // 限制在范围内
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
                    // first run: save defaults
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
    }
}