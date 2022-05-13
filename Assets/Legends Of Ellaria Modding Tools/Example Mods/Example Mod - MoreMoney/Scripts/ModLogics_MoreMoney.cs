using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModLogics_MoreMoney : ModLogics_Base
{
    private class MoreMoneyConfig
    {
        public int ExtraMoneyPerDayMin = 100;
        public int ExtraMoneyPerDayMax = 300;
    }

    private MoreMoneyConfig Config = new MoreMoneyConfig() { ExtraMoneyPerDayMin = 100, ExtraMoneyPerDayMax = 300 };

    public override bool Init()
    {
        string config = LoadConfig();

        if (string.IsNullOrEmpty(config) == false)
        {
            try
            {
                MoreMoneyConfig newConfig = JsonUtility.FromJson<MoreMoneyConfig>(config);

                if (newConfig != null)
                {
                    Config = newConfig;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        try
        {
            SaveConfig(JsonUtility.ToJson(Config));
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        return base.Init();
    }
    public override void OnIngameDayPassed()
    {
        
        int amountToAdd = Random.Range(Config.ExtraMoneyPerDayMin, Config.ExtraMoneyPerDayMax);

        GameEngine.WorldData.PlayerFactionRef.AddResources("Gold", amountToAdd);

        GameEngine.Engine_Player.HEX_FPS_RTS_Manager.AddTextToQuestText("Received " + amountToAdd + " gold from an unknown donor");
    }
}
