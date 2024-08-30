using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Video;
using UnityEngine.EventSystems;
public class DisplayCategories : MonoBehaviour
{
    public GameObject categoryPrefab; // Prefab for the category button
    public GameObject imagePrefab; // Prefab for displaying images
    public GameObject videoPrefab; // Prefab for displaying videos thumbnails
    public Transform CategoryNameContent; // Reference to the Content GameObject in the Scroll View for categories
    public Transform MediaContent; // Reference to the Content GameObject in the Scroll View for media
    public Transform FullScreenpanel;
    public GameObject FullScreenImagePrefab;
    public GameObject FullScreenVideoPrefab;
    private Material skyboxMaterial; // Reference to the skybox material
    public GameObject[] objectsToHide; // Array to hold the objects to hide (e.g., plane, wall)
    public GameObject controlPanel; // Correct this to controlPanel
    public GameObject galleryViewPanel;
    public GameObject projectWallPanel;
    private Material originalSkyboxMaterial; // Store the original skybox material
    public GameObject hotspotPrefab; // Prefab for hotspots
    public Transform hotspotContainer; // Container for hotspots
    private int currentImageIndex = 0; // To keep track of the current 360° image
    private List<GalleryItem> vrTourItems; // To store VR tour items
    public Sprite[] pathIcons; // Array to hold all sprites
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

