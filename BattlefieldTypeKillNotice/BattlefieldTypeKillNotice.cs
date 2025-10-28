
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DG.Tweening;
using Duckov;
using Duckov.Options.UI;
using Duckov.UI;
using Duckov.Utilities;
using FMOD;
using FMODUnity;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace BattlefieldTypeKillNotice
{
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        private enum KillType
        {
            Normal,
            Headshot
        }

        private struct KillNoticeNode
        {
            public RectTransform RectTrans;
            public CanvasGroup CanvasGr;
            public Image Icon;
            public Image Circle;
            public Sequence? MoveTweenSeq;
        }

        private static bool _loaded = false;
        private RectTransform? _uiRootRectTrans;
        private RectTransform? _configUIRootRectTrans;
        private List<KillNoticeNode> _configNodes = new List<KillNoticeNode>();
        private GameObject? _nodeTemplate;
        private int _nodeCount = 0;
        private TextMeshProUGUI? _enemyNameText;
        private TextMeshProUGUI? _expText;
        private TextMeshProUGUI? _configEnemyNameText;
        private TextMeshProUGUI? _configExpText;
        private Tween? _expFadeTween;
        private Tween? _expRollTween;
        private long _prevRollTarget = 0;
        private static string _selfPath = "";
        private static ConfigPanel.KillNoticeConfig _cachedConfig;
        private static Dictionary<KillType, List<Sprite>> _dictIcon = new Dictionary<KillType, List<Sprite>>();
        private static Dictionary<KillType, List<Sprite>> _dictEffectCircle = new Dictionary<KillType, List<Sprite>>();
        private static Dictionary<KillType, Sound> _dictAudio = new Dictionary<KillType, Sound>();
        private readonly Stack<KillNoticeNode> _nodePool = new Stack<KillNoticeNode>();
        private readonly List<KillNoticeNode> _showingNodes = new List<KillNoticeNode>();
        private static long _prevExp = 0;
        private static long _curExp = 0;
        private static readonly string[] IconTypeFolderList = { "0-Duck", "1-Duck", "2-BF5" };

        protected override void OnAfterSetup()
        {
            ConfigPanel.Initialize(this);
            _cachedConfig = ConfigPanel.Config;
            _selfPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LoadAllResources();
        }

        private void OnEnable()
        {
            if (!_uiRootRectTrans)
            {
                BuildUI();
            }
            Health.OnDead += OnDead;
            EXPManager.onExpChanged += OnExpChanged;
            LevelManager.OnAfterLevelInitialized += UpdateCacheExp;
        }

        private void OnDisable()
        {
            Health.OnDead -= OnDead;
            EXPManager.onExpChanged -= OnExpChanged;
            LevelManager.OnAfterLevelInitialized -= UpdateCacheExp;
            DOTween.Kill(this);
            DOTween.Kill(_enemyNameText);
            DOTween.Kill(_expText);
            if (_expFadeTween != null && _expFadeTween.IsActive())
            {
                _expFadeTween.Kill();
                _expFadeTween = null;
            }
            if (_expRollTween != null && _expRollTween.IsActive())
            {
                _expRollTween.Kill();
                _expRollTween = null;
            }
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            DOTween.Kill(_enemyNameText);
            DOTween.Kill(_expText);
            if (_expFadeTween != null && _expFadeTween.IsActive())
            {
                _expFadeTween.Kill();
                _expFadeTween = null;
            }
            if (_expRollTween != null && _expRollTween.IsActive())
            {
                _expRollTween.Kill();
                _expRollTween = null;
            }
            _uiRootRectTrans = null;
            _nodeTemplate = null;
            _nodeCount = 0;
            _enemyNameText = null;
            _expText = null;
        }

        public void OnToggleConfigPanel(bool isShow)
        {
            if (isShow && !_uiRootRectTrans)
            {
                BuildUI();
            }
            _configUIRootRectTrans?.gameObject.SetActive(isShow);
        }

        public void OnConfigChange()
        {
            _cachedConfig = ConfigPanel.Config;
            _uiRootRectTrans.anchoredPosition = new Vector2(_cachedConfig.XOffset, -200 + _cachedConfig.YOffset);
            _enemyNameText.fontSize = _cachedConfig.EnemyInfoTextFontSize;
            _enemyNameText.rectTransform.anchoredPosition = new Vector2(_cachedConfig.EnemyInfoTextXOffset, -_cachedConfig.IconSize + 40 + _cachedConfig.EnemyInfoTextYOffset);
            _expText.fontSize = _cachedConfig.ExpInfoTextFontSize;
            _expText.rectTransform.anchoredPosition = new Vector2(_cachedConfig.ExpInfoTextXOffset, -_cachedConfig.IconSize - 10 + _cachedConfig.ExpInfoTextYOffset);
            _uiRootRectTrans.transform.localScale = Vector3.one * _cachedConfig.TotalScale;
        }

        private void Update()
        {
            if (ConfigPanel.ShowWindow)
            {
                if (!_uiRootRectTrans)
                {
                    return;
                }
                // 正在配置中，实时根据配置修改相关显示
                _configUIRootRectTrans.anchoredPosition = new Vector2(ConfigPanel.Config.XOffset, -200 + ConfigPanel.Config.YOffset);
                _configUIRootRectTrans.localScale = Vector3.one * ConfigPanel.Config.TotalScale;
                var nodeNormal = _configNodes[0];
                var nodeHeadshot =  _configNodes[1];
                if (ConfigPanel.Config.ShowIcons)
                {
                    nodeNormal.RectTrans.gameObject.SetActive(true);
                    nodeHeadshot.RectTrans.gameObject.SetActive(true);
                    nodeNormal.Icon.sprite = _dictIcon[KillType.Normal][ConfigPanel.Config.IconType];
                    nodeNormal.Icon.rectTransform.sizeDelta = Vector2.one * ConfigPanel.Config.IconSize;
                    nodeNormal.RectTrans.sizeDelta = Vector2.one * ConfigPanel.Config.IconSize;
                    nodeNormal.RectTrans.anchoredPosition = new Vector2(-ConfigPanel.Config.IconSpacing * 0.5f, 0);
                    nodeHeadshot.Icon.sprite = _dictIcon[KillType.Headshot][ConfigPanel.Config.IconType];
                    nodeHeadshot.RectTrans.sizeDelta = Vector2.one * (ConfigPanel.Config.IconSize * ConfigPanel.Config.HeadshotScale);
                    nodeHeadshot.Icon.rectTransform.sizeDelta = Vector2.one * (ConfigPanel.Config.IconSize * ConfigPanel.Config.HeadshotScale);
                    nodeHeadshot.RectTrans.anchoredPosition = new Vector2(ConfigPanel.Config.IconSpacing * 0.5f, 0);
                }
                else
                {
                    nodeNormal.RectTrans.gameObject.SetActive(false);
                    nodeHeadshot.RectTrans.gameObject.SetActive(false);
                }

                if (ConfigPanel.Config.ShowText)
                {
                    _configEnemyNameText.gameObject.SetActive(true);
                    _configExpText.gameObject.SetActive(true);
                    _configEnemyNameText.fontSize = ConfigPanel.Config.EnemyInfoTextFontSize;
                    _configEnemyNameText.rectTransform.anchoredPosition = new Vector2(ConfigPanel.Config.EnemyInfoTextXOffset, -ConfigPanel.Config.IconSize + 40 + ConfigPanel.Config.EnemyInfoTextYOffset);
                    _configExpText.fontSize = ConfigPanel.Config.ExpInfoTextFontSize;
                    _configExpText.rectTransform.anchoredPosition = new Vector2(ConfigPanel.Config.ExpInfoTextXOffset, -ConfigPanel.Config.IconSize - 10 + ConfigPanel.Config.ExpInfoTextYOffset);
                }
                else
                {
                    _configEnemyNameText.gameObject.SetActive(false);
                    _configExpText.gameObject.SetActive(false);
                }
            }
#if DEBUG_MODE
            if (Input.GetKeyDown(KeyCode.H))
            {
                DebugOnKill(KillType.Normal);
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                DebugOnKill(KillType.Headshot);
            }
#endif
        }

#if DEBUG_MODE
        private void DebugOnKill(KillType killType)
        {
            if (!_uiRootRectTrans)
            {
                BuildUI();
            }
            // 播放音效
            RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out var channelGroup);
            RuntimeManager.CoreSystem.playSound(_dictAudio[killType], channelGroup, false, out var channel);
            channel.setVolume(_cachedConfig.Volume);
            if (_cachedConfig.ShowText)
            {
                // 显示/刷新文本
                UpdateKillText("TestKill");
            }
            if (_cachedConfig.ShowIcons)
            {
                // 更新图标
                AddKillIcon(killType);
            }
        }
