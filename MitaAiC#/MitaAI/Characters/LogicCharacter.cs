using MelonLoader;
using Il2Cpp;
using UnityEngine;
using UnityEngine.AI;
using System.Collections; // Добавлено для IEnumerator

namespace MitaAI
{
    public class LogicCharacter
    {
        private const float DEFAULT_CHECK_INTERVAL = 2.0f;
        private const float UNSAFE_VERTICAL_SPEED = -10f;
        private const float UNSAFE_Y_POSITION = -100f;
        private const float NAV_MESH_SAMPLE_DISTANCE = 100f;

        private static LogicCharacter instance;
        public static LogicCharacter Instance => instance ??= new LogicCharacter();

        private bool isInitialized;
        private GameObject characterObject;
        private float lastCheckTime;
        private NavMeshAgent navMeshAgent;
        private Animator animator;
        private MitaCore.character characterType; // Тип персонажа (Creepy, Sleepy и т.д.)

        // Состояние персонажа для охоты
        private enum CharacterState { normal, hunt }
        private CharacterState currentState = CharacterState.normal;

        private GameObject creepyKnife; //не рабочее, нужно найти настоящий gameobject

        private LogicCharacter() { } //пока-что нечего

        public void Initialize(GameObject character, MitaCore.character type)
        {
            if (isInitialized) return;
            SetCharacterObject(character);
            characterType = type;
            AdjustCharacterSettings(); // Настройка параметров в зависимости от типа
            isInitialized = true;
            // Инициализация ножа для Creepy
            if (characterType == MitaCore.character.Creepy)
            {
                Transform knifeTransform = characterObject.transform.Find("Knife");
                if (knifeTransform != null)
                {
                    creepyKnife = knifeTransform.gameObject;
                }
                else
                {
                    MelonLogger.Msg("Knife not found for Creepy during initialization");
                }
            }
        }

        private void SetCharacterObject(UnityEngine.GameObject character)
        {
            characterObject = character;
            if (characterObject != null)
            {
                navMeshAgent = characterObject.GetComponent<NavMeshAgent>();
                animator = characterObject.GetComponent<Animator>();
            }
        }

        private void AdjustCharacterSettings()
        {
            if (navMeshAgent == null) return;
            switch (characterType)
            {
                case MitaCore.character.Creepy:
                    // Сюда будет прописывать логика в зависимости от персонажа
                    break;
                case MitaCore.character.Sleepy:
                    // ...
                    break; //позже для других персонажей
                default:
                    // ...
                    break;
            }
        }

        public void Update()
        {
            if (!isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - lastCheckTime >= DEFAULT_CHECK_INTERVAL)
            {
                lastCheckTime = currentTime;
                MonitorCharacterState();
            }
        }

        // Остановка движения например для логики с сонной
        //public void StopMovement()
        //{
        //    if (navMeshAgent != null && navMeshAgent.enabled)
        //    {
        //        navMeshAgent.ResetPath();
        //    }
        //}

        // Установка скорости
        //public void SetSpeed(float speed)
        //{
        //    moveSpeed = speed;
        //    if (navMeshAgent != null) navMeshAgent.speed = speed;
        //}

        private void MonitorCharacterState() //пусть будет возможно на будующее
        {
            if (characterObject == null) return;
            CheckAnimationState();
        }

        private void CheckAnimationState() //пример как можно было бы сделать с анимациями
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;

            var currentClipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (currentClipInfo.Length == 0) return;

            string currentAnim = currentClipInfo[0].clip.name;
            if (currentAnim.Contains("Fall") || currentAnim.Contains("Idle"))
            {
                // Падение обнаружено
            }
        }

        // Начало охоты для Creepy
        public void BeginHunt()
        {
            if (characterType != MitaCore.character.Creepy) return;

            try
            {
                MelonLogger.Msg("BeginHunt for Creepy");
                if (creepyKnife != null)
                {
                    creepyKnife.SetActive(true);
                }
                else
                {
                    MelonLogger.Msg("Creepy knife is null in BeginHunt");
                }

                currentState = CharacterState.hunt;
                MelonCoroutines.Start(Hunting());

                // Отключаем возможность ходить через NavMeshAgent (аналог ActivationCanWalk(false))
                if (navMeshAgent != null)
                {
                    navMeshAgent.enabled = false;
                }

                // Управление анимациями через Animator напрямую
                if (animator != null)
                {
                    animator.Play("Creepy TakeKnife_0"); // Заменить на актуальное имя анимации
                    animator.SetBool("IsWalkingWithKnife", true); // Пример, заменить на реальный параметр
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("BeginHunt for Creepy: " + ex);
            }
        }

        // Завершение охоты для Creepy
        public void EndHunt()
        {
            if (characterType != MitaCore.character.Creepy) return;

            try
            {
                if (animator != null)
                {
                    animator.SetBool("IsWalkingWithKnife", false); // Сброс параметра анимации
                    animator.Play("Creepy Walk_1"); // Заменить на актуальное имя анимации
                }

                if (creepyKnife != null)
                {
                    creepyKnife.SetActive(false);
                }

                // Восстановление стандартного стиля движения
                MitaCore.movementStyle = MitaCore.MovementStyles.walkNear;
                if (navMeshAgent != null)
                {
                    navMeshAgent.enabled = true;
                    navMeshAgent.ResetPath();
                }

                currentState = CharacterState.normal;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("EndHunt for Creepy: " + ex);
            }
        }

        // Корутина преследования для Creepy
        private IEnumerator Hunting()
        {
            if (characterType != MitaCore.character.Creepy) yield break;

            float startTime = Time.unscaledTime;
            float lastMessageTime = -45f;
            yield return new WaitForSeconds(1f);

            // Включаем NavMeshAgent обратно для преследования
            if (navMeshAgent != null)
            {
                navMeshAgent.enabled = true;
            }

            while (currentState == CharacterState.hunt)
            {
                if (navMeshAgent == null || MitaCore.Instance.playerPersonObject == null) yield break;

                float distanceToPlayer = Vector3.Distance(
                    characterObject.transform.position,
                    MitaCore.Instance.playerPersonObject.transform.position
                );

                if (distanceToPlayer > 1f)
                {
                    navMeshAgent.SetDestination(MitaCore.Instance.playerPersonObject.transform.position);
                }
                else
                {
                    try
                    {
                        MelonCoroutines.Start(MitaCore.Instance.ActivateAndDisableKiller(3));
                        EndHunt(); // Завершаем охоту после убийства
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error("Hunting error for Creepy: " + ex);
                    }
                    yield break;
                }

                float elapsedTime = Time.unscaledTime - startTime;
                if (elapsedTime - lastMessageTime >= 45f)
                {
                    string message = $"Игрок жив уже {elapsedTime.ToString("F2")} секунд. Скажи что-нибудь короткое. ";
                    if (Mathf.FloorToInt(elapsedTime) % 60 == 0)
                    {
                        message += "Может быть, пора усложнять игру... (Менять скорости или применять эффекты)";
                    }
                    MitaCore.Instance.sendSystemMessage(message);
                    lastMessageTime = elapsedTime;
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        //private void CheckCharacterPosition() //тож пример с использованием позиции
        //{
        //    Vector3 position = characterObject.transform.position;
        //    if (position.y < UNSAFE_Y_POSITION || float.IsInfinity(position.y) || float.IsNaN(position.y))
        //    {
        //        ResetCharacterPosition();
        //    }
        //}

    }
}
