using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;  // For TMP_InputField
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class LoginUIManager : MonoBehaviour
{
    public GameObject loginForm;  // GameObject for the login form
    public GameObject otpForm;    // GameObject for the OTP form
    public TMP_InputField inputField;  // TMP_InputField for phone/email input
    public TMP_InputField otpInputField;  // TMP_InputField for OTP input
    public TextMeshProUGUI errorText;
    public TextMeshProUGUI otpMessageText;   // Text for displaying the OTP message
    public TextMeshProUGUI otpFailedText;
    public Button submitButton;    // Button for submitting the form
    public Button verifyOtpButton; // Button for verifying OTP
    private string encStr;  // To store the encrypted string from the server

    void Start()
    {
        otpForm.SetActive(false); // Hide OTP form initially

        // submitButton.onClick.AddListener(OnSubmit);
        // resendOtpButton.onClick.AddListener(OnResendOtp);
        // verifyOtpButton.onClick.AddListener(OnVerifyOtp);
    }

    public void OnSubmit()
    {
        string contact = inputField.text.Trim(); // Get and trim the input
        if (string.IsNullOrEmpty(contact))
        {
            Debug.LogError("Contact field is empty or invalid.");
            errorText.text = "Please enter a valid phone number or email.";
            return;
        }
        else if (!IsEmail(contact) && !IsPhoneNumber(contact))
        {
            Debug.LogError("Contact field is empty or invalid.");
            errorText.text = "Please enter a valid phone number or email.";
            return;
        }

        StartCoroutine(SendLoginRequest(contact));
    }

    IEnumerator SendLoginRequest(string contact)
    {
        errorText.text = "";
        string url = "https://api.mantrareal.com/auth/login";
        var payload = new Dictionary<string, string>
        {
            { "contact", contact }
        };
        string jsonData = JsonUtility.ToJson(new ContactWrapper(contact));

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-mantra-app", "gharpe.vr");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                // Show error message to the 
                errorText.text = "Email or phone not registered.";
                yield return new WaitForSeconds(5f);
                errorText.text = "";
            }
            else
            {
                // Parse the response
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
                Debug.Log("status :" + response.status);
                if (response.status == "success")
                {
                    encStr = response.enc_str;
                    // Change the button text
                    submitButton.GetComponentInChildren<TMP_Text>().text = "Resend OTP";
                    ShowOtpForm(contact);
                }
            }
        }
    }

    void ShowOtpForm(string contact)
    {
        // Determine the message based on whether the contact is a phone number or email
        if (IsEmail(contact))
        {
            otpMessageText.text = "We have sent an OTP to your email. Please enter it below to verify.";
        }
        else
        {
            otpMessageText.text = "We have sent an OTP to your phone number. Please enter it below to verify.";
        }

        otpForm.SetActive(true);     // Show OTP form
        otpMessageText.gameObject.SetActive(true);  // Show the OTP message text
    }

    public void OnVerifyOtp()
    {
        string otp = otpInputField.text.Trim(); // Get and trim the OTP input
        if (string.IsNullOrEmpty(otp))
        {
            Debug.LogError("OTP field is empty or invalid.");
            return;
        }

        StartCoroutine(SendVerifyRequest(otp, encStr));
    }

    IEnumerator SendVerifyRequest(string otp, string encStr)
    {
        string url = "https://api.mantrareal.com/auth/verify";
        var payload = new Dictionary<string, string>
        {
            { "otp", otp },
            { "enc_str", encStr }
        };
        string jsonData = JsonUtility.ToJson(new OtpWrapper(otp, encStr));

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-mantra-app", "gharpe.vr");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Error: " + request.error);
                // Show error message to the user
            }
            else
            {
                // Parse the response
                VerifyResponse response = JsonUtility.FromJson<VerifyResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(response.access_token))
                {
                    StoreAccessToken(response.access_token);
                    Debug.Log("OTP Verified and Access Token stored.");
                    // Proceed with the logged-in user flow
                }
                else
                {
                    otpFailedText.text = "Verification failed. Please check the OTP and try again.";
                    yield return new WaitForSeconds(5f);
                    otpFailedText.text = "";
                    Debug.LogError("Error: Invalid OTP.");
                    // Show error message to the user
                }
            }
        }
    }

    void StoreAccessToken(string accessToken)
    {
        // Store the access token as a cookie or use PlayerPrefs
        PlayerPrefs.SetString("access_token", accessToken);
        PlayerPrefs.Save();
        SceneManager.LoadScene("ProjectScene");
        // Note: Unity's PlayerPrefs is not encrypted. Consider using a more secure method for sensitive data.
    }

    private bool IsEmail(string contact)
    {
        // Simple check to determine if the contact is an email
        return Regex.IsMatch(contact, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
    }

    private bool IsPhoneNumber(string contact)
    {
        // Regular expression for validating phone numbers
        return Regex.IsMatch(contact, @"^\+?[1-9]\d{1,14}$");
    }
    // Wrapper class for JSON serialization
    [System.Serializable]
    public class ContactWrapper
    {
        public string contact;

        public ContactWrapper(string contact)
        {
            this.contact = contact;
        }
    }

    [System.Serializable]
    public class OtpWrapper
    {
        public string otp;
        public string enc_str;

        public OtpWrapper(string otp, string enc_str)
        {
            this.otp = otp;
            this.enc_str = enc_str;
        }
    }

    [System.Serializable]
    public class LoginResponse
    {
        public string enc_str;
        public string status;
        public string detail;
    }

    [System.Serializable]
    public class VerifyResponse
    {
        public string access_token;
    }
}
