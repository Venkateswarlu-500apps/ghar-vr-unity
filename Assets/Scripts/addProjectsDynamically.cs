using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;  // Make sure this is included to use Convert

public class AddProjectsDynamically : MonoBehaviour
{
    public string jsonFilePath = "projects.json"; // Set this to your file name if in StreamingAssets
    public GameObject content; // Reference to the Content object with a Grid Layout Group

    [System.Serializable]
    public class Project
    {
        public string _id;
        public string name;
        public string description;
        public List<string> devices;
        public string fileName;
        public string domain_user_id;
        public string uid;
        public string logo; // Base64 encoded image data
    }

    [System.Serializable]
    public class ProjectList
    {
        public List<Project> results;
        public int count;
    }

    void Start()
    {
        // Load JSON data from StreamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, jsonFilePath);
        
        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            ProcessJsonData(jsonData);
        }
        else
        {
            Debug.LogError("JSON file not found at path: " + path);
        }
    }

    void ProcessJsonData(string jsonData)
    {
        if (content != null && !string.IsNullOrEmpty(jsonData))
        {
            ProjectList projectList = JsonUtility.FromJson<ProjectList>(jsonData);
            foreach (var project in projectList.results)
            {
                AppendProjectCard(project);
            }
        }
        else
        {
            Debug.LogError("Content object or JSON data is not set.");
        }
    }
void AppendProjectCard(Project project)
{
    try
    {
        // Remove the data URL prefix if present
        string base64Data = project.logo;
        if (base64Data.StartsWith("data:image/jpeg;base64,"))
        {
            base64Data = base64Data.Substring("data:image/jpeg;base64,".Length);
        }

        // Convert the Base64 string to a byte array
        byte[] imageBytes = Convert.FromBase64String(base64Data);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(imageBytes);

        // Create a new GameObject to hold the image
        GameObject imageObject = new GameObject("GridImage");
        imageObject.transform.SetParent(content.transform, false);

        // Add the Image component to the new GameObject
        Image imageComponent = imageObject.AddComponent<Image>();

        // Convert the texture to a Sprite and assign it to the Image component
        Rect rect = new Rect(0, 0, texture.width, texture.height);
        imageComponent.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

        // Adjust the RectTransform to fit within the grid cell
        RectTransform rectTransform = imageComponent.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        // Add a TextMeshProUGUI component to display the project name
        GameObject textObject = new GameObject("ImageText");
        textObject.transform.SetParent(imageObject.transform, false);

        TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
        textComponent.text = project.name;
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Bottom;

        // Set up text to truncate with ellipsis
        textComponent.enableWordWrapping = false;
        textComponent.overflowMode = TextOverflowModes.Ellipsis;

        // Adjust the RectTransform of the Text to be positioned at the bottom of the image
        RectTransform textRectTransform = textComponent.GetComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0, 0);
        textRectTransform.anchorMax = new Vector2(1, 0);
        textRectTransform.pivot = new Vector2(0.5f, 0);
        textRectTransform.anchoredPosition = new Vector2(0, 10);
        textRectTransform.sizeDelta = new Vector2(0, 30);

        // Add a Button component to the imageObject
        Button button = imageObject.AddComponent<Button>();

        // Add the click event listener
        button.onClick.AddListener(() => OnProjectCardClicked(project));

        // Ensure the image is placed within the grid layout properly
        imageObject.transform.SetAsLastSibling();
    }
    catch (FormatException e)
    {
        Debug.LogError($"Invalid Base64 string for project: {project.name}. Error: {e.Message}");
    }
}

// Method to handle the click event
void OnProjectCardClicked(Project project)
{
    Debug.Log($"Project card clicked: {project.name}");
    // Add your logic here, such as opening a detailed view, navigating to another scene, etc.
}

}
