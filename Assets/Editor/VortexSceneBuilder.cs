#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using Vortex.Player;
using Vortex.CameraSystem;
using Vortex.Core;
using Vortex.Combat;
using Vortex.Enemies;
using Vortex.UI;

namespace Vortex.EditorTools
{
    public static class VortexSceneBuilder
    {
        [MenuItem("Vortex Tools/1. Build Sprint 1 (Player & Arena)", false, 1)]
        public static void BuildSprint1Scene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Sprint 1 Setup");
            int undoGroup = Undo.GetCurrentGroup();

            // a) Criar Plane "Arena_Floor" na posição (0,0,0) com escala (10, 1, 10) e cor cinza chumbo escura
            GameObject floor = GameObject.Find("Arena_Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Arena_Floor";
                Undo.RegisterCreatedObjectUndo(floor, "Create Arena Floor");
            }
            floor.transform.position = Vector3.zero;
            floor.transform.rotation = Quaternion.identity;
            floor.transform.localScale = new Vector3(10f, 1f, 10f);

            Material floorMat = GetOrCreateMaterial("M_ArenaFloor_DarkGray", new Color(0.12f, 0.12f, 0.14f), false);
            if (floorMat != null && floor.TryGetComponent<Renderer>(out var floorRenderer))
            {
                floorRenderer.sharedMaterial = floorMat;
            }

            // b) Criar GameObject "Player" na posição (0, 1, 0), primitiva Capsule, Rigidbody, PlayerMovement, Tag "Player" e cor Ciano
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                Undo.RegisterCreatedObjectUndo(player, "Create Player");
            }
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.transform.rotation = Quaternion.identity;
            player.transform.localScale = Vector3.one;

            EnsureTagExists("Player");
            player.tag = "Player";

            if (!player.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = Undo.AddComponent<Rigidbody>(player);
            }
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezePositionY 
                           | RigidbodyConstraints.FreezeRotationX 
                           | RigidbodyConstraints.FreezeRotationZ;

            if (!player.TryGetComponent<PlayerMovement>(out _))
            {
                Undo.AddComponent<PlayerMovement>(player);
            }

            Material playerMat = GetOrCreateMaterial("M_Player_Cyan", new Color(0.0f, 0.85f, 0.95f), true);
            if (playerMat != null && player.TryGetComponent<Renderer>(out var playerRenderer))
            {
                playerRenderer.sharedMaterial = playerMat;
            }

            // c) Configurar Main Camera existente com TopDownCameraFollow e conectar referência do Player
            Camera mainCam = Camera.main;
            GameObject camObj = mainCam != null ? mainCam.gameObject : GameObject.FindWithTag("MainCamera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                camObj.tag = "MainCamera";
                Undo.RegisterCreatedObjectUndo(camObj, "Create Main Camera");
            }

            if (!camObj.TryGetComponent<TopDownCameraFollow>(out var camFollow))
            {
                camFollow = Undo.AddComponent<TopDownCameraFollow>(camObj);
            }
            Undo.RecordObject(camFollow, "Assign Camera Target");
            camFollow.SetTarget(player.transform);
            EditorUtility.SetDirty(camFollow);

            // d) Criar _AppManager com script PCAppConfig
            GameObject appManager = GameObject.Find("_AppManager");
            if (appManager == null)
            {
                appManager = new GameObject("_AppManager");
                Undo.RegisterCreatedObjectUndo(appManager, "Create _AppManager");
            }
            if (!appManager.TryGetComponent<PCAppConfig>(out _))
            {
                Undo.AddComponent<PCAppConfig>(appManager);
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorUtility.SetDirty(floor);
            EditorUtility.SetDirty(player);
            EditorUtility.SetDirty(camObj);
            EditorUtility.SetDirty(appManager);

            Selection.activeGameObject = player;
            Debug.Log("<color=#00FF99>[Vortex Tools]</color> Sprint 1 (Player & Arena) construída com sucesso!");
        }

        [MenuItem("Vortex Tools/2. Build Sprint 2 (Combat & Enemies)", false, 2)]
        public static void BuildSprint2Scene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Sprint 2 Setup");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Garantir que a pasta de Prefabs existe
            EnsureFolderHierarchy("Assets", "Prefabs");

            // 2. Garantir tags essenciais
            EnsureTagExists("Player");
            EnsureTagExists("Enemy");

            // 3. Gerar e salvar Prefab do Swarmer
            GameObject swarmerPrefab = CreateOrUpdateSwarmerPrefab();

            // 4. Gerar e salvar Prefab do PulseProjectile
            GameObject projectilePrefab = CreateOrUpdateProjectilePrefab();

