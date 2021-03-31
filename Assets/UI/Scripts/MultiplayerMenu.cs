﻿using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MultiplayerMenu : MonoBehaviour
{
    public InputField addressField;
    public InputField nameField;
    
    public void ConnectButton()
    {
        GameNetworkManager.isHost = false;
        SceneManager.LoadScene("Game");
    }

    private void Update()
    {
        GameNetworkManager.serverAddress = addressField.text;
        GameNetworkManager.PlayerName = nameField.text;
    }
    
    public void Cancel()
    {
        SceneManager.LoadScene("MainMenu");
    }
}