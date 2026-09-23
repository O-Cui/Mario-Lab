using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class LabSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MarioLab.unity";
    private const string GeneratedArtFolder = "Assets/Generated/Art";
    private static readonly Color32 Clear = new Color32(0, 0, 0, 0);

    [MenuItem("Tools/Mario Lab/Build Complete Lab Scene")]
    public static void Build()
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedArtFolder);
        EnsureFolder("Assets/Prefabs");
        EnsureTag("Ground");
        EnsureTag("Enemy");
        EnsureLayer("Ground");
        ConfigureExecutionOrder();
        ConfigureSourceSprites();

        Sprite marioSprite = LoadSprite("Assets/Sprites/mario.png", "mario_stand_right");
        Sprite goombaSprite = LoadSprite("Assets/Sprites/enemies.png", "brown_goomba_1");
        if (marioSprite == null || goombaSprite == null)
        {
            throw new InvalidOperationException("Required Mario or Goomba sprite slice could not be loaded.");
        }

        const string miscPath = "Assets/Sprites/misc-3.gif";
        Sprite brickTile = LoadSprite(miscPath, "Brick");
        Sprite questionTile = LoadSprite(miscPath, "Question Block");
        Sprite cloud1 = LoadSprite(miscPath, "1 Cloud");
        Sprite cloud2 = LoadSprite(miscPath, "2 Clouds");
        Sprite cloud3 = LoadSprite(miscPath, "3 Clouds");
        Sprite shrub1 = LoadSprite(miscPath, "1 Shrub");
        Sprite shrub3 = LoadSprite(miscPath, "3 Shrubs");
        Sprite smallHill = LoadSprite(miscPath, "Small Hill");
        Sprite bigHill = LoadSprite(miscPath, "Big Hill");
        Sprite coin = LoadSprite(miscPath, "Coin");
        Sprite mushroom = LoadSprite(miscPath, "Magic Mushroom");
        Sprite buttonSprite = CreatePixelSprite("RestartButton", 48, 16, ButtonPixel);
        PhysicsMaterial2D noFriction = CreatePhysicsMaterial();
        EnsureTMPEssentials();
        TMP_FontAsset uiFont = CreateUIFontAsset();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camera = CreateCamera();
        CreateGlobalLight();

        GameObject world = new GameObject("World");
        Transform background = new GameObject("Background Scenery").transform;
        background.SetParent(world.transform);
        Transform staticGeometry = new GameObject("Static Geometry").transform;
        staticGeometry.SetParent(world.transform);
        Transform decorations = new GameObject("Collectibles and Props").transform;
        decorations.SetParent(world.transform);

        BuildBackground(background, cloud1, cloud2, cloud3, shrub1, shrub3, smallHill, bigHill);
        BuildGround(staticGeometry, brickTile, noFriction);
        BuildBlocks(staticGeometry, decorations, brickTile, questionTile, coin, mushroom, noFriction);

        GameObject mario = CreateMario(marioSprite, noFriction);
        GameObject enemies = new GameObject("Enemies");
        // The Goomba source slice has one transparent pixel below its feet.
        // Lower its pivot by 1/16 unit so the visible sprite rests on the ground.
        GameObject goomba = CreateGoomba("Goomba", goombaSprite, new Vector2(4.5f, -2.5625f), enemies.transform);
        CreateUserInterface(mario, enemies, goomba, uiFont, buttonSprite);

        CameraFollow follow = camera.gameObject.AddComponent<CameraFollow>();
        follow.target = mario.transform;
        follow.minX = 0f;
        follow.maxX = 24f;

        PrefabUtility.SaveAsPrefabAsset(mario, "Assets/Prefabs/Mario.prefab");
        PrefabUtility.SaveAsPrefabAsset(goomba, "Assets/Prefabs/Goomba.prefab");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene, ScenePath))
        {
            throw new InvalidOperationException("Unity could not save the Mario lab scene.");
        }

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateScene();
        if (!Application.isBatchMode)
        {
            RenderPreview(camera);
        }
        Debug.Log("MARIO_LAB_SETUP_COMPLETE");
    }

    private static void ConfigureSourceSprites()
    {
        TextureImporter mario = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/mario.png");
        mario.textureType = TextureImporterType.Sprite;
        mario.spriteImportMode = SpriteImportMode.Multiple;
        mario.spritePixelsPerUnit = 16f;
        mario.filterMode = FilterMode.Point;
        mario.textureCompression = TextureImporterCompression.Uncompressed;
        mario.mipmapEnabled = false;
        mario.alphaIsTransparency = true;
        mario.SaveAndReimport();

        TextureImporter enemy = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/enemies.png");
        enemy.textureType = TextureImporterType.Sprite;
        enemy.spriteImportMode = SpriteImportMode.Multiple;
        enemy.spritePixelsPerUnit = 16f;
        enemy.filterMode = FilterMode.Point;
        enemy.textureCompression = TextureImporterCompression.Uncompressed;
        enemy.mipmapEnabled = false;
        enemy.alphaIsTransparency = true;
#pragma warning disable 0618
        enemy.spritesheet = new[]
        {
            new SpriteMetaData
            {
                name = "brown_goomba_1",
                rect = new Rect(0f, 245f, 16f, 16f),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            }
        };
#pragma warning restore 0618
        enemy.SaveAndReimport();

        foreach (string path in new[] { "Assets/Sprites/misc-3.gif", "Assets/Sprites/characters.gif" })
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }

        TextureImporter misc = (TextureImporter)AssetImporter.GetAtPath("Assets/Sprites/misc-3.gif");
        misc.spriteImportMode = SpriteImportMode.Multiple;
        TextureImporterSettings miscSettings = new TextureImporterSettings();
        misc.ReadTextureSettings(miscSettings);
        miscSettings.spriteMeshType = SpriteMeshType.FullRect;
        misc.SetTextureSettings(miscSettings);
