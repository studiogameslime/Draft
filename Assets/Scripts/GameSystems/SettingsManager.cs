using System;
using System.Collections;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

// Requires Google Sign-In for Unity package
#if UNITY_ANDROID && !UNITY_EDITOR
using Google;
#endif

public class SettingsManager : MonoBehaviour
{
    [Header("Google Sign-In (Android)")]
    [Tooltip("OAuth 2.0 Client ID of type 'Web application' (from Google Cloud Console)")]
    [SerializeField] private string webClientId = "";

    private const string TAG = "[AuthGoogle]";

    // This button should be attached to the button in settings panel
    public void SignInWithGoogleButton()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(SignInWithGoogleRoutine());
#else
        Debug.LogWarning(TAG + " Google Sign-In is only supported on Android builds (not Editor).");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator SignInWithGoogleRoutine()
    {
        // Wait until Firebase is ready (your Bootstrap flag)
        while (!FirebaseBootstrap.FirebaseReady)
            yield return null;

        if (string.IsNullOrEmpty(webClientId))
        {
            Debug.LogError(TAG + " Missing webClientId. Set it in the inspector.");
            yield break;
        }

        var auth = FirebaseAuth.DefaultInstance;

        // Configure Google Sign-In
        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            RequestEmail = true,
            RequestProfile = true
        };

        Debug.Log(TAG + " Starting Google Sign-In...");

        var signInTask = GoogleSignIn.DefaultInstance.SignIn();

        // Wait until Google Sign-In task completes
        while (!signInTask.IsCompleted)
            yield return null;

        if (signInTask.IsCanceled)
        {
            Debug.LogWarning(TAG + " Google Sign-In canceled.");
            yield break;
        }

        if (signInTask.IsFaulted)
        {
            Debug.LogError(TAG + " Google Sign-In failed: " + signInTask.Exception);
            yield break;
        }

        var googleUser = signInTask.Result;
        if (googleUser == null || string.IsNullOrEmpty(googleUser.IdToken))
        {
            Debug.LogError(TAG + " Google Sign-In returned no user or no IdToken.");
            yield break;
        }

        // Create Firebase credential from Google tokens
        Credential credential = GoogleAuthProvider.GetCredential(googleUser.IdToken, googleUser.AccessToken);

        // If user is anonymous -> LINK (keeps the same UID, so your Firestore doc stays the same)
        if (auth.CurrentUser != null && auth.CurrentUser.IsAnonymous)
        {
            Debug.Log(TAG + " Linking anonymous user with Google credential... uid=" + auth.CurrentUser.UserId);

            var linkTask = auth.CurrentUser.LinkWithCredentialAsync(credential);
            while (!linkTask.IsCompleted)
                yield return null;

            if (linkTask.IsCanceled || linkTask.IsFaulted)
            {
                Debug.LogError(TAG + " LinkWithCredentialAsync failed: " + linkTask.Exception);
                yield break;
            }

            Debug.Log(TAG + " Link success. uid=" + auth.CurrentUser.UserId);
        }
        else
        {
            // Otherwise sign in normally
            Debug.Log(TAG + " Signing in with Google credential...");

            var firebaseTask = auth.SignInWithCredentialAsync(credential);
            while (!firebaseTask.IsCompleted)
                yield return null;

            if (firebaseTask.IsCanceled || firebaseTask.IsFaulted)
            {
                Debug.LogError(TAG + " SignInWithCredentialAsync failed: " + firebaseTask.Exception);
                yield break;
            }

            Debug.Log(TAG + " Sign-in success. uid=" + auth.CurrentUser.UserId);
        }

        // Optional: you can trigger a cloud reload here if you want, but only if your save logic expects it.
        // Example:
        // if (FirebaseSaveSync.Instance != null) FirebaseSaveSync.Instance.BeginDownload();
    }
#endif
}