#endif

        private static void LoadAllResources()
        {
            if (_loaded)
            {
                Debug.Log("[BattlefieldTypeKillNoticeMod] All resources loaded, skip load.");
                return;
            }
            var resPath = _cachedConfig.UseLocalResource ? Path.Combine(_selfPath, "Replace") : _selfPath;
            Debug.Log("[BattlefieldTypeKillNoticeMod]Start load audio resources.");
            // 加载音效和纹理
            foreach (KillType t in Enum.GetValues(typeof(KillType)))
            {
                var audioName = t.ToString();
                var audioPath = Path.Combine(resPath, "audio", $"{audioName}.wav");
                Debug.Log($"[BattlefieldTypeKillNoticeMod]Try load {audioName}.wav");
                if (!File.Exists(audioPath))
                {
                    Debug.LogError($"[BattlefieldTypeKillNoticeMod]{audioName}.wav not found.");
                    return;
                }

                Sound sound;
                RESULT result = RuntimeManager.CoreSystem.createSound(audioPath, MODE.LOOP_OFF, out sound);
                if (result != RESULT.OK)
                {
                    Debug.LogError($"[BattlefieldTypeKillNoticeMod]Failed to load {audioName}.wav.");
                    return;
                }

                _dictAudio.TryAdd(t, sound);
                Debug.Log($"[BattlefieldTypeKillNoticeMod]Loaded {audioName}.wav.");

                for (int i = 0; i < IconTypeFolderList.Length; i++)
                {
                    var typeName = IconTypeFolderList[i];
                    var iconName = t.ToString();
                    var effectCircleName = t + "_circle";
                    var iconPath = Path.Combine(resPath, "sprite", typeName, $"{iconName}.png");
                    Debug.Log($"[BattlefieldTypeKillNoticeMod]Try load {iconPath}");
                    if (!File.Exists(iconPath))
                    {
                        Debug.LogError($"[BattlefieldTypeKillNoticeMod]{iconPath} not found.");
                        return;
                    }

                    byte[] bytes = File.ReadAllBytes(iconPath);
                    Texture2D texture = new Texture2D(256, 256);
                    if (ImageConversion.LoadImage(texture, bytes))
                    {
                        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            new Vector2(256f, 256f));
                        if (!_dictIcon.ContainsKey(t))
                        {
                            _dictIcon[t] = new List<Sprite>();
                        }
                        _dictIcon[t].Add(sprite);
                        Debug.Log($"[BattlefieldTypeKillNoticeMod]Loaded {iconPath}");
                    }
                    else
                    {
                        Debug.LogError($"[BattlefieldTypeKillNoticeMod]Failed to load {iconPath}.");
                        return;
                    }

                    var circlePath = Path.Combine(resPath, "sprite", typeName, $"{effectCircleName}.png");
                    Debug.Log($"[BattlefieldTypeKillNoticeMod]Try load {circlePath}");
                    if (!File.Exists(circlePath))
                    {
                        Debug.LogError($"[BattlefieldTypeKillNoticeMod]{circlePath} not found.");
                        return;
                    }

                    byte[] circleBytes = File.ReadAllBytes(circlePath);
                    Texture2D circleTexture = new Texture2D(256, 256);
                    if (ImageConversion.LoadImage(circleTexture, circleBytes))
                    {
                        var sprite = Sprite.Create(circleTexture,
                            new Rect(0, 0, circleTexture.width, circleTexture.height), new Vector2(256f, 256f));
                        if (!_dictEffectCircle.ContainsKey(t))
                        {
                            _dictEffectCircle[t] = new List<Sprite>();
                        }
                        _dictEffectCircle[t].Add(sprite);
                        Debug.Log($"[BattlefieldTypeKillNoticeMod]Loaded {circlePath}");
                    }
                    else
                    {
                        Debug.LogError($"[BattlefieldTypeKillNoticeMod]Failed to load {circlePath}.");
                        return;
                    }
                }
            }

            Debug.Log("[BattlefieldTypeKillNoticeMod]Load All Resources Complete.");
            _loaded = true;
        }
        
        private void BuildUI()
        {
            HUDManager hudManager = FindObjectOfType<HUDManager>();
            if (!hudManager)
            {
                Debug.LogError("[BattlefieldTypeKillNoticeMod] Could not find HUDManager, build UI failed.");
                return;
            }

            Debug.Log("[BattlefieldTypeKillNoticeMod] Start build UI.");
            _nodePool.Clear();
            _showingNodes.Clear();
            _configNodes.Clear();
            _uiRootRectTrans = new GameObject("BattlefieldTypeKillNoticeUI").AddComponent<RectTransform>();
            _uiRootRectTrans.transform.SetParent(hudManager.transform);
            _uiRootRectTrans.anchoredPosition = new Vector2(_cachedConfig.XOffset, -200 + _cachedConfig.YOffset);
            _enemyNameText = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI, _uiRootRectTrans.transform);
            _enemyNameText.gameObject.name = "EnemyNameText";
            _enemyNameText.alignment = TextAlignmentOptions.Center;
            _enemyNameText.fontSize = _cachedConfig.EnemyInfoTextFontSize;
            _enemyNameText.rectTransform.anchoredPosition = new Vector2(_cachedConfig.EnemyInfoTextXOffset, -_cachedConfig.IconSize + 40 + _cachedConfig.EnemyInfoTextYOffset);
            _enemyNameText.rectTransform.sizeDelta = new Vector2(800, 200);
            _expText = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI, _uiRootRectTrans.transform);
            _expText.gameObject.name = "PointClaimText";
            _expText.alignment = TextAlignmentOptions.Center;
            _expText.fontSize = _cachedConfig.ExpInfoTextFontSize;
            _expText.rectTransform.anchoredPosition = new Vector2(_cachedConfig.ExpInfoTextXOffset, -_cachedConfig.IconSize - 10 + _cachedConfig.ExpInfoTextYOffset);
            _expText.rectTransform.sizeDelta = new Vector2(800, 200);
            _nodeTemplate = new GameObject("Node");
            _nodeTemplate.transform.SetParent(_uiRootRectTrans.transform);
            _nodeTemplate.AddComponent<RectTransform>().sizeDelta = Vector2.one * _cachedConfig.IconSize;
            _nodeTemplate.AddComponent<CanvasGroup>();
            var icon = new GameObject("Icon").AddComponent<Image>().rectTransform;
            icon.SetParent(_nodeTemplate.transform);
            icon.sizeDelta = Vector2.one * _cachedConfig.IconSize;
            var circle = new GameObject("Circle").AddComponent<Image>().rectTransform;
            circle.SetParent(_nodeTemplate.transform);
            circle.sizeDelta = Vector2.one * _cachedConfig.IconSize;
            _nodeTemplate.gameObject.SetActive(false);
            // 预先实例化 5 个元素
            for (int i = 0; i < 5; i++)
            {
                var trans = Instantiate(_nodeTemplate, _uiRootRectTrans.transform).transform;
                _nodeCount++;
                trans.gameObject.name = $"Node_{_nodeCount}";
                var newNode = new KillNoticeNode();
                newNode.RectTrans = trans.GetComponent<RectTransform>();
                newNode.CanvasGr = trans.GetComponent<CanvasGroup>();
                newNode.Icon = trans.Find("Icon").GetComponent<Image>();
                newNode.Circle = trans.Find("Circle").GetComponent<Image>();
                _nodePool.Push(newNode);
            }
            _uiRootRectTrans.transform.localScale = Vector3.one * _cachedConfig.TotalScale;
            // 游戏内配置时显示的样例 UI
            _configUIRootRectTrans = Instantiate(_uiRootRectTrans.gameObject, hudManager.transform).GetComponent<RectTransform>();
            _configUIRootRectTrans.anchoredPosition = _uiRootRectTrans.anchoredPosition;
            for (int i = 0; i < 2; i++)
            {
                var trans = _configUIRootRectTrans.GetChild(i + 3);
                var newNode = new KillNoticeNode();
                newNode.RectTrans = trans.GetComponent<RectTransform>();
                newNode.CanvasGr = trans.GetComponent<CanvasGroup>();
                newNode.Icon = trans.Find("Icon").GetComponent<Image>();
                newNode.Circle = trans.Find("Circle").GetComponent<Image>();
                newNode.Circle.gameObject.SetActive(false);
                trans.gameObject.SetActive(true);
                _configNodes.Add(newNode);
            }
            _configEnemyNameText = _configUIRootRectTrans.Find("EnemyNameText").GetComponent<TextMeshProUGUI>();
            _configEnemyNameText.text = "[KunKun] +250";
            _configExpText = _configUIRootRectTrans.Find("PointClaimText").GetComponent<TextMeshProUGUI>();
            _configExpText.text = "250";
            _configUIRootRectTrans.gameObject.SetActive(false);
        }

        private void OnDead(Health health, DamageInfo damageInfo)
        {
            if (!_uiRootRectTrans)
            {
                BuildUI();
            }

            if (!_loaded || _uiRootRectTrans == null || !health)
            {
                return;
            }

            if (damageInfo.fromCharacter.IsMainCharacter)
            {
                OnKill(health, damageInfo);
            }
        }

        private void OnKill(Health health, DamageInfo damageInfo)
        {
            KillType killType = GetKillTypeByDamageInfo(damageInfo);

            // 播放音效
            RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out var channelGroup);
            RuntimeManager.CoreSystem.playSound(_dictAudio[killType], channelGroup, false, out var channel);
            channel.setVolume(_cachedConfig.Volume);
            if (_cachedConfig.ShowText)
            {
                // 显示/刷新文本
                UpdateKillText(health.TryGetCharacter()?.characterPreset?.DisplayName);
            }
            if (_cachedConfig.ShowIcons)
            {
                // 更新图标
                AddKillIcon(killType);
            }
        }

        private void UpdateKillText(string? enemyName)
        {
            var addedExp = _curExp - _prevExp;
            if (!string.IsNullOrEmpty(enemyName))
            {
                DOTween.Kill(_enemyNameText);
                _enemyNameText.text = $"[{enemyName}] +{addedExp}";
                _enemyNameText.alpha = 0;
                DOTween.Sequence()
                    .Append(_enemyNameText.DOFade(1f, 0.1f))
                    .AppendInterval(3)
                    .Append(_enemyNameText.DOFade(0f, 0.1f))
                    .SetId(_enemyNameText);
            }

            if (_expFadeTween != null && _expFadeTween.IsPlaying())
            {
                // Fade 中不做处理
            }
            else
            {
                _expText.alpha = 0;
                _expFadeTween = _expText.DOFade(1f, 0.1f);
            }

            long val = 0;
            long target = _prevRollTarget + addedExp;
            _prevRollTarget = target;
            if (_expRollTween != null && _expRollTween.IsActive())
            {
                _expRollTween.Kill();
                val = long.Parse(_expText.text);
            }

            _expRollTween = DOTween.Sequence()
                .Append(DOTween.To(() => val, x =>
                {
                    val = x;
                    _expText.text = val.ToString();
                }, target, 1f))
                .AppendInterval(2.1f)
                .Append(_expText.DOFade(0f, 0.1f)).OnComplete(() => { _prevRollTarget = 0; });
        }
        
        private void AddKillIcon(KillType killType)
        {
            // 当前所有显示中的元素向右移动
            var halfTotalWidth = _cachedConfig.IconSpacing * (_showingNodes.Count + 1) * 0.5f;
            for (var i = 0; i < _showingNodes.Count; i++)
            {
                var index = _showingNodes.Count - i;
                var curNode = _showingNodes[i];
                Vector2 targetPos = new Vector2(_cachedConfig.IconSpacing * index - halfTotalWidth + _cachedConfig.IconSpacing * 0.5f, 0);
                if (curNode.MoveTweenSeq != null && curNode.MoveTweenSeq.IsPlaying())
                {
                    curNode.MoveTweenSeq.Append(curNode.RectTrans.DOAnchorPos(targetPos, 0.1f));
                }
                else
                {
                    curNode.MoveTweenSeq = DOTween.Sequence().Append(curNode.RectTrans.DOAnchorPos(targetPos, 0.1f));
                }
            }

            var node = SpawnNode();
            node.RectTrans.gameObject.SetActive(true);
            node.Icon.sprite = _dictIcon[killType][_cachedConfig.IconType];
            node.Icon.rectTransform.sizeDelta = Vector2.one * (_cachedConfig.IconSize * (killType == KillType.Headshot ? _cachedConfig.HeadshotScale : 1));
            node.Circle.sprite = _dictEffectCircle[killType][_cachedConfig.IconType];
            node.RectTrans.anchoredPosition = _showingNodes.Count == 0
                ? Vector2.zero
                : new Vector2(_cachedConfig.IconSpacing * 0.5f - halfTotalWidth, 0);
            node.CanvasGr.alpha = 0;
            node.CanvasGr.DOFade(1, 0.1f);
            switch (killType)
            {
                case KillType.Normal:
                    node.Circle.gameObject.SetActive(false);
                    node.RectTrans.localScale = Vector3.one * 2f;
                    node.RectTrans.DOScale(Vector3.one, 0.15f).SetEase(Ease.InQuad);
                    break;
                case KillType.Headshot:
                    node.Circle.gameObject.SetActive(true);
                    node.Circle.color = new Color(1, 1, 1, 0);
                    node.Circle.transform.localScale = Vector3.one;
                    node.RectTrans.localScale = Vector3.one * 3f;
                    node.RectTrans.DOScale(Vector3.one, 0.2f).SetEase(Ease.InQuad).OnComplete(() =>
                    {
                        node.Circle.transform.DOScale(2, 0.3f).SetEase(Ease.OutQuad);
                        DOTween.Sequence().Append(node.Circle.DOColor(Color.white, 0.1f))
                            .Append(node.Circle.DOColor(new Color(1, 1, 1, 0), 0.2f));
                    });
                    break;
            }

            _showingNodes.Add(node);
            DOVirtual.DelayedCall(_cachedConfig.IconDuration, DequeueNode).SetId(this);
        }
        
        private void UpdateCacheExp()
        {
            _prevExp = EXPManager.CachedExp;
            _curExp = EXPManager.CachedExp;
        }
        
        private void OnExpChanged(long newExp)
        {
            _prevExp = _curExp;
            _curExp = newExp;
        }
        
        private void DequeueNode()
        {
            if (_showingNodes.Count <= 0)
            {
                return;
            }
            var node = _showingNodes[0];
            _showingNodes.RemoveAt(0);
            node.CanvasGr.DOFade(0, 0.1f).OnComplete(() =>
            {
                DespawnNode(node);
            });
            if (_showingNodes.Count > 0)
            {
                // 剩下所有显示中的 Node 平移到新位置
                var halfTotalWidth = _cachedConfig.IconSpacing * _showingNodes.Count * 0.5f;
                for (var i = 0; i < _showingNodes.Count; i++)
                {
                    var index = _showingNodes.Count - i - 1;
                    var curNode = _showingNodes[i];
                    Vector2 targetPos = new Vector2(_cachedConfig.IconSpacing * index - halfTotalWidth + _cachedConfig.IconSpacing * 0.5f, 0);
                    if (curNode.MoveTweenSeq != null && curNode.MoveTweenSeq.IsPlaying())
                    {
                        curNode.MoveTweenSeq.Append(curNode.RectTrans.DOAnchorPos(targetPos, 0.1f));
                    }
                    else
                    {
                        curNode.MoveTweenSeq = DOTween.Sequence().Append(curNode.RectTrans.DOAnchorPos(targetPos, 0.1f));
                    }
                }
            }
        }

        private KillNoticeNode SpawnNode()
        {
            if (_nodePool.Count <= 0)
            {
                var trans = Instantiate(_nodeTemplate, _uiRootRectTrans.transform).transform;
                _nodeCount++;
                trans.gameObject.name = $"Node_{_nodeCount}";
                var newNode = new KillNoticeNode();
                newNode.RectTrans = trans.GetComponent<RectTransform>();
                newNode.CanvasGr = trans.GetComponent<CanvasGroup>();
                newNode.Icon = trans.Find("Icon").GetComponent<Image>();
                newNode.Circle = trans.Find("Circle").GetComponent<Image>();
                return newNode;
            }

            return _nodePool.Pop();
        }

        private void DespawnNode(KillNoticeNode node)
        {
            if (node.MoveTweenSeq != null && node.MoveTweenSeq.IsActive())
            {
                node.MoveTweenSeq.Kill();
            }
            node.MoveTweenSeq = null;
            node.RectTrans.gameObject.SetActive(false);
            _nodePool.Push(node);
        }

        private static KillType GetKillTypeByDamageInfo(DamageInfo damageInfo)
        {
            if (damageInfo.critRate > 0.5f)
            {
                return KillType.Headshot;
            }
            else
            {
                return KillType.Normal;
            }
        }
    }
}