            // 5. Garantir existência e configuração do Player
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player == null)
            {
                BuildSprint1Scene();
                player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            }

            if (!player.TryGetComponent<PlayerHealth>(out var playerHealth))
            {
                playerHealth = Undo.AddComponent<PlayerHealth>(player);
            }

            if (!player.TryGetComponent<PulseBlaster>(out var pulseBlaster))
            {
                pulseBlaster = Undo.AddComponent<PulseBlaster>(player);
            }

            EditorUtility.SetDirty(player);

            // 6. Criar ou atualizar _GameplayManagers contendo ObjectPooler e WaveSpawner
            GameObject managersObj = GameObject.Find("_GameplayManagers");
            if (managersObj == null)
            {
                managersObj = new GameObject("_GameplayManagers");
                Undo.RegisterCreatedObjectUndo(managersObj, "Create _GameplayManagers");
            }

            // ObjectPooler
            if (!managersObj.TryGetComponent<ObjectPooler>(out var pooler))
            {
                pooler = Undo.AddComponent<ObjectPooler>(managersObj);
            }

            pooler.RegisterPoolConfig("Swarmer", swarmerPrefab, 50);
            pooler.RegisterPoolConfig("PulseProjectile", projectilePrefab, 60);
            EditorUtility.SetDirty(pooler);

            // WaveSpawner
            if (!managersObj.TryGetComponent<WaveSpawner>(out var spawner))
            {
                spawner = Undo.AddComponent<WaveSpawner>(managersObj);
            }

            spawner.SetPlayer(player.transform);
            EditorUtility.SetDirty(spawner);
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = managersObj;
            Debug.Log("<color=#00FF99>[Vortex Tools]</color> Sprint 2 (Combat & Enemies) construída com sucesso! Prefabs, PlayerHealth, PulseBlaster e WaveSpawner prontos para jogar.");
        }

        [MenuItem("Vortex Tools/3. Build Sprint 3 (XP, Upgrades & HUD)", false, 3)]
        public static void BuildSprint3Scene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Sprint 3 Setup");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Garantir que as Sprints 1 e 2 foram construídas
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player == null || GameObject.Find("_GameplayManagers") == null)
            {
                BuildSprint2Scene();
                player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            }

            // 2. Criar e registrar o Prefab XPGem no ObjectPooler
            GameObject xpGemPrefab = CreateOrUpdateXPGemPrefab();

            GameObject managersObj = GameObject.Find("_GameplayManagers");
            if (managersObj != null && managersObj.TryGetComponent<ObjectPooler>(out var pooler))
            {
                pooler.RegisterPoolConfig("XPGem", xpGemPrefab, 100);
                EditorUtility.SetDirty(pooler);
            }

            // 3. Adicionar e conectar PlayerStats no Player
            if (!player.TryGetComponent<PlayerStats>(out var stats))
            {
                stats = Undo.AddComponent<PlayerStats>(player);
            }
            EditorUtility.SetDirty(player);

            // 4. Configurar Canvas, HUD e Janela de Level Up
            SetupHUDAndLevelUpUI();

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#00FF99>[Vortex Tools]</color> Sprint 3 (XP, Upgrades & HUD) construída com sucesso! Loop completo pronto para jogar.");
        }

        [MenuItem("Vortex Tools/4. Build Sprint 4 (Full Arsenal, Waves, 3D Models & EndGame)", false, 4)]
        public static void BuildSprint4Scene()
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Build Sprint 4 Setup");
            int undoGroup = Undo.GetCurrentGroup();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // 1. Garantir que as Sprints 1, 2 e 3 foram construídas
            GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (player == null || GameObject.Find("_GameplayManagers") == null)
            {
                BuildSprint3Scene();
                player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            }

            // 2. Criar ou atualizar os Prefabs de Bruiser e CoreBoss, e atualizar Swarmer e XPGem com malhas FBX
            GameObject swarmerPrefab = CreateOrUpdateSwarmerPrefab();
            GameObject bruiserPrefab = CreateOrUpdateBruiserPrefab();
            GameObject coreBossPrefab = CreateOrUpdateCoreBossPrefab();
            GameObject xpGemPrefab = CreateOrUpdateXPGemPrefab();
            GameObject projectilePrefab = CreateOrUpdateProjectilePrefab();

            // 3. Atualizar malha 3D do Player se PlayerCore.fbx existir
            Mesh playerMesh = LoadMeshFromModel("PlayerCore");
            if (playerMesh != null && player.TryGetComponent<MeshFilter>(out var pMf))
            {
                pMf.sharedMesh = playerMesh;
            }
            Material playerMat = GetOrCreateMaterial("M_Player_Cyan", new Color(0.0f, 0.85f, 0.95f), true);
            if (playerMat != null && player.TryGetComponent<Renderer>(out var pRend))
            {
                pRend.sharedMaterial = playerMat;
            }

            // 4. Registrar todas as pools no ObjectPooler
            GameObject managersObj = GameObject.Find("_GameplayManagers");
            if (managersObj == null)
            {
                managersObj = new GameObject("_GameplayManagers");
                Undo.RegisterCreatedObjectUndo(managersObj, "Create _GameplayManagers");
            }

            if (!managersObj.TryGetComponent<ObjectPooler>(out var pooler))
            {
                pooler = Undo.AddComponent<ObjectPooler>(managersObj);
            }
            pooler.RegisterPoolConfig("Swarmer", swarmerPrefab, 60);
            pooler.RegisterPoolConfig("Bruiser", bruiserPrefab, 30);
            pooler.RegisterPoolConfig("CoreBoss", coreBossPrefab, 4);
            pooler.RegisterPoolConfig("PulseProjectile", projectilePrefab, 60);
            pooler.RegisterPoolConfig("XPGem", xpGemPrefab, 100);
            EditorUtility.SetDirty(pooler);

            // 5. Configurar WaveSpawner
            if (!managersObj.TryGetComponent<WaveSpawner>(out var spawner))
            {
                spawner = Undo.AddComponent<WaveSpawner>(managersObj);
            }
            spawner.SetPlayer(player.transform);
            EditorUtility.SetDirty(spawner);

            // 6. Anexar OrbitingBlades e VortexShockwave ao Player
            if (!player.TryGetComponent<PulseBlaster>(out _))
            {
                Undo.AddComponent<PulseBlaster>(player);
            }

            if (!player.TryGetComponent<OrbitingBlades>(out var blades))
            {
                blades = Undo.AddComponent<OrbitingBlades>(player);
            }
            blades.currentLevel = 0; // Desativada até Level Up

            if (!player.TryGetComponent<VortexShockwave>(out var shockwave))
            {
                shockwave = Undo.AddComponent<VortexShockwave>(player);
            }
            shockwave.currentLevel = 0; // Desativada até Level Up

            EditorUtility.SetDirty(player);

            // 7. Criar e configurar GameManager em _GameplayManagers
            if (!managersObj.TryGetComponent<GameManager>(out var gm))
            {
                gm = Undo.AddComponent<GameManager>(managersObj);
            }
            EditorUtility.SetDirty(gm);
            EditorUtility.SetDirty(managersObj);

            // 8. Montar e conectar EndGameUI no Canvas
            SetupEndGameUI();

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            Undo.CollapseUndoOperations(undoGroup);

            Selection.activeGameObject = managersObj;
            Debug.Log("<color=#00FF99><b>[Vortex Tools]</b></color> Sprint 4 (Full Arsenal, Waves, 3D Models & EndGame) construída com sucesso! Todos os sistemas, malhas 3D e fim de jogo prontos.");
        }

        private static GameObject CreateOrUpdateSwarmerPrefab()
        {
            EnsureFolderHierarchy("Assets", "Prefabs");
            string prefabPath = "Assets/Prefabs/Swarmer.prefab";

            EnsureTagExists("Enemy");

            GameObject tempSwarmer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempSwarmer.name = "Swarmer";
            tempSwarmer.tag = "Enemy";
            tempSwarmer.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            Mesh swarmerMesh = LoadMeshFromModel("Swarmer");
            if (swarmerMesh != null && tempSwarmer.TryGetComponent<MeshFilter>(out var mf))
            {
                mf.sharedMesh = swarmerMesh;
            }

            if (tempSwarmer.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }

            if (!tempSwarmer.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = tempSwarmer.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!tempSwarmer.TryGetComponent<EnemyAI>(out var ai))
            {
                ai = tempSwarmer.AddComponent<EnemyAI>();
            }
            ai.SetStats(speed: 4.5f, damage: 5f, resistance: 0f, height: 0.8f);

            if (!tempSwarmer.TryGetComponent<EnemyHealth>(out var health))
            {
                health = tempSwarmer.AddComponent<EnemyHealth>();
            }
            health.SetBaseScale(new Vector3(0.8f, 0.8f, 0.8f));
            health.Configure(15f, "Swarmer");

            Material mat = GetOrCreateMaterial("M_Swarmer_OrangeRed", new Color(1.0f, 0.22f, 0.05f), true);
            if (mat != null && tempSwarmer.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = mat;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempSwarmer, prefabPath);
            Object.DestroyImmediate(tempSwarmer);

            return savedPrefab;
        }

        private static GameObject CreateOrUpdateProjectilePrefab()
        {
            EnsureFolderHierarchy("Assets", "Prefabs");
            string prefabPath = "Assets/Prefabs/PulseProjectile.prefab";

            GameObject tempProj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempProj.name = "PulseProjectile";
            tempProj.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            if (tempProj.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }

            if (!tempProj.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = tempProj.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!tempProj.TryGetComponent<Projectile>(out _))
            {
                tempProj.AddComponent<Projectile>();
            }

            Material mat = GetOrCreateMaterial("M_Projectile_Cyan", new Color(0.1f, 0.95f, 1.0f), true);
            if (mat != null && tempProj.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = mat;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempProj, prefabPath);
            Object.DestroyImmediate(tempProj);

            return savedPrefab;
        }

        private static GameObject CreateOrUpdateXPGemPrefab()
        {
            EnsureFolderHierarchy("Assets", "Prefabs");
            string prefabPath = "Assets/Prefabs/XPGem.prefab";

            GameObject tempGem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempGem.name = "XPGem";
            tempGem.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            tempGem.transform.rotation = Quaternion.Euler(45f, 45f, 45f);

            Mesh gemMesh = LoadMeshFromModel("XPGem");
            if (gemMesh != null && tempGem.TryGetComponent<MeshFilter>(out var mf))
            {
                mf.sharedMesh = gemMesh;
            }

            if (tempGem.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }

            if (!tempGem.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = tempGem.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!tempGem.TryGetComponent<XPGem>(out _))
            {
                tempGem.AddComponent<XPGem>();
            }

            Material mat = GetOrCreateMaterial("M_XPGem_EmeraldCyan", new Color(0.0f, 1.0f, 0.65f), true);
            if (mat != null && tempGem.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = mat;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGem, prefabPath);
            Object.DestroyImmediate(tempGem);

            return savedPrefab;
        }

        private static GameObject CreateOrUpdateBruiserPrefab()
        {
            EnsureFolderHierarchy("Assets", "Prefabs");
            string prefabPath = "Assets/Prefabs/Bruiser.prefab";

            EnsureTagExists("Enemy");

            GameObject tempBruiser = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tempBruiser.name = "Bruiser";
            tempBruiser.tag = "Enemy";
            tempBruiser.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);

            Mesh bruiserMesh = LoadMeshFromModel("Bruiser");
            if (bruiserMesh != null && tempBruiser.TryGetComponent<MeshFilter>(out var mf))
            {
                mf.sharedMesh = bruiserMesh;
            }

            if (tempBruiser.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }

            if (!tempBruiser.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = tempBruiser.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!tempBruiser.TryGetComponent<EnemyAI>(out var ai))
            {
                ai = tempBruiser.AddComponent<EnemyAI>();
            }
            ai.SetStats(speed: 2.2f, damage: 15f, resistance: 0.75f, height: 1.0f);

            if (!tempBruiser.TryGetComponent<EnemyHealth>(out var health))
            {
                health = tempBruiser.AddComponent<EnemyHealth>();
            }
            health.SetBaseScale(new Vector3(1.2f, 1.2f, 1.2f));
            health.Configure(60f, "Bruiser");

            Material mat = GetOrCreateMaterial("M_Bruiser_Magenta", new Color(0.85f, 0.1f, 0.75f), true);
            if (mat != null && tempBruiser.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = mat;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempBruiser, prefabPath);
            Object.DestroyImmediate(tempBruiser);

            return savedPrefab;
        }

        private static GameObject CreateOrUpdateCoreBossPrefab()
        {
            EnsureFolderHierarchy("Assets", "Prefabs");
            string prefabPath = "Assets/Prefabs/CoreBoss.prefab";

            EnsureTagExists("Enemy");

            GameObject tempBoss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tempBoss.name = "CoreBoss";
            tempBoss.tag = "Enemy";
            tempBoss.transform.localScale = new Vector3(3.0f, 3.0f, 3.0f);

            Mesh bossMesh = LoadMeshFromModel("CoreBoss");
            if (bossMesh != null && tempBoss.TryGetComponent<MeshFilter>(out var mf))
            {
                mf.sharedMesh = bossMesh;
            }

            if (tempBoss.TryGetComponent<Collider>(out var col))
            {
                col.isTrigger = true;
            }

            if (!tempBoss.TryGetComponent<Rigidbody>(out var rb))
            {
                rb = tempBoss.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            if (!tempBoss.TryGetComponent<EnemyAI>(out var ai))
            {
                ai = tempBoss.AddComponent<EnemyAI>();
            }
            ai.SetStats(speed: 3.5f, damage: 30f, resistance: 0.95f, height: 2.2f);

            if (!tempBoss.TryGetComponent<EnemyHealth>(out var health))
            {
                health = tempBoss.AddComponent<EnemyHealth>();
            }
            health.SetBaseScale(new Vector3(3.0f, 3.0f, 3.0f));
            health.Configure(800f, "CoreBoss");

            Material mat = GetOrCreateMaterial("M_CoreBoss_CrimsonGold", new Color(1.0f, 0.25f, 0.05f), true);
            if (mat != null && tempBoss.TryGetComponent<Renderer>(out var rend))
            {
                rend.sharedMaterial = mat;
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempBoss, prefabPath);
            Object.DestroyImmediate(tempBoss);

            return savedPrefab;
        }

        private static void SetupHUDAndLevelUpUI()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Sprite whiteSprite = GetOrCreateWhiteSprite();

            // 1. Garantir EventSystem
            GameObject es = GameObject.Find("EventSystem");
            if (es == null)
            {
                es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
            }
            else
            {
#if ENABLE_INPUT_SYSTEM
                if (!es.TryGetComponent<InputSystemUIInputModule>(out _))
                {
                    if (es.TryGetComponent<UnityEngine.EventSystems.StandaloneInputModule>(out var std))
                    {
                        Object.DestroyImmediate(std);
                    }
                    es.AddComponent<InputSystemUIInputModule>();
                }
#endif
            }

            // 2. Garantir HUD_Canvas
            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("HUD_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create HUD_Canvas");
            }

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObj);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObj);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasObj);

            HUDController hud = GetOrAddComponent<HUDController>(canvasObj);
            LevelUpUI levelUpUI = GetOrAddComponent<LevelUpUI>(canvasObj);

            // ==================== CONSTRUÇÃO DA TOP HUD ====================
            Transform topHudTr = canvasObj.transform.Find("TopHUD");
            GameObject topHudObj = topHudTr != null ? topHudTr.gameObject : new GameObject("TopHUD", typeof(RectTransform));
            topHudObj.transform.SetParent(canvasObj.transform, false);

            RectTransform topHudRt = GetOrAddComponent<RectTransform>(topHudObj);
            topHudRt.anchorMin = new Vector2(0f, 1f);
            topHudRt.anchorMax = new Vector2(1f, 1f);
            topHudRt.pivot = new Vector2(0.5f, 1f);
            topHudRt.anchoredPosition = Vector2.zero;
            topHudRt.sizeDelta = new Vector2(0f, 110f);

            // XP Bar Background
            GameObject xpBg = GetOrCreateUIChild(topHudObj, "XP_Bar_Background");
            RectTransform xpBgRt = GetOrAddComponent<RectTransform>(xpBg);
            xpBgRt.anchorMin = new Vector2(0.04f, 0.45f);
            xpBgRt.anchorMax = new Vector2(0.96f, 0.85f);
            xpBgRt.offsetMin = Vector2.zero;
            xpBgRt.offsetMax = Vector2.zero;
            Image xpBgImg = GetOrAddComponent<Image>(xpBg);
            xpBgImg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            // XP Bar Fill
            GameObject xpFill = GetOrCreateUIChild(xpBg, "XP_Bar_Fill");
            RectTransform xpFillRt = GetOrAddComponent<RectTransform>(xpFill);
            xpFillRt.anchorMin = Vector2.zero;
            xpFillRt.anchorMax = Vector2.one;
            xpFillRt.offsetMin = Vector2.zero;
            xpFillRt.offsetMax = Vector2.zero;
            Image xpFillImg = GetOrAddComponent<Image>(xpFill);
            xpFillImg.sprite = whiteSprite;
            xpFillImg.type = Image.Type.Filled;
            xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            xpFillImg.color = new Color(0.0f, 0.85f, 1.0f, 1f);
            xpFillImg.fillAmount = 0f;

            // Level Text
            GameObject lvlObj = GetOrCreateUIChild(topHudObj, "Level_Text");
            RectTransform lvlRt = GetOrAddComponent<RectTransform>(lvlObj);
            lvlRt.anchorMin = new Vector2(0.04f, 0.05f);
            lvlRt.anchorMax = new Vector2(0.2f, 0.42f);
            lvlRt.offsetMin = Vector2.zero;
            lvlRt.offsetMax = Vector2.zero;
            Text lvlTxt = GetOrAddComponent<Text>(lvlObj);
            lvlTxt.font = defaultFont;
            lvlTxt.fontSize = 24;
            lvlTxt.fontStyle = FontStyle.Bold;
            lvlTxt.color = Color.white;
            lvlTxt.alignment = TextAnchor.MiddleLeft;
            lvlTxt.text = "LV. 1";

            // Timer Text (centralizado abaixo da barra de XP)
            GameObject tmrObj = GetOrCreateUIChild(topHudObj, "Timer_Text");
            RectTransform tmrRt = GetOrAddComponent<RectTransform>(tmrObj);
            tmrRt.anchorMin = new Vector2(0.42f, 0.05f);
            tmrRt.anchorMax = new Vector2(0.58f, 0.42f);
            tmrRt.offsetMin = Vector2.zero;
            tmrRt.offsetMax = Vector2.zero;
            Text tmrTxt = GetOrAddComponent<Text>(tmrObj);
            tmrTxt.font = defaultFont;
            tmrTxt.fontSize = 26;
            tmrTxt.fontStyle = FontStyle.Bold;
            tmrTxt.color = new Color(0.9f, 0.95f, 1f);
            tmrTxt.alignment = TextAnchor.MiddleCenter;
            tmrTxt.text = "00:00";

            // Kills Text (canto superior direito)
            GameObject kllObj = GetOrCreateUIChild(topHudObj, "Kills_Text");
            RectTransform kllRt = GetOrAddComponent<RectTransform>(kllObj);
            kllRt.anchorMin = new Vector2(0.8f, 0.05f);
            kllRt.anchorMax = new Vector2(0.96f, 0.42f);
            kllRt.offsetMin = Vector2.zero;
            kllRt.offsetMax = Vector2.zero;
            Text kllTxt = GetOrAddComponent<Text>(kllObj);
            kllTxt.font = defaultFont;
            kllTxt.fontSize = 22;
            kllTxt.fontStyle = FontStyle.Bold;
            kllTxt.color = new Color(1f, 0.45f, 0.2f);
            kllTxt.alignment = TextAnchor.MiddleRight;
            kllTxt.text = "KILLS: 0";

            // Barra de Vida do Jogador (Top Left)
            GameObject hpBg = GetOrCreateUIChild(topHudObj, "Player_HP_Background");
            RectTransform hpBgRt = GetOrAddComponent<RectTransform>(hpBg);
            hpBgRt.anchorMin = new Vector2(0.18f, 0.12f);
            hpBgRt.anchorMax = new Vector2(0.38f, 0.35f);
            hpBgRt.offsetMin = Vector2.zero;
            hpBgRt.offsetMax = Vector2.zero;
            Image hpBgImg = GetOrAddComponent<Image>(hpBg);
            hpBgImg.color = new Color(0.18f, 0.05f, 0.05f, 0.85f);

            GameObject hpFill = GetOrCreateUIChild(hpBg, "Player_HP_Fill");
            RectTransform hpFillRt = GetOrAddComponent<RectTransform>(hpFill);
            hpFillRt.anchorMin = Vector2.zero;
            hpFillRt.anchorMax = Vector2.one;
            hpFillRt.offsetMin = Vector2.zero;
            hpFillRt.offsetMax = Vector2.zero;
            Image hpFillImg = GetOrAddComponent<Image>(hpFill);
            hpFillImg.sprite = whiteSprite;
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.color = new Color(0.2f, 0.95f, 0.4f, 1f);
            hpFillImg.fillAmount = 1f;

            hud.SetupReferences(xpFillImg, lvlTxt, tmrTxt, kllTxt, hpFillImg);

            // ==================== CONSTRUÇÃO DO LEVEL UP PANEL ====================
            Transform lvlPanelTr = canvasObj.transform.Find("LevelUp_Panel");
            GameObject lvlPanel = lvlPanelTr != null ? lvlPanelTr.gameObject : new GameObject("LevelUp_Panel", typeof(RectTransform));
            lvlPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform lvlPanelRt = GetOrAddComponent<RectTransform>(lvlPanel);
            lvlPanelRt.anchorMin = Vector2.zero;
            lvlPanelRt.anchorMax = Vector2.one;
            lvlPanelRt.offsetMin = Vector2.zero;
            lvlPanelRt.offsetMax = Vector2.zero;
            Image lvlPanelImg = GetOrAddComponent<Image>(lvlPanel);
            lvlPanelImg.color = new Color(0.03f, 0.04f, 0.07f, 0.96f);

            // Header Title
            GameObject titleObj = GetOrCreateUIChild(lvlPanel, "Panel_Title");
            RectTransform titleRt = GetOrAddComponent<RectTransform>(titleObj);
            titleRt.anchorMin = new Vector2(0.1f, 0.82f);
            titleRt.anchorMax = new Vector2(0.9f, 0.94f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            Text titleTxt = GetOrAddComponent<Text>(titleObj);
            titleTxt.font = defaultFont;
            titleTxt.fontSize = 32;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = new Color(0.1f, 0.95f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "ANOMALIA ESTABILIZADA";

            // Subtitle
            GameObject subObj = GetOrCreateUIChild(lvlPanel, "Panel_Subtitle");
            RectTransform subRt = GetOrAddComponent<RectTransform>(subObj);
            subRt.anchorMin = new Vector2(0.1f, 0.76f);
            subRt.anchorMax = new Vector2(0.9f, 0.82f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            Text subTxt = GetOrAddComponent<Text>(subObj);
            subTxt.font = defaultFont;
            subTxt.fontSize = 18;
            subTxt.color = new Color(0.7f, 0.75f, 0.85f);
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.text = "Selecione uma melhoria quântica para continuar a sobrevivência";

            // 3 Cartões de Upgrade
            LevelUpUI.UpgradeCardUI[] cards = new LevelUpUI.UpgradeCardUI[3];
            Vector2[] cardMinAnchors = { new Vector2(0.11f, 0.22f), new Vector2(0.38f, 0.22f), new Vector2(0.65f, 0.22f) };
            Vector2[] cardMaxAnchors = { new Vector2(0.35f, 0.72f), new Vector2(0.62f, 0.72f), new Vector2(0.89f, 0.72f) };

            for (int i = 0; i < 3; i++)
            {
                cards[i] = CreateCardUI(lvlPanel, $"UpgradeCard_{i + 1}", cardMinAnchors[i], cardMaxAnchors[i], defaultFont);
            }

            levelUpUI.SetupCards(lvlPanel, cards);

            // Desativa painel de level up por padrão
            lvlPanel.SetActive(false);

            EditorUtility.SetDirty(canvasObj);
        }

        private static LevelUpUI.UpgradeCardUI CreateCardUI(GameObject parent, string name, Vector2 minAnchor, Vector2 maxAnchor, Font font)
        {
            GameObject cardObj = GetOrCreateUIChild(parent, name);
            RectTransform rt = GetOrAddComponent<RectTransform>(cardObj);
            rt.anchorMin = minAnchor;
            rt.anchorMax = maxAnchor;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = GetOrAddComponent<Image>(cardObj);
            bg.color = new Color(0.09f, 0.11f, 0.17f, 0.98f);

            // Borda de realce no topo do cartão
            GameObject accent = GetOrCreateUIChild(cardObj, "AccentLine");
            RectTransform accentRt = GetOrAddComponent<RectTransform>(accent);
            accentRt.anchorMin = new Vector2(0f, 0.97f);
            accentRt.anchorMax = new Vector2(1f, 1f);
            accentRt.offsetMin = Vector2.zero;
            accentRt.offsetMax = Vector2.zero;
            Image accentImg = GetOrAddComponent<Image>(accent);
            accentImg.color = new Color(0.1f, 0.85f, 1f);

            // Card Title Text
            GameObject titleObj = GetOrCreateUIChild(cardObj, "Card_Title");
            RectTransform titleRt = GetOrAddComponent<RectTransform>(titleObj);
            titleRt.anchorMin = new Vector2(0.06f, 0.72f);
            titleRt.anchorMax = new Vector2(0.94f, 0.94f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;
            Text titleTxt = GetOrAddComponent<Text>(titleObj);
            titleTxt.font = font;
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.color = Color.white;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "Upgrade Title";

            // Card Description Text
            GameObject descObj = GetOrCreateUIChild(cardObj, "Card_Description");
            RectTransform descRt = GetOrAddComponent<RectTransform>(descObj);
            descRt.anchorMin = new Vector2(0.08f, 0.28f);
            descRt.anchorMax = new Vector2(0.92f, 0.70f);
            descRt.offsetMin = Vector2.zero;
            descRt.offsetMax = Vector2.zero;
            Text descTxt = GetOrAddComponent<Text>(descObj);
            descTxt.font = font;
            descTxt.fontSize = 17;
            descTxt.color = new Color(0.8f, 0.85f, 0.92f);
            descTxt.alignment = TextAnchor.MiddleCenter;
            descTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            descTxt.verticalOverflow = VerticalWrapMode.Truncate;
            descTxt.text = "Upgrade Description Details";

            // Select Button
            GameObject btnObj = GetOrCreateUIChild(cardObj, "Select_Button");
            RectTransform btnRt = GetOrAddComponent<RectTransform>(btnObj);
            btnRt.anchorMin = new Vector2(0.12f, 0.08f);
            btnRt.anchorMax = new Vector2(0.88f, 0.24f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            Image btnImg = GetOrAddComponent<Image>(btnObj);
            btnImg.color = new Color(0.0f, 0.55f, 0.85f);
            Button btn = GetOrAddComponent<Button>(btnObj);

            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.0f, 0.55f, 0.85f);
            colors.highlightedColor = new Color(0.2f, 0.75f, 1f);
            colors.pressedColor = new Color(0.0f, 0.4f, 0.65f);
            btn.colors = colors;

            // Button Text
            GameObject btnTxtObj = GetOrCreateUIChild(btnObj, "Text");
            RectTransform btnTxtRt = GetOrAddComponent<RectTransform>(btnTxtObj);
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;
            Text btnTxt = GetOrAddComponent<Text>(btnTxtObj);
            btnTxt.font = font;
            btnTxt.fontSize = 18;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.text = "SELECIONAR";

            return new LevelUpUI.UpgradeCardUI
            {
                cardRoot = cardObj,
                titleText = titleTxt,
                descriptionText = descTxt,
                selectButton = btn
            };
        }

        private static GameObject GetOrCreateUIChild(GameObject parent, string childName)
        {
            Transform t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;

            GameObject newChild = new GameObject(childName, typeof(RectTransform));
            newChild.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(newChild, $"Create {childName}");
            return newChild;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go.TryGetComponent<T>(out var comp))
            {
                return comp;
            }
            return Undo.AddComponent<T>(go);
        }

        private static Sprite GetOrCreateWhiteSprite()
        {
            Texture2D tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }

        private static Material GetOrCreateMaterial(string matName, Color color, bool emissive)
        {
            const string folderPath = "Assets/Art/Materials";
            EnsureFolderHierarchy("Assets", "Art");
            EnsureFolderHierarchy("Assets/Art", "Materials");

            string assetPath = $"{folderPath}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") 
                             ?? Shader.Find("Standard") 
                             ?? Shader.Find("Unlit/Color");

                mat = new Material(shader) { name = matName };
                ConfigureMaterialColors(mat, color, emissive);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                ConfigureMaterialColors(mat, color, emissive);
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }

        private static void ConfigureMaterialColors(Material mat, Color color, bool emissive)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
            mat.color = color;

            if (emissive && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 1.5f);
            }
        }

        private static void EnsureFolderHierarchy(string parent, string child)
        {
            string combined = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureTagExists(string tagName)
        {
            if (System.Array.IndexOf(UnityEditorInternal.InternalEditorUtility.tags, tagName) >= 0)
            {
                return;
            }

            Object[] tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (tagAssets == null || tagAssets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(tagAssets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
                if (t.stringValue.Equals(tagName)) return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
        }

        private static void SetupEndGameUI()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

            GameObject canvasObj = GameObject.Find("HUD_Canvas");
            if (canvasObj == null)
            {
                SetupHUDAndLevelUpUI();
                canvasObj = GameObject.Find("HUD_Canvas");
            }

            EndGameUI endGameUI = GetOrAddComponent<EndGameUI>(canvasObj);

            // 1. Painel de Fim de Jogo
            Transform panelTr = canvasObj.transform.Find("EndGame_Panel");
            GameObject panelObj = panelTr != null ? panelTr.gameObject : new GameObject("EndGame_Panel", typeof(RectTransform));
            panelObj.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRt = GetOrAddComponent<RectTransform>(panelObj);
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;

            Image panelBg = GetOrAddComponent<Image>(panelObj);
            panelBg.color = new Color(0.02f, 0.03f, 0.06f, 0.96f);

            // 2. Header Text ("VITÓRIA" / "GAME OVER")
            GameObject headerObj = GetOrCreateUIChild(panelObj, "Result_Header_Text");
            RectTransform headerRt = GetOrAddComponent<RectTransform>(headerObj);
            headerRt.anchorMin = new Vector2(0.1f, 0.72f);
            headerRt.anchorMax = new Vector2(0.9f, 0.88f);
            headerRt.offsetMin = Vector2.zero;
            headerRt.offsetMax = Vector2.zero;
            Text headerTxt = GetOrAddComponent<Text>(headerObj);
            headerTxt.font = defaultFont;
            headerTxt.fontSize = 46;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.text = "VITÓRIA";
            headerTxt.color = new Color(0.0f, 1.0f, 0.85f);

            // 3. Subtitle Text
            GameObject subObj = GetOrCreateUIChild(panelObj, "Result_Subtitle_Text");
            RectTransform subRt = GetOrAddComponent<RectTransform>(subObj);
            subRt.anchorMin = new Vector2(0.1f, 0.63f);
            subRt.anchorMax = new Vector2(0.9f, 0.71f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            Text subTxt = GetOrAddComponent<Text>(subObj);
            subTxt.font = defaultFont;
            subTxt.fontSize = 18;
            subTxt.alignment = TextAnchor.MiddleCenter;
            subTxt.text = "A anomalia espacial foi contida e o núcleo quântico estabilizou!";
            subTxt.color = new Color(0.75f, 0.8f, 0.9f);

            // 4. Survival Time Text
            GameObject timeObj = GetOrCreateUIChild(panelObj, "Survival_Time_Text");
            RectTransform timeRt = GetOrAddComponent<RectTransform>(timeObj);
            timeRt.anchorMin = new Vector2(0.15f, 0.48f);
            timeRt.anchorMax = new Vector2(0.85f, 0.58f);
            timeRt.offsetMin = Vector2.zero;
            timeRt.offsetMax = Vector2.zero;
            Text timeTxt = GetOrAddComponent<Text>(timeObj);
            timeTxt.font = defaultFont;
            timeTxt.fontSize = 24;
            timeTxt.fontStyle = FontStyle.Bold;
            timeTxt.alignment = TextAnchor.MiddleCenter;
            timeTxt.text = "Tempo Sobrevivido: 10:00";
            timeTxt.color = Color.white;

            // 5. Total Kills Text
            GameObject killsObj = GetOrCreateUIChild(panelObj, "Total_Kills_Text");
            RectTransform killsRt = GetOrAddComponent<RectTransform>(killsObj);
            killsRt.anchorMin = new Vector2(0.15f, 0.38f);
            killsRt.anchorMax = new Vector2(0.85f, 0.48f);
            killsRt.offsetMin = Vector2.zero;
            killsRt.offsetMax = Vector2.zero;
            Text killsTxt = GetOrAddComponent<Text>(killsObj);
            killsTxt.font = defaultFont;
            killsTxt.fontSize = 22;
            killsTxt.fontStyle = FontStyle.Bold;
            killsTxt.alignment = TextAnchor.MiddleCenter;
            killsTxt.text = "Inimigos Derrotados: 0";
            killsTxt.color = new Color(1.0f, 0.55f, 0.2f);

            // 6. Restart Button
            GameObject btnObj = GetOrCreateUIChild(panelObj, "Restart_Button");
            RectTransform btnRt = GetOrAddComponent<RectTransform>(btnObj);
            btnRt.anchorMin = new Vector2(0.35f, 0.20f);
            btnRt.anchorMax = new Vector2(0.65f, 0.32f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;

            Image btnImg = GetOrAddComponent<Image>(btnObj);
            btnImg.color = new Color(0.0f, 0.55f, 0.85f);
            Button restartBtn = GetOrAddComponent<Button>(btnObj);

            ColorBlock colors = restartBtn.colors;
            colors.normalColor = new Color(0.0f, 0.55f, 0.85f);
            colors.highlightedColor = new Color(0.15f, 0.8f, 1f);
            colors.pressedColor = new Color(0.0f, 0.4f, 0.65f);
            restartBtn.colors = colors;

            GameObject btnTxtObj = GetOrCreateUIChild(btnObj, "Text");
            RectTransform btnTxtRt = GetOrAddComponent<RectTransform>(btnTxtObj);
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;
            Text btnTxt = GetOrAddComponent<Text>(btnTxtObj);
            btnTxt.font = defaultFont;
            btnTxt.fontSize = 20;
            btnTxt.fontStyle = FontStyle.Bold;
            btnTxt.color = Color.white;
            btnTxt.alignment = TextAnchor.MiddleCenter;
            btnTxt.text = "JOGAR NOVAMENTE";

            endGameUI.SetupReferences(panelObj, headerTxt, subTxt, timeTxt, killsTxt, restartBtn);
            panelObj.SetActive(false);

            EditorUtility.SetDirty(canvasObj);
        }

        private static Mesh LoadMeshFromModel(string modelName)
        {
            string path = $"Assets/Art/Models/{modelName}.fbx";
            Mesh directMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (directMesh != null) return directMesh;

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (subAssets != null)
            {
                foreach (var sub in subAssets)
                {
                    if (sub is Mesh m && !m.name.StartsWith("__preview__"))
                    {
                        return m;
                    }
                }
            }
            return null;
        }
    }
}
#endif
