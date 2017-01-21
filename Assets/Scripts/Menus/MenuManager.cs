﻿using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;


public sealed class MenuManager : MonoBehaviour
{
    public static Type current_menu = typeof(MenuPreGameHome);
    public Dictionary<Type, Menu> menu_dict = new Dictionary<Type, Menu>();

    private void Start()
    {
        // add all menu subclasses to the menu_dict, which will be used to access certain menus
        foreach (Type type in Assembly.GetExecutingAssembly().GetTypes().Where(t=>t.IsSubclassOf(typeof(Menu))))
        {
            menu_dict.Add(type, (Menu)Activator.CreateInstance(type));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            menu_dict[current_menu].Esc();
    }

    // ------------------------------------------------- OnGUI -------------------------------------------------
    private void OnGUI()
    {
        menu_dict[current_menu].RunGUI();
        GUIDebug();
    }

    
    // ------------------------------------------------- GUIDebug -------------------------------------------------
    private void GUIDebug()
    {
		if (!Input.GetKey(KeyCode.RightControl) && !Input.GetKey(KeyCode.RightCommand))
            return;
        if (SceneManager.GetActiveScene().name == NetworkManager.singleton.offlineScene)
        {
            GUI.Label(new Rect(Screen.width - 200, Screen.height - 40, 200, 20), "For debugging. No touchie!");
            if (GUI.Button(new Rect(Screen.width - 200, Screen.height - 20, 100, 20), "LAN MAKE"))
            {
                FindObjectOfType<Server>().LAN_mode = true;
                Settings.PLAYER_NAME = Network.player.ipAddress;
                current_menu = typeof(MenuInGameConfigureGame);
                NetworkManager.singleton.StartHost();
            }
            
            if (GUI.Button(new Rect(Screen.width - 100, Screen.height - 20, 100, 20), "LAN JOIN"))
            {
                FindObjectOfType<Server>().LAN_mode = true;
                NetworkManager.singleton.networkAddress = Settings.PLAYER_NAME;
                Settings.PLAYER_NAME = Network.player.ipAddress;
                current_menu = typeof(MenuInGameConfigureGame);
                NetworkManager.singleton.StartClient();
            }
            
        }
    }
}
