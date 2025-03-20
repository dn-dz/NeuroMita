using Il2Cpp;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using static Il2CppRootMotion.FinalIK.InteractionObject;

namespace MitaAI
{
    public static class CharacterControl
    {

        // Сюда перенести все сharacter и changeMita
        static MitaCore.character cart = MitaCore.character.None;
        static public Character gameMaster = null;
        public static MitaCore.character get_cart()
        {
            if (cart == MitaCore.character.None) init_cart();
            return cart;
        }

        public static void init_cart()
        {
            if (Utils.Random(1, 2)) cart = MitaCore.character.Cart_portal;
            else cart = MitaCore.character.Cart_divan;
        }


        public static HashSet<Character> Characters = new HashSet<Character>();
        
        private static List<Character> getActiveCharacters()
        {
            List<Character> activeCharacters = new List<Character>();
            List<Character> ClearCharacters = new List<Character>();

            foreach (var character in Characters)
            {
                if (character == null)
                {
                    ClearCharacters.Add(character);
                    continue;
                }

                if (!character.enabled) continue;

                float distance = 25f;
                if (character.isCartdige) distance = 3.5f;

                
                float factDistance = Utils.getDistanceBetweenObjects(MitaCore.Instance.playerPersonObject,character.gameObject);

                MelonLogger.Msg($"{character.character} found in Characters");

                if (factDistance <= distance)
                {

                    MelonLogger.Msg($"{character.character} added to active Characters");
                    activeCharacters.Add(character);
                }
            }
            foreach (var character in ClearCharacters) Characters.Remove(character);


            return activeCharacters;
        }
        public static string getObjectName(Character character)
        {
            GameObject Mita = MitaCore.getMitaByEnum(character.character, true);
            string objectName = "";
            if (Mita != null) objectName += $", its game object {Mita}";

            return objectName;
        }
        public static string getObjectName(MitaCore.character character)
        {
            GameObject Mita = MitaCore.getMitaByEnum(character, true);
            string objectName = "";
            if (Mita != null) objectName += $", its game object {Mita}";

            return objectName;
        }

        // Назначает максимальный приоритет персонажу
        public static void SetNextSpeaker(MitaCore.character nextCharacter)
        {
            List<Character> activeCharacters = getActiveCharacters();
            activeCharacters = activeCharacters.OrderByDescending(character => character.PointsOrder).ToList();
            
            foreach (var character in activeCharacters)
            {

                if (character.character == nextCharacter)
                {
                    character.PointsOrder = activeCharacters.First().PointsOrder + 1 ;
                }
            }

        }
        
        // Дает информацию о собеседниках
        public static string getSpeakersInfo(MitaCore.character toWhom)
        {
            List<Character> activeCharacters =  getActiveCharacters();

            if (activeCharacters == null) return "";

            if (activeCharacters.Count <= 1) return "";

            string message = "";
            message += $"[DIALOGUE] You are in dialogue with several ({activeCharacters.Count+1}) speakers: \n Player";
            foreach (Character character in activeCharacters)
            {

                string objectName = getObjectName(character);
                message += $"\n{CharacterControl.extendCharsString(character.character)}{objectName})";
                if (character.character == toWhom) message += "(you)";
            }
            message += "\n";
            return message;
        }

        public static void DecreaseOrderPoints(Character character)
        {
            if (character.isCartdige) character.DecreseOrderPoints();
            else character.DecreseOrderPoints();
        }

        public static void resetOrders(bool fillRandom = false)
        {
            List<Character> activeCharacters = getActiveCharacters();
            foreach (Character ch in activeCharacters)
            { 
                ch.PointsOrder = 0;

                if (fillRandom) ch.PointsOrder += UnityEngine.Random.Range(0, 25);

            }
        }