    void Start()
    {
        // Store the original skybox material
        originalSkyboxMaterial = RenderSettings.skybox;

        if (controlPanel != null)
        {
            // Exit button setup
            GameObject exitButtonObject = controlPanel.transform.Find("ExitVrButton").gameObject; // Get the GameObject of the Exit button
            Button exitButton = exitButtonObject.GetComponent<Button>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(Exit360View);
                exitButtonObject.SetActive(false); // Hide the button's GameObject
            }
            else
            {
                Debug.LogError("Exit button not found in the control panel.");
            }

            // Back button setup
            GameObject backButtonObject = controlPanel.transform.Find("BackButton").gameObject; // Get the GameObject of the Back button
            Button backButton = backButtonObject.GetComponent<Button>();
            if (backButton != null)
            {
                backButton.onClick.AddListener(BackToProjectsWall);
            }
            else
            {
                Debug.LogError("Back button not found in the control panel.");
            }
        }
        else
        {
            Debug.LogError("Control panel is not assigned in the inspector.");
        }
    }

    private void BackToProjectsWall()
    {
        Debug.Log("Navigating back to the projects wall...");
        // Implement your logic to navigate back to the projects wall
        // Restore the original skybox if changed
        RenderSettings.skybox = originalSkyboxMaterial;

        // Show the previously hidden objects
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(true);
        }

        // Clear the full-screen panel
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        // Clear existing media display
        foreach (Transform child in MediaContent)
        {
            Destroy(child.gameObject);
        }

        // Clear existing category display
        foreach (Transform child in CategoryNameContent)
        {
            Destroy(child.gameObject);
        }

        // Clear existing hotspots
        foreach (Transform child in hotspotContainer)
        {
            Destroy(child.gameObject);
        }

        // Additional cleanup, like stopping any ongoing 360 video or image load
        if (videoPlayerObject != null)
        {
            VideoPlayer videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.Stop(); // Stop the video playback
            }

            Destroy(videoPlayerObject); // Destroy the VideoPlayer GameObject
            videoPlayerObject = null; // Reset the reference
        }

        // Hide the control panel
        if (controlPanel != null)
        {
            GameObject exitButtonObject = controlPanel.transform.Find("ExitVrButton").gameObject; // Get the GameObject of the Exit button
            exitButtonObject.SetActive(false);
            controlPanel.SetActive(false);
        }

        if (projectWallPanel != null && galleryViewPanel != null)
        {
            Debug.Log("projectWallPanel projectWallPanel-------------------projectWallPanel----------projectWallPanel--------- ");
            projectWallPanel.SetActive(true);  // Show the project wall panel
            galleryViewPanel.SetActive(false);   // Hide the gallery view panel
        }

        // Navigate back to the projects wall - this could be a different scene, a UI state change, or re-enabling specific UI elements
        Debug.Log("Navigating back to the projects wall...");
    }

    public void InitializeDisplay()
    {
        Debug.Log("initializing display");

        // Hide the control panel
        if (controlPanel != null)
        {
            controlPanel.SetActive(true);
        }

        // This is the method you should call when switching to the gallery view
        // Retrieve the selected project from PlayerPrefs
        string projectJson = PlayerPrefs.GetString("SelectedProject", "");
        if (!string.IsNullOrEmpty(projectJson))
        {
            Project selectedProject = JsonUtility.FromJson<Project>(projectJson);
            if (selectedProject != null)
            {
                // Populate the categories in the UI
                PopulateCategories(selectedProject.project_json.categoriesWithGallery);
            }
            else
            {
                Debug.LogError("Failed to deserialize project data from PlayerPrefs.");
            }
        }
        else
        {
            Debug.LogError("No project data found in PlayerPrefs.");
        }
    }

    public void PopulateCategories(List<ProjectCategory> categories)
    {
        foreach (var category in categories)
        {
            // Check if the category has any items
            if (category.items.Count <= 0)
            {
                continue; // Skip the category if there are no items
            }
            // Instantiate a new category button from the prefab
            GameObject categoryButton = Instantiate(categoryPrefab, CategoryNameContent);

            // Set the text to the category name
            TextMeshProUGUI textComponent = categoryButton.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = category.category_name;
            }

            // Add a click event to the category button
            Button button = categoryButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnCategoryClicked(category));
            }
        }
    }

    private void OnCategoryClicked(ProjectCategory category)
    {
        Debug.Log($"Category clicked: {category.category_name}");
        switch (category.category_type)
        {
            case "I":
                Debug.Log("Category image clicked");
                DisplayMedia(category.items, "Image");
                break;
            case "V":
                Debug.Log("Category video clicked");
                DisplayMedia(category.items, "Video");
                break;
            case "3":
                Debug.Log("Category 360 image clicked");
                DisplayMedia(category.items, "360Image");
                break;
            case "W":
                Debug.Log("Category VRTour clicked");
                DisplayMedia(category.items, "VRTour");
                break;
            default:
                Debug.Log("Unknown category clicked");
                break;
        }
    }

    private void DisplayMedia(List<GalleryItem> items, string mediaType)
    {
        // Clear existing media display
        foreach (Transform child in MediaContent)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        if (items.Count > 0)
        {
            // Automatically display the first item in full-screen
            if (mediaType == "Image")
            {
                ShowImageFullScreen(items[0].file_url);
            }
            else if (mediaType == "Video" && items[0].is_360 != "true")
            {
                ShowVideoFullScreen(items[0]);
            }
            else if (mediaType == "360Image")
            {
                // Show360ImageFullScreen(items[0].file_url);
            }
            else if (mediaType == "VRTour")
            {
                StartVRTour(items);
                return;
            }

            // Display filtered media in the scroll view
            foreach (var item in items)
            {
                // Debug.Log("is_360", item.is_360);
                GameObject mediaItem = Instantiate(mediaType == "Video" ? videoPrefab : imagePrefab, MediaContent);

                Debug.Log("item.is_360" + item.is_360);

                // Check if the Button component is present
                Button buttonComponent = mediaItem.GetComponent<Button>();
                if (buttonComponent == null)
                {
                    Debug.LogError("Button component is missing from the prefab.");
                    continue; // Skip to the next iteration if Button is missing
                }

                // Set the media name text
                TextMeshProUGUI textComponent = mediaItem.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = item.name;
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in prefab!");
                }

                // Set the media image or thumbnail
                Image imgComponent = mediaItem.GetComponent<Image>(); // Note: No GetComponentInChildren since Image is on the root
                if (imgComponent != null && mediaType != "Video")
                {
                    StartCoroutine(LoadImage(item.file_url, imgComponent));
                }
                // Add a click listener to the button to handle media clicks
                buttonComponent.onClick.AddListener(() => OnMediaClicked(item, mediaType));
            }
        }
        else
        {
            Debug.Log("No media found for the selected category.");
        }
    }

    private void StartVRTour(List<GalleryItem> items)
    {
        vrTourItems = items; // Store the VR tour items
        currentImageIndex = 0; // Default to the first image

        // Find the image marked as default
        for (int i = 0; i < vrTourItems.Count; i++)
        {
            // Check if the image is marked as default
            if (vrTourItems[i].is_default_image == "true") // Compare as string
            {
                currentImageIndex = i;
                break; // Stop as soon as we find the default image
            }
        }

        Show360ImageFullScreen(vrTourItems[currentImageIndex].file_url);

        // Ensure walk_through_data is not null before passing it
        if (vrTourItems[currentImageIndex].walk_through_data != null)
        {
            CreateHotspots(vrTourItems[currentImageIndex].walk_through_data);
        }
        else
        {
            Debug.LogWarning("No walk_through_data found for the default image.");
        }
    }
    private void CreateHotspots(List<HotspotData> hotspots)
    {
        Debug.Log($"Creating {hotspots.Count} hotspots");

        // Clear existing hotspots
        foreach (Transform child in hotspotContainer)
        {
            Destroy(child.gameObject);
        }

        // Get the camera (assuming the main camera is used for VR)
        Camera vrCamera = Camera.main;
        if (vrCamera == null)
        {
            Debug.LogError("Main camera not found. Ensure the camera is tagged as MainCamera.");
            return;
        }

        // Iterate over hotspot data and create hotspots
        foreach (var hotspotData in hotspots)
        {
            // Check if position exists in the hotspot data
            if (hotspotData.position != null)
            {
                Debug.Log($"Creating hotspot at position: {hotspotData.position}, ImageUid: {hotspotData.imageUid}");

                // Instantiate the hotspot prefab
                GameObject hotspot = Instantiate(hotspotPrefab, hotspotContainer);

                // Adjust position and invert X-axis
                hotspot.transform.localPosition = new Vector3(
                    -hotspotData.position.x,
                    hotspotData.position.y,
                    hotspotData.position.z
                );

                // Reset rotation to identity to ensure no unwanted rotation
                hotspot.transform.localRotation = Quaternion.identity;

                // Calculate the direction from the hotspot to the camera
                Vector3 directionToCamera = (vrCamera.transform.position - hotspot.transform.position).normalized;

                // Correct the rotation to make the hotspot face the camera
                hotspot.transform.rotation = Quaternion.LookRotation(directionToCamera);

                // Flip the hotspot around the Y-axis to ensure text is not mirrored
                hotspot.transform.Rotate(0, 180, 0);

                // Handle the button if an icon URL is present
                Button hotspotButton = hotspot.GetComponentInChildren<Button>();
                if (!string.IsNullOrEmpty(hotspotData.pathIconUrl))
                {
                    if (hotspotButton != null && !string.IsNullOrEmpty(hotspotData.imageUid))
                    {
                        hotspotButton.onClick.AddListener(() => ChangeVRImage(hotspotData.imageUid));
                        Debug.Log($"Hotspot button listener added for ImageUid: {hotspotData.imageUid}");

                        string filename = System.IO.Path.GetFileName(hotspotData.pathIconUrl);
                        AssignIconFromArray(filename, hotspotButton.GetComponent<Image>());
                    }

                    // Remove message text if button is present
                    TextMeshProUGUI messageText = hotspot.GetComponentInChildren<TextMeshProUGUI>();
                    if (hotspotData.message != null && !string.IsNullOrEmpty(hotspotData.message.text))
                    {
                        messageText.text = hotspotData.message.text;
                        messageText.color = HexToColor(hotspotData.message.color);
                        messageText.fontSize = hotspotData.message.size;
                    }
                    else
                    {
                        Debug.Log("No message text found");
                        Destroy(messageText.gameObject);
                    }
                }
                else if (hotspotData.message != null && !string.IsNullOrEmpty(hotspotData.message.text))
                {
                    // Remove button if only a message is needed
                    if (hotspotButton != null)
                    {
                        Destroy(hotspotButton.gameObject);
                    }

                    // Create a new TextMeshProUGUI object to display the message
                    GameObject textObject = new GameObject("HotspotMessage");
                    textObject.transform.SetParent(hotspot.transform, false); // Set the parent to the hotspot

                    // Set the position relative to the hotspot
                    textObject.transform.localPosition = Vector3.zero;

                    // Add TextMeshProUGUI component
                    TextMeshProUGUI messageText = textObject.AddComponent<TextMeshProUGUI>();
                    messageText.text = hotspotData.message.text;
                    messageText.color = HexToColor(hotspotData.message.color);
                    messageText.fontSize = hotspotData.message.size;
                    messageText.alignment = TextAlignmentOptions.Center;

                    // Set additional properties to make the text visible
                    messageText.enableWordWrapping = true;
                    messageText.rectTransform.sizeDelta = new Vector2(200, 100); // Set appropriate size
                }
            }
        }
    }


    // Method to convert Hex color to Unity Color
    private Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color color))
        {
            return color;
        }
        Debug.LogError($"Invalid hex color: {hex}. Defaulting to white.");
        return Color.white;
    }


    // Method to assign the icon from the array
    private void AssignIconFromArray(string filename, Image targetImage)
    {
        foreach (Sprite sprite in pathIcons)
        {
            if (sprite.name == filename.Replace(".png", ""))
            {
                targetImage.sprite = sprite;
                Debug.Log($"Assigned icon from array: {filename}");
                return;
            }
        }
        Debug.LogError($"Icon with filename {filename} not found in the array.");
    }

    private void ChangeVRImage(string imageUid)
    {
        Debug.Log("Changing VR image to: " + imageUid);

        for (int i = 0; i < vrTourItems.Count; i++)
        {
            Debug.Log("vrTourItems[i].uid: " + vrTourItems[i].uid);

            // Trim both strings to ensure there are no leading/trailing spaces causing mismatch
            if (vrTourItems[i].uid == imageUid)
            {
                currentImageIndex = i;
                Show360ImageFullScreen(vrTourItems[currentImageIndex].file_url);
                CreateHotspots(vrTourItems[currentImageIndex].walk_through_data);
                break;
            }
        }
    }

    private void Show360ImageFullScreen(string imageUrl)
    {
        // Start loading the 360 image and hide the objects after loading completes
        CoroutineManager.StartManagedCoroutine(Load360Image(imageUrl));

        // Clear the full-screen panel if needed
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        // Show the exit button immediately
        GameObject exitButtonObject = controlPanel.transform.Find("ExitVrButton").gameObject; // Get the GameObject of the Exit button
        exitButtonObject.SetActive(true);
    }

    private IEnumerator Load360Image(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);

                // Check if the material is assigned, otherwise create one
                if (skyboxMaterial == null)
                {
                    skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
                }

                // Assign the texture to the skybox material
                skyboxMaterial.SetTexture("_MainTex", texture);

                // Assign the material as the skybox
                RenderSettings.skybox = skyboxMaterial;

                // Optionally, rotate the skybox to ensure the correct starting angle
                RenderSettings.skybox.SetFloat("_Rotation", 0f); // Adjust the rotation as needed

                // After the image is successfully loaded and applied, hide the specified objects
                foreach (GameObject obj in objectsToHide)
                {
                    obj.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Failed to load 360 image: " + request.error);
            }
        }
    }

    private void Exit360View()
    {
        // Stop the video player and destroy it if it exists
        if (videoPlayerObject != null)
        {
            VideoPlayer videoPlayer = videoPlayerObject.GetComponent<VideoPlayer>();
            if (videoPlayer != null)
            {
                videoPlayer.Stop(); // Stop the video playback
            }

            Destroy(videoPlayerObject); // Destroy the VideoPlayer GameObject
            videoPlayerObject = null; // Reset the reference
        }

        // Restore the original skybox material
        RenderSettings.skybox = originalSkyboxMaterial;

        // Clear existing hotspots
        foreach (Transform child in hotspotContainer)
        {
            Destroy(child.gameObject);
        }

        // Show the previously hidden objects
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(true);
        }

        // Hide the control panel
        if (controlPanel != null)
        {
            GameObject exitButtonObject = controlPanel.transform.Find("ExitVrButton").gameObject; // Get the GameObject of the Exit button
            exitButtonObject.SetActive(false);
        }
    }

    private IEnumerator LoadImage(string url, Image targetImage)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(www);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                targetImage.sprite = sprite;
            }
            else
            {
                Debug.LogError($"Failed to load image from URL: {url}. Error: {www.error}");
            }
        }
    }

    private void OnMediaClicked(GalleryItem media, string mediaType)
    {
        Debug.Log($"Media clicked: {media.name}, URL: {media.file_url}, mediaType: {mediaType}");

        // Clear the full-screen panel
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        if (mediaType == "Image")
        {
            // Handle normal image click
            Debug.Log("Image clicked");
            ShowImageFullScreen(media.file_url);
        }
        else if (mediaType == "Video")
        {
            // Handle video click
            Debug.Log("Video clicked: ");
            ShowVideoFullScreen(media);
        }
        else if (mediaType == "360Image")
        {
            // Handle 360 image click
            Debug.Log("360 Image clicked");
            Show360ImageFullScreen(media.file_url);
        }
    }

    private void ShowImageFullScreen(string imageUrl)
    {
        // Clear the full-screen panel
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }
        // Instantiate the full-screen image prefab
        GameObject fullScreenImage = Instantiate(FullScreenImagePrefab, FullScreenpanel);

        // Get the Image component from the instantiated prefab
        Image imgComponent = fullScreenImage.GetComponent<Image>();
        if (imgComponent != null)
        {
            StartCoroutine(LoadImage(imageUrl, imgComponent));
        }
        else
        {
            Debug.LogError("Image component not found in FullScreenImagePrefab!");
        }
    }

    private void ShowVideoFullScreen(GalleryItem video)
    {
        // Check if the video is 360°
        if (video.is_360 == "true")
        {
            Debug.Log("360 video clicked");
            Show360VideoFullScreen(video.file_url);
            return;
        }
        string videoUrl = video.file_url;

        // Clear the full-screen panel
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        // Instantiate the full-screen video prefab
        GameObject fullScreenVideo = Instantiate(FullScreenVideoPrefab, FullScreenpanel);

        // Get the VideoPlayer component from the instantiated prefab
        VideoPlayer videoPlayer = fullScreenVideo.GetComponent<VideoPlayer>();

        // Get the RawImage component from the instantiated prefab
        RawImage rawImage = fullScreenVideo.GetComponent<RawImage>();

        if (videoPlayer != null && rawImage != null)
        {
            videoPlayer.url = videoUrl; // Set the video URL
            videoPlayer.Play(); // Play the video automatically

            // Set the RawImage texture to the VideoPlayer's Render Texture
            rawImage.texture = videoPlayer.targetTexture;
        }
        else
        {
            Debug.LogError("VideoPlayer or RawImage component not found in FullScreenVideoPrefab!");
        }
    }

    private void Show360VideoFullScreen(string videoUrl)
    {
        // Start loading the 360 video and apply it to the skybox
        StartCoroutine(Load360Video(videoUrl));

        // Clear the full-screen panel if needed
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        // Show the exit button immediately
        GameObject exitButtonObject = controlPanel.transform.Find("ExitVrButton").gameObject; // Get the GameObject of the Exit button
        exitButtonObject.SetActive(true);
    }

    private GameObject videoPlayerObject; // Store the reference to the VideoPlayer GameObject
    private IEnumerator Load360Video(string videoUrl)
    {
        // Create a new GameObject to hold the VideoPlayer
        videoPlayerObject = new GameObject("360VideoPlayer");
        VideoPlayer videoPlayer = videoPlayerObject.AddComponent<VideoPlayer>();

        // Set the video URL
        videoPlayer.url = videoUrl;
        videoPlayer.isLooping = true; // Loop the video if needed

        // Wait for the video to prepare
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        // Get the video's resolution
        int videoWidth = (int)videoPlayer.width;
        int videoHeight = (int)videoPlayer.height;

        Debug.Log($"Video Resolution: {videoWidth} x {videoHeight}");

        // Create a RenderTexture with the same resolution as the video
        RenderTexture renderTexture = new RenderTexture(videoWidth, videoHeight, 16);
        videoPlayer.targetTexture = renderTexture;

        // Play the video
        videoPlayer.Play();

        // Check if the material is assigned, otherwise create one
        if (skyboxMaterial == null)
        {
            skyboxMaterial = new Material(Shader.Find("Skybox/Panoramic"));
        }

        // Assign the RenderTexture to the skybox material
        skyboxMaterial.SetTexture("_MainTex", renderTexture);

        // Assign the material as the skybox
        RenderSettings.skybox = skyboxMaterial;

        // Optionally, rotate the skybox to ensure the correct starting angle
        RenderSettings.skybox.SetFloat("_Rotation", 0f); // Adjust the rotation as needed

        // After the video is successfully loaded and applied, hide the specified objects
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(false);
        }
    }
}
