using Il2Cpp;
using MelonLoader;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections;
using System.Globalization;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.AI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MitaAI.Mita;
using MitaAI.PlayerControls;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Networking.Match;
using System.Text.Json;
using UnityEngine.Playables;


[assembly: MelonInfo(typeof(MitaAI.MitaCore), "MitaAI", "1.0.0", "Dmitry", null)]
[assembly: MelonGame("AIHASTO", "MiSideFull")]

namespace MitaAI
{
    public class MitaCore : MelonMod
    {
        // Ссылка на экземпляр MitaCore, если он нужен
        public static MitaCore Instance;
        private LogicCharacter characterLogic;
            
        public MitaCore()
        {
            Instance = this;
            Settings.Initialize();
        }
        // Метод, который будет вызываться после выполнения PrivateMethod
        
        public GameObject MitaObject;
        public GameObject MitaPersonObject;
        public MitaPerson Mita;

        public static GameObject CrazyObject;
        public static GameObject CreepyObject; // Объект для уродливой Миты
        public static GameObject CappyObject;
        public static GameObject KindObject;
        public static GameObject ShortHairObject;
        public static GameObject MilaObject; // Объект для нового персонажа
        public static GameObject SleepyObject; // Объект для нового персонажа

        Animator_FunctionsOverride MitaAnimatorFunctions;
        public Character_Look MitaLook;
        public static GameObject Console;
        static public GameObject getMitaByEnum(character character, bool getMitaPersonObject = false)
        {
            GameObject mitaObject;
            switch (character)
            {
                case character.Crazy:
                    mitaObject = CrazyObject;
                    break;
                case character.Kind:
                    mitaObject = KindObject;
                    break;
                case character.ShortHair:
                    mitaObject = ShortHairObject;
                    break;
                case character.Cappy:
                    mitaObject = CappyObject;
                    break;
                case character.Mila:
                    mitaObject = MilaObject;
                    break;
                case character.Sleepy:
                    mitaObject = SleepyObject;
                    break;
                case character.Creepy:
                    mitaObject = CreepyObject;
                    break;

                // Cartdiges
                case character.Cart_divan:
                    mitaObject = Console;
                    break;
                case character.Cart_portal:
                    mitaObject = Console;
                    break;


                default:
                    mitaObject = CrazyObject;
                    break;

            }
            if (getMitaPersonObject && !character.ToString().Contains("Cart") ) mitaObject = findMitaPersonObject(mitaObject);

            return mitaObject;

        }
        public static GameObject findMitaPersonObject(GameObject MitaObject)
        {
            GameObject MitaPersonObject = null;
            // Рекурсивно добавляем всех детей, если текущий объект не исключен
            int childCount = MitaObject.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = MitaObject.transform.GetChild(i);
                // Проверяем, содержит ли имя дочернего объекта подстроку "Mita Person"
                if (child != null && (child.name.Contains("Person") || child.name.Contains("Mita")))
                {
                    MelonLogger.Msg("found  MitaPersonObject");
                    MitaPersonObject = child.gameObject;
                    break; // Прерываем цикл, как только нашли первый подходящий объект
                }
            }
            return MitaPersonObject;
        }

        public void addChangeMita(GameObject NewMitaObject = null,character character = character.Crazy, bool ChangeAnimationControler = true, bool turnOfOld = true,bool changePosition = true,bool changeAnimation = true)
        {
            if (NewMitaObject == null)
            {
                NewMitaObject = getMitaByEnum(character);

            }

            MelonLogger.Msg($"Change Mita {currentCharacter} to {character} Begin");

            try
            {


                if (NewMitaObject == null)
                {
                    MelonLogger.Msg("New Mita Object is null!!!");
                    return;
                }
                
                if (turnOfOld)
                {
                    MitaObject.active = false;
                }
                
                currentCharacter = character;
                NewMitaObject.SetActive(true);
                MelonLogger.Msg("Change Mita activated her");
                MitaObject = NewMitaObject;

                MitaPersonObject =  findMitaPersonObject(MitaObject);



                if (MitaPersonObject == null)
                {
                    MelonLogger.Msg("Mita (Mita Person comp) is null");
                    MitaPersonObject = MitaObject.transform.Find("MitaPerson Future").gameObject;
                };

                if (changePosition) MitaPersonObject.transform.position = Vector3.zero;

                if (MitaPersonObject.GetComponent<Character>() == null)
                {
                    var comp = MitaPersonObject.AddComponent<Character>();
                    comp.init(character);
                }

                // Интеграция CharacterLogic
                Character characterComponent = MitaPersonObject.GetComponent<Character>();
                if (characterComponent == null)
                {
                    characterComponent.init(character); // Предполагается, что init инициализирует персонажа
                }

                MitaLook = MitaPersonObject.transform.Find("IKLifeCharacter").GetComponent<Character_Look>();

                if (MitaLook.forwardPerson == null)
                {
                    MitaLook.forwardPerson = MitaPersonObject.transform;
                }
                if (character == MitaCore.character.Creepy)
                {
                    LogicCharacter.Instance.Initialize(MitaPersonObject, character);
                }

                MelonLogger.Msg("333");

                MitaAnimatorFunctions = MitaPersonObject.GetComponent<Animator_FunctionsOverride>();
                Mita = MitaObject.GetComponent<MitaPerson>();


                if (Mita == null) { 

                    MelonLogger.Msg("MitaPersonObject is null");
                };
                MitaAnimationModded.mitaAnimatorFunctions = MitaAnimatorFunctions;
                var animator = MitaPersonObject.GetComponent<Animator>();
                MitaAnimationModded.animator = animator;


                MelonLogger.Msg($"AnimContr Status name {animator.runtimeAnimatorController.name}  count {animator.runtimeAnimatorController.animationClips.Length} ");

                try
                {
                    
                    var audioSource = MitaPersonObject.transform.Find("Armature/Hips/Spine/Chest/Neck2/Neck1/Head").GetComponent<AudioSource>();
                    AudioControl.dataValues_Sounds.audioSource = audioSource;
                    if (audioSource == null) MelonLogger.Msg("Audiosourse is null(((");
                    
                    // Получаем тип компонента
                    Type audioDialogueType = typeof(Il2Cpp.AudioDialogue);

                    // Получаем поле audioVoice через Reflection
                    FieldInfo audioVoiceField = audioDialogueType.GetField("audioVoice", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                    if (audioVoiceField != null)
                    {
                        // Получаем компонент AudioDialogue
                        var audioDialogue = AudioControl.MitaDualogueSpeak.GetComponent<Il2Cpp.AudioDialogue>();

                        // Устанавливаем значение поля через Reflection
                        audioVoiceField.SetValue(audioDialogue, audioSource);
                    }
                    else
                    {
                        Debug.LogError("Поле audioVoice не найдено!");
                    }

                }
                catch (Exception ex)
                {

                    MelonLogger.Msg($"5a0 {ex}");
                }
                MelonLogger.Msg($"AnimContr Status name {animator.runtimeAnimatorController.name}  count {animator.runtimeAnimatorController.animationClips.Length} ");

                if (ChangeAnimationControler)
                {
                    try
                    {

                        MitaAnimationModded.animator.runtimeAnimatorController = MitaAnimationModded.runtimeAnimatorController;
                        //AnimatorOverrideController AOC = MitaAnimationModded.animator.runtimeAnimatorController as AnimatorOverrideController;
                        //AOC.runtimeAnimatorController = MitaAnimationModded.runtimeAnimatorController;
                    }
                    catch (Exception ex)
                    {

                        MelonLogger.Msg($"5a {ex}");
                    }
                }

                try
                {
                    
                    
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"5a {ex}");
                }


                MelonLogger.Msg($"AnimContr Status name {animator.runtimeAnimatorController.name}  count {animator.runtimeAnimatorController.animationClips.Length} ");
                MelonLogger.Msg("666");
                //MitaAnimationModded.overrideController = MitaAnimationModded.runtimeAnimatorController;
                //location21_World.mitaTransform = MitaPersonObject.transform;
                //location21_World.
                try
                { 
                    Location34_Communication = MitaObject.GetComponentInChildren<Location34_Communication>();
                    if (Location34_Communication == null ) Location34_Communication = GameObject.Instantiate(Loc34_Template, MitaObject.transform).GetComponent< Location34_Communication>();
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"Location34_Communication set error: {ex}");
                }


                try
                {
                    Location34_Communication.play = true;
                    Location34_Communication.mitaAnimator = MitaPersonObject.GetComponent<Animator_FunctionsOverride>();
                    Location34_Communication.mita = Mita;

                    MitaAnimationModded.location34_Communication = Location34_Communication;
                    MitaAnimationModded.location34_Communication.mita = Mita;
                    MitaAnimationModded.location34_Communication.mitaCanWalk = true;
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"Loc setting set error: {ex}");
                }


                Settings.MitaType.Value = character;
                Settings.Save();
                MelonLogger.Msg($"AnimContr Status name {animator.runtimeAnimatorController.name}  count {animator.runtimeAnimatorController.animationClips.Length} ");
                
                if (!changeAnimation) MelonCoroutines.Start(walkingFix());

               
                MitaAnimationModded.init(MitaAnimatorFunctions, Location34_Communication, ChangeAnimationControler, changeAnimation);


                
     


                if (!changeAnimation) MitaAnimationModded.resetToIdleAnimation();

                MitaClothesModded.init_hair();
                MelonLogger.Msg($"AnimContr Status name {animator.runtimeAnimatorController.name}  count {animator.runtimeAnimatorController.animationClips.Length} ");