        // Передает достпных для разговора персонажей, притом снижает приоритет первого чтобы очередь перемещалась
        public static List<MitaCore.character> GetCharactersToAnswer()
        {
            List<Character> activeCharacters = getActiveCharacters();

            activeCharacters = activeCharacters.OrderByDescending(character => character.PointsOrder).ToList();


            if (activeCharacters.Count > 0)
            {
                DecreaseOrderPoints(activeCharacters.First());
            }
            
            List<MitaCore.character> characters = new List<MitaCore.character>();
            foreach (var character in activeCharacters)
            {
                MelonLogger.Msg($"{character.character} found in activeCharacters");
                characters.Add(character.character);
            }

            
            return characters;
        }

        // Пришло ли время ГеймМастеру вмешаться
        private static bool GameMasterCase(MitaCore.character from)
        {
            if (gameMaster == null) return false;
            try
            {
                if (gameMaster.enabled && from != MitaCore.character.GameMaster)
                {
                    MelonLogger.Msg("nextAnswer Attempt GameMaster");
                    if (gameMaster.isTimeToCorrect())
                    {
                        MelonLogger.Msg("nextAnswer Success Attempt GameMaster");
                        string m = "Проследи за диалогом (если он уже начался, то уже реагируй на текущий), выполняя инструкции и основываясь на текущих данных разговора. Приветствия не нужно";
                        MitaCore.Instance.sendSystemMessage(m, MitaCore.character.GameMaster);
                        return true;
                    }
                    
                    // Если игрок, то пора лимит
                    if (from != MitaCore.character.Player)
                    {
                        return false;
                        //limit = 1;  
                    }

                }

            }
            catch (Exception ex)
            {

                MelonLogger.Error(ex);
            }

            return false;
        }


        static int limit = 0;
        public static float limitMod = 100;
        public static void nextAnswer(string response, MitaCore.character from)
        {
            MelonLogger.Msg($"nextAnswer from {from}, limit now {limit}");

            if (GameMasterCase(from)) return;

            // Получаем список персонажей
            List<MitaCore.character> characters = GetCharactersToAnswer();
            if (characters == null) return;


            // Добавляем отправителя в список говорящих
            characters.Remove(from);
            // Удаляем из characters всех, кто уже говорил

            // Если больше некому отвечать
            if (characters.Count <= 0)
            {
                MelonLogger.Msg("No characters left to answer.");
                return;
            }


            // Логика для сообщений от ИИ
            if (from!=MitaCore.character.Player && limit < Math.Round(characters.Count*limitMod/100))
            {
                MitaCore.character character = characters.First();
                MelonLogger.Msg($"nextAnswer to {character}");
                // Если отправитель и получатель совпадают, выходим
                if (from == character)
                {
                    MelonLogger.Msg("Sender and receiver are the same. Skipping.");
                    return;
                }
                limit += 1;
                // Отправляем сообщение и добавляем получателя в список говорящих

                string message = getSpeakersInfo(character);

                string nextSpeaker = "";
                string objectNameNext = "";
                if (limit + 1 == characters.Count)
                {
                    nextSpeaker = "Player";
                }
                else
                {
                    if (characters.Count >= 2) {
                        nextSpeaker = CharacterControl.extendCharsString(characters[1]);
                        objectNameNext = getObjectName(characters[1]);
                    }

                    else nextSpeaker = "Player";
                }

                string objectName = getObjectName(from);
                MelonLogger.Msg($"send to {character}");
                message += $"[SPEAKER] {CharacterControl.extendCharsString(from)}{objectName} said: <{response}>. Next speaker is {objectNameNext} Respond to him or name somebody you want to speak with.";

                MitaCore.Instance.sendSystemMessage(message, character);

            }
            else
            {
                MelonLogger.Msg($"nextAnswer reset ch count {limit}/{Math.Round(characters.Count * limitMod / 100)}");

                // Сбрасываем список говорящих
                limit = 1;
                resetOrders(true);
                MelonLogger.Msg("Speakers list cleared.");
            }
        }

        public static string extendCharsString(MitaCore.character character)
        {
            if (character.ToString().Contains("Cart")) return $"{character} (cartridge)";
            else return $"{character} (Mita)";
        }
    }
}
