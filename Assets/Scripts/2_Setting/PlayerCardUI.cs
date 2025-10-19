using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public Image playerAvatarImage;

    [Header("Step Icons")]
    public GameObject[] stepIncompleteIcons; // Array: [0]=Step 1, [1]=Step 2, [2]=Step 3, [3]=Step 4
    public GameObject[] stepCompleteIcons;   // Array: Corresponding complete icons

    // This function will be called by TutorialManager
    public void UpdateProgress(PlayerTutorialProgress progress)
    {
        // Player Name
        playerNameText.text = progress.playerInfo.name;

        // 步驟 1 (向前)
        if (stepIncompleteIcons.Length > 0 && stepCompleteIcons.Length > 0)
        {
            stepIncompleteIcons[0].SetActive(!progress.completedForward);
            stepCompleteIcons[0].SetActive(progress.completedForward);
        }

        // 步驟 2 (向左)
        if (stepIncompleteIcons.Length > 1 && stepCompleteIcons.Length > 1)
        {
            stepIncompleteIcons[1].SetActive(!progress.completedLeft);
            stepCompleteIcons[1].SetActive(progress.completedLeft);
        }

        // 步驟 3 (向右)
        if (stepIncompleteIcons.Length > 2 && stepCompleteIcons.Length > 2)
        {
            stepIncompleteIcons[2].SetActive(!progress.completedRight);
            stepCompleteIcons[2].SetActive(progress.completedRight);
        }

        // 步驟 4 (向後)
        if (stepIncompleteIcons.Length > 3 && stepCompleteIcons.Length > 3)
        {
            stepIncompleteIcons[3].SetActive(!progress.completedBackward);
            stepCompleteIcons[3].SetActive(progress.completedBackward);
        }
    }
}