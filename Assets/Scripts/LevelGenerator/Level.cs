﻿using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;


/* Level.
 * 
 * Details:
 *  A single script to be put on a networked object. This script will generate rooms randomly, but within the
 *  confines of our game logic. Ideally (Untested) this script will create 2 or more spawn points, and we can
 *  then assign those spawn points as bases for the two teams.
 *  
 * 
 * Technicals:
 *  We first load in the resources which are our rooms. They are located in Assets/Resources/rooms
 *  
 *  We define a value for acceleration. While we are accelerating, we don't want to create rooms
 *  that are only 1 entrance, because this will mean the created room is a dead end and we can
 *  therefore no longer create new rooms
 *  
 *  We define a value for deceleration. While we are decelerating, we start filling the open
 *  entrances. We prioritize 1 entrance rooms, but in the end there will be some slots that
 *  we can only fill with multi-entrance rooms. We assign all the 1 entrance rooms, then fill
 *  the rest.
 *  
 *  We also keep our prefabs of rooms in a class - RoomLibrary
 *  This class is a way of organizing our rooms. We layer the details as a
 *  
 *          Dictionary<Dictionary<List<Room>>>
 *  
 *  The first dictionary is organized by number of entrances. This is important for us to be able to
 *  efficiently retrieve a room with a specified entrance.
 *  The second dictionary is organized by a Vector2 size, which is the dimensions of a room. This is
 *  important for us to be able to look through all rooms of a set entrance, and pick a size.
 *  Lastly, we have a List of Rooms, as many unique rooms can have a similar number of entrances and
 *  size. We usually access this List randomly, so we can get a different room every time.
 *  
 * Public Gets:
 *  spawn_rooms - contains all rooms that are capable of being spawn rooms.
 *  current_rooms - contains all the rooms generated and accepted by the level.
 *  done - whether our level generation is done or not.
 * 
 * auth Wesley Wu
 */
public class Level : NetworkBehaviour
{
    /// <summary>
    /// A library of all the different types of rooms we've loaded.
    /// </summary>
    private RoomLibrary _room_lib;


    /// <summary>
    /// A list of all the rooms already generated by our generator.
    /// </summary>
    public List<Room> current_rooms;

    /// <summary>
    /// When to stop accelerating. Don't make this bigger than _decelerate_at!
    /// </summary>
    private int _accelerate_until = 4;

    /// <summary>
    /// When to start decelerating
    /// </summary>
    private int _decelerate_at = 10;

    /// <summary>
    /// How many times we've tried generating a room.
    /// </summary>
    private int _generation_count; // Unused
    
    /// <summary>
    /// A check of whether our level generation is done or not.
    /// </summary>
    public bool done = false;

    /// <summary>
    /// A list of all our spawn rooms.
    /// </summary>
    public List<Room> spawn_rooms;

    private Dictionary<Team, Room> _spawn;

    [SyncVar]
    public Vector2 SpawnA;

    [SyncVar]
    public Vector2 SpawnB;

    private void OnCompleteRooms()
    {
        FindObjectOfType<Player>().RefreshVision();
        foreach (Room r in current_rooms)
        {
            Transform dl = r.transform.Find("2DLight");
            if (dl != null)
                dl.GetComponent<DynamicLight>().Rebuild();
        }
    }

    private void Awake()
    {
        current_rooms = new List<Room>();
        spawn_rooms = new List<Room>();
        _spawn = new Dictionary<Team, Room>();
        done = false;
        _LoadResources();
    }

    private void Start()
    {
        if (!isServer)
            return;
        CreateFirstRoom(new Vector2(0, 0));
        StartCoroutine("GenerateRooms"); // We put this in Start() because rooms have to access the levelgenerator, and it isn't created until Awake() finishes
    }


    public int AvailableEntrances()
    {
        int to_return = 0;
        foreach (Room room in current_rooms)
            to_return += room.AvailableEntrances();
        return to_return;
    }
    /// <summary>
    /// Updates the available entrances of all rooms. Try not to use this too often, it loops through every single room!
    /// </summary>
    public void UpdateAvailableEntrancesAll()
    {
        foreach (Room room in current_rooms)
            room.UpdateAvailableEntrances(current_rooms);
    }

    private void _LoadResources()
    {
        _LoadRooms();
    }

    private void _LoadRooms()
    {
        string _debugger_info = "";
        _room_lib = new RoomLibrary();
        GameObject[] rooms = Resources.LoadAll<GameObject>("Rooms");
        if (rooms.Length == 0)
            Debug.LogError("No rooms found!");
        foreach (GameObject g in rooms)
        {
            Room r = g.GetComponent<Room>();
            _room_lib.Add(r);
        }
        _debugger_info += "LoadRooms() done.\n" + _room_lib.ToString();
        Debug.Log(_debugger_info);
    }

    private void CreateFirstRoom(Vector2 pos)
    {
        Room r = Instantiate<Room>(_room_lib.GetRandom(12, new Vector2(21, 21)));

        r.transform.parent = this.transform;

        if (r.is_spawn)
            spawn_rooms.Add(r);
        current_rooms.Add(r);
        UpdateAvailableEntrancesAll();
        NetworkServer.Spawn(r.gameObject);
    }

