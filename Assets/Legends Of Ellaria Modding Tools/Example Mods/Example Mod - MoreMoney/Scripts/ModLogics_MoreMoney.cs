using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModLogics_MoreMoney : ModLogics_Base
{
    public int ExtraMoneyPerDayMin = 100;
    public int ExtraMoneyPerDayMax = 300;

    public override void OnIngameHourPassed()
    {
        
        int amountToAdd = Random.Range(ExtraMoneyPerDayMin, ExtraMoneyPerDayMax);

        GameEngine.WorldData.PlayerFactionRef.AddResources("Gold", amountToAdd);

        GameEngine.Engine_Player.HEX_FPS_RTS_Manager.AddTextToQuestText("Received " + amountToAdd + " gold from an unknown donor");
    }
}