                Rigidbody rigidbody = MitaPersonObject.GetComponent<Rigidbody>();
                if (rigidbody == null)
                {
                    rigidbody = MitaPersonObject.AddComponent<Rigidbody>();
                    rigidbody.freezeRotation = true;
                    rigidbody.useGravity = false;
                    rigidbody.centerOfMass = new Vector3(0, 0.65f, 0);
                    rigidbody.mass = 2f;
                    rigidbody.maxAngularVelocity = 0.3f; //0.3 как и было
                    rigidbody.maxDepenetrationVelocity = 0.3f; //было до этого = 0.9f когда пробовал влад;
                    rigidbody.drag = 15;
                    rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; //ContinuousDynamic вместо Continuous для обработки динамических обьектов
                    rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                }
;

            }
            catch (Exception ex)
            {

                MelonLogger.Error("Mita change: ", ex);
            }

            MelonLogger.Msg("Change Mita Final");
        }
        IEnumerator walkingFix()
        {
            yield return new WaitForSeconds(7f);
            Mita.AiShraplyStop();
            //Mita.AiWalkToTarget(worldHouse);
        }



        public enum character
        {
            Player,
            None = -1,// Добавляем нового персонажа
            Crazy = 0,
            Cappy = 1,
            Kind = 2,
            Cart_portal = 3,
            ShortHair = 4,
            Cart_divan,
            Mila,
            Sleepy,
            Creepy,
            GameMaster

        }

        public character currentCharacter = character.Crazy;
        public enum MovementStyles
        {
            walkNear = 0,
            follow = 1,
            stay = 2,
            noclip = 3,
            layingOnTheFloorAsDead = 4,
            sittingAndCrying

        }
        public static MovementStyles movementStyle = MovementStyles.walkNear;
        enum MitaState
        {
            normal = 0,
            hunt = 1

        }
        MitaState mitaState = MitaState.normal;

        EmotionType currentEmotion = EmotionType.none;

        public GameObject knife;

        PlayerPerson playerPerson;
        public GameObject playerPersonObject;
        public GameObject playerObject;
        PlayerCameraEffects playerEffects;
        GameObject playerEffectsObject;
        public GameObject playerControllerObject;
        public GameController playerController;

        public GameController gameController;
        public static Text HintText;

        public GameObject cartridgeReader;

        public float distance = 0f;
        public string currentInfo = "";
        Location34_Communication Location34_Communication;
        Location21_World location21_World;

        public static Transform worldTogether;
        public static Transform worldHouse;
        public static Transform worldBasement;
        public static Transform world;
        public static Transform worldBackrooms2;



        const int simbolsPerSecond = 15;
        const float minDialoguePartLen = 0.50f;
        const float maxDialoguePartLen = 8f;
        const float delayModifier = 1.05f;

        static public Menu MainMenu;
        private GameObject CustomDialog;
        private GameObject CustomDialogPlayer;
        public static GameObject playerCamera;
        public GameObject AnimationKiller;
        public static BlackScreen blackScreen;



        private const float Interval = 0.35f;
        private float timer = 0f;


        Vector3 lastPosition;

        //Queue<string> waitForSoundsQ = new Queue<string>();
        string waitForSounds = "0";
        //private readonly object waitForSoundsLock = new object();

        public string playerMessage = "";
        public List<character> playerMessageCharacters = new List<character>();

        public Queue<(string,character)> systemMessages = new Queue<(string, character)>();
        Queue<(string, character)> systemInfos = new Queue<(string, character)>();

        Queue<string> patches_to_sound_file = new Queue<string>();
        string patch_to_sound_file = "";

        static Dictionary<int,string> sound_files = new Dictionary<int,string>();

        public string hierarchy = "-";

        static List<AnimationClip> MitaAnims = new List<AnimationClip>();
        
        static public Il2CppAssetBundle bundle;
        //static public Il2CppAssetBundle bundle2;

        static string requiredSceneName = "Scene 4 - StartSecret";
        public string requiredSave = "SaveGame startsecret";
        static string CurrentSceneName;

        public static bool isRequiredScene()
        {
            return CurrentSceneName == requiredSceneName;
        }


        static public bool AllLoaded = false;
        public static bool isAllLoadeed()
        {
            return AllLoaded;
        }




        private const float MitaBoringInterval = 90f;
        private float MitaBoringtimer = 0f;

        bool manekenGame = false;




        HarmonyLib.Harmony harmony;
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            characterLogic = LogicCharacter.Instance;
            harmony = new HarmonyLib.Harmony("1");
            MitaClothesModded.init(harmony);
            NetworkController.Initialize();
        }

        public override void OnLateInitializeMelon()
        {
            base.OnLateInitializeMelon();
            try
            {   
                bundle = AssetBundleLoader.initBundle();

                if (bundle == null)
                {
                    MelonLogger.Msg("AssetBundleLoader не прогрузился(" );
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg(e);
            }


        }


        public void sendSystem(string m,bool info, character character = character.None)
        {
            if (info) sendSystemInfo(m,character);
            else sendSystemMessage(m,character);

        }
        public void sendSystemMessage(string m,character character = character.None)
        {
            if (character == character.None) character = currentCharacter;
            systemMessages.Enqueue( (m, character) );

            EventsModded.regEvent();
        }
        public void sendSystemInfo(string m, character character = character.None)
        {

            if (character == character.None) character = currentCharacter;
            systemInfos.Enqueue((m, character));
        }

        public void playerKilled()
        {
          
            sendSystemMessage("Игрок был укушен манекеном. Манекен выключился (его можно перезапустить)");
            //playerPerson.transform.parent.position = GetRandomLoc().position;

            try
            {
                Component effectComponent = playerEffectsObject.GetComponentByName("Glitch");
                if (effectComponent is Il2CppObjectBase il2cppComponent)
                {
                    // Если это Il2CppObjectBase
                    LoggerInstance.Msg($"Il2Cpp component detected: {il2cppComponent.GetType().Name}");

                    // Проверяем, имеет ли компонент свойство enabled
                    var enabledProperty = il2cppComponent.GetType().GetProperty("enabled");
                    var behaviour = il2cppComponent.TryCast<Behaviour>();
                    behaviour.enabled = true;

                    // Запускаем корутину, передавая Il2Cpp-компонент
                    MelonCoroutines.Start(Utils.HandleIl2CppComponent(il2cppComponent, 5f));

                }


            }
            catch (Exception ex)
            {

                MelonLogger.Error(ex);
            }



        }
        public void playerClickSafe()
        {
            sendSystemMessage("Игрок кликает на кнопку твоего сейфа");
        }



        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            if (sceneName == requiredSceneName)
            {
                sendSystemInfo("Игрок покинул твой уровень");
                MitaCore.AllLoaded = false;
            }
            base.OnSceneWasUnloaded(buildIndex, sceneName);
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)

        {
            

            LoggerInstance.Msg("Scene loaded " + sceneName);
            if (!TotalInitialization.additiveLoadedScenes.Contains(sceneName))
            {
                LoggerInstance.Msg("Scene loaded not addictive" + sceneName);
                CurrentSceneName = sceneName;
            }
            else
            {
                LoggerInstance.Msg("Scene loaded addictive " + sceneName);
                return;
            }

            if (CurrentSceneName == "SceneMenu")
            {


                UINeuroMita.init();
                TotalInitialization.GetObjectsFromMenu();


            }

            else if (requiredSceneName == CurrentSceneName)
            {

                InitializeGameObjects();
            }
        }



        public enum Rooms
        {
            Kitchen = 0,
            MainHall = 1,
            Bedroom = 2,
            Toilet = 3,
            Basement = 4,
            Unknown = -1
        }

        public Rooms roomPlayer = Rooms.Unknown;
        public Rooms roomMita = Rooms.Unknown;

        public void CheckRoom(PlayerMove playerMove)
        {
            if (playerMove == null) return;
            roomPlayer = (Rooms)GetRoomID(playerMove.transform);
        }

        public Rooms GetRoomID(Transform playerTransform)
        {
            if (playerTransform == null)
                return Rooms.Unknown;

            Vector3 position = playerTransform.position;
            float posX = position.x;
            float posZ = position.z;
            float posY = position.y;

            if (Utils.getDistanceBetweenObjects(worldHouse.gameObject,playerPersonObject)>50f) return Rooms.Unknown;

            if (posY <= -0.1f)
                return Rooms.Basement;

            if (posX > 5.3f)
                return posZ >= 0 ? Rooms.Kitchen : Rooms.Bedroom;

            if (posX > -4f && posX < 5f)
                return Rooms.MainHall;

            if (posX > -11.0f && posX < -4.3f)
                return Rooms.Toilet;

            return Rooms.Unknown;
        }

        GameObject Loc34_Template;


        private void InitializeGameObjects()
        {

            GameObject Trigger = GameObject.Find("World/Quests/Quest 1/Trigger Entry Kitchen");
            Trigger.SetActive(false);


            GameObject Location34_CommunicationObject = GameObject.Find("World/Quests/Quest 1/Addon");
            Location34_Communication = GameObject.Find("World/Quests/Quest 1/Addon").GetComponent<Location34_Communication>();

            Location34_Communication.mitaCanWalk = true;
            Location34_Communication.indexSwitchAnimation = 1;
            Location34_Communication.play = true;

            CollectChildObjects(Location34_CommunicationObject);

            Loc34_Template = Location34_Communication.gameObject;

            Mita = GameObject.Find("Mita")?.GetComponent<MitaPerson>();
            MitaObject = GameObject.Find("Mita").gameObject;
            
            MitaPersonObject = MitaObject.transform.Find("MitaPerson Mita").gameObject;
            CrazyObject = MitaObject;

            Location34_Communication.transform.SetParent(CrazyObject.transform);


            var comp = MitaPersonObject.AddComponent<Character>();
            comp.init(MitaCore.character.Crazy);

            currentCharacter = character.Crazy;

            MitaLook = MitaObject.transform.Find("MitaPerson Mita/IKLifeCharacter").gameObject.GetComponent<Character_Look>();
            MitaAnimatorFunctions = MitaPersonObject.GetComponent<Animator_FunctionsOverride>();
            MitaAnimationModded.init(MitaAnimatorFunctions, Location34_Communication);
            Mita.AiShraplyStop();

            //GameObject eyeObject = Utils.TryfindChild(MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Neck2/Neck1/Head/Right Eye");
            //eyeModifier = new EyeGlowModifier(eyeObject);
            //eyeObject = Utils.TryfindChild(MitaPersonObject.transform, "Armature/Hips/Spine/Chest/Neck2/Neck1/Head/Left Eye");
            //eyeModifier = new EyeGlowModifier(eyeObject);

            //Mita.gameObject.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

            playerPerson = GameObject.Find("Person")?.GetComponent<PlayerPerson>();
            playerPersonObject = playerPerson.gameObject;
            playerObject = playerPerson.transform.parent.gameObject;
            playerControllerObject = playerObject.transform.parent.gameObject;
            playerController = playerControllerObject.GetComponent<GameController>();

            playerObject.GetComponent<PlayerMove>().speedPlayer = 1f;
            playerObject.GetComponent<PlayerMove>().canRun = true;

            if (playerPersonObject.GetComponent<AudioSource>() == null) AudioControl.playerAudioSource = playerPersonObject.AddComponent<AudioSource>();

            // Отключить если нужно
            Character GM = playerPersonObject.AddComponent<Character>();
            GM.init_GameMaster();
            GM.enabled = false;



            playerEffects = playerPerson.transform.parent.Find("HeadPlayer/MainCamera").gameObject.GetComponent<PlayerCameraEffects>();
            playerEffectsObject = playerPerson.transform.parent.Find("HeadPlayer/MainCamera/CameraPersons").gameObject;

            Text HintText =  GameObject.Find("GameController/Interface/HintScreen/Text").GetComponent<Text>();

            blackScreen = GameObject.Find("Game/Interface/BlackScreen").GetComponent<BlackScreen>();
            try
            {
                playerCamera = playerPerson.transform.parent.gameObject.transform.FindChild("HeadPlayer/MainCamera").gameObject;
                LoggerInstance.Msg("Camera found" + playerCamera.name);
            }
            catch (Exception)
            {

                LoggerInstance.Msg("Camera not LoggerInstance.Msg(\"Camera found\" + playerCamera.name);found" + playerCamera.name);
            }



            LoggerInstance.Msg(Mita != null ? "Mita found!" : "Mita not found.");
            LoggerInstance.Msg(playerPerson != null ? "Player found!" : "Player not found.");

            if (Mita == null || playerPerson == null) return;


            CommandProcessor.Initialize(this, playerObject.transform,MitaObject.transform,Location34_Communication);

                
            worldHouse = GameObject.Find("World")?.transform;
            World worldSettings = worldHouse.gameObject.GetComponent<World>();
            MitaObject.transform.SetParent(worldHouse);
            location21_World = worldHouse.gameObject.AddComponent<Location21_World>();

            PlayerAnimationModded.Init(playerObject, worldHouse, playerObject.GetComponent<PlayerMove>());
            LightingAndDaytime.Init(location21_World, worldHouse);
            ShaderReplacer.init();


            MelonCoroutines.Start(StartDayTime());
            //MelonCoroutines.Start(UpdateLighitng());




            try
            {
                AudioControl.Init(worldHouse);
            }
            catch (Exception ex) { LoggerInstance.Error(ex); }

            worldSettings.limitFloor = -200f;
            if (worldHouse == null)
            {
                LoggerInstance.Msg("World object not found.");

            }


            Transform dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Mita/Dialogue Finish Aihastion/DMita 2");

            if (dialogOriginal == null)
            {
                LoggerInstance.Msg("Target object 'DMita 2' not found.");
                dialogOriginal = worldHouse.Find("Quests/Quest 1 Start/3D Text 5");

                if (dialogOriginal == null)
                {

                    LoggerInstance.Msg("Target object '3D Text 5' not found.");
                }

            }

            CustomDialog = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialog.name = "Custom Dialogue Mita";

            // Опускаем объект CustomDialog на 200 единиц по оси Y
            Vector3 newPosition = CustomDialog.transform.position; // Получаем текущую позицию
            newPosition.y -= 200; // Уменьшаем Y на 200
            CustomDialog.transform.position = newPosition; // Применяем новую позицию


            Dialogue_3DText CustomDialogText = CustomDialog.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;

            dialogOriginal = worldHouse.Find("Quests/Quest 1/Dialogues/Dialogue Player/Dialogue Hello/DPlayer 3");
            CustomDialogPlayer = GameObject.Instantiate(dialogOriginal.gameObject, worldHouse.Find("Quests/Quest 1/Dialogues"));
            CustomDialogPlayer.name = "Custom Dialogue Player";

            CustomDialogText = CustomDialogPlayer.GetComponent<Dialogue_3DText>();

            CustomDialogText.nextText = null;
            CustomDialogText.sizeHeight = 0.0687f;
            CustomDialogText.sizeSymbol = 0.0014f;
            CustomDialogText.sizeWidth = 0.75f;
            CustomDialogText.xPrint = 0.413f;
            CustomDialogText.indexString = -1;
            CustomDialogText.showSubtitles = true;

            MelonLogger.Msg($"Attempt Interactions before");
            try
            {
                Interactions.init();
            }
            catch (Exception)
            {

                throw;
            }
           
            
            //MelonLogger.Msg($"Attempt after");
            MelonLogger.Msg($"Attempt Interactions end");
            try
            {
                MelonCoroutines.Start( TotalInitialization.AddOtherScenes() );
            }
            catch (Exception e)
            {
                MelonLogger.Error(e);
            }

      

            //Interactions.Test(GameObject.Find("Table"));
            MelonCoroutines.Start(RealTimer());




            
            //TestBigMita();
        }
        void TestBigMita()
        {
            LoggerInstance.Msg("Start TestBigMita");
            MitaObject.transform.FindChild("MitaPerson Mita").localScale = new Vector3(15f,15f,15f);
            
            Vector3 direction = (MitaObject.transform.position - playerObject.transform.position).normalized;

            MitaObject.transform.SetPositionAndRotation(new Vector3(15f, 0f, 15f), Quaternion.LookRotation(direction));

            try
            {
                GameObject floor = GameObject.Instantiate(Utils.TryfindChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom/FloorBedroom").gameObject);
                floor.transform.localScale = new Vector3(30f, 1, 30f);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg("TestBigMita end " + ex);
            }
            
            worldHouse.Find("House").gameObject.SetActive(false);
            worldBasement.Find("House").gameObject.SetActive(false);

        }





        public static void DeleteChildren(GameObject parent)
        {
            if (parent == null)
            {
                MelonLoader.MelonLogger.Error("Parent object is null.");
                return;
            }

            // Создаем временный массив, чтобы избежать изменения итерируемой коллекции
            Transform[] children = new Transform[parent.transform.childCount];
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                children[i] = parent.transform.GetChild(i);
            }

            // Удаляем каждого ребенка
            foreach (Transform child in children)
            {
                GameObject.Destroy(child.gameObject);
            }

            MelonLoader.MelonLogger.Msg($"All children of {parent.name} have been deleted.");
        }


        bool FirstTime = true;

        public IEnumerator RealTimer()
        {
            while (true)
            {
                // Проверяем условия
                if ( !isRequiredScene() )
                {
                    yield break;

                }

                if (!AllLoaded)
                {
                    yield return null; // Пропускаем итерацию, если условия не выполнены
                    continue;// Пропускаем итерацию, если условия не выполнены
                }

                // Обновляем таймеры
                timer += Time.deltaTime;
                MitaBoringtimer += Time.deltaTime;

                // Проверяем, достиг ли timer значения Interval
                if (timer >= Interval)
                {
                    timer = 0f; // Сбрасываем таймер
                    yield return HandleDialogue(); // Запускаем HandleDialogue и ждем его завершения
                }

                yield return null; // Ждем следующего кадра
            }
        }

        private IEnumerator UpdateLighitng()
        {
            yield return new WaitForSeconds(1f);
            while (true)
            {
                try
                {
                    LightingAndDaytime.CheckDay();

                }
                catch (Exception e)
                {

                    LoggerInstance.Msg("Error LightingAndDaytime CheckDay" + e);
                }
                yield return new WaitForSeconds(2.3f); // Ждем 7 секунд перед следующим циклом
            }
        }

        private IEnumerator StartDayTime()
        {

            yield return new WaitForSeconds(1f);
            LightingAndDaytime.setTimeDay(0.5f);
        }








        public float getDistanceToPlayer()
        {
            if (MitaPersonObject == null || playerObject == null) { return 0f; }
            return Utils.getDistanceBetweenObjects(MitaPersonObject, playerObject);


        }

        private float lastActionTime = -Mathf.Infinity;  // Для отслеживания времени последнего действия
        private const float actionCooldown = 8f;  // Интервал в секундах
        private IEnumerator HandleDialogue()
        {
            //MelonLogger.Msg("HandleDialogue");

            string playerText = playerMessage;
            playerMessage = "";
            string dataToSent = "waiting";
            string dataToSentSystem = "-";
            string info = "-";
            character characterToWas = character.None;
            character characterToSend = currentCharacter;
            List<character> Characters = playerMessageCharacters;


            float currentTime = Time.unscaledTime;
            if (currentTime - lastActionTime > actionCooldown)
            {
                //MelonLogger.Msg("Ready to send");
                if (playerText != "")
                {
                    LoggerInstance.Msg("HAS playerMessage");


                    if (Characters.Count > 0) { 
                            if (Characters.First().ToString().Contains("Cart"))
                            {
                                characterToSend = Characters.First();
                            }
                            else
                            {
                                MitaBoringtimer = 0f;
                            }


                        sendInfoListeners(playerText, Characters, characterToSend,null);

                    }


                    dataToSent = playerText;
                    
                    lastActionTime = Time.unscaledTime;
                }
                else if (systemMessages.Count > 0)
                {
                    LoggerInstance.Msg("HAS SYSTEM MESSAGES");
                    MitaBoringtimer = 0f;
                  

                    //Отправляю залпом.
                    while (systemMessages.Count() > 0)
                    {
                        var message = systemMessages.Dequeue();
                        dataToSentSystem += message.Item1 + "\n";
                        characterToSend = message.Item2;
                        if (characterToWas == character.None || characterToWas == characterToSend)
                        {
                            characterToWas = message.Item2;
                        }
                        else
                        {
                            sendSystemMessage(message.Item1, characterToSend);
                            break;
                        }

                    }


                    lastActionTime = Time.unscaledTime;

                }
                else if (MitaBoringtimer >= MitaBoringInterval && mitaState == MitaState.normal)
                {
                    MitaBoringtimer = 0f;
                    dataToSentSystem = "Player did nothing for 90 seconds";
                    lastActionTime = Time.unscaledTime;
                }
            }



            string response = "";

            if (systemInfos.Count > 0)
            {
                //LoggerInstance.Msg("HAS SYSTEM INFOS");
                //Отправляю залпом.
                while (systemInfos.Count() > 0)
                {
                    var message = systemInfos.Dequeue();
                    character ch = message.Item2;

                    if (ch == characterToSend)
                    {
                        info += message.Item1 + "\n";
                    }
                    else
                    {
                        sendSystemInfo(message.Item1, ch);
                        break;
                    }
                }

            }
            if (characterToSend != currentCharacter)
            {
                if (characterToSend != character.GameMaster)
                {
                    addChangeMita(getMitaByEnum(characterToSend), characterToSend, false, false, false, false);
                }
                else
                {
                    currentCharacter = characterToSend;
                }
            }

            if (dataToSent != "waiting" || dataToSentSystem != "-") prepareForSend();

            
            Task<Dictionary<string, JsonElement>> responseTask = NetworkController.GetResponseFromPythonSocketAsync(dataToSent, dataToSentSystem, info, characterToSend);



            float timeout = 40f;     // Лимит времени ожидания
            float waitMessageTimer = 0.5f;
            float elapsedTime = 0f; // Счетчик времени
            float lastCallTime = 0f; // Время последнего вызова

            
            while (!responseTask.IsCompleted)
            {
                elapsedTime += 0.1f;
                if (elapsedTime >= timeout)
                {
                    MelonLogger.Msg("Too long waiting for text");
                    break;
                }

                //MelonLogger.Msg($"!responseTask.IsCompleted{elapsedTime}/{timeout}");
                if (elapsedTime - lastCallTime >= waitMessageTimer)
                {
                    try
                    {
                        List<String> parts = new List<String> { "..." };
                        MelonCoroutines.Start(ShowDialoguesSequentially(parts, true));
                        lastCallTime = elapsedTime; // Обновляем время последнего вызова
                    }
                    catch (Exception ex)
                    {

                        MelonLogger.Msg(ex);
                    }

                }
                yield return new WaitForSecondsRealtime(0.1f);
            }

            string patch = null;
            bool GM_ON = false;
            bool GM_READ = false;
            bool GM_VOICE = false;
            int id = 0;
            if (responseTask.IsCompleted)
            {
                Dictionary<string, JsonElement> messageData2 = responseTask.Result;
                try
                {

                    id = messageData2["id"].GetInt32();
                    string type = messageData2["type"].GetString();

                    string new_character = messageData2["character"].GetString();
                    response = messageData2["response"].GetString();
                    bool connectedToSilero = messageData2["silero"].GetBoolean();

                    int idSound = messageData2["id_sound"].GetInt32();
                    patch = messageData2.ContainsKey("patch_to_sound_file") ? messageData2["patch_to_sound_file"].GetString() : "";
                    string user_input = messageData2.ContainsKey("user_input") ? messageData2["user_input"].GetString() : "";

                    GM_ON = messageData2.ContainsKey("GM_ON") ? messageData2["GM_ON"].GetBoolean() : false;
                    GM_READ = messageData2.ContainsKey("GM_READ") ? messageData2["GM_READ"].GetBoolean() : false;
                    GM_VOICE = messageData2.ContainsKey("GM_VOICE") ? messageData2["GM_VOICE"].GetBoolean() : false;
                    int GM_REPEAT = messageData2.ContainsKey("GM_REPEAT") ? messageData2["GM_REPEAT"].GetInt32() : 2;

                    int limitmod = messageData2.ContainsKey("CC_Limit_mod") ? messageData2["CC_Limit_mod"].GetInt32() : 100;


                    if (!string.IsNullOrEmpty(patch)) sound_files[idSound] = patch;

                    if (CharacterControl.gameMaster != null)
                    {
                        CharacterControl.gameMaster.timingEach = GM_REPEAT;
                        CharacterControl.gameMaster.enabled = GM_ON;
                        NetworkController.connectedToSilero = connectedToSilero;

                    }
                    CharacterControl.limitMod = limitmod;
                    if (!string.IsNullOrEmpty(user_input)) InputControl.UpdateInput(user_input);
                }
                catch (Exception ex)
                {

                    MelonLogger.Error(ex);
                }

            }
            else
            {
                response = "Too long waited for text from python";
                NetworkController.connectedToSilero = false;
            }

            
            
            if (!string.IsNullOrEmpty(patch)) patches_to_sound_file.Enqueue(patch);
            if (response != "")
            {
                LoggerInstance.Msg($"after GetResponseFromPythonSocketAsync char {characterToSend} {GM_READ} {GM_VOICE}");

                if (characterToSend.ToString().Contains("Cart")) MelonCoroutines.Start(DisplayResponseAndEmotionCoroutine(id,response, AudioControl.cartAudioSource));
                else if (characterToSend == character.GameMaster) {
                    if (GM_READ) MelonCoroutines.Start(DisplayResponseAndEmotionCoroutine(id,response, AudioControl.playerAudioSource, GM_VOICE));
                }
                else MelonCoroutines.Start(DisplayResponseAndEmotionCoroutine(id,response));

                if (characterToSend != character.GameMaster) sendInfoListeners(Utils.CleanFromTags(response), Characters, characterToSend, CharacterControl.extendCharsString(characterToSend));
                else sendInfoListenersFromGm(Utils.CleanFromTags(response), Characters, characterToSend);


                //Тестово - хочешь чтобы было без лишнего отрубай это

                if (playerText != "") characterToSend = character.Player;
                MelonCoroutines.Start(testNextAswer(response, characterToSend));



            }
            


        }

        IEnumerator testNextAswer(string response, character currentCharacter)
        {
            yield return new WaitForSeconds(0.25f);
            while (dialogActive)
            {
                yield return null;
            }

            CharacterControl.nextAnswer(Utils.CleanFromTags(response), currentCharacter);
        }


        public void sendInfoListeners(string message,List<character> characters = null, character exluding = character.None, string from = "Игрок")
        {
            MelonLogger.Msg($"sendInfoListeners char {characters} exl {exluding} from {from}");

            if ( characters == null ) characters = CharacterControl.GetCharactersToAnswer();

            if ( exluding == character.None ) exluding = currentCharacter;


            string charName = CharacterControl.extendCharsString(exluding);

            if (CharacterControl.gameMaster != null) characters.Add(character.GameMaster);
            //characters.Remove(exluding);

            foreach (character character in characters)
            {
                string speakersText = CharacterControl.getSpeakersInfo(character);

                if (character != exluding)
                {
                    string messageToListener = "";
                    messageToListener += speakersText;

                    messageToListener += $"[SPEAKER] : {from} said: {message} and was answered by {charName}";
                    sendSystemInfo(messageToListener, character );
                }
            }
 

        }
        public void sendInfoListenersFromGm(string message, List<character> characters = null, character exluding = character.None)
        {
            if (characters == null) characters = CharacterControl.GetCharactersToAnswer();

            if (exluding == character.None) exluding = currentCharacter;
            character from = character.GameMaster;

            string charName = CharacterControl.extendCharsString(exluding);




            foreach (character character in characters)
            {
                string speakersText = CharacterControl.getSpeakersInfo(character);

                if (character != exluding)
                {
                    string messageToListener = "";
                    messageToListener += speakersText;

                    messageToListener += $"[GAME_MASTER] : {from} said: {message}";


                    sendSystemInfo(messageToListener, character);
                }
            }
        }


        public void prepareForSend()
        {

            if (currentCharacter == character.GameMaster)
            {
                currentInfo = formCurrentInfoGameMaster();
                return;
            }

            try
            {
                if (Vector3.Distance(Mita.transform.GetChild(0).position, lastPosition) > 2f)
                {

                        lastPosition = Mita.transform.GetChild(0).position;
                        List<string> excludedNames = new List<string> { "Hips", "Maneken" };
                        if (roomMita == Rooms.Basement) hierarchy = ObjectHierarchyHelper.GetObjectsInRadiusAsTree(Mita.gameObject, 10f, worldBasement.Find("House").transform, excludedNames);
                        else hierarchy = ObjectHierarchyHelper.GetObjectsInRadiusAsTree(Mita.gameObject, 10f, worldHouse.Find("House").transform, excludedNames);

                        //LoggerInstance.Msg(hierarchy);
                    

                }
                if (string.IsNullOrEmpty(hierarchy)) hierarchy = "-";

                distance = getDistanceToPlayer();
                roomPlayer = GetRoomID(playerPerson.transform);
                roomMita = GetRoomID(Mita.transform);

                try
                {
                    currentInfo = formCurrentInfo();
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"formCurrentInfo error {ex}");
                    currentInfo = "";
                }
                
            }
            catch (Exception ex)
            {

                MelonLogger.Error($"prepareForSend {ex}");
            }
           
        }

        // Глобальный список для хранения дочерних объектов
        List<GameObject> globalChildObjects = new List<GameObject>();

        // Функция для получения дочерних объектов и добавления их в глобальный список
        void CollectChildObjects(GameObject parentObject)
        {
            // Проверяем, что объект не null
            if (parentObject == null)
            {
                LoggerInstance.Error("Parent object is null!");
                return;
            }

            // Получаем Transform родительского объекта
            Transform parentTransform = parentObject.transform;

            // Перебираем всех детей
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform childTransform = parentTransform.GetChild(i);

                if (childTransform != null)
                {
                    // Добавляем дочерний объект в глобальный список
                    globalChildObjects.Add(childTransform.gameObject);

                    if (i == 0)
                    {
                        GameObject newPoint = GameObject.Instantiate(childTransform.gameObject,new Vector3(12.8382f,-2.9941f,-16.8005f),Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement 1";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");

                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(17.0068f, -2.9941f, -13.2256f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement 2";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");


                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(10.9679f, -2.9941f, -19.5763f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement Camera";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");

                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(19.6421f, -2.9941f, -14.9584f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Basement Safe";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");

                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(11.2978f, 0, -7.3997f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Enter_Basement";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");
                        
                        newPoint = GameObject.Instantiate(childTransform.gameObject, new Vector3(11.1936f, 0, -8.9503f), Quaternion.identity, childTransform.parent);
                        newPoint.name = "Point Leave_Basement";
                        globalChildObjects.Add(newPoint);
                        remakeArrayl34(Location34_Communication, newPoint, "b");



                    }

                }
            }
            
            // Выводим общее количество детей
            LoggerInstance.Msg($"Total children collected: {globalChildObjects.Count}");
        }

        
        public void remakeArrayl34(Location34_Communication Location34_Communication, GameObject newPoint, string room)
        {
            LoggerInstance.Msg($"Start Il2CppReferenceArray {Location34_Communication} 33 {newPoint} ");
            // Создаем новый массив с размером на 1 больше
            Il2CppReferenceArray<Location34_PositionForMita> newArray = new Il2CppReferenceArray<Location34_PositionForMita>(Location34_Communication.positionsForMita.Length + 1);
            LoggerInstance.Msg($" Il2CppReferenceArray222");
            // Копируем старые данные
            for (int i = 0; i < Location34_Communication.positionsForMita.Length; i++)
            {
                newArray[i] = Location34_Communication.positionsForMita[i];
            }

            LoggerInstance.Msg($" Il2CppReferenceArray333");
            // Добавляем новый элемент
            Location34_PositionForMita l = new Location34_PositionForMita();
            LoggerInstance.Msg($" Il2CppReferenceArray444");
            l.target = newPoint.transform;
            l.room = room;
            LoggerInstance.Msg($" Il2CppReferenceArray5");
            newArray[newArray.Length - 1] = l;

            Location34_Communication.positionsForMita = newArray;
            LoggerInstance.Msg($"End");
            
        }

        public string GetLocations()
        {
            MelonLogger.Msg("TryGetLocations");

            string message = "Objects names in <> for walk or teleport to";

            for (int i = 0; i < globalChildObjects.Count; i++)
            {
                if (globalChildObjects[i] == null) continue;

                if( globalChildObjects[i].name.Contains("Point") || globalChildObjects[i].name.Contains("Position")  )
                {

                    message += $"<{globalChildObjects[i].name}>";

                }

            }

            return message;

        }

        public Transform GetRandomLoc()
        {
            LoggerInstance.Msg("Before try Tring GetRandomLoc");
            try
            {
                LoggerInstance.Msg("Tring GetRandomLoc");
                // Проверяем, что список не пустой
                if (globalChildObjects == null || globalChildObjects.Count == 0)
                {
                    LoggerInstance.Error("globalChildObjects is null or empty!");
                    return null; // Возвращаем null, если список пуст
                }

                // Генерируем случайный индекс
                int randomIndex = UnityEngine.Random.Range(0, globalChildObjects.Count);

                // Получаем случайный объект по индексу
                GameObject randomObject = globalChildObjects[randomIndex];

                // Проверяем, что объект действительно существует и имеет компонент Transform
                if (randomObject == null)
                {
                    LoggerInstance.Error("Random object is null!");
                    return null; // Возвращаем null, если объект не найден
                }

                // Логируем имя объекта
                LoggerInstance.Msg($"Random object selected: {randomObject.name}");

                // Возвращаем компонент Transform
                return randomObject.transform;
            }
            catch (Exception)
            {
                LoggerInstance.Msg("Error with random loc");
                return null;
            }

        }
        bool dialogActive = false;
        private IEnumerator DisplayResponseAndEmotionCoroutine(int id,string response, AudioSource audioSource = null,bool voice = true)
        {
            while (dialogActive) { yield return null; }
            dialogActive = true;

            LoggerInstance.Msg("DisplayResponseAndEmotionCoroutine");
            

            // Пример кода, который будет выполняться на главном потоке
            yield return null; // Это нужно для того, чтобы выполнение произошло после завершения текущего кадра
           
            string patch_to_sound = "";
            
            if (voice)
            {

                float elapsedTime = 0f; // Счетчик времени
                float timeout = 30f;     // Лимит времени ожидания
                float waitingTimer = 0.75f;
                float lastCallTime = 0f; // Время последнего вызова
                
                // Ждем, пока patch_to_sound_file перестанет быть пустым или не истечет время ожидания
                while (string.IsNullOrEmpty(patch_to_sound) && elapsedTime < timeout && NetworkController.connectedToSilero) //&& waitForSounds=="1")
                {
                    //LoggerInstance.Msg("DisplayResponseAndEmotionCicle");
                    if (sound_files.ContainsKey(id))
                    {
                        patch_to_sound = sound_files[id];
                        sound_files[id] = null;
                        break;
                    }


                    if (elapsedTime - lastCallTime >= waitingTimer)
                    {
                        //MelonLogger.Msg($"!responseTask.IsCompleted{elapsedTime}/{timeout}");
                        List<String> parts = new List<String> { "***" };
                        MelonCoroutines.Start(ShowDialoguesSequentially(parts, true));
                        lastCallTime = elapsedTime; // Обновляем время последнего вызова
                    }

                    elapsedTime += 0.1f; // Увеличиваем счетчик времени

                    yield return new WaitForSecondsRealtime(0.1f);             // Пауза до следующего кадра
                }
            
                yield return null;
                // Если время ожидания истекло, можно выполнить какой-то fallback-лог
                if (string.IsNullOrEmpty(patch_to_sound))
                {
                    LoggerInstance.Msg("Timeout reached, patch_to_sound_file is still empty.");
                }
            }

            // После того как patch_to_sound_file стал не пустым, вызываем метод DisplayResponseAndEmotion
            yield return DisplayResponseAndEmotion(response, patch_to_sound, audioSource);

            dialogActive = false;
        }

        private IEnumerator DisplayResponseAndEmotion(string response, string patch_to_sound, AudioSource audioSource = null)
        {
            LoggerInstance.Msg("DisplayResponseAndEmotion");

    
                string modifiedResponse = SetMovementStyle(response);

                AudioClip audioClip = null;

                if (!string.IsNullOrEmpty(patch_to_sound))
                {
                    try
                    {
                        LoggerInstance.Msg("patch_to_sound not null");
                        audioClip = NetworkController.LoadAudioClipFromFileAsync(patch_to_sound).Result;
                        
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Error loading audio file: {ex.Message}");
                    }
                }

                float delay = modifiedResponse.Length / simbolsPerSecond;

                if (audioSource != null) PlaySound(audioClip, audioSource);
                else MelonCoroutines.Start(PlayMitaSound(delay, audioClip, modifiedResponse.Length));


                List<string> dialogueParts = SplitText(modifiedResponse, maxLength: 70);

                // Запуск диалогов последовательно, с использованием await или вложенных корутин
                yield return MelonCoroutines.Start(ShowDialoguesSequentially(dialogueParts, false));
            

        }

        private IEnumerator ShowDialoguesSequentially(List<string> dialogueParts, bool itIsWaitingDialogue)
        {
            InputControl.BlockInputField(true);
            foreach (string part in dialogueParts)
            {

                string partCleaned = Utils.CleanFromTags(part); // Очищаем от всех тегов
                float delay = Math.Clamp(partCleaned.Length / simbolsPerSecond, minDialoguePartLen, maxDialoguePartLen); 


                yield return MelonCoroutines.Start(ShowDialogue(part, delay, itIsWaitingDialogue));

                
            }
            if (!itIsWaitingDialogue && CommandProcessor.ContinueCounter > 0) CommandProcessor.ContinueCounter = CommandProcessor.ContinueCounter - 1;
            InputControl.BlockInputField(false);


            
        }


        private IEnumerator ShowDialogue(string part, float delay, bool itIsWaitingDialogue = false)
        {

           

            string modifiedPart = part;
            List<string> commands;
            EmotionType emotion = EmotionType.none;
            
            if (!itIsWaitingDialogue)
            {
                LoggerInstance.Msg("ShowDialogue");
                try
                {

                    LoggerInstance.Msg("Begin try:" + modifiedPart);
                    modifiedPart = SetFaceStyle(modifiedPart);
                    modifiedPart = MitaClothesModded.ProcessClothes(modifiedPart);
                    modifiedPart = ProcessPlayerEffects(modifiedPart);
                    modifiedPart = MitaAnimationModded.setAnimation(modifiedPart);
                    modifiedPart = AudioControl.ProcessMusic(modifiedPart);
                    modifiedPart = CommandProcessor.ProcesHint(modifiedPart);
                    (emotion, modifiedPart) = SetEmotionBasedOnResponse(modifiedPart);
                    LoggerInstance.Msg("After SetEmotionBasedOnResponse " + modifiedPart);

                    (commands, modifiedPart) = CommandProcessor.ExtractCommands(modifiedPart);
                    if (commands.Count > 0)
                    {
                        CommandProcessor.ProcessCommands(commands);
                    }
                    LoggerInstance.Msg("After ExtractCommands " + modifiedPart);
                    modifiedPart = Utils.CleanFromTags(modifiedPart);
                }
                catch (Exception ex)
                {
                    LoggerInstance.Error($"Error processing part of response: {ex.Message}");
                }
            }
            
            GameObject currentDialog = InstantiateDialog();

            Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();


            answer.textPrint = modifiedPart;
            changeTextColor(currentDialog);

            answer.timeShow = delay;
            answer.speaker = MitaPersonObject;

            if (modifiedPart!="***"&&modifiedPart!="...") MelonLogger.Msg($"Text is {answer.textPrint}");
            if (!itIsWaitingDialogue) addDialogueMemory(answer);
            if (emotion != EmotionType.none) answer.emotionFinish = emotion;
            currentEmotion = emotion;

            currentDialog.SetActive(true);  
            if ( !NetworkController.connectedToSilero && !itIsWaitingDialogue ) MelonCoroutines.Start(AudioControl.PlayTextAudio(part));

            yield return new WaitForSeconds(delay * delayModifier);
            //MelonLogger.Msg($"Deleting dialogue {currentDialog.name}");
            Utils.DestroyAfterTime(currentDialog, delay * 1.15f + 5f);

        }

        void changeTextColor(GameObject currentDialog)
        {
            if (currentCharacter == character.Crazy) return;

            Color characterColor = GetCharacterTextColor(currentCharacter);
            var textMesh = currentDialog.GetComponentInChildren<Text>();
            if (textMesh != null)
            {
                textMesh.color = characterColor;
            }
        }




    private IEnumerator PlayMitaSound(float delay, AudioClip audioClip, int len)
        {
            LoggerInstance.Msg("PlayMitaSound");



            // Если есть аудио, проигрываем его до начала текста
            if (audioClip != null)
            {
                GameObject currentDialog = InstantiateDialog();
                
                Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                LoggerInstance.Msg("Loading voice...");
                answer.timeSound = delay;
                answer.LoadVoice(audioClip);
                audioClip = null;
                MelonLogger.Msg($"Setting speaker {MitaPersonObject.name}");
                answer.speaker = MitaPersonObject;

                currentDialog.SetActive(true);

                yield return new WaitForSeconds(delay * 1.15f);
                //MelonLogger.Msg($"Deleting dialogue {currentDialog.name}");

                Utils.DestroyAfterTime(currentDialog, delay * 1.15f + 5f);

                
            }

            
            LoggerInstance.Msg("Dialogue part finished and destroyed.");
        }

        private void PlaySound(AudioClip audioClip,AudioSource audioSource)
        {
            LoggerInstance.Msg("PlaySound not Dialogue");

            audioSource.clip = audioClip;
            audioSource.Play();


        }

        public IEnumerator PlayerTalk(string text)
        {
            GameObject currentDialog = null;


            float delay = Math.Clamp(text.Length / simbolsPerSecond, minDialoguePartLen,maxDialoguePartLen);

            currentDialog = InstantiateDialog(false);
            if (currentDialog != null)
            {

                try
                {
                    Dialogue_3DText answer = currentDialog.GetComponent<Dialogue_3DText>();
                    if (answer == null)
                    {
                        throw new Exception("Dialogue_3DText component not found.");
                    }

                    answer.speaker = playerPerson.gameObject;
                    answer.textPrint = text;
                    MelonLogger.Msg($"Player Text is {answer.textPrint}");
                    answer.themeDialogue = Dialogue_3DText.Dialogue3DTheme.Player;
                    answer.timeShow = delay;
                    addDialogueMemory(answer);


                    currentDialog.SetActive(true);
                    MitaBoringtimer = 0f;
                }
                catch (Exception ex)
                {
                    LoggerInstance.Msg($"PlayerTalk: {ex.Message}");
                }
                
                yield return new WaitForSeconds(delay*1.15f);

                Utils.DestroyAfterTime(currentDialog, (delay * 1.15f) + 5f);



            }
            else
            {
                LoggerInstance.Msg("currentDialog is null.");
            }


        }
        // Добавляет диалог в историю
        private void addDialogueMemory(Dialogue_3DText dialogue_3DText)
        {
            TextDialogueMemory textDialogueMemory = new TextDialogueMemory();
            textDialogueMemory.text = dialogue_3DText.textPrint;
            if (dialogue_3DText.themeDialogue == Dialogue_3DText.Dialogue3DTheme.Mita)
            {
                Color characterColor = GetCharacterTextColor(currentCharacter);
                textDialogueMemory.clr = Color.white;
                textDialogueMemory.clr2 = characterColor;
                textDialogueMemory.clr1 = Color.white;
            }
            else
            {
                textDialogueMemory.clr = new Color(1f, 0.6f, 0f);
                textDialogueMemory.clr2 = new Color(1f, 0.6f, 0f);
                textDialogueMemory.clr1 = new Color(1f, 0.6f, 0f);
            }
            playerController.dialoguesMemory.Add(textDialogueMemory);
        }

        #region Hunting
        public void beginHunt()
        {
            try
            {
                LoggerInstance.Msg("beginHunt ");
                knife.SetActive(true);
                mitaState = MitaState.hunt;
                MelonCoroutines.Start(hunting());
                Location34_Communication.ActivationCanWalk(false);
                //MitaPersonObject.GetComponent<Animator_FunctionsOverride>().AnimationClipWalk(AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita RunWalkKnife")); //
                MitaAnimationModded.EnqueueAnimation("Mita TakeKnife_0");
                MitaAnimationModded.setIdleWalk("Mita WalkKnife");
                
                
                
            }
            catch (Exception ex)
            {

                LoggerInstance.Error("beginHunt " + ex);
            }
            
        }
        IEnumerator hunting()
        {
            float startTime = Time.unscaledTime; // Запоминаем время старта корутины
            float lastMessageTime = -45f; // Чтобы сообщение появилось сразу через 15 секунд

            yield return new WaitForSeconds(1f);

            while (mitaState == MitaState.hunt)
            {
                if (getDistanceToPlayer() > 1f)
                {
                    Mita.AiWalkToTarget(playerPerson.transform);
                }
                else
                {
                    try
                    {
                        MelonCoroutines.Start(ActivateAndDisableKiller(3));
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error(ex);
                    }

                    yield break;
                }

                // Вычисляем время с начала корутины
                float elapsedTime = Time.unscaledTime - startTime;

                // Каждые 15 секунд вызываем функцию (если прошло время)
                if (elapsedTime - lastMessageTime >= 45f)
                {
                    string message = $"Игрок жив уже {elapsedTime.ToString("F2")} секунд. Скажи что-нибудь короткое. ";
                    if (Mathf.FloorToInt(elapsedTime) % 60 == 0) message += "Может быть, пора усложнять игру... (Менять скорости или спавнить манекенов или применять эффекты)";
                    sendSystemMessage(message);

                    lastMessageTime = elapsedTime; // Обновляем время последнего вызова
                }

                yield return new WaitForSeconds(0.5f);
            }

        }
        public void endHunt()
        {
            //MitaPersonObject.GetComponent<Animator_FunctionsOverride>().AnimationClipWalk(AssetBundleLoader.LoadAnimationClipByName(bundle, "Mita Walk"));
            MitaAnimationModded.setIdleWalk("Mita Walk_1");
            knife.SetActive(false);
            movementStyle = MovementStyles.walkNear;
            Location34_Communication.ActivationCanWalk(true);
            mitaState = MitaState.normal;
            MitaSharplyStopTimed(0.5f);
        }

        #endregion

        private IEnumerator MitaSharplyStopTimed(float time)
        {
            yield return new WaitForSeconds(time);
            Mita.AiShraplyStop();
        }



        // Корутин для активации и деактивации объекта
        public IEnumerator ActivateAndDisableKiller(float delay)
        {
            MelonLogger.Msg("Player killed");

            if (AnimationKiller.transform.Find("PositionsKill").childCount > 0)
            {
                AnimationKiller.transform.Find("PositionsKill").GetChild(0).SetPositionAndRotation(playerPerson.transform.position, playerPerson.transform.rotation);
            }
            AnimationKiller.SetActive(true); // Включаем объект


            // Сохраняем исходную позицию и ориентацию Миты
            Vector3 originalPosition = MitaPersonObject.transform.position;
            Quaternion originalRotation = MitaPersonObject.transform.rotation;
            MitaPersonObject.transform.SetPositionAndRotation(new Vector3(500, 500, 500), Quaternion.identity);
            yield return new WaitForSeconds(0.1f);
            try
            {
                AnimationKiller.GetComponent<Location6_MitaKiller>().Kill(); // Вызываем метод Kill()
                endHunt();
            }
            catch (Exception e)
            {

                MelonLogger.Msg(e);
            }
            
            yield return new WaitForSecondsRealtime(delay);

            sendSystemMessage("You successfully killed the player using knife, and he respawned somewhere.");

            AnimationKiller.SetActive(false); // Включаем объект
            // Возвращаем Миту в исходное положение
            Utils.TryTurnChild(worldHouse, "House/HouseGameNormal Tamagotchi/HouseGame Tamagotchi/House/Bedroom", true);

            try
            {
                MitaPersonObject.transform.SetPositionAndRotation(originalPosition, originalRotation);
                Mita.AiShraplyStop();
            }
            catch (Exception)
            {

                throw;
            }

            PlayerAnimationModded.TurnHandAnim();

            if (AudioControl.getCurrrentMusic() != "Music 4 Tension")
            {
                MelonLogger.Msg("Need to turn of Tension");
                AudioControl.TurnAudio("Music 4 Tension", false);
                
                yield return new WaitForSecondsRealtime(2f);

                AudioControl.TurnAudio("Music 4 Tension", false);
            }

            

        }




        private List<string> SplitText(string text, int maxLength)
        {
            List<string> parts = new List<string>();
            Dictionary<string, string> placeholders = new Dictionary<string, string>();

            // Регулярное выражение для поиска служебных частей
            string pattern = @"<.*?>.*?</.*?>";
            int placeholderCounter = 0;

            // Заменяем служебные части на уникальные маркеры
            string processedText = Regex.Replace(text, pattern, match =>
            {
                string placeholder = $"@@{placeholderCounter}@@";
                placeholders[placeholder] = match.Value; // Сохраняем оригинальный текст
                placeholderCounter++;
                return placeholder;
            });

            // Разделяем по строкам
            string[] lines = processedText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Если длина строки меньше maxLength, добавляем её сразу
                if (line.Length <= maxLength)
                {
                    parts.Add(line.Trim());
                }
                else
                {
                    // Разделяем на предложения
                    string[] sentences = line.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    string currentPart = "";

                    foreach (string sentence in sentences)
                    {
                        string trimmedSentence = sentence.Trim();
                        if ((currentPart.Length + trimmedSentence.Length + 1) <= maxLength)
                        {
                            currentPart += (currentPart.Length > 0 ? " " : "") + trimmedSentence + ".";
                        }
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                            currentPart = trimmedSentence + ".";
                        }
                    }

                    // Добавляем оставшийся текст
                    if (!string.IsNullOrWhiteSpace(currentPart)) parts.Add(currentPart.Trim());
                }
            }

            // Восстанавливаем служебные части
            for (int i = 0; i < parts.Count; i++)
            {
                foreach (var placeholder in placeholders)
                {
                    parts[i] = parts[i].Replace(placeholder.Key, placeholder.Value);
                }
            }

            return parts;
        }

        public GameObject InstantiateDialog(bool Mita = true)
        {
            Transform world = GameObject.Find("World")?.transform;
            if (world == null)
            {
                LoggerInstance.Msg("World object not found.");
                return null;
            }
            if (Mita)
            {
                return GameObject.Instantiate(CustomDialog, world.Find("Quests/Quest 1/Dialogues"));
            }
            else
            {
                return GameObject.Instantiate(CustomDialogPlayer, world.Find("Quests/Quest 1/Dialogues"));
            }
        }

        private (EmotionType, string) SetEmotionBasedOnResponse(string response)
        {
            LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse: " + response);
            try
            {
                var (emotions, cleanedResponse) = ExtractEmotions(response);
                EmotionType emotion = EmotionType.none;
                if (emotions.Count > 0)
                {
                    Enum.TryParse<EmotionType>(emotions[0], true, out var result);

                    Mita.FaceEmotionType(result);
                    if (emotions.Count > 1)
                    {
                        Enum.TryParse<EmotionType>(emotions[1], true, out var result2);
                        emotion = result2;
                    }

                }
                LoggerInstance.Msg($"Inside SetEmotionBasedOnResponse end: " + cleanedResponse);
                return (emotion, cleanedResponse);
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error extracting emotion: {ex.Message}");
                //Mita.FaceEmotionType(GetRandomEmotion());
                return (EmotionType.none, response); // Если произошла ошибка, возвращаем исходный текст
            }
        }


        public static (List<string>, string) ExtractEmotions(string response)
        {

            MelonLogger.Msg($"Inside ExtractEmotions start: " + response);
            List<string> emotions = new List<string>();
            string pattern = @"<e>(.*?)</e>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    emotions.Add(match.Groups[1].Value);
                }
            }

            // Удаляем теги эмоций из текста
            string cleanedResponse = Regex.Replace(response, @"<e>.*?</e>", "");

            MelonLogger.Msg($"Inside ExtractEmotions end: " + cleanedResponse);
            return (emotions, cleanedResponse);

        }
        public string SetFaceStyle(string response)
        {

            // Регулярное выражение для извлечения эмоций
            string pattern = @"<f>(.*?)</f>";
            Match match = Regex.Match(response, pattern);

            string faceStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<f>.*?</f>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                faceStyle = match.Groups[1].Value;
            }

            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null || currentCharacter!=character.Crazy)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }

                // Устанавливаем лицо, если оно найдено
                switch (faceStyle)
                {
                    case "Смущаться":
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(1);
                        break;
                    case "Маска грусти":
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(2);
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with FaceStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }

        public string SetMovementStyle(string response)
        {
            // Регулярное выражение для извлечения эмоций
            string pattern = @"<m>(.*?)</m>";
            Match match = Regex.Match(response, pattern);

            string MovementStyle = string.Empty;
            string cleanedResponse = Regex.Replace(response, @"<m>.*?</m>", ""); // Очищаем от всех тегов

            if (match.Success)
            {
                // Если эмоция найдена, устанавливаем её в переменную faceStyle
                MovementStyle = match.Groups[1].Value;
            }
            try
            {
                // Проверка на наличие объекта Mita перед применением эмоции
                if (Mita == null || Mita.gameObject == null)
                {
                    LoggerInstance.Error("Mita object is null or Mita.gameObject is not active.");
                    return cleanedResponse; // Возвращаем faceStyle и очищенный текст
                }
                // Устанавливаем лицо, если оно найдено
                switch (MovementStyle)
                {
                    case "Следовать рядом с игроком":
                        movementStyle = MovementStyles.walkNear;
                        Location34_Communication.ActivationCanWalk(true);
                        break;
                    case "Следовать за игроком":
                        movementStyle = MovementStyles.follow;
                        Location34_Communication.ActivationCanWalk(false);
                        MelonCoroutines.Start(FollowPlayer());
                        MelonCoroutines.Start(LookOnPlayer());
                        break;
                    case "Стоять на месте":
                        MitaSetStaing();
                        break;
                    case "NoClip":
                        movementStyle = MovementStyles.noclip;
                        Location34_Communication.ActivationCanWalk(false);
                        MelonCoroutines.Start(FollowPlayerNoclip());
                        break;
                    default:
                        //Mita.FaceColorUpdate();
                        //Mita.FaceLayer(0);
                        break;
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Problem with SetMovementStyle: {ex.Message}");
            }

            // Возвращаем кортеж: лицо и очищенный текст
            return cleanedResponse;
        }
        public void MitaSetStaing()
        {
            movementStyle = MovementStyles.stay;
            Location34_Communication.ActivationCanWalk(false);
            MelonCoroutines.Start(LookOnPlayer());
        }


        public IEnumerator LookOnPlayer()
        {
            while (movementStyle != MovementStyles.walkNear)
            {
                if (!Mita.GetComponent<NavMeshAgent>().enabled) 
                {
                    try
                    {
                        MitaLook.LookOnPlayerAndRotate();
                    }
                    catch (Exception e)
                    {

                        MelonLogger.Msg(e);
                    }
                    //if (Utils.Random(6, 10))
                   
                    //else MitaLook.LookRandom();
                
                }
                yield return new WaitForSecondsRealtime(1);
            }
        }


        public IEnumerator FollowPlayer(float distance = 1f)
        {

            while (movementStyle == MovementStyles.follow)
            {
                if (getDistanceToPlayer() > distance)
                {
                    Mita.AiWalkToTarget(playerPersonObject.transform);
                    
                }
                else
                {
                    Mita.AiShraplyStop();
                    yield return new WaitForSeconds(2f);
                }

                yield return new WaitForSeconds(0.55f);
            }


        }
        public IEnumerator FollowPlayerNoclip(float distance = 1.1f)
        {
            MelonLogger.Msg("Begin noClip");
            MitaPersonObject.GetComponent<CapsuleCollider>().enabled = false;
            while (movementStyle == MovementStyles.noclip && getDistanceToPlayer() > distance)
            {

                yield return MelonCoroutines.Start(MoveToPositionNoClip(25));

                yield return new WaitForSeconds(1f);
            }
            MitaPersonObject.GetComponent<CapsuleCollider>().enabled = true;

        }
        private IEnumerator MoveToPositionNoClip(float speed)
        {
            while (movementStyle == MovementStyles.noclip && getDistanceToPlayer() > 0.9f)
            {
                Vector3 targetPosition = playerPerson.gameObject.transform.position;
                // Двигаем персонажа напрямую к цели (без учета препятствий)
                MitaPersonObject.transform.position = Vector3.MoveTowards(MitaPersonObject.transform.position, targetPosition, speed * Time.deltaTime);

                // Можно добавить поворот персонажа в направлении движения (опционально)
                Vector3 direction = (targetPosition - MitaPersonObject.transform.position).normalized;
                if (direction != Vector3.zero)
                    MitaPersonObject.transform.rotation = Quaternion.LookRotation(direction);

                yield return null; // Ждем следующий кадр
            }

            // Когда достигли цели
            Debug.Log("NoClip movement completed!");
        }

        private string ProcessPlayerEffects(string response)
        {
            LoggerInstance.Msg("Starting ProcessPlayerEffects...");

            List<string> effects = new List<string>();
            string pattern = @"<v>(.*?)</v>";
            MatchCollection matches = Regex.Matches(response, pattern);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    effects.Add(match.Groups[1].Value);
                    LoggerInstance.Msg($"Found effect tag: {match.Groups[1].Value}");
                }
            }

            string result = Regex.Replace(response, @"<v>.*?</v>", "");
            LoggerInstance.Msg("Removed effect tags from response.");

            try
            {
                foreach (string effectAndTime in effects)
                {
                    LoggerInstance.Msg($"Processing effect and time: {effectAndTime}");

                    string[] parts = effectAndTime.Split(',');
                    string effect = "";
                    float time = 1f;

                    if (parts.Length == 2 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out time))
                    {
                        effect = parts[0];
                        LoggerInstance.Msg($"Parsed effect: {effect}, time: {time}");
                    }
                    else
                    {
                        LoggerInstance.Msg($"Invalid format for effect and time: {effectAndTime}");
                        continue;
                    }

                    Component effectComponent = null;
                    try
                    {
                        LoggerInstance.Msg($"Attempting to find component for effect: {effect}");
                        switch (effect.ToLower())
                        {
                            case "глитч":
                                effectComponent = playerEffectsObject.GetComponentByName("Glitch");
                                break;
                            case "телемост":
                                playerEffects.EffectDatamosh(true);
                                MelonCoroutines.Start(DisableEffectAfterDelay(playerEffects, "EffectDatamosh", time)); // Запускаем корутину для выключения эффекта
                                break;
                            case "тв-удар":
                                playerEffects.EffectClickTelevision();
                                break;
                            case "помехи":
                                effectComponent = playerEffectsObject.GetComponentByName("Noise");
                                break;
                            case "негатив":
                                effectComponent = playerEffectsObject.GetComponentByName("Negative");
                                break;
                            case "кровь":
                                MelonCoroutines.Start(AddRemoveBloodEffect(time));
                                break;
                            case "blure":
                                MelonCoroutines.Start(DisableEffectAfterDelay(playerEffects, "Blure", time)); // Запускаем корутину для выключения эффекта
                                break;
                            default:
                                LoggerInstance.Msg($"Unknown effect: {effect}");
                                continue;
                        }

                        if (effectComponent != null)
                        {
                            if (effectComponent is MonoBehaviour monoBehaviourComponent)
                            {
                                // Если это стандартный MonoBehaviour
                                monoBehaviourComponent.enabled = true; // Включаем компонент
                                MelonCoroutines.Start(Utils.ToggleComponentAfterTime(monoBehaviourComponent, time)); // Запускаем корутину
                            }
                            else if (effectComponent is Il2CppObjectBase il2cppComponent)
                            {
                                // Если это Il2CppObjectBase
                                LoggerInstance.Msg($"Il2Cpp component detected: {il2cppComponent.GetType().Name}");

                                // Проверяем, имеет ли компонент свойство enabled
                                var enabledProperty = il2cppComponent.GetType().GetProperty("enabled");
                                var behaviour = il2cppComponent.TryCast<Behaviour>();
                                behaviour.enabled = true;

                                // Запускаем корутину, передавая Il2Cpp-компонент
                                MelonCoroutines.Start(Utils.HandleIl2CppComponent(il2cppComponent, time));

                            }
                            else
                            {
                                LoggerInstance.Warning($"Component {effectComponent?.GetType().Name} is not a MonoBehaviour or Il2CppObjectBase.");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Msg($"Error processing effect '{effect}': {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Error in ProcessPlayerEffects: {ex.Message}");
            }

            LoggerInstance.Msg("Finished ProcessPlayerEffects.");
            return result;
        }

        public IEnumerator AddRemoveBloodEffect(float time)
        {
            playerEffects.FastVegnetteActive(true);
            yield return new WaitForSecondsRealtime(5);
            playerEffects.FastVegnetteActive(false);
        }



        private IEnumerator DisableEffectAfterDelay(Il2CppObjectBase il2cppComponent, string effectMethodName, float delay)
        {
            yield return new WaitForSeconds(delay); // Ожидаем заданное время

            // Получаем тип компонента Il2Cpp
            var componentType = il2cppComponent.GetType();

            // Используем метод GetMethod для получения метода с именем effectMethodName
            var method = componentType.GetMethod(effectMethodName);

            if (method != null)
            {
                // Передаём параметр false для выключения эффекта
                method.Invoke(il2cppComponent, new object[] { false });
                LoggerInstance.Msg($"Effect {effectMethodName} has been disabled after {delay} seconds.");
            }
            else
            {
                LoggerInstance.Warning($"Effect method {effectMethodName} not found on Il2Cpp component.");
            }
        }

        private EmotionType GetRandomEmotion()
        {
            EmotionType[] emotions = (EmotionType[])Enum.GetValues(typeof(EmotionType));
            int randomIndex = UnityEngine.Random.Range(0, emotions.Length);
            return emotions[randomIndex];
        }

       
        public string formCurrentInfo()
        {

            string info = "-";
            try
            {

                if (MitaPersonObject != null) info += $"Your game object name is <{MitaPersonObject.name}>\n";
                info += $"Current movement type: {movementStyle.ToString()}\n";
                if (MitaAnimationModded.currentIdleAnim!="") info += $"Current idle anim: {MitaAnimationModded.currentIdleAnim}\n";
                if (MitaAnimationModded.currentIdleAnim == "Mita Fall Idle") info += "You are fall, use another idle animation if want to end this animaton!\n";
                if (MitaAnimationModded.currentIdleAnim == "Mila CryNo") info += "You are sitting and crying, use another idle animation if want to end this animaton!\n";

                info += $"Current emotion anim: {currentEmotion}\n";

                try 
                {
                    var glasses = MitaPersonObject.transform.Find("World/Acts/Mita/MitaPerson/Head/Mita'sGlasses").gameObject;
                    info += $"Очки: {(glasses.activeSelf ? "надеты" : "сняты")}\n";
                    // хз что то попробовал ниже но не уверен
                if (glasses.activeSelf)
                {
                    info += "you put on glasses, if you want to take them off use the command remove glasses.\n";
                }
                else
                {
                    info += "you took off glasses, if you want to put them on use the command put on glasses.\n";
                }
                }
                catch (Exception) { }

                MelonLogger.Msg("CurrentInfo 2");


                if (mitaState == MitaState.hunt) info += $"You are hunting player with knife:\n";

                info += $"Your size: {MitaPersonObject.transform.localScale.x}\n";
                info += $"Your speed: {MitaPersonObject.GetComponent<NavMeshAgent>().speed}\n";

                if (getDistanceToPlayer() > 50f) info += $"You are outside game map, player dont hear you, you should teleport somewhere\n";

                info += $"Player size: {playerObject.transform.localScale.x}\n";
                info += $"Player speed: {playerObject.GetComponent<PlayerMove>().speedPlayer}\n";

                if (false)
                    {
                    info += $"Game house time (%): {location21_World.dayNow}\n";
                    info += $"Current lighing color: {location21_World.timeDay.colorDay}\n";
                    }

                if (MitaGames.activeMakens.Count>0) info = info + $"Menekens count: {MitaGames.activeMakens.Count}\n";
                info += AudioControl.MusicInfo();
                info += $"Your clothes: {MitaClothesModded.currentClothes}\n";

                info += MitaClothesModded.getCurrentHairColor();
                if (PlayerAnimationModded.currentPlayerMovement == PlayerAnimationModded.PlayerMovement.sit) info += $"Player is sitting\n";
                else if (PlayerAnimationModded.currentPlayerMovement == PlayerAnimationModded.PlayerMovement.taken) info += $"Player is in your hand. you can throw him using <a>Скинуть игрока</a>\n";

                info += PlayerMovement.getPlayerDistance(true);

                try
                {
                    info += GetLocations();
                    info += Interactions.getObservedObjects();
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"CurrentInfo 6.5 {ex}");
                }

                

                if (HintText!=null) info += $"Current player's hint text {HintText.text}";

                try
                {
                    info += CharacterControl.getSpeakersInfo(currentCharacter);
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"CurrentInfo getSpeakersInfo{ex}"); 
                }
                



            }
            catch (Exception ex)
            {

                LoggerInstance.Error($"formCurrentInfo {ex}");
            }
            return info;
        }
        public string formCurrentInfoGameMaster()
        {

            string info = "-";
            try
            {


                try
                {
                    info += CharacterControl.getSpeakersInfo(currentCharacter);
                }
                catch (Exception ex)
                {

                    MelonLogger.Error($"CurrentInfo getSpeakersInfo{ex}");
                }




            }
            catch (Exception ex)
            {

                LoggerInstance.Error($"formCurrentInfo {ex}");
            }
            return info;
        }

        public override void OnUpdate()
        {
            try
            {
                if (isAllLoadeed()){
                    Interactions.Update();
                    InputControl.processInpute();
                    PlayerMovement.onUpdate();
                    characterLogic?.Update(); // добавляем для метода update в characterlogic
                }



            }
            catch (Exception e)
            {

                MelonLogger.Msg(e);
            }
            
        }

        public static Color GetCharacterTextColor(character character)
        {
            switch (character)
            {
                case character.Crazy:
                    return new Color(1f, 0.4f, 0.8f); // розовый
                case character.Cappy:
                    return new Color(1f, 1f, 0.1f); // мягкий оранжевый 
                case character.Kind:
                    return new Color(0f, 1f, 0f); //поставил зеленый пока что
                case character.ShortHair:
                    return new Color(1f, 0.9f, 0.4f); // мягкий желтый
                case character.Mila:
                    return new Color(0.4f, 0.6f, 1f); // голубой
                case character.Sleepy:
                    return new Color(1f, 1f, 1f); // мягкий розовый
                case character.Creepy:
                    return new Color(1f, 0f, 0f); // красный
                default:
                    return Color.white;
            }
        }

        public void GlassesObj()
        {
            MitaPersonObject.transform.Find("World/Acts/Mita/MitaPerson/Head/Mita'sGlasses").gameObject.SetActive(true);
        }

        public void GlassesObj(bool state)
        {
            try 
            {
                var glasses = MitaPersonObject.transform.Find("World/Acts/Mita/MitaPerson/Head/Mita'sGlasses").gameObject;
                glasses.SetActive(state);
                sendSystemInfo(state ? "Очки надеты" : "Очки сняты");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Ошибка при работе с очками: {ex.Message}");
            }
        }

    }



}
