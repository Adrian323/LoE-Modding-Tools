using System.Collections;
using UnityEngine;
using Steamworks;

public class SteamSubscribedItemsTest : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(GetSubscribedItemsList());
    }

    IEnumerator GetSubscribedItemsList()
    {
        while (SteamManager.Initialized == false)
        {
            yield return null;
        }

        uint numOfSubscribedItems = SteamUGC.GetNumSubscribedItems();

        PublishedFileId_t[] subscribedInfo = new PublishedFileId_t[numOfSubscribedItems];

        uint test = SteamUGC.GetSubscribedItems(subscribedInfo, numOfSubscribedItems);

        for (int i = 0; i < subscribedInfo.Length; i++)
        {
            ulong sizeOnDisc = 0;

            string localFolder;
            ulong sizeOnDisk;
            uint timestamp;
            System.DateTime timestampParsed = System.DateTime.MinValue;
            if (SteamUGC.GetItemInstallInfo(subscribedInfo[i], out sizeOnDisk, out localFolder, 260, out timestamp))
            {
                Debug.Log(localFolder);
                //timestampParsed = new System.DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
                //timestampParsed = timestampParsed.AddSeconds(timestamp).ToLocalTime();
            }
        }

    }
}