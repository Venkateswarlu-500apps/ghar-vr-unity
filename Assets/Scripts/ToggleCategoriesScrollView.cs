using UnityEngine;
using UnityEngine.UI;

public class ToggleCategoriesScrollView : MonoBehaviour
{
    public GameObject categoriesScrollView; // Reference to the CategoriesScrollView
    public Button toggleButton; // Reference to the toggle button
    public RectTransform toggleButtonRectTransform; // RectTransform of the toggle button for position adjustments
    public Sprite downArrow; // Reference to the down arrow sprite
    public Sprite upArrow; // Reference to the up arrow sprite

    private bool isHidden = false; // To track if the container is hidden
    private Vector2 originalPosition; // To store the original position of the toggle button

    void Start()
    {
        // Store the original position of the toggle button
        originalPosition = toggleButtonRectTransform.anchoredPosition;

        // Add listener to the button
        toggleButton.onClick.AddListener(ToggleScrollView);
    }

    void ToggleScrollView()
    {
        isHidden = !isHidden;

        if (isHidden)
        {
            Debug.Log("Hiding: Before: " + toggleButtonRectTransform.anchoredPosition);
            // Hide the container
            categoriesScrollView.SetActive(false);

            // Change the button image to up arrow
            toggleButton.image.sprite = upArrow;

            // Move the button to the bottom
            toggleButtonRectTransform.anchoredPosition = new Vector2(toggleButtonRectTransform.anchoredPosition.x, -296f);
            Debug.Log("Hiding: After: " + toggleButtonRectTransform.anchoredPosition);
        }
        else
        {
            Debug.Log("Showing: Before: " + toggleButtonRectTransform.anchoredPosition);
            // Show the container
            categoriesScrollView.SetActive(true);

            // Change the button image to down arrow
            toggleButton.image.sprite = downArrow;

            // Restore the original position
            toggleButtonRectTransform.anchoredPosition = new Vector2(toggleButtonRectTransform.anchoredPosition.x, originalPosition.y);
            Debug.Log("Showing: After: " + toggleButtonRectTransform.anchoredPosition);
        }
    }

}
