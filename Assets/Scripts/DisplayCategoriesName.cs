using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Video;

public class DisplayCategories : MonoBehaviour
{
    public string jsonFileName = "categories.json"; // Name of your JSON file
    public string mediaJsonFileName = "medias.json"; // Name of your JSON file for media
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
    private Material originalSkyboxMaterial; // Store the original skybox material
    private MediaResponse mediaResponse;

    [System.Serializable]
    public class Category
    {
        public string category;
        public string name;
        public string uid;
        public string status;
    }

    [System.Serializable]
    public class ProjectCategories
    {
        public string _id;
        public string project_id;
        public List<Category> categories;
        public string created_at;
        public string updated_at;
        public string domain_user_id;
        public string uid;
    }

    [System.Serializable]
    public class ProjectCategoryList
    {
        public List<ProjectCategories> results;
        public int count;
    }

    [System.Serializable]
    public class MediaFile
    {
        public string _id;
        public string name;
        public string file_url;
        public string category_id;
        public string project_id;
        public string status;
        public string created_at;
        public string updated_at;
        public string domain_user_id;
        public string uid;
    }

    [System.Serializable]
    public class MediaResponse
    {
        public List<MediaFile> results;
        public int count;
    }

    void Start()
    {
        StartCoroutine(LoadData());

        // Store the original skybox material
        originalSkyboxMaterial = RenderSettings.skybox;

        if (controlPanel != null)
        {
            Button exitButton = controlPanel.GetComponentInChildren<Button>();
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(Exit360View);
            }
            else
            {
                Debug.LogError("Exit button not found in the control panel.");
            }
        }
        else
        {
            Debug.LogError("Control panel is not assigned in the inspector.");
        }

