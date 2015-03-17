using GooglePlayGames;
using GooglePlayGames.BasicApi.Multiplayer;
using UnityEngine;

public class MultiplayerController 
{
    private static MultiplayerController _instance = null;
 
    private MultiplayerController() {
        PlayGamesPlatform.DebugLogEnabled = true;
        PlayGamesPlatform.Activate ();
    }
 
    public static MultiplayerController Instance {
        get {
            if (_instance == null) {
                _instance = new MultiplayerController();
            }
            return _instance;
        }
    }
  
    public void SignInAndStartMPGame() {
        if (! PlayGamesPlatform.Instance.localUser.authenticated) {
            PlayGamesPlatform.Instance.localUser.Authenticate((bool success) => {
                if (success) {
                    Debug.Log ("We're signed in! Welcome " + PlayGamesPlatform.Instance.localUser.userName);
                    // We could start our game now
                } else {
                    Debug.Log ("Oh... we're not signed in.");
                }
            });
        } else {
            Debug.Log ("You're already signed in.");
            // We could also start our game now
        }
    }

    public void TrySilentSignIn() {
        if (! PlayGamesPlatform.Instance.localUser.authenticated) {
            PlayGamesPlatform.Instance.Authenticate ((bool success) => {
                if (success) {
                    Debug.Log ("Silently signed in! Welcome " + PlayGamesPlatform.Instance.localUser.userName);
                 } else {
                    Debug.Log ("SilentSignIn: Oh... we're not signed in.");
                 }
            }, true);
        } else {
            Debug.Log("SilentSignIn: We're already signed in");
        }
    }

    public void SignOut() {
        PlayGamesPlatform.Instance.SignOut ();
    }
 
    public bool IsAuthenticated() {
        return PlayGamesPlatform.Instance.localUser.authenticated;
    }
}