using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class ProjectCardManager : MonoBehaviour
{
    public GameObject projectCardPrefab;  // Reference to the Project Card prefab
    public Transform content;  // Reference to the Content GameObject (inside the Scroll View)
    
    [System.Serializable]
    public class ProjectData
    {
        public string name;
        public string imageUrl;
    }

    [System.Serializable]
    public class ProjectDataList
    {
        public List<ProjectData> projects;
    }

    void Start()
    {
        // JSON data string - in a real application, you might load this from a file or a web request
        string json = @"
        {
            ""projects"": [
                {""name"": ""Project 1"", ""imageUrl"": ""https://example.com/image1.jpg""},
                {""name"": ""Project 2"", ""imageUrl"": ""https://example.com/image2.jpg""},
                {""name"": ""Project 3"", ""imageUrl"": ""https://example.com/image3.jpg""}
            ]
        }";

        // Parse the JSON and create the project cards
        LoadProjectsFromJson(json);
    }

    public void LoadProjectsFromJson(string json)
    {
        // Parse the JSON into a list of project data
        ProjectDataList projectList = JsonUtility.FromJson<ProjectDataList>(json);
        StartCoroutine(PopulateProjectCards(projectList.projects));
    }

    private IEnumerator PopulateProjectCards(List<ProjectData> projects)
    {
        foreach (var project in projects)
        {
            // Instantiate the project card prefab
            GameObject newCard = Instantiate(projectCardPrefab, content);

            // Set the project name on the card
            Text nameTextComponent = newCard.transform.GetComponentInChildren<Text>();
            nameTextComponent.text = project.name;

            // Download the project image and assign it to the card
            Image imageComponent = newCard.transform.GetComponentInChildren<Image>();
            yield return StartCoroutine(DownloadImage(project.imageUrl, imageComponent));
        }
    }

    private IEnumerator DownloadImage(string url, Image imageComponent)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError(request.error);
        }
        else
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