        controlPanel.SetActive(false); // Initially hide the control panel
    }

    IEnumerator LoadData()
    {
        string jsonFilePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string mediaJsonFilePath = Path.Combine(Application.streamingAssetsPath, mediaJsonFileName);

        // Load the categories JSON file
        using (UnityWebRequest jsonRequest = UnityWebRequest.Get(jsonFilePath))
        {
            yield return jsonRequest.SendWebRequest();

            if (jsonRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonData = jsonRequest.downloadHandler.text;
                Debug.Log("Categories JSON loaded successfully.");
                PopulateCategories(jsonData);
            }
            else
            {
                Debug.LogError($"Error loading JSON file: {jsonRequest.error}");
            }
        }

        // Load the media JSON file
        using (UnityWebRequest mediaRequest = UnityWebRequest.Get(mediaJsonFilePath))
        {
            yield return mediaRequest.SendWebRequest();

            if (mediaRequest.result == UnityWebRequest.Result.Success)
            {
                string mediaJsonData = mediaRequest.downloadHandler.text;
                Debug.Log("Media JSON loaded successfully.");
                mediaResponse = JsonUtility.FromJson<MediaResponse>(mediaJsonData);
            }
            else
            {
                Debug.LogError($"Error loading media JSON file: {mediaRequest.error}");
            }
        }
    }

    // Function to populate categories
    public void PopulateCategories(string jsonData)
    {
        ProjectCategoryList categoryList = JsonUtility.FromJson<ProjectCategoryList>(jsonData);

        foreach (var projectCategory in categoryList.results)
        {
            foreach (var category in projectCategory.categories)
            {
                // Instantiate a new category button from the prefab
                GameObject categoryButton = Instantiate(categoryPrefab, CategoryNameContent);

                // Set the text to the category name
                TextMeshProUGUI textComponent = categoryButton.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent != null)
                {
                    textComponent.text = category.name;
                }

                // Add a click event to the category button
                Button button = categoryButton.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnCategoryClicked(category));
                }
            }
        }
    }

    // Function that handles what happens when a category button is clicked
    private void OnCategoryClicked(Category category)
    {
        Debug.Log($"Category clicked: {category.name}");
        switch (category.category)
        {
            case "I":
                Debug.Log("Category image clicked");
                displayImagesMedia(category.uid);
                break;
            case "V":
                Debug.Log("Category video clicked");
                displayVideosMedia(category.uid);
                break;
            case "3":
                Debug.Log("Category 360 image clicked");
                display360ImagesMedia(category.uid);
                break;
            default:
                Debug.Log("Unknown category clicked");
                break;
        }
    }
    private void display360ImagesMedia(string categoryUid)
    {
        // Clear existing media display
        foreach (Transform child in MediaContent)
        {
            Destroy(child.gameObject);
        }

        // Filter media based on category_id and ensure it’s a 360 image
        var filteredMedia = mediaResponse.results.FindAll(media => media.category_id == categoryUid);

        if (filteredMedia.Count > 0)
        {
            // Automatically display the first 360 image in full-screen
            Show360ImageFullScreen(filteredMedia[0].file_url);

            // Display filtered 360 images in the scroll view
            foreach (var media in filteredMedia)
            {
                GameObject mediaItem = Instantiate(imagePrefab, MediaContent);

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
                    textComponent.text = media.name;
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in imagePrefab!");
                }

                // Set the media thumbnail image (if applicable)
                Image imgComponent = mediaItem.GetComponent<Image>(); // Note: No GetComponentInChildren since Image is on the root
                if (imgComponent != null)
                {
                    StartCoroutine(LoadImage(media.file_url, imgComponent)); // This assumes you have a thumbnail or can use the 360 image itself
                }
                else
                {
                    Debug.LogError("Image component not found in imagePrefab!");
                }

                // Add a click listener to the button to handle 360 image clicks
                buttonComponent.onClick.AddListener(() => OnMediaClicked(media, "360Image"));
            }
        }
        else
        {
            Debug.Log("No 360 images found for the selected category.");
        }
    }

    private void Show360ImageFullScreen(string imageUrl)
    {
        // Start loading the 360 image and hide the objects after loading completes
        StartCoroutine(Load360Image(imageUrl));

        // Clear the full-screen panel if needed
        foreach (Transform child in FullScreenpanel)
        {
            Destroy(child.gameObject);
        }

        // Show the control panel (e.g., exit button) immediately
        controlPanel.SetActive(true);
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
        // Show the previously hidden objects
        foreach (GameObject obj in objectsToHide)
        {
            obj.SetActive(true);
        }

        // Hide the control panel
        if (controlPanel != null)
        {
            controlPanel.SetActive(false);
        }

        // Restore the original skybox material
        RenderSettings.skybox = originalSkyboxMaterial;
    }
    private void displayImagesMedia(string categoryUid)
    {
        // Clear existing media display
        foreach (Transform child in MediaContent)
        {
            Destroy(child.gameObject);
        }

        // Filter media based on category_id
        var filteredMedia = mediaResponse.results.FindAll(media => media.category_id == categoryUid);

        if (filteredMedia.Count > 0)
        {
            // Automatically display the first image in full-screen
            ShowImageFullScreen(filteredMedia[0].file_url);

            // Display filtered images in the scroll view
            foreach (var media in filteredMedia)
            {
                GameObject mediaItem = Instantiate(imagePrefab, MediaContent);

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
                    textComponent.text = media.name;
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in imagePrefab!");
                }

                // Set the media image
                Image imgComponent = mediaItem.GetComponent<Image>(); // Note: No GetComponentInChildren since Image is on the root
                if (imgComponent != null)
                {
                    StartCoroutine(LoadImage(media.file_url, imgComponent));
                }
                else
                {
                    Debug.LogError("Image component not found in imagePrefab!");
                }

                // Add a click listener to the button to handle image clicks
                buttonComponent.onClick.AddListener(() => OnMediaClicked(media, "Image"));
            }
        }
        else
        {
            Debug.Log("No images found for the selected category.");
        }
    }


    private void displayVideosMedia(string categoryUid)
    {
        // Clear existing media display
        foreach (Transform child in MediaContent)
        {
            Destroy(child.gameObject);
        }

        // Filter media based on category_id and ensure it’s a video (e.g., ends with .mp4)
        var filteredMedia = mediaResponse.results.FindAll(media => media.category_id == categoryUid && media.file_url.EndsWith(".mp4"));

        if (filteredMedia.Count > 0)
        {
            // Automatically display the first video in full-screen
            ShowVideoFullScreen(filteredMedia[0].file_url);

            // Display filtered videos in the scroll view
            foreach (var media in filteredMedia)
            {
                GameObject mediaItem = Instantiate(videoPrefab, MediaContent);

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
                    textComponent.text = media.name;
                }
                else
                {
                    Debug.LogError("TextMeshProUGUI component not found in videoPrefab!");
                }

                // Add a click listener to the button to handle video clicks
                buttonComponent.onClick.AddListener(() => OnMediaClicked(media, "Video"));
            }
        }
        else
        {
            Debug.Log("No videos found for the selected category.");
        }
    }


    IEnumerator LoadImage(string url, Image targetImage)
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


    private void OnMediaClicked(MediaFile media, string mediaType)
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
            Debug.Log("Video clicked");
            ShowVideoFullScreen(media.file_url);
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

    private void ShowVideoFullScreen(string videoUrl)
    {
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

            // Optionally, set the aspect ratio or scaling of the RawImage
            // rawImage.SetNativeSize(); // If you want the RawImage to match the video size
        }
        else
        {
            Debug.LogError("VideoPlayer or RawImage component not found in FullScreenVideoPrefab!");
        }
    }


}