#pragma warning disable 0618
        misc.spritesheet = new[]
        {
            Slice("Magic Mushroom", 52, 1193, 16, 16),
            Slice("Small Hill", 48, 1057, 48, 19),
            Slice("Big Hill", 99, 1057, 80, 35),
            Slice("2 Clouds", 46, 1030, 48, 24),
            Slice("3 Clouds", 96, 1030, 64, 24),
            Slice("1 Cloud", 162, 1030, 32, 24),
            Slice("1 Shrub", 51, 983, 32, 16),
            Slice("3 Shrubs", 85, 983, 64, 16),
            Slice("Brick", 373, 1189, 16, 16),
            Slice("Question Block", 372, 1076, 16, 16),
            Slice("Coin", 427, 1076, 10, 16)
        };
#pragma warning restore 0618
        misc.SaveAndReimport();
    }

#pragma warning disable 0618
    private static SpriteMetaData Slice(string name, float x, float y, float width, float height)
    {
        return new SpriteMetaData
        {
            name = name,
            rect = new Rect(x, y, width, height),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };
    }
#pragma warning restore 0618

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0.5f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(92, 148, 252, 255);
        return camera;
    }

    private static void CreateGlobalLight()
    {
        GameObject lightObject = new GameObject("Global Light 2D");
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    private static GameObject CreateMario(Sprite sprite, PhysicsMaterial2D material)
    {
        GameObject mario = new GameObject("Mario", typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(PlayerMovement), typeof(JumpOverGoomba));
        mario.tag = "Player";
        mario.transform.position = new Vector3(-7f, -2.45f, 0f);
        SpriteRenderer renderer = mario.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;
        Rigidbody2D body = mario.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 1f;
        body.linearDamping = 3f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = mario.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.95f);
        collider.sharedMaterial = material;
        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        movement.speed = 10f;
        movement.maxSpeed = 20f;
        movement.upSpeed = 10f;
        return mario;
    }

    private static GameObject CreateGoomba(string name, Sprite sprite, Vector2 position, Transform parent)
    {
        GameObject goomba = new GameObject(name, typeof(SpriteRenderer), typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyMovement));
        goomba.tag = "Enemy";
        goomba.transform.SetParent(parent);
        goomba.transform.position = position;
        SpriteRenderer renderer = goomba.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        Rigidbody2D body = goomba.GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = goomba.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.9f, 0.9f);
        EnemyMovement movement = goomba.GetComponent<EnemyMovement>();
        movement.maxOffset = 5f;
        movement.enemyPatrolTime = 2f;
        return goomba;
    }

    private static void BuildGround(Transform parent, Sprite sprite, PhysicsMaterial2D material)
    {
        GameObject ground = CreateTiledObject("Ground", sprite, new Vector2(8f, -4f), new Vector2(48f, 2f), 1, parent);
        MarkAsGround(ground);
        BoxCollider2D collider = ground.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(48f, 2f);
        collider.sharedMaterial = material;
    }

    private static void BuildBackground(Transform parent, Sprite cloud1, Sprite cloud2, Sprite cloud3, Sprite shrub1, Sprite shrub3, Sprite smallHill, Sprite bigHill)
    {
        CreateSpriteObject("Big Hill", bigHill, new Vector2(-3.5f, -2.25f), 0, parent, Vector2.one);
        CreateSpriteObject("Small Hill", smallHill, new Vector2(13f, -2.6f), 0, parent, Vector2.one);
        CreateSpriteObject("3 Shrubs", shrub3, new Vector2(3f, -2.6f), 0, parent, Vector2.one);
        CreateSpriteObject("1 Shrub", shrub1, new Vector2(20f, -2.6f), 0, parent, Vector2.one);
        CreateSpriteObject("1 Cloud", cloud1, new Vector2(-4f, 3.3f), 0, parent, Vector2.one);
        CreateSpriteObject("2 Clouds", cloud2, new Vector2(7f, 2.6f), 0, parent, Vector2.one);
        CreateSpriteObject("3 Clouds", cloud3, new Vector2(18f, 3.7f), 0, parent, Vector2.one);
        CreateSpriteObject("2 Clouds (2)", cloud2, new Vector2(28f, 2.8f), 0, parent, Vector2.one);
    }

    private static void BuildBlocks(Transform geometry, Transform decorations, Sprite brick, Sprite question, Sprite coin, Sprite mushroom, PhysicsMaterial2D material)
    {
        float[] xPositions = { -1f, 0f, 1f, 2f, 11f, 12f, 13f, 14f, 22f, 23f, 24f };
        foreach (float x in xPositions)
        {
            bool isQuestion = Mathf.Approximately(x, 0f) || Mathf.Approximately(x, 12f) || Mathf.Approximately(x, 23f);
            GameObject block = CreateSpriteObject(isQuestion ? "Question Block" : "Brick", isQuestion ? question : brick, new Vector2(x, 0f), 2, geometry, Vector2.one);
            MarkAsGround(block);
            BoxCollider2D collider = block.AddComponent<BoxCollider2D>();
            collider.sharedMaterial = material;
        }

        for (int i = 0; i < 5; i++)
        {
            CreateSpriteObject("Coin", coin, new Vector2(5f + i * 1.1f, 1.8f + Mathf.Sin(i * Mathf.PI / 4f)), 2, decorations, Vector2.one);
        }
        CreateSpriteObject("Magic Mushroom", mushroom, new Vector2(12f, 1.1f), 2, decorations, Vector2.one);
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Vector2 position, int order, Transform parent, Vector2 scale)
    {
        GameObject gameObject = new GameObject(name, typeof(SpriteRenderer));
        gameObject.transform.SetParent(parent);
        gameObject.transform.position = position;
        gameObject.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return gameObject;
    }

    private static GameObject CreateTiledObject(string name, Sprite sprite, Vector2 position, Vector2 size, int order, Transform parent)
    {
        GameObject gameObject = CreateSpriteObject(name, sprite, position, order, parent, Vector2.one);
        SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;
        return gameObject;
    }

    private static Sprite CreatePixelSprite(string name, int width, int height, Func<int, int, Color32> pixel)
    {
        string path = GeneratedArtFolder + "/" + name + ".asset";
        Sprite existing = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (existing != null)
        {
            return existing;
        }

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = name + " Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixel(x, y);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 16f, 0u, SpriteMeshType.FullRect);
        sprite.name = name;
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }

    private static PhysicsMaterial2D CreatePhysicsMaterial()
    {
        const string path = "Assets/Generated/NoFriction.physicsMaterial2D";
        PhysicsMaterial2D material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (material != null)
        {
            return material;
        }
        material = new PhysicsMaterial2D("No Friction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static TMP_FontAsset CreateUIFontAsset()
    {
        const string path = "Assets/Fonts/prstart SDF.asset";
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null)
        {
            return existing;
        }

        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/prstart.ttf");
        if (source == null)
        {
            throw new InvalidOperationException("Assets/Fonts/prstart.ttf is missing.");
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(source);
        fontAsset.name = "prstart SDF";
        AssetDatabase.CreateAsset(fontAsset, path);
        if (fontAsset.material != null && !AssetDatabase.Contains(fontAsset.material))
        {
            fontAsset.material.name = "prstart Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }
        foreach (Texture2D atlas in fontAsset.atlasTextures)
        {
            if (atlas != null && !AssetDatabase.Contains(atlas))
            {
                atlas.name = "prstart Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            }
        }
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static void EnsureTMPEssentials()
    {
        if (TMP_Settings.instance != null)
        {
            return;
        }

        string package = Directory.GetFiles("Library/PackageCache", "TMP Essential Resources.unitypackage", SearchOption.AllDirectories).FirstOrDefault();
        if (string.IsNullOrEmpty(package))
        {
            throw new InvalidOperationException("Unity's TMP Essential Resources package could not be found.");
        }

        AssetDatabase.ImportPackage(Path.GetFullPath(package), false);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (TMP_Settings.instance == null)
        {
            throw new InvalidOperationException("TMP Essential Resources did not create TMP Settings. Run Window > TextMeshPro > Import TMP Essential Resources.");
        }
    }

    private static void CreateUserInterface(GameObject mario, GameObject enemies, GameObject goomba, TMP_FontAsset font, Sprite buttonSprite)
    {
        GameObject canvasObject = new GameObject("HUD Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        TextMeshProUGUI scoreText = CreateText("ScoreText", canvasObject.transform, font, "Score: 0", 34f, TextAlignmentOptions.Left);
        RectTransform scoreRect = scoreText.rectTransform;
        scoreRect.anchorMin = new Vector2(0f, 1f);
        scoreRect.anchorMax = new Vector2(0f, 1f);
        scoreRect.pivot = new Vector2(0f, 1f);
        scoreRect.anchoredPosition = new Vector2(55f, -45f);
        scoreRect.sizeDelta = new Vector2(520f, 80f);

        GameObject buttonObject = new GameObject("RestartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = new Vector2(-55f, -35f);
        buttonRect.sizeDelta = new Vector2(330f, 90f);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = buttonSprite;
        buttonImage.type = Image.Type.Simple;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.9f, 0.5f, 1f);
        colors.pressedColor = new Color(0.75f, 0.55f, 0.2f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;

        TextMeshProUGUI buttonLabel = CreateText("Label", buttonObject.transform, font, "RESTART", 28f, TextAlignmentOptions.Center);
        Stretch(buttonLabel.rectTransform, 12f);

        GameObject gameOverPanel = new GameObject("GameOverPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameOverPanel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(780f, 210f);
        gameOverPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        TextMeshProUGUI gameOverText = CreateText("GameOverText", gameOverPanel.transform, font, "GAME OVER\nScore: 0\nPRESS RESTART", 34f, TextAlignmentOptions.Center);
        Stretch(gameOverText.rectTransform, 20f);
        gameOverPanel.SetActive(false);

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.GetComponent<EventSystem>().firstSelectedGameObject = null;

        JumpOverGoomba jump = mario.GetComponent<JumpOverGoomba>();
        jump.enemyLocation = goomba.transform;
        jump.scoreText = scoreText;
        jump.boxSize = new Vector3(0.65f, 0.12f, 0f);
        jump.maxDistance = 0.55f;
        jump.layerMask = 1 << LayerMask.NameToLayer("Ground");

        PlayerMovement movement = mario.GetComponent<PlayerMovement>();
        movement.scoreText = scoreText;
        movement.enemies = enemies;
        movement.jumpOverGoomba = jump;
        movement.gameOverPanel = gameOverPanel;
        UnityEventTools.AddIntPersistentListener(button.onClick, movement.RestartButtonCallback, 0);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void MarkAsGround(GameObject gameObject)
    {
        gameObject.tag = "Ground";
        gameObject.layer = LayerMask.NameToLayer("Ground");
    }

    private static Sprite LoadSprite(string path, string name)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(sprite => sprite.name == name);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void EnsureTag(string tag)
    {
        if (UnityEditorInternal.InternalEditorUtility.tags.Contains(tag))
        {
            return;
        }
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }

    private static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0)
        {
            return;
        }

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int index = 8; index < layers.arraySize; index++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(layer.stringValue))
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
        throw new InvalidOperationException("No empty user layer is available for the Ground layer.");
    }

    private static void ConfigureExecutionOrder()
    {
        SetExecutionOrder("Assets/Scripts/PlayerMovement.cs", -100);
        SetExecutionOrder("Assets/Scripts/JumpOverGoomba.cs", 0);
        SetExecutionOrder("Assets/Scripts/EnemyMovement.cs", 100);
    }

    private static void SetExecutionOrder(string path, int order)
    {
        MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        if (script == null)
        {
            throw new InvalidOperationException("Cannot set execution order; script missing: " + path);
        }
        MonoImporter.SetExecutionOrder(script, order);
    }

    private static void ValidateScene()
    {
        GameObject mario = GameObject.Find("Mario");
        GameObject ground = GameObject.Find("Ground");
        GameObject enemies = GameObject.Find("Enemies");
        GameObject canvas = GameObject.Find("HUD Canvas");
        GameObject restartButton = GameObject.Find("RestartButton");
        if (mario == null || ground == null || enemies == null || canvas == null || restartButton == null || mario.GetComponent<PlayerMovement>() == null || mario.GetComponent<JumpOverGoomba>() == null || enemies.GetComponentsInChildren<EnemyMovement>().Length < 1)
        {
            throw new InvalidOperationException("Scene validation failed: required Mario lab objects/components are missing.");
        }
        Debug.Log($"Scene validation passed: {SceneManager.GetActiveScene().rootCount} roots, {enemies.transform.childCount} Goombas.");
    }

    private static void RenderPreview(Camera camera)
    {
        try
        {
            const int width = 960;
            const int height = 540;
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            RenderMode originalMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            Camera originalCamera = canvas != null ? canvas.worldCamera : null;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
            }
            RenderTexture target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetTempPath(), "MarioLabPreview.png"), image.EncodeToPNG());
            if (canvas != null)
            {
                canvas.renderMode = originalMode;
                canvas.worldCamera = originalCamera;
            }
            camera.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Preview rendering was skipped: " + exception.Message);
        }
    }

    private static Color32 GroundPixel(int x, int y)
    {
        Color32 dark = new Color32(92, 44, 12, 255);
        Color32 orange = new Color32(228, 92, 16, 255);
        Color32 light = new Color32(252, 188, 68, 255);
        if (x == 0 || y == 0 || y == 8 || (y < 8 && x == 8) || (y >= 8 && x == 4)) return dark;
        return (x + y) % 5 == 0 ? light : orange;
    }

    private static Color32 BrickPixel(int x, int y)
    {
        if (x == 0 || y == 0 || y == 7 || y == 15 || (y < 7 && x == 8) || (y > 7 && x == 4) || (y > 7 && x == 12)) return new Color32(92, 40, 12, 255);
        return new Color32(220, 76, 24, 255);
    }

    private static Color32 QuestionPixel(int x, int y)
    {
        Color32 dark = new Color32(116, 48, 8, 255);
        Color32 gold = new Color32(248, 176, 24, 255);
        Color32 light = new Color32(255, 232, 104, 255);
        if (x < 2 || x > 13 || y < 2 || y > 13) return dark;
        bool question = (y >= 10 && x >= 6 && x <= 9) || (y == 9 && (x == 5 || x == 10)) || (y >= 7 && y <= 8 && x == 9) || (y == 6 && x >= 7 && x <= 9) || (y == 4 && x == 8);
        return question ? light : gold;
    }

    private static Color32 PipePixel(int x, int y)
    {
        if (x < 2 || x > 13) return new Color32(0, 88, 0, 255);
        if (x < 5) return new Color32(80, 216, 44, 255);
        if (x < 8) return new Color32(184, 248, 116, 255);
        return new Color32(24, 160, 24, 255);
    }

    private static Color32 CloudPixel(int x, int y)
    {
        bool inside = Circle(x, y, 8, 6, 6) || Circle(x, y, 16, 9, 8) || Circle(x, y, 24, 6, 6) || (x >= 4 && x <= 27 && y <= 7);
        if (!inside) return Clear;
        return y < 2 ? new Color32(124, 188, 252, 255) : new Color32(255, 255, 255, 255);
    }

    private static Color32 BushPixel(int x, int y)
    {
        bool inside = Circle(x, y, 8, 6, 6) || Circle(x, y, 16, 9, 8) || Circle(x, y, 24, 6, 6) || (x >= 4 && x <= 27 && y <= 6);
        if (!inside) return Clear;
        return y < 2 ? new Color32(0, 112, 20, 255) : new Color32(0, 184, 32, 255);
    }

    private static Color32 HillPixel(int x, int y)
    {
        int halfWidth = Mathf.RoundToInt((y / 23f) * 22f);
        bool inside = y <= 22 && Mathf.Abs(x - 24) <= 24 - halfWidth;
        if (!inside) return Clear;
        if ((x + y) % 13 == 0) return new Color32(0, 120, 20, 255);
        return new Color32(64, 200, 48, 255);
    }

    private static Color32 CoinPixel(int x, int y)
    {
        bool inside = x >= 1 && x <= 8 && y >= 1 && y <= 14 && !(x <= 2 && (y <= 2 || y >= 13)) && !(x >= 7 && (y <= 2 || y >= 13));
        if (!inside) return Clear;
        return x == 1 || x == 8 || y == 1 || y == 14 ? new Color32(196, 84, 8, 255) : new Color32(255, 216, 48, 255);
    }

    private static Color32 MushroomPixel(int x, int y)
    {
        if (y >= 7)
        {
            bool cap = Circle(x, y, 8, 8, 8);
            if (!cap) return Clear;
            bool spot = Circle(x, y, 5, 11, 2) || Circle(x, y, 11, 12, 2);
            return spot ? new Color32(255, 244, 208, 255) : new Color32(220, 32, 24, 255);
        }
        if (x >= 5 && x <= 10 && y >= 1) return new Color32(255, 220, 168, 255);
        return Clear;
    }

    private static Color32 ButtonPixel(int x, int y)
    {
        Color32 edge = new Color32(92, 40, 12, 255);
        Color32 shadow = new Color32(196, 72, 16, 255);
        Color32 face = new Color32(244, 144, 32, 255);
        Color32 shine = new Color32(255, 208, 96, 255);
        if (x == 0 || x == 47 || y == 0 || y == 15) return edge;
        if (y <= 2 || x <= 2 || x >= 45) return shadow;
        if (y >= 12) return shine;
        return face;
    }

    private static bool Circle(int x, int y, int centerX, int centerY, int radius)
    {
        int dx = x - centerX;
        int dy = y - centerY;
        return dx * dx + dy * dy <= radius * radius;
    }
}
