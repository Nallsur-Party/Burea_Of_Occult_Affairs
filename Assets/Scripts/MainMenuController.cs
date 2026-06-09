using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button endShiftButton;
    [SerializeField] private string gameplaySceneName = "MechanicsTestScene";

    private void Awake()
    {
        ResolveButtons();
        BindButtons();
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("MainMenuController | gameplaySceneName is empty.", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void EndShift()
    {
        Debug.Log("MainMenuController | Ending shift and quitting application.", this);

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ResolveButtons()
    {
        if (startGameButton == null)
        {
            startGameButton = FindButtonByText("Начать смену", "Начать игру");
        }

        if (endShiftButton == null)
        {
            endShiftButton = FindButtonByText("Закончить смену", "Закончить игру", "Выход");
        }
    }

    private void BindButtons()
    {
        BindButton(startGameButton, StartGame);
        BindButton(endShiftButton, EndShift);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            Debug.LogWarning("MainMenuController could not find one of the menu buttons.", this);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static Button FindButtonByText(params string[] candidateTexts)
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        HashSet<string> candidates = new HashSet<string>(candidateTexts.Where(text => !string.IsNullOrWhiteSpace(text)));

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null || button.gameObject == null)
            {
                continue;
            }

            GameObject buttonObject = button.gameObject;
            if (!buttonObject.scene.IsValid() || !buttonObject.scene.isLoaded)
            {
                continue;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null && candidates.Contains(label.text.Trim()))
            {
                return button;
            }

            if (candidates.Contains(buttonObject.name.Trim()))
            {
                return button;
            }
        }

        return null;
    }
}
