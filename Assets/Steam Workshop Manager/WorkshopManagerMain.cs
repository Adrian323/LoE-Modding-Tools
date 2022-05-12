using Steamworks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorkshopManagerMain : MonoBehaviour
{
    public WorkshopManagerItem ItemPrefab;
    public RectTransform GridTransform;

    public UMod.ModTools.Export.ExportSettings ExportSettings;

    public List<WorkshopManagerItem> ExportItems;

    public bool HasLoaded;


    // Private methods
    //private SteamAPICall_t createItemCall;
    //private CallResult<CreateItemResult_t> createCallRes;
    //private UGCUpdateHandle_t updateHandle;

    ulong GetModID(int index)
    {
        string pathIDStr = PlayerPrefs.GetString("ExportID(" + index + ")");

        ulong id = 0;

        ulong.TryParse(pathIDStr, out id);

        return id;
    }

    void SetModID(int index, ulong id)
    {
        PlayerPrefs.SetString("ExportID(" + index + ")", id.ToString());
    }

    // Start is called before the first frame update
    void Start()
    {
        while (GridTransform.childCount > 0)
        {
            Transform t = GridTransform.GetChild(0);
            t.SetParent(null);
            GameObject.Destroy(t.gameObject);
        }

        if ((ExportSettings == null)
            || (ItemPrefab == null)
            || (GridTransform == null))
        {
            WorkshopMessagesManager.SetMessageText("Missing Components...", Color.red);
            return;
        }

        ExportItems = new List<WorkshopManagerItem>();

        if (ExportSettings.ExportProfiles != null)
        {
            for (int i =0; i < ExportSettings.ExportProfiles.Length; i++)
            {
                UMod.ModTools.Export.ExportProfileSettings exportProfileSettings = ExportSettings.ExportProfiles[i];
            
                WorkshopManagerItem exportInstance = GameObject.Instantiate(ItemPrefab);

                exportInstance.transform.SetParent(GridTransform, false);

                exportInstance.TitleText.text = exportProfileSettings.ModName;
                exportInstance.DescriptionText.text = exportProfileSettings.ModDescription;

                exportInstance.ModIdText.text = GetModID(i).ToString();

                if (string.IsNullOrEmpty(exportInstance.DescriptionText.text))
                {
                    exportInstance.DescriptionText.text = "NO DESCRIPTION";
                }

                if (exportProfileSettings.ModIcon != null)
                {
                    Rect textRect = new Rect(0, 0, exportProfileSettings.ModIcon.width, exportProfileSettings.ModIcon.height);

                    exportInstance.ThumbnailImage.sprite = Sprite.Create(exportProfileSettings.ModIcon, textRect, Vector2.zero);
                }

                exportInstance.CalculatedPath = exportProfileSettings.ModExportPath + "/" + exportProfileSettings.ModName + ".umod";

                if (File.Exists(exportInstance.CalculatedPath) == false)
                {
                    exportInstance.SetStatus(WorkshopManagerItem.Status.FileNotFound);
                }
                else
                {
                    exportInstance.SetStatus(WorkshopManagerItem.Status.Unknown);
                }

                int itemIndex = ExportItems.Count;

                exportInstance.CreateButton.onClick.AddListener(() => OnClickedCreateAction(itemIndex));

                exportInstance.UpdateButton.onClick.AddListener(() => OnClickedUpdateAction(itemIndex));

                ExportItems.Add(exportInstance);
            }

            StartCoroutine(RefreshItemsStatusRoutine());
        }
    }

    void OnClickedUpdateAction(int itemIndex)
    {
        SetButtonsState(false);

        StartCoroutine(UpdateItemRoutine_Main(itemIndex));
    }

    void OnClickedCreateAction(int itemIndex)
    {
        SetButtonsState(false);

        StartCoroutine(CreateItemRoutine_Main(itemIndex));
    }

    IEnumerator UpdateItemRoutine_Main(int index)
    {
        if (SteamManager.Initialized == false)
        {
            WorkshopMessagesManager.SetMessageText("Not connected to steam");
            yield break;
        }

        SetButtonsState(false);

        WorkshopMessagesManager.SetMessageText("Updating...");

        ulong id = GetModID(index);

        if (id == 0)
        {
            WorkshopMessagesManager.SetMessageText("Mod doesn't exist");
        }
        else
        {
            yield return UpdateItemRoutine(index, id);
        }

        SetButtonsState(true);
    }


    IEnumerator CreateItemRoutine_Main(int index)
    {
        if (SteamManager.Initialized == false)
        {
            WorkshopMessagesManager.SetMessageText("Not connected to steam");
            yield break;
        }

        SetButtonsState(false);

        yield return CreateItemRoutine(index);

        ExportItems[index].ModIdText.text = GetModID(index).ToString();

        SetButtonsState(true);
    }

    IEnumerator CreateItemRoutine(int index)
    {
        //click.Play();
        //1. All workshop items begin their existence with a call to ISteamUGC::CreateItem

        WorkshopMessagesManager.SetMessageText("Creating Item...");

        CreateItemResult_t pCallback = new CreateItemResult_t();
        pCallback.m_eResult = EResult.k_EResultCancelled;

        bool success = false;
        bool finished = false;

        CallResult<CreateItemResult_t> createCallRes = CallResult<CreateItemResult_t>.Create((pC, bIOF) =>
          {
              pCallback = pC;
              success = bIOF;
              finished = true;
          }
            );

        ////2. Next Register a call result handler for CreateItemResult_t
        SteamAPICall_t handle = SteamUGC.CreateItem(new AppId_t(621070), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        createCallRes.Set(handle);

        while (finished == false)
        {
            yield return null;
        }

        if (pCallback.m_eResult == EResult.k_EResultCancelled)
        {
            WorkshopMessagesManager.SetMessageText("Could not uplaod");
            yield break;
        }

        //3. First check the m_eResult to ensure that the item was created successfully.
        //4. When the call result handler is executed, read the m_nPublishedFileId value and store for future updates to the workshop item (e.g. in a project file associated with the creation tool).
        //Debug.Log("m_eResult: " + pCallback.m_eResult + "   Published File ID: " + pCallback.m_nPublishedFileId + "   User needs to accept legal agreement: " + pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement);
        //5. The m_bUserNeedsToAcceptWorkshopLegalAgreement variable should also be checked and if it's true, the user should be redirected to accept the legal agreement. See the Workshop Legal Agreement section for more details.
        //https://partner.steamgames.com/doc/features/workshop/implementation#Legal

        if (pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            WorkshopMessagesManager.SetMessageText("Steam Agreement Pending");
            redirectToLegal();
            yield break;
        }

        //Once a workshop item has been created and a PublishedFileId_t value has been returned, the content of the workshop item can be populated and uploaded to the Steam Workshop.
        //An item update begins with a call to SteamUGC.StartItemUpdate

        if (pCallback.m_eResult != EResult.k_EResultOK)
        {
            WorkshopMessagesManager.SetMessageText("Error: " + GetActionResultString(pCallback.m_eResult));
            yield break;
        }
        else
        {
            WorkshopMessagesManager.SetMessageText("Result: " + GetActionResultString(pCallback.m_eResult));
        }

        if (pCallback.m_nPublishedFileId.m_PublishedFileId == 0)
        {
            WorkshopMessagesManager.SetMessageText("Unknown Error");
            yield break;
        }

        PlayerPrefs.SetString("ExportID(" + index + ")", pCallback.m_nPublishedFileId.m_PublishedFileId.ToString());
    }

    string GetActionResultString(EResult eResult)
    {
        string trimmed = eResult.ToString().Replace("k_EResult", string.Empty);

        return UnityEditor.ObjectNames.NicifyVariableName(trimmed);
    }

    IEnumerator UpdateItemRoutine(int index, ulong id)
    {
        UMod.ModTools.Export.ExportProfileSettings exportProfile = ExportSettings.ExportProfiles[index];

        if (exportProfile.ModIcon == null)
        {
            WorkshopMessagesManager.SetMessageText("Mod doesn't have an icon");
            yield break;
        }

        if (string.IsNullOrEmpty(ExportItems[index].ModUpdateNotes.text) == true)
        {
            WorkshopMessagesManager.SetMessageText("No update notes set");
            yield break;
        }

        string iconPath = UnityEditor.AssetDatabase.GetAssetPath(exportProfile.ModIcon);

        string uploadDirectory = System.IO.Path.GetFullPath("Steam Upload Directory").Replace("\\","/");

        string fileName = ExportSettings.ExportProfiles[index].ModName + ".umod";

        string sourceFile = Path.Combine(exportProfile.ModExportPath, fileName.Replace("\\", "/"));

        if (File.Exists(sourceFile) == false)
        {
            WorkshopMessagesManager.SetMessageText("Mod file was not found");
            yield break;
        }

        if (Directory.Exists(uploadDirectory))
        {
            Directory.Delete(uploadDirectory, true);
        }

        Directory.CreateDirectory(uploadDirectory);

        CopyFile(sourceFile, uploadDirectory);
        string previewFileLocation = CopyFile(iconPath, uploadDirectory);

        PublishedFileId_t publishedField = new PublishedFileId_t(id);

        UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), publishedField); //may need to do a create and onFunction to set the handle of UGCUpdateHandle_t

        SteamUGC.SetItemTitle(updateHandle, exportProfile.ModName);
        SteamUGC.SetItemDescription(updateHandle, exportProfile.ModDescription);

        SteamUGC.SetItemPreview(updateHandle, previewFileLocation);

        SteamUGC.SetItemContent(updateHandle, uploadDirectory);

        SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic); //k_ERemoteStoragePublishedFileVisibilityPublic = 0, so it should be set to public with this line

        //Once the update calls have been completed, calling ISteamUGC::SubmitItemUpdate will initiate the upload process to the Steam Workshop.

        SubmitItemUpdateResult_t p_callback = new SubmitItemUpdateResult_t();
        p_callback.m_eResult = EResult.k_EResultCancelled;

        bool success = false;
        bool finished = false;

        CallResult<SubmitItemUpdateResult_t> createCallRes = CallResult<SubmitItemUpdateResult_t>.Create((pC, bIOF) =>
        {
            p_callback = pC;
            success = bIOF;
            finished = true;
        }
           );

        SteamAPICall_t steamAPICall_t = SteamUGC.SubmitItemUpdate(updateHandle, ExportItems[index].ModUpdateNotes.text);

        //createCallRes.Set(steamAPICall_t, updateHandle);

        createCallRes.Set(steamAPICall_t);

        while (finished == false)
        {
            yield return null;
        }

        if (p_callback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            WorkshopMessagesManager.SetMessageText("Steam Agreement Pending");
            redirectToLegal();
            yield break;
        }
        else
        {
            WorkshopMessagesManager.SetMessageText("Result: " + GetActionResultString(p_callback.m_eResult));
        }

    }

    string  CopyFile(string sourcePath, string destinationDirectory)
    {
        string fileName = Path.GetFileName(sourcePath);

        string destinationFile = Path.Combine(destinationDirectory, fileName).Replace("\\", "/");

        File.Copy(sourcePath, destinationFile);

        return destinationFile;
    }

    public void redirectToLegal()
    {
        SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/workshoplegalagreement");
    }

    void SetButtonsState(bool state)
    {
        foreach (WorkshopManagerItem item in ExportItems)
        {
            SetButtonState(item, state);
        }
    }

    void SetButtonState(WorkshopManagerItem item, bool state)
    {
        item.UpdateButton.interactable = state;
        item.CreateButton.interactable = state;

        item.UpdateButtonText.color = state ? Color.black : Color.gray;
        item.CreateButtonText.color = state ? Color.black : Color.gray;
    }

    IEnumerator RefreshItemsStatusRoutine()
    {
        SetButtonsState(false);
        WorkshopMessagesManager.SetMessageText("Loading...");

        do
        {
            yield return null;
        } while (SteamManager.Initialized == false);

        foreach (WorkshopManagerItem item in ExportItems)
        {
            if (item.CurrentStatus == WorkshopManagerItem.Status.FileNotFound)
            {
                //SetButtonState(item, false);
            }
            else if (item.CurrentStatus == WorkshopManagerItem.Status.Unknown)
            {
                SetButtonState(item, true);
                //////////////////// OLD CODE /////////////////////
                //CreateItemResult_t pCallback;
                //bool success;
                //bool finished;

                //PublishedFileId_t publishedField = new PublishedFileId_t();

                //SteamUGC.CreateItem(AppId_t
                ////publishedField.
                //SteamUGC.CreateQueryUserUGCRequest GetItemState(publishedField);
                //CallResult<PublishedFileId_t> createCallRes = CallResult<PublishedFileId_t>.Create((pCallback, bIOFailure) =>
                //{

                //});

                ////2. Next Register a call result handler for CreateItemResult_t
                //SteamAPICall_t handle = SteamUGC.CreateItem(new AppId_t(621070), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                //createCallRes.Set(handle);
                ////////////////////////////////////////////////
                ///

          
                //click.Play();
                //1. All workshop items begin their existence with a call to ISteamUGC::CreateItem
                //CallResult<CreateItemResult_t>  createCallRes = CallResult<CreateItemResult_t>.Create(OnCreateItem);

                ////2. Next Register a call result handler for CreateItemResult_t
                //SteamAPICall_t handle = SteamUGC.CreateItem(new AppId_t(621070), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
                //createCallRes.Set(handle);


            }
        }

        //if (itemToLoad != null)
        //{

        //}

        WorkshopMessagesManager.SetMessageText("Waiting for command");
    }

   /*

    void OnCreateItem(CreateItemResult_t pCallback, bool bIOFailure)
    {
        //3. First check the m_eResult to ensure that the item was created successfully.
        //4. When the call result handler is executed, read the m_nPublishedFileId value and store for future updates to the workshop item (e.g. in a project file associated with the creation tool).
        //Debug.Log("m_eResult: " + pCallback.m_eResult + "   Published File ID: " + pCallback.m_nPublishedFileId + "   User needs to accept legal agreement: " + pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement);
        //5. The m_bUserNeedsToAcceptWorkshopLegalAgreement variable should also be checked and if it's true, the user should be redirected to accept the legal agreement. See the Workshop Legal Agreement section for more details.
        //https://partner.steamgames.com/doc/features/workshop/implementation#Legal

        if (!pCallback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            //Once a workshop item has been created and a PublishedFileId_t value has been returned, the content of the workshop item can be populated and uploaded to the Steam Workshop.
            //An item update begins with a call to SteamUGC.StartItemUpdate

            updateHandle = SteamUGC.StartItemUpdate(new AppId_t(621070), pCallback.m_nPublishedFileId); //may need to do a create and onFunction to set the handle of UGCUpdateHandle_t
            SteamUGC.SetItemTitle(updateHandle, title);
            SteamUGC.SetItemDescription(updateHandle, description);
            SteamUGC.SetItemContent(updateHandle, path);

            string newImagePath = "";
            if (m_textPath != null)
            {
                newImagePath = m_textPath.Replace("\\", "/");
            }
            if (File.Exists(newImagePath))
            {
                SteamUGC.SetItemPreview(updateHandle, newImagePath);
                //print("Setting " + newImagePath + " as preview image");
            }

            SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic); //k_ERemoteStoragePublishedFileVisibilityPublic = 0, so it should be set to public with this line

            //Once the update calls have been completed, calling ISteamUGC::SubmitItemUpdate will initiate the upload process to the Steam Workshop.
            SteamUGC.SubmitItemUpdate(updateHandle, "New workshop item");
            SteamFriends.ActivateGameOverlayToWebPage("steam://url/CommunityFilePage/" + pCallback.m_nPublishedFileId);

            Debug.Log("Uploaded ");
            //animationBoi.SetTrigger("submittedToWorkshop");
        }
        else
        {
            redirectToLegal();
        }
    }

    public void redirectToLegal()
    {
        SteamFriends.ActivateGameOverlayToWebPage("https://steamcommunity.com/sharedfiles/workshoplegalagreement");
    }

    //public void SubmitToWorkshop()
    //{
    //    //path = Application.persistentDataPath + "/" + title;


    //    if (File.Exists(path) && properImageSelected)
    //    {
    //        if (SteamManager.Initialized)
    //        {
    //            //click.Play();
    //            //1. All workshop items begin their existence with a call to ISteamUGC::CreateItem
    //            createCallRes = CallResult<CreateItemResult_t>.Create(OnCreateItem);

    //            //2. Next Register a call result handler for CreateItemResult_t
    //            SteamAPICall_t handle = SteamUGC.CreateItem(new AppId_t(621070), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
    //            createCallRes.Set(handle);
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("Could not upload");
    //    }
    //}
   */
}
