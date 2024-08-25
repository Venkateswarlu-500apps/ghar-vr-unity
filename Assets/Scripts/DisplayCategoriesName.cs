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
    public GameObject spherePrefab; // Prefab for the 360 image sphere
    public Transform FullScreen3DImagePanel; // Panel to contain the 360 image sphere

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
        // Load JSON data from StreamingAssets
        string path = Path.Combine(Application.streamingAssetsPath, jsonFileName);
        string mediaPath = Path.Combine(Application.streamingAssetsPath, mediaJsonFileName);

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            PopulateCategories(jsonData);
        }
        else
        {
            Debug.LogError("JSON file not found at path: " + path);
        }

        if (File.Exists(mediaPath))
        {
            string mediaJsonData = File.ReadAllText(mediaPath);
            mediaResponse = JsonUtility.FromJson<MediaResponse>(mediaJsonData);
        }
        else
        {
            Debug.LogError("Media JSON file not found at path: " + mediaPath);
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
        if (category.category == "I")
        {
            Debug.Log("Category image clicked");
            displayImagesMedia(category.uid);
        }
        else if (category.category == "V")
        {
            Debug.Log("Category video clicked");
            displayVideosMedia(category.uid);
        }
        else if (category.category == "3")
        {
            Debug.Log("Category 360 image clicked");
            //  display360ImagesMedia(category.uid);
        }
        else
        {
            Debug.Log("Unknown category clicked");
        }
    }

    private void display360ImagesMedia(string categoryUid)
    {
        // Clear the full-screen panel
        foreach (Transform child in FullScreen3DImagePanel)
        {
            Destroy(child.gameObject);
        }

        // Filter media based on category_id and ensure it’s a 360 image
        var filteredMedia = mediaResponse.results.FindAll(media => media.category_id == categoryUid);

        if (filteredMedia.Count > 0)
        {
            // Automatically display the first 360 image in full-screen
            Show360ImageFullScreen(filteredMedia[0].file_url);
        }
        else
        {
            Debug.Log("No 360 images found for the selected category.");
        }
    }

    private void Show360ImageFullScreen(string imageUrl)
    {
        // Clear the full-screen panel
        foreach (Transform child in FullScreen3DImagePanel)
        {
            Destroy(child.gameObject);
        }

        // Instantiate the sphere prefab
        GameObject sphere = Instantiate(spherePrefab, FullScreen3DImagePanel);

        // Get the Renderer component from the sphere
        Renderer renderer = sphere.GetComponent<Renderer>();
        if (renderer != null)
        {
            StartCoroutine(Load360Image(imageUrl, renderer.material));
        }
        else
        {
            Debug.LogError("Renderer component not found in spherePrefab!");
        }
    }

    private IEnumerator Load360Image(string url, Material material)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                material.mainTexture = texture; // Apply the texture to the material
            }
            else
            {
                Debug.LogError("Failed to load 360 image: " + request.error);
            }
        }
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


    private IEnumerator LoadImage(string url, Image imgComponent)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                imgComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError("Failed to load image: " + request.error);
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
            // Handle image click
            Debug.Log("Image clicked");
            ShowImageFullScreen(media.file_url);
        }
        else if (mediaType == "Video")
        {
            // Handle video click
            Debug.Log("Video clicked");
            ShowVideoFullScreen(media.file_url);
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