    private IEnumerator GenerateRooms()
    {
        while (!done)
        {

            if (current_rooms.Count < _accelerate_until)
            {
                Accelerate();
            }
            else
            {
                Decelerate();
            }
            
            if (AvailableEntrances() == 0)
            {
                done = true;
                Debug.Log("Level generation done. " + spawn_rooms.Count + " viable spawn rooms.");
                if (spawn_rooms.Count >= Enum.GetValues(typeof(Team)).Length - 1) // If our map is acceptable!
                {
                    AssignSpawnRooms();
                    CreateNucleus();
                    OnCompleteRooms();
                    break;
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                    StopCoroutine("GenerateRooms");
                    
                    Reset();
                }
            }
            
            yield return null;
        }
    }

    private void Reset()
    {
        foreach (Room r in current_rooms)
        {
            Destroy(r.gameObject);
        }
        current_rooms = new List<Room>();
        spawn_rooms = new List<Room>();
        done = false;
        CreateFirstRoom(new Vector2(0, 0));
        StartCoroutine("GenerateRooms");
        
    }

    private void AssignSpawnRooms()
    {
        // We want to assign the two rooms that are furthest from one another.
        _spawn[Team.A] = spawn_rooms[0];
        _spawn[Team.B] = spawn_rooms[0];

        for (int i = 0; i < spawn_rooms.Count; i++)
        {
            for (int j = i; j < spawn_rooms.Count; j++)
            {
                if (Vector2.Distance(spawn_rooms[i].transform.position, spawn_rooms[j].transform.position) > Vector2.Distance(_spawn[Team.A].transform.position, _spawn[Team.B].transform.position))
                {
                    _spawn[Team.A] = spawn_rooms[i];
                    _spawn[Team.B] = spawn_rooms[j];
                }
            }
        }
        _spawn[Team.A].name += " SpawnA";
        _spawn[Team.B].name += " SpawnB";

        SpawnA = _spawn[Team.A].transform.position + new Vector3(9, 9, 0);
        SpawnB = _spawn[Team.B].transform.position + new Vector3(9, 9, 0);
    }

    private void CreateNucleus()
    {
        Nucleus n = Instantiate<Nucleus>(Resources.Load<Nucleus>("Interface/Nucleus"));
        n.transform.position = SpawnA + new Vector2(0.5f, 0.5f);
        n.team = Team.A;
        NetworkServer.Spawn(n.gameObject);

        n = Instantiate<Nucleus>(Resources.Load<Nucleus>("Interface/Nucleus"));
        n.transform.position = SpawnB + new Vector2(0.5f, 0.5f);
        n.team = Team.B;
        NetworkServer.Spawn(n.gameObject);
    }

    private void Accelerate()
    {
        Room r = null;
        int count = 0;

        while (true)
        {
            count++; // Increment count, which we will check to make sure we dont run this indefinitely
            if (count > 100000)
            {
                Debug.LogError("We ran Accelerate() randomly 100000 times but could not find a room");
                return;
            }

            int entrances = UnityEngine.Random.Range(2, _room_lib.entrance_max + 1); // Set our num entrances randomly (but within bounds)

            int s = UnityEngine.Random.Range(1, _room_lib.size_max + 1); // Set our size randomly (but within bounds)
            r = _room_lib.GetRandom(entrances, new Vector2(s, s)); // Try to create a room. This will be null if the specifications don't exist.


            if (r != null) // If we found a room with the specifications...
            {
                r = Instantiate<Room>(r);
                if (r.Add(current_rooms))
                {
                    r.transform.parent = this.transform;
                    if (r.is_spawn)
                        spawn_rooms.Add(r);
                    current_rooms.Add(r);
                    UpdateAvailableEntrancesAll();
                    NetworkServer.Spawn(r.gameObject);
                    return;
                }
                else
                {
                    Destroy(r.gameObject);
                }
            }
        }
    }

    private void Decelerate()
    {
        Room r = null;
        int count = 0;
        
        while (true)
        {
            count++;
            if (count > 100000)
            {
                Debug.LogError("We ran Decelerate() randomly 100000 times but could not find a room");
                return;
            }

            // When we decelerate, we're trying to fill up the remaining entrances, so we loop through everything that gives us a priority.
            for (int entrances = 1; entrances < _room_lib.entrance_max + 1; entrances++) // Start with small entrances
            {
                // a way to get a list of all rooms of a certain entrance.

                List<Room> list_rooms_of_entrance = _room_lib.Get(entrances);

                list_rooms_of_entrance.Reverse();
                foreach (Room room in list_rooms_of_entrance)
                {
                    r = Instantiate<Room>(room);
                    if (r.Add(current_rooms))
                    {
                        r.transform.parent = this.transform;
                        if (r.is_spawn)
                            spawn_rooms.Add(r);
                        current_rooms.Add(r);
                        UpdateAvailableEntrancesAll();
                        NetworkServer.Spawn(r.gameObject);
                        return;
                    }
                    else
                    {
                        Destroy(r.gameObject);
                    }
                }
            }
        }
    }
}