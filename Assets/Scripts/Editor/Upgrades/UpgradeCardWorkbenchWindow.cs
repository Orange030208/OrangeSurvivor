#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class UpgradeCardWorkbenchWindow : EditorWindow
{
    private const string DEFAULT_CARD_FOLDER = "Assets/Resources/Data/UpgradeCards/Cards";
    private const string DEFAULT_POOL_PATH = "Assets/Resources/Data/UpgradeCards/Pool/Default Upgrade Card Pool.asset";
    private const float LEFT_WIDTH = 270f;
    private const float RIGHT_WIDTH = 330f;

    private readonly List<UpgradeCardSO> cards = new();
    private readonly List<CardValidationMessage> validationMessages = new();
    private readonly List<WeaponDataSO> simulatedOwnedWeapons = new();

    private UpgradeCardSO selectedCard;
    private UpgradeCardPoolSO selectedPool;
    private SerializedObject cardObject;
    private SerializedObject poolObject;
    private ReorderableList tagsList;
    private ReorderableList propertyModifiersList;
    private ReorderableList specialFeaturesList;
    private ReorderableList requiredTagsList;
    private ReorderableList requiredWeaponsList;
    private ReorderableList mutuallyExclusiveList;
    private ReorderableList poolCardsList;
    private ReorderableList simulatedWeaponsList;

    private Vector2 cardListScroll;
    private Vector2 editorScroll;
    private Vector2 sideScroll;
    private string searchText = string.Empty;
    private UpgradeCardRarity? rarityFilter;
    private bool showPoolTools = true;
    private bool showRollSimulation = true;
    private bool liveValidate = true;
    private int simulationWave = 1;
    private string newCardId = "new_upgrade_card";
    private string newCardTitle = "新升级卡";
    private UpgradeCardRarity newCardRarity = UpgradeCardRarity.Common;
    private int newCardBaseWeight = 100;

    [MenuItem("Survivors/Upgrades/Card Workbench")]
    public static void Open()
    {
        UpgradeCardWorkbenchWindow window = GetWindow<UpgradeCardWorkbenchWindow>("Card Workbench");
        window.minSize = new Vector2(1050f, 620f);
        window.Show();
    }

    private void OnEnable()
    {
        selectedPool = AssetDatabase.LoadAssetAtPath<UpgradeCardPoolSO>(DEFAULT_POOL_PATH);
        RefreshCards();
        if (selectedCard == null && cards.Count > 0)
        {
            SelectCard(cards[0]);
        }

        BuildSimulatedWeaponsList();
    }

    private void OnGUI()
    {
        EnsureSelectionObjects();
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawLeftPane(GUILayout.Width(LEFT_WIDTH));
        DrawEditorPane(GUILayout.ExpandWidth(true));
        DrawRightPane(GUILayout.Width(RIGHT_WIDTH));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(52f)))
        {
            RefreshCards();
        }

        if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(52f)))
        {
            SaveSelectedCard();
        }

        if (GUILayout.Button("校验全部", EditorStyles.toolbarButton, GUILayout.Width(72f)))
        {
            ValidateAllCards();
        }

        GUILayout.Space(8f);
        liveValidate = GUILayout.Toggle(liveValidate, "实时校验", EditorStyles.toolbarButton, GUILayout.Width(72f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField($"卡牌: {cards.Count}", EditorStyles.miniLabel, GUILayout.Width(72f));
        EditorGUILayout.EndHorizontal();
    }

    private void DrawLeftPane(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
        EditorGUILayout.LabelField("卡牌库", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        searchText = EditorGUILayout.TextField(searchText, EditorStyles.toolbarSearchField);
        if (GUILayout.Button("×", EditorStyles.toolbarButton, GUILayout.Width(24f)))
        {
            searchText = string.Empty;
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(!rarityFilter.HasValue, "全部", EditorStyles.miniButtonLeft))
        {
            rarityFilter = null;
        }

        foreach (UpgradeCardRarity rarity in Enum.GetValues(typeof(UpgradeCardRarity)))
        {
            bool selected = rarityFilter == rarity;
            if (GUILayout.Toggle(selected, rarity.ToString(), EditorStyles.miniButtonMid))
            {
                rarityFilter = rarity;
            }
        }
        EditorGUILayout.EndHorizontal();

        DrawCreatePanel();
        EditorGUILayout.Space(4f);

        cardListScroll = EditorGUILayout.BeginScrollView(cardListScroll);
        List<UpgradeCardSO> visibleCards = GetVisibleCards();
        for (int i = 0; i < visibleCards.Count; i++)
        {
            DrawCardListItem(visibleCards[i]);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void DrawCreatePanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("新增卡牌", EditorStyles.boldLabel);
        newCardId = EditorGUILayout.TextField("Card Id", newCardId);
        newCardTitle = EditorGUILayout.TextField("标题", newCardTitle);
        newCardRarity = (UpgradeCardRarity)EditorGUILayout.EnumPopup("稀有度", newCardRarity);
        newCardBaseWeight = EditorGUILayout.IntField("基础权重", Mathf.Max(1, newCardBaseWeight));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("创建"))
        {
            CreateCardFromPanel();
        }

        GUI.enabled = selectedCard != null;
        if (GUILayout.Button("复制当前"))
        {
            DuplicateSelectedCard();
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawCardListItem(UpgradeCardSO card)
    {
        if (card == null)
        {
            return;
        }

        GUIStyle style = card == selectedCard ? EditorStyles.helpBox : GUI.skin.box;
        EditorGUILayout.BeginVertical(style);
        EditorGUILayout.BeginHorizontal();
        Rect colorRect = GUILayoutUtility.GetRect(8f, 34f, GUILayout.Width(8f));
        EditorGUI.DrawRect(colorRect, GetRarityColor(card.Rarity));

        if (GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Width(0f), GUILayout.Height(0f)))
        {
            SelectCard(card);
        }

        EditorGUILayout.BeginVertical();
        if (GUILayout.Button(string.IsNullOrWhiteSpace(card.Title) ? "(未命名)" : card.Title, EditorStyles.boldLabel))
        {
            SelectCard(card);
        }

        EditorGUILayout.LabelField($"{card.CardId}  ·  {card.Rarity}  ·  W {card.BaseWeight}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawEditorPane(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(options);
        if (selectedCard == null)
        {
            EditorGUILayout.HelpBox("请选择或创建一张升级卡。", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        cardObject.Update();
        editorScroll = EditorGUILayout.BeginScrollView(editorScroll);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("卡牌配置", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("定位资产", GUILayout.Width(84f)))
        {
            Selection.activeObject = selectedCard;
            EditorGUIUtility.PingObject(selectedCard);
        }
        EditorGUILayout.EndHorizontal();

        DrawSection("基础", () =>
        {
            EditorGUILayout.PropertyField(cardObject.FindProperty("cardId"), new GUIContent("Card Id"));
            EditorGUILayout.PropertyField(cardObject.FindProperty("title"), new GUIContent("标题"));
            EditorGUILayout.PropertyField(cardObject.FindProperty("icon"), new GUIContent("图标"));
            EditorGUILayout.PropertyField(cardObject.FindProperty("rarity"), new GUIContent("稀有度"));
            tagsList?.DoLayoutList();
        });

        DrawSection("抽取", () =>
        {
            EditorGUILayout.PropertyField(cardObject.FindProperty("maxPickCount"), new GUIContent("最大选择次数"));
            EditorGUILayout.PropertyField(cardObject.FindProperty("baseWeight"), new GUIContent("基础权重"));
            SerializedProperty conditions = cardObject.FindProperty("offerConditions");
            if (conditions != null)
            {
                EditorGUILayout.PropertyField(conditions.FindPropertyRelative("minWave"), new GUIContent("最小波次"));
                requiredTagsList?.DoLayoutList();
                requiredWeaponsList?.DoLayoutList();
                mutuallyExclusiveList?.DoLayoutList();
            }
        });

        DrawSection("描述", () =>
        {
            EditorGUILayout.PropertyField(cardObject.FindProperty("description"), new GUIContent("自定义描述"), GUILayout.MinHeight(62f));
            if (GUILayout.Button("用自动描述填充"))
            {
                FillDescriptionFromAutoText();
            }
        });

        DrawSection("属性修饰", () =>
        {
            propertyModifiersList?.DoLayoutList();
        });

        DrawSection("特殊能力", () =>
        {
            specialFeaturesList?.DoLayoutList();
        });

        EditorGUILayout.EndScrollView();

        if (cardObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(selectedCard);
            if (liveValidate)
            {
                ValidateSelectedCard();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRightPane(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, options);
        sideScroll = EditorGUILayout.BeginScrollView(sideScroll);

        DrawPreview();
        EditorGUILayout.Space(6f);
        DrawValidationPanel();
        EditorGUILayout.Space(6f);
        DrawPoolPanel();
        EditorGUILayout.Space(6f);
        DrawSimulationPanel();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreview()
    {
        EditorGUILayout.LabelField("实时预览", EditorStyles.boldLabel);
        Rect cardRect = GUILayoutUtility.GetRect(RIGHT_WIDTH - 28f, 210f);
        DrawCardPreview(cardRect);
    }

    private void DrawValidationPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("校验", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("校验当前", GUILayout.Width(82f)))
        {
            ValidateSelectedCard();
        }
        EditorGUILayout.EndHorizontal();

        if (validationMessages.Count == 0)
        {
            EditorGUILayout.HelpBox("未发现问题。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < validationMessages.Count; i++)
            {
                CardValidationMessage message = validationMessages[i];
                EditorGUILayout.HelpBox(message.Text, message.Type);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPoolPanel()
    {
        showPoolTools = EditorGUILayout.Foldout(showPoolTools, "卡池工具", true);
        if (!showPoolTools)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.BeginChangeCheck();
        selectedPool = (UpgradeCardPoolSO)EditorGUILayout.ObjectField("目标卡池", selectedPool, typeof(UpgradeCardPoolSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            poolObject = selectedPool != null ? new SerializedObject(selectedPool) : null;
            BuildPoolList();
        }

        GUI.enabled = selectedPool != null && selectedCard != null;
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(IsSelectedCardInPool() ? "已在卡池" : "加入卡池"))
        {
            AddSelectedCardToPool();
        }

        if (GUILayout.Button("从卡池移除"))
        {
            RemoveSelectedCardFromPool();
        }
        EditorGUILayout.EndHorizontal();
        GUI.enabled = true;

        if (poolObject != null)
        {
            poolObject.Update();
            poolCardsList?.DoLayoutList();
            if (poolObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(selectedPool);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSimulationPanel()
    {
        showRollSimulation = EditorGUILayout.Foldout(showRollSimulation, "抽取条件模拟", true);
        if (!showRollSimulation)
        {
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        simulationWave = EditorGUILayout.IntField("模拟波次", Mathf.Max(1, simulationWave));
        simulatedWeaponsList?.DoLayoutList();

        GUI.enabled = selectedCard != null;
        if (GUILayout.Button("判定当前卡牌是否可出现"))
        {
            ValidateSelectedCard();
        }
        GUI.enabled = true;

        if (selectedCard != null)
        {
            bool canOffer = CanOfferSelectedCardInSimulation(out string reason);
            EditorGUILayout.HelpBox(canOffer ? "当前模拟条件下可以出现。" : reason, canOffer ? MessageType.Info : MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSection(string title, Action drawContent)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        drawContent?.Invoke();
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4f);
    }

    private void DrawCardPreview(Rect rect)
    {
        if (selectedCard == null)
        {
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
            EditorGUI.LabelField(rect, "未选择卡牌", CenteredWhiteLabel());
            return;
        }

        UpgradeCardOptionSnapshot snapshot = selectedCard.ToSnapshot(null);
        Color rarityColor = GetRarityColor(snapshot.Rarity);
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.13f, 0.15f));
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 5f), rarityColor);

        Rect iconRect = new(rect.x + 16f, rect.y + 18f, 56f, 56f);
        EditorGUI.DrawRect(iconRect, new Color(0.21f, 0.22f, 0.25f));
        if (snapshot.Icon != null)
        {
            GUI.DrawTexture(iconRect, AssetPreview.GetAssetPreview(snapshot.Icon) ?? snapshot.Icon.texture, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.LabelField(iconRect, "Icon", CenteredMiniLabel());
        }

        Rect titleRect = new(rect.x + 84f, rect.y + 18f, rect.width - 100f, 24f);
        GUI.Label(titleRect, string.IsNullOrWhiteSpace(snapshot.Title) ? "(未命名)" : snapshot.Title, PreviewTitleStyle());
        EditorGUI.LabelField(new Rect(titleRect.x, titleRect.yMax + 2f, titleRect.width, 18f), $"{snapshot.Rarity} · {snapshot.CardId}", EditorStyles.miniLabel);
        EditorGUI.LabelField(new Rect(titleRect.x, titleRect.yMax + 20f, titleRect.width, 18f), $"可选 {snapshot.PickCount}/{snapshot.MaxPickCount} · 权重 {selectedCard.BaseWeight}", EditorStyles.miniLabel);

        Rect tagRect = new(rect.x + 16f, rect.y + 88f, rect.width - 32f, 24f);
        DrawTags(tagRect, snapshot.Tags);

        Rect descRect = new(rect.x + 16f, rect.y + 122f, rect.width - 32f, rect.height - 138f);
        GUI.Label(descRect, snapshot.Description, PreviewDescriptionStyle());
    }

    private void DrawTags(Rect rect, IReadOnlyList<UpgradeCardTag> tags)
    {
        float x = rect.x;
        if (tags == null || tags.Count == 0)
        {
            EditorGUI.LabelField(rect, "无标签", EditorStyles.miniLabel);
            return;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            string label = tags[i].ToString();
            float width = Mathf.Min(82f, EditorStyles.miniButton.CalcSize(new GUIContent(label)).x + 12f);
            Rect tagRect = new(x, rect.y, width, 20f);
            GUI.Label(tagRect, label, EditorStyles.miniButton);
            x += width + 4f;
            if (x > rect.xMax - 48f)
            {
                break;
            }
        }
    }

    private void RefreshCards()
    {
        cards.Clear();
        string[] guids = AssetDatabase.FindAssets("t:UpgradeCardSO", new[] { DEFAULT_CARD_FOLDER });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            UpgradeCardSO card = AssetDatabase.LoadAssetAtPath<UpgradeCardSO>(path);
            if (card != null)
            {
                cards.Add(card);
            }
        }

        cards.Sort((left, right) =>
        {
            int rarityCompare = left.Rarity.CompareTo(right.Rarity);
            return rarityCompare != 0 ? rarityCompare : string.CompareOrdinal(left.CardId, right.CardId);
        });

        if (selectedCard != null && !cards.Contains(selectedCard))
        {
            selectedCard = null;
            cardObject = null;
        }

        BuildPoolList();
        ValidateSelectedCard();
    }

    private List<UpgradeCardSO> GetVisibleCards()
    {
        string filter = searchText?.Trim();
        List<UpgradeCardSO> result = new();
        for (int i = 0; i < cards.Count; i++)
        {
            UpgradeCardSO card = cards[i];
            if (card == null)
            {
                continue;
            }

            if (rarityFilter.HasValue && card.Rarity != rarityFilter.Value)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter) &&
                !Contains(card.CardId, filter) &&
                !Contains(card.Title, filter))
            {
                continue;
            }

            result.Add(card);
        }

        return result;
    }

    private void SelectCard(UpgradeCardSO card)
    {
        if (selectedCard == card)
        {
            return;
        }

        selectedCard = card;
        cardObject = selectedCard != null ? new SerializedObject(selectedCard) : null;
        BuildCardLists();
        ValidateSelectedCard();
        Repaint();
    }

    private void EnsureSelectionObjects()
    {
        if (selectedPool != null && poolObject == null)
        {
            poolObject = new SerializedObject(selectedPool);
            BuildPoolList();
        }

        if (selectedCard != null && cardObject == null)
        {
            cardObject = new SerializedObject(selectedCard);
            BuildCardLists();
        }
    }

    private void BuildCardLists()
    {
        if (cardObject == null)
        {
            tagsList = null;
            propertyModifiersList = null;
            specialFeaturesList = null;
            requiredTagsList = null;
            requiredWeaponsList = null;
            mutuallyExclusiveList = null;
            return;
        }

        tagsList = CreateSimpleList(cardObject.FindProperty("tags"), "标签");
        propertyModifiersList = CreateSimpleList(cardObject.FindProperty("propertyModifiers"), "属性修饰");
        specialFeaturesList = CreateFeatureList(cardObject.FindProperty("specialFeatures"), "特殊能力");

        SerializedProperty conditions = cardObject.FindProperty("offerConditions");
        requiredTagsList = CreateSimpleList(conditions?.FindPropertyRelative("requiredTagPickCounts"), "要求标签选择数");
        requiredWeaponsList = CreateSimpleList(conditions?.FindPropertyRelative("requiredOwnedWeapons"), "要求已拥有武器");
        mutuallyExclusiveList = CreateSimpleList(conditions?.FindPropertyRelative("mutuallyExclusiveCardIds"), "互斥卡牌 Id");
    }

    private void BuildPoolList()
    {
        if (selectedPool == null)
        {
            poolObject = null;
            poolCardsList = null;
            return;
        }

        poolObject ??= new SerializedObject(selectedPool);
        poolCardsList = CreateSimpleList(poolObject.FindProperty("cards"), "卡池卡牌");
    }

    private void BuildSimulatedWeaponsList()
    {
        SerializedObject dummyObject = null;
        simulatedWeaponsList = new ReorderableList(simulatedOwnedWeapons, typeof(WeaponDataSO), true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, "模拟已拥有武器"),
            drawElementCallback = (rect, index, _, _) =>
            {
                simulatedOwnedWeapons[index] = (WeaponDataSO)EditorGUI.ObjectField(
                    new Rect(rect.x, rect.y + 1f, rect.width, EditorGUIUtility.singleLineHeight),
                    simulatedOwnedWeapons[index],
                    typeof(WeaponDataSO),
                    false);
            },
            onAddCallback = _ => simulatedOwnedWeapons.Add(null)
        };
    }

    private ReorderableList CreateSimpleList(SerializedProperty property, string header)
    {
        if (property == null)
        {
            return null;
        }

        ReorderableList list = new(property.serializedObject, property, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, header),
            elementHeightCallback = index => EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(index), true) + 4f,
            drawElementCallback = (rect, index, _, _) =>
            {
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                rect.y += 2f;
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            }
        };

        return list;
    }

    private ReorderableList CreateFeatureList(SerializedProperty property, string header)
    {
        if (property == null)
        {
            return null;
        }

        ReorderableList list = new(property.serializedObject, property, true, true, true, true)
        {
            drawHeaderCallback = rect => EditorGUI.LabelField(rect, header),
            elementHeightCallback = index => GetManagedReferenceElementHeight(property.GetArrayElementAtIndex(index)),
            drawElementCallback = (rect, index, _, _) => DrawManagedReferenceElement(rect, property.GetArrayElementAtIndex(index), index),
            onAddDropdownCallback = (buttonRect, _) => ShowFeatureAddMenu(buttonRect, property)
        };

        return list;
    }

    private void ShowFeatureAddMenu(Rect buttonRect, SerializedProperty property)
    {
        GenericMenu menu = new();
        List<Type> featureTypes = GetConcreteFeatureTypes();
        for (int i = 0; i < featureTypes.Count; i++)
        {
            Type featureType = featureTypes[i];
            string menuName = ObjectNames.NicifyVariableName(featureType.Name);
            menu.AddItem(new GUIContent(menuName), false, () =>
            {
                int index = property.arraySize;
                property.InsertArrayElementAtIndex(index);
                property.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(featureType);
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                ValidateSelectedCard();
            });
        }

        if (featureTypes.Count == 0)
        {
            menu.AddDisabledItem(new GUIContent("No selectable FeatureEffectBase types found"));
        }

        menu.DropDown(buttonRect);
    }

    private void CreateCardFromPanel()
    {
        EnsureFolder(DEFAULT_CARD_FOLDER);
        string cardId = NormalizeCardId(newCardId);
        string path = AssetDatabase.GenerateUniqueAssetPath($"{DEFAULT_CARD_FOLDER}/{cardId}.asset");

        UpgradeCardSO card = CreateInstance<UpgradeCardSO>();
        card.InitializeRuntime(
            cardId,
            string.IsNullOrWhiteSpace(newCardTitle) ? cardId : newCardTitle.Trim(),
            newCardRarity,
            Mathf.Max(1, newCardBaseWeight),
            Array.Empty<UpgradeCardTag>(),
            string.Empty,
            Array.Empty<PropModifierData>(),
            Array.Empty<FeatureEffectBase>());

        AssetDatabase.CreateAsset(card, path);
        AssetDatabase.SaveAssets();
        RefreshCards();
        SelectCard(card);
        AddSelectedCardToPool();
    }

    private void DuplicateSelectedCard()
    {
        if (selectedCard == null)
        {
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(selectedCard);
        string copyPath = AssetDatabase.GenerateUniqueAssetPath($"{DEFAULT_CARD_FOLDER}/{selectedCard.CardId}_copy.asset");
        if (!AssetDatabase.CopyAsset(sourcePath, copyPath))
        {
            return;
        }

        AssetDatabase.ImportAsset(copyPath);
        UpgradeCardSO copy = AssetDatabase.LoadAssetAtPath<UpgradeCardSO>(copyPath);
        if (copy != null)
        {
            SerializedObject copyObject = new(copy);
            copyObject.FindProperty("cardId").stringValue = $"{selectedCard.CardId}_copy";
            copyObject.FindProperty("title").stringValue = $"{selectedCard.Title} Copy";
            copyObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(copy);
        }

        AssetDatabase.SaveAssets();
        RefreshCards();
        SelectCard(copy);
    }

    private void SaveSelectedCard()
    {
        if (cardObject != null)
        {
            cardObject.ApplyModifiedProperties();
        }

        if (selectedCard != null)
        {
            EditorUtility.SetDirty(selectedCard);
        }

        if (poolObject != null)
        {
            poolObject.ApplyModifiedProperties();
        }

        if (selectedPool != null)
        {
            EditorUtility.SetDirty(selectedPool);
        }

        AssetDatabase.SaveAssets();
        ValidateSelectedCard();
    }

    private void FillDescriptionFromAutoText()
    {
        if (selectedCard == null || cardObject == null)
        {
            return;
        }

        List<string> lines = new();
        IReadOnlyList<PropModifierData> modifiers = selectedCard.PropertyModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            lines.Add(modifiers[i].GetAutoDescription());
        }

        IReadOnlyList<FeatureEffectBase> features = selectedCard.SpecialFeatures;
        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] != null && !string.IsNullOrWhiteSpace(features[i].Description))
            {
                lines.Add(features[i].Description);
            }
        }

        cardObject.FindProperty("description").stringValue = lines.Count > 0 ? string.Join("\n", lines) : "获得一项升级。";
        cardObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedCard);
    }

    private void AddSelectedCardToPool()
    {
        if (selectedPool == null || selectedCard == null)
        {
            return;
        }

        poolObject ??= new SerializedObject(selectedPool);
        SerializedProperty cardsProperty = poolObject.FindProperty("cards");
        for (int i = 0; i < cardsProperty.arraySize; i++)
        {
            if (cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue == selectedCard)
            {
                return;
            }
        }

        cardsProperty.InsertArrayElementAtIndex(cardsProperty.arraySize);
        cardsProperty.GetArrayElementAtIndex(cardsProperty.arraySize - 1).objectReferenceValue = selectedCard;
        poolObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedPool);
    }

    private void RemoveSelectedCardFromPool()
    {
        if (selectedPool == null || selectedCard == null)
        {
            return;
        }

        poolObject ??= new SerializedObject(selectedPool);
        SerializedProperty cardsProperty = poolObject.FindProperty("cards");
        for (int i = cardsProperty.arraySize - 1; i >= 0; i--)
        {
            if (cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue == selectedCard)
            {
                cardsProperty.DeleteArrayElementAtIndex(i);
            }
        }

        poolObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(selectedPool);
    }

    private bool IsSelectedCardInPool()
    {
        if (selectedPool == null || selectedCard == null || selectedPool.Cards == null)
        {
            return false;
        }

        return selectedPool.Cards.Contains(selectedCard);
    }

    private void ValidateSelectedCard()
    {
        validationMessages.Clear();
        if (selectedCard == null)
        {
            return;
        }

        ValidateCard(selectedCard, validationMessages);
        CanOfferSelectedCardInSimulation(out string simulationReason);
        if (!string.IsNullOrWhiteSpace(simulationReason) && !simulationReason.StartsWith("当前", StringComparison.Ordinal))
        {
            validationMessages.Add(CardValidationMessage.Info($"模拟抽取: {simulationReason}"));
        }
    }

    private void ValidateAllCards()
    {
        int errorCount = 0;
        int warningCount = 0;
        List<CardValidationMessage> messages = new();
        for (int i = 0; i < cards.Count; i++)
        {
            messages.Clear();
            ValidateCard(cards[i], messages);
            errorCount += messages.Count(message => message.Type == MessageType.Error);
            warningCount += messages.Count(message => message.Type == MessageType.Warning);
        }

        EditorUtility.DisplayDialog("卡牌校验完成", $"共 {cards.Count} 张卡牌。\n错误: {errorCount}\n警告: {warningCount}", "OK");
        ValidateSelectedCard();
    }

    private void ValidateCard(UpgradeCardSO card, List<CardValidationMessage> messages)
    {
        if (card == null)
        {
            messages.Add(CardValidationMessage.Error("卡牌为空。"));
            return;
        }

        if (string.IsNullOrWhiteSpace(card.CardId))
        {
            messages.Add(CardValidationMessage.Error("Card Id 不能为空。"));
        }
        else if (cards.Count(other => other != null && other != card && string.Equals(other.CardId, card.CardId, StringComparison.Ordinal)) > 0)
        {
            messages.Add(CardValidationMessage.Error($"Card Id 重复: {card.CardId}。"));
        }

        if (string.IsNullOrWhiteSpace(card.Title))
        {
            messages.Add(CardValidationMessage.Warning("标题为空。"));
        }

        if (card.BaseWeight <= 0)
        {
            messages.Add(CardValidationMessage.Error("基础权重必须大于 0。"));
        }

        if (card.MaxPickCount <= 0)
        {
            messages.Add(CardValidationMessage.Error("最大选择次数必须大于 0。"));
        }

        if (!card.HasAnyEffect())
        {
            messages.Add(CardValidationMessage.Error("卡牌没有任何属性修饰或特殊能力。"));
        }

        ValidateTags(card, messages);
        ValidatePropertyModifiers(card, messages);
        ValidateFeatures(card, messages);
        ValidateOfferConditions(card, messages);
    }

    private void ValidateTags(UpgradeCardSO card, List<CardValidationMessage> messages)
    {
        IReadOnlyList<UpgradeCardTag> tags = card.Tags;
        if (tags == null || tags.Count == 0)
        {
            messages.Add(CardValidationMessage.Warning("没有配置标签，抽取权重不会获得构筑标签加成。"));
            return;
        }

        HashSet<UpgradeCardTag> seen = new();
        for (int i = 0; i < tags.Count; i++)
        {
            if (!seen.Add(tags[i]))
            {
                messages.Add(CardValidationMessage.Warning($"标签重复: {tags[i]}。"));
            }
        }
    }

    private void ValidatePropertyModifiers(UpgradeCardSO card, List<CardValidationMessage> messages)
    {
        IReadOnlyList<PropModifierData> modifiers = card.PropertyModifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (Mathf.Approximately(modifiers[i].value, 0f))
            {
                messages.Add(CardValidationMessage.Warning($"第 {i + 1} 个属性修饰值为 0。"));
            }
        }
    }

    private void ValidateFeatures(UpgradeCardSO card, List<CardValidationMessage> messages)
    {
        IReadOnlyList<FeatureEffectBase> features = card.SpecialFeatures;
        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] == null)
            {
                messages.Add(CardValidationMessage.Error($"第 {i + 1} 个特殊能力为空。"));
            }
        }
    }

    private void ValidateOfferConditions(UpgradeCardSO card, List<CardValidationMessage> messages)
    {
        UpgradeCardOfferConditions conditions = card.OfferConditions;
        if (conditions == null)
        {
            messages.Add(CardValidationMessage.Error("抽取条件为空。"));
            return;
        }

        if (conditions.MinWave < 1)
        {
            messages.Add(CardValidationMessage.Error("最小波次必须大于等于 1。"));
        }

        IReadOnlyList<UpgradeCardTagPickRequirement> tagRequirements = conditions.RequiredTagPickCounts;
        HashSet<UpgradeCardTag> requiredTags = new();
        for (int i = 0; i < tagRequirements.Count; i++)
        {
            UpgradeCardTagPickRequirement requirement = tagRequirements[i];
            if (requirement.MinPickCount < 1)
            {
                messages.Add(CardValidationMessage.Error($"第 {i + 1} 个标签要求次数必须大于 0。"));
            }

            if (!requiredTags.Add(requirement.Tag))
            {
                messages.Add(CardValidationMessage.Warning($"标签要求重复: {requirement.Tag}。"));
            }
        }

        IReadOnlyList<WeaponDataSO> requiredWeapons = conditions.RequiredOwnedWeapons;
        for (int i = 0; i < requiredWeapons.Count; i++)
        {
            if (requiredWeapons[i] == null)
            {
                messages.Add(CardValidationMessage.Error($"第 {i + 1} 个要求武器为空。"));
            }
        }

        IReadOnlyList<string> exclusions = conditions.MutuallyExclusiveCardIds;
        HashSet<string> seenExclusions = new(StringComparer.Ordinal);
        for (int i = 0; i < exclusions.Count; i++)
        {
            string excludedId = exclusions[i];
            if (string.IsNullOrWhiteSpace(excludedId))
            {
                messages.Add(CardValidationMessage.Error($"第 {i + 1} 个互斥卡牌 Id 为空。"));
                continue;
            }

            if (string.Equals(excludedId, card.CardId, StringComparison.Ordinal))
            {
                messages.Add(CardValidationMessage.Error("卡牌不能与自己互斥。"));
            }

            if (!seenExclusions.Add(excludedId))
            {
                messages.Add(CardValidationMessage.Warning($"互斥卡牌 Id 重复: {excludedId}。"));
            }

            if (!cards.Any(other => other != null && string.Equals(other.CardId, excludedId, StringComparison.Ordinal)))
            {
                messages.Add(CardValidationMessage.Warning($"互斥卡牌不存在: {excludedId}。"));
            }
        }
    }

    private bool CanOfferSelectedCardInSimulation(out string reason)
    {
        reason = string.Empty;
        if (selectedCard == null)
        {
            reason = "未选择卡牌。";
            return false;
        }

        UpgradeCardOfferContext context = new(null, simulationWave, simulatedOwnedWeapons);
        if (selectedCard.OfferConditions != null && !selectedCard.OfferConditions.AreSatisfied(context))
        {
            if (simulationWave < selectedCard.OfferConditions.MinWave)
            {
                reason = $"当前模拟波次不足，需要第 {selectedCard.OfferConditions.MinWave} 波。";
                return false;
            }

            IReadOnlyList<WeaponDataSO> requiredWeapons = selectedCard.OfferConditions.RequiredOwnedWeapons;
            for (int i = 0; i < requiredWeapons.Count; i++)
            {
                if (requiredWeapons[i] != null && !context.HasOwnedWeapon(requiredWeapons[i]))
                {
                    reason = $"当前模拟条件缺少武器: {requiredWeapons[i].ItemName}。";
                    return false;
                }
            }

            if (selectedCard.OfferConditions.RequiredTagPickCounts.Count > 0)
            {
                reason = "当前模拟没有历史选择状态，带标签选择数要求的卡牌会被阻挡。";
                return false;
            }

            reason = "当前模拟条件不满足。";
            return false;
        }

        reason = "当前模拟条件满足。";
        return true;
    }

    private static float GetManagedReferenceElementHeight(SerializedProperty element)
    {
        float height = EditorGUIUtility.singleLineHeight + 6f;
        if (element == null || string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return height;
        }

        foreach (SerializedProperty child in EnumerateVisibleChildren(element))
        {
            if (IsHiddenInInspector(child))
            {
                continue;
            }

            height += EditorGUI.GetPropertyHeight(child, true) + 2f;
        }

        return height;
    }

    private static void DrawManagedReferenceElement(Rect rect, SerializedProperty element, int index)
    {
        rect.y += 2f;
        string label = GetManagedReferenceTypeName(element);
        if (string.IsNullOrEmpty(label))
        {
            label = $"Element {index}";
        }

        Rect headerRect = new(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(headerRect, label, EditorStyles.boldLabel);
        if (element == null || string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return;
        }

        float y = headerRect.yMax + 2f;
        foreach (SerializedProperty child in EnumerateVisibleChildren(element))
        {
            if (IsHiddenInInspector(child))
            {
                continue;
            }

            float height = EditorGUI.GetPropertyHeight(child, true);
            EditorGUI.PropertyField(new Rect(rect.x, y, rect.width, height), child, true);
            y += height + 2f;
        }
    }

    private static IEnumerable<SerializedProperty> EnumerateVisibleChildren(SerializedProperty property)
    {
        SerializedProperty iterator = property.Copy();
        SerializedProperty end = iterator.GetEndProperty();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
        {
            enterChildren = false;
            yield return iterator.Copy();
        }
    }

    private static bool IsHiddenInInspector(SerializedProperty property)
    {
        Type parentType = GetManagedReferenceType(property.serializedObject, property.propertyPath);
        if (parentType == null)
        {
            return false;
        }

        FieldInfo field = parentType.GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return field != null && Attribute.IsDefined(field, typeof(HideInInspector), true);
    }

    private static Type GetManagedReferenceType(SerializedObject serializedObject, string propertyPath)
    {
        int markerIndex = propertyPath.IndexOf(".Array.data[", StringComparison.Ordinal);
        if (markerIndex < 0)
        {
            return null;
        }

        string arrayPath = propertyPath.Substring(0, markerIndex);
        SerializedProperty arrayProperty = serializedObject.FindProperty(arrayPath);
        if (arrayProperty == null)
        {
            return null;
        }

        int bracketStart = propertyPath.IndexOf('[', markerIndex);
        int bracketEnd = propertyPath.IndexOf(']', bracketStart + 1);
        if (bracketStart < 0 || bracketEnd < 0)
        {
            return null;
        }

        if (!int.TryParse(propertyPath.Substring(bracketStart + 1, bracketEnd - bracketStart - 1), out int index))
        {
            return null;
        }

        if (index < 0 || index >= arrayProperty.arraySize)
        {
            return null;
        }

        SerializedProperty element = arrayProperty.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(element.managedReferenceFullTypename))
        {
            return null;
        }

        string[] parts = element.managedReferenceFullTypename.Split(' ');
        return parts.Length == 2 ? Type.GetType($"{parts[1]}, {parts[0]}") : null;
    }

    private static List<Type> GetConcreteFeatureTypes()
    {
        List<Type> types = new();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<FeatureEffectBase>())
        {
            if (type.IsAbstract || type.IsGenericType)
            {
                continue;
            }

            if (Attribute.IsDefined(type, typeof(HideInFeatureMenuAttribute), false))
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) == null)
            {
                continue;
            }

            types.Add(type);
        }

        types.Sort((left, right) => string.CompareOrdinal(left.Name, right.Name));
        return types;
    }

    private static string GetManagedReferenceTypeName(SerializedProperty property)
    {
        if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
        {
            return null;
        }

        string[] parts = property.managedReferenceFullTypename.Split(' ');
        return parts.Length == 2 ? ObjectNames.NicifyVariableName(parts[1].Split('.').Last()) : null;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = System.IO.Path.GetFileName(path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static string NormalizeCardId(string value)
    {
        string raw = string.IsNullOrWhiteSpace(value) ? "new_upgrade_card" : value.Trim().ToLowerInvariant();
        char[] chars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray();
        string result = new(chars);
        while (result.Contains("__", StringComparison.Ordinal))
        {
            result = result.Replace("__", "_");
        }

        return result.Trim('_');
    }

    private static bool Contains(string source, string filter)
    {
        return !string.IsNullOrEmpty(source) && source.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Color GetRarityColor(UpgradeCardRarity rarity)
    {
        return rarity switch
        {
            UpgradeCardRarity.Common => new Color(0.72f, 0.78f, 0.82f),
            UpgradeCardRarity.Rare => new Color(0.25f, 0.55f, 0.95f),
            UpgradeCardRarity.Epic => new Color(0.66f, 0.38f, 0.9f),
            UpgradeCardRarity.Legendary => new Color(1f, 0.66f, 0.18f),
            _ => Color.white
        };
    }

    private static GUIStyle PreviewTitleStyle()
    {
        GUIStyle style = new(EditorStyles.boldLabel)
        {
            fontSize = 16,
            wordWrap = true
        };
        style.normal.textColor = Color.white;
        return style;
    }

    private static GUIStyle PreviewDescriptionStyle()
    {
        GUIStyle style = new(EditorStyles.wordWrappedLabel)
        {
            fontSize = 12,
            padding = new RectOffset(4, 4, 4, 4)
        };
        style.normal.textColor = new Color(0.88f, 0.9f, 0.92f);
        return style;
    }

    private static GUIStyle CenteredWhiteLabel()
    {
        GUIStyle style = new(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = Color.white;
        return style;
    }

    private static GUIStyle CenteredMiniLabel()
    {
        GUIStyle style = new(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter
        };
        return style;
    }

    private readonly struct CardValidationMessage
    {
        public readonly MessageType Type;
        public readonly string Text;

        private CardValidationMessage(MessageType type, string text)
        {
            Type = type;
            Text = text;
        }

        public static CardValidationMessage Error(string text)
        {
            return new CardValidationMessage(MessageType.Error, text);
        }

        public static CardValidationMessage Warning(string text)
        {
            return new CardValidationMessage(MessageType.Warning, text);
        }

        public static CardValidationMessage Info(string text)
        {
            return new CardValidationMessage(MessageType.Info, text);
        }
    }
}
#endif
