using MelonLoader;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


namespace MitaAI
{

    [RegisterTypeInIl2Cpp]
    public class Character : MonoBehaviour
    {
        public MitaCore.character character;
        public bool isCartdige;
        public bool isGameMaster = false;
        public int PointsOrder = 0;

        public void init(MitaCore.character character)
        {
            this.character = character;
            CharacterControl.Characters.Add(this);
        }
        public void init_cartridge()
        {

            this.isCartdige = true;
            init(CharacterControl.get_cart());

        }


        public void DecreseOrderPoints(int n = 25)
        {
            PointsOrder -= n;
        }

        public void init_GameMaster()
        {
            character = MitaCore.character.GameMaster;
            this.isGameMaster = true;
            //CharacterControl.Characters.Add(this);
            CharacterControl.gameMaster = this;
        }

        public int timingEach = 3;
        int timingNow = 1;
        public bool isTimeToCorrect()
        {
            bool isIt = timingNow >= timingEach;

            if (isIt) timingNow = 1;
            else timingNow += 1;

            return isIt;

        }
    }
    // Не наследуется(((
    public class GameMaster : Character
    {
        public void init_GameMaster()
        {
            character = MitaCore.character.GameMaster;
            this.isGameMaster = true;
            //CharacterControl.Characters.Add(this);
            CharacterControl.gameMaster = this;
        }

        int timingEach = 3;
        int timingNow = 1;
        public bool isTimeToCorrect()
        {
            bool isIt = timingNow == timingEach;

            if (isIt) timingNow = 0;
            else timingNow = 1;

            return isIt;

        }
    }
}
