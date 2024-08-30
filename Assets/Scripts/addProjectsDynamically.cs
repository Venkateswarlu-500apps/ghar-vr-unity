using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Networking;

public class AddProjectsDynamically : MonoBehaviour
{
    public string ProjectsApiUrl = "https://api.mantrareal.com/db/projects?page=1&limit=25&sort=desc&search=";
    private string accessToken; // To store the access token
    public GameObject content; // Reference to the Content object with a Grid Layout Group
    public GameObject projectCardPrefab; // Assign this prefab in the Unity Inspector
    public GameObject galleryViewPanel;
    public GameObject projectWallPanel;


    [System.Serializable]
    public class GalleryItem
    {
        public string name;
        public string uid;
        public string file_url;
        public string? tts_description;
        public string? is_360;
        public string? is_default_image;
        public List<HotspotData> walk_through_data; // Data for hotspots
    }

    [System.Serializable]
    public class HotspotMessage
    {
        public string text;
        public string color;
        public int size;
    }

    [System.Serializable]
    public class HotspotData
    {
        public Vector3 position;
        public string imageUid;
        public string pathIconUrl;
        public HotspotMessage message;
    }

    [System.Serializable]
    public class ProjectCategory
    {
        public string category_name;
        public string category_type;
        public List<GalleryItem> items;
    }

    [System.Serializable]
    public class ProjectJson
    {
        public List<ProjectCategory> categoriesWithGallery;
    }

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
        public ProjectJson project_json;
    }

    [System.Serializable]
    public class ProjectList
    {
        public List<Project> results;
        public int count;
    }

    void Start()
    {
        // Retrieve the access token from PlayerPrefs
        accessToken = PlayerPrefs.GetString("access_token", "");

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("Access token not found. Please log in.");
          //  return;
        }

        // Start fetching project data
        StartCoroutine(FetchProjectData());
    }

    IEnumerator FetchProjectData()
    {
        Debug.Log("Started fetching");
        using (UnityWebRequest request = UnityWebRequest.Get(ProjectsApiUrl))
        {
            // Set the headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-mantra-app", "gharpe.vr");
          //  request.SetRequestHeader("Authorization", "Bearer " + accessToken); // Use the retrieved token
             request.SetRequestHeader("Authorization", "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJoLmFyaXNoYW5rZXIuNTAwYXBwc0BnbWFpbC5jb20iLCJfaWQiOiI2NmJlZjY1ODI3M2M1NmQ3NDI2ZTZjMTQiLCJleHAiOjE3MjY5ODI4Nzl9.3xt-XAbef6LA6Mo5bnUzCq_usX-oO8BpYOFkT80wOhw");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error fetching project data: " + request.error);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                // Save data
                PlayerPrefs.SetString("projectData", jsonData);
                PlayerPrefs.Save();
                ProcessJsonData(jsonData);
            }
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
            // Instantiate the project card prefab
            GameObject projectObject = Instantiate(projectCardPrefab, content.transform);

            // Set the project name text
            TextMeshProUGUI nameComponent = projectObject.GetComponentInChildren<TextMeshProUGUI>();
            if (nameComponent != null)
            {
                nameComponent.text = project.name;
            }
            else
            {
                Debug.LogError("TextMeshProUGUI component not found in project card prefab.");
            }

            // Set the logo image if it exists
            Image logoImage = projectObject.GetComponentInChildren<Image>();
            if (logoImage != null && !string.IsNullOrEmpty(project.logo))
            {
                string base64Data = project.logo;
                if (base64Data.StartsWith("data:image/jpeg;base64,"))
                {
                    base64Data = base64Data.Substring("data:image/jpeg;base64,".Length);
                }
                else if (base64Data.StartsWith("data:image/png;base64,"))
                {
                    base64Data = base64Data.Substring("data:image/png;base64,".Length);
                }

                byte[] imageBytes = Convert.FromBase64String(base64Data);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(imageBytes);

                Rect rect = new Rect(0, 0, texture.width, texture.height);
                logoImage.sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogWarning("Logo image is null or not found.");
            }

            // Add the click event listener to the button
            Button button = projectObject.GetComponent<Button>();
            if (button != null)
            {
                Debug.Log("Adding click listener to the button.");
                button.onClick.AddListener(() => OnProjectCardClicked(project));
            }
            else
            {
                Debug.LogError("Button component not found in project card prefab.");
            }

            // Ensure the project card is placed within the grid layout properly
            projectObject.transform.SetAsLastSibling();
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing project: {project.name}. Error: {e.Message}");
        }
    }

    // Method to handle the click event
    void OnProjectCardClicked(Project project)
    {
        Debug.Log($"Project card clicked: {project.name}");
        // Serialize the selected project to JSON and save it in PlayerPrefs
        string projectJson = JsonUtility.ToJson(project);
        PlayerPrefs.SetString("SelectedProject", projectJson);
        PlayerPrefs.Save();

        // Switch to the gallery view
        ShowGalleryView();
    }


    void ShowGalleryView()
    {
        // Assuming you are using panels to switch between views, deactivate the project wall panel and activate the gallery view panel
        if (projectWallPanel != null && galleryViewPanel != null)
        {
            projectWallPanel.SetActive(false);  // Hide the project wall panel
            galleryViewPanel.SetActive(true);   // Show the gallery view panel
        }
        else
        {
            Debug.LogError("Panel references not set in the inspector.");
        }

        // Trigger the loading of the gallery view content
        DisplayCategories displayCategories = galleryViewPanel.GetComponent<DisplayCategories>();
        if (displayCategories != null)
        {
            Debug.Log("calling  the InitializeDisplay");
            displayCategories.InitializeDisplay();  // Call the custom method instead of Start()
        }
        else
        {
            Debug.LogError("DisplayCategories script not found on the gallery view panel.");
        }
    }

    // This method is called when the GameObject or its component is enabled
    private void OnEnable()
    {
        Debug.Log($"{gameObject.name} is now active.");
        // RunFunctionWhenActive();
    }

}
