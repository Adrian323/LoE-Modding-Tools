using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkshopManagerItem : MonoBehaviour
{
#if UNITY_EDITOR

    public enum Status
    {
        Unknown,
        FileNotFound,
        NotUploaded,
        Uploaded
    }
    public Status CurrentStatus;

    public void SetStatus(Status status)
    {
        CurrentStatus = status;

        ModStatusText.text = UnityEditor.ObjectNames.NicifyVariableName(CurrentStatus.ToString());
    }

    public string CalculatedPath;

    public UnityEngine.UI.Image ThumbnailImage;
    public UnityEngine.UI.Text TitleText;
    public UnityEngine.UI.Text DescriptionText;
    

    public UnityEngine.UI.Button CreateButton;
    public UnityEngine.UI.Text CreateButtonText;

    public UnityEngine.UI.Button UpdateButton;
    public UnityEngine.UI.Text UpdateButtonText;

    public UnityEngine.UI.Text ModIdText;
    public UnityEngine.UI.Text ModUpdateNotes;

    public UnityEngine.UI.Text ModStatusText;

    public UMod.ModTools.Export.ExportProfileSettings RelatedExport;


#endif
}
