﻿using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class PortToSpawn : NetworkBehaviour
{
    public Team team;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.tag == "Player")
        {
            //col.gameObject.GetComponent<PlayerInfo>().CmdUpdateTeam(team);
            //col.gameObject.GetComponent<PlayerController>().PortToSpawn();
            if (!isServer)
                return;
            if (FindObjectOfType<Level>().done)
            {
                col.gameObject.GetComponent<Player>().ChangeTeam(team);
                col.gameObject.GetComponent<Player>().RpcPortToSpawn(team);
            }
        }
    }
}