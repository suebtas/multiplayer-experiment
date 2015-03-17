﻿using UnityEngine;
using System.Collections;

public class MainMenuScript : MonoBehaviour {

	
	public Texture2D[] buttonTextures;
    private float buttonWidth;
    private float buttonHeight;	

    public Texture2D signOutButton;
	
	void OnGUI() {
		for (int i = 0; i < 2; i++) {
			if (GUI.Button (new Rect ((float)Screen.width * 0.5f - (buttonWidth / 2),
			                          (float)Screen.height * (0.6f + (i * 0.2f)) - (buttonHeight / 2),
			                          buttonWidth,
			                          buttonHeight), buttonTextures[i])) {
				Debug.Log("Mode " + i + " was clicked!");

				if (i == 0) {
					// Single player mode!
					RetainedUserPicksScript.Instance.multiplayerGame = false;
					Application.LoadLevel("PickCarMenu");
				} else if (i == 1) {
					RetainedUserPicksScript.Instance.multiplayerGame = true;
                    //Debug.Log("We would normally load a multiplayer game here");
                    MultiplayerController.Instance.SignInAndStartMPGame();
                }
			}
		}

        //if (MultiplayerController.Instance.IsAuthenticated()) {
            if (GUI.Button(new Rect(Screen.width  - (buttonWidth * 0.75f),
                                    Screen.height - (buttonHeight * 0.75f),
                                    buttonWidth * 0.75f,
                                    buttonHeight * 0.75f), signOutButton)) {
                if (MultiplayerController.Instance.IsAuthenticated())
                    MultiplayerController.Instance.SignOut();
            }
        //}
	}
    
    void Start() {
        
        // I know that 301x55 looks good on a 660-pixel wide screen, so we can extrapolate from there
        buttonWidth = 301.0f * Screen.width / 660.0f;
        buttonHeight = 55.0f * Screen.width / 660.0f;
        
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        MultiplayerController.Instance.TrySilentSignIn();
    }       
}
