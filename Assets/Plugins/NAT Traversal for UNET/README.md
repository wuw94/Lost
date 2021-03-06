Plugin Version: 1.52
Unity Version: 5.5.*

# NAT Traversal
Adds NAT punch-through and automatic port-forwarding to Unity's [high-level networking API](http://docs.unity3d.com/Manual/UNetUsingHLAPI.html).
Reduces latency and saves you money by connecting players directly whenever possible, even when they are behind a router.
Automatically falls back to Unity's relay servers only if punch-through fails, so players can always connect.

Supports Windows, Linux, and OSX.

<a href="(https://www.assetstore.unity3d.com/en/#!/content/58948/)">![Available on the asset store](http://grabblesgame.com/nat-traversal/docs/asset_store_button.png)</a>

**Direct connect whenever possible:**
Why waste your hard earned money using bandwidth on the relay servers if you don't have to? A direct connection means less lag and less dependence on infrastructure that you don't control.

**Seamless connection replacing:**
Your players will always use the best connection possible, seamlessly switching from relay to direct connections as they become available.

**Faster connection:**
Gets your players in the game faster by not waiting for a UNET match to be created or joined before connecting.

**Host Migration:**
Fully supports UNet's built in Host Migration. No more dropped games, even if the host leaves.

*Note: Punch-through requires an externally hosted server to run the included Facilitator on.*

*Note: Web builds are not supported. Unfortunately this plugin does not work with WebSockets and there is no plan to add WebSocket support is the future. Sorry!*

# How to Use
If you haven't already signed up for Unity's [Online Services](https://unity3d.com/services/multiplayer) you should do that now.
Also be sure to [create a project](https://developer.cloud.unity3d.com/projects) and [set up the multiplayer service](http://docs.unity3d.com/Manual/UNetInternetServicesOverview.html) in Unity.

NAT punch-through requires an external server, often referred to as a **Facilitator**, in order to broker the connections between peers.
The first thing you'll want to do is get the Facilitator up and running.

## Step 1 - The Facilitator

*Note: If you want to skip this step you can **temporarily** use my Facilitator for testing. Just leave `grabblesgame.com` as your \ref NATTraversal.NetworkManager.facilitatorIP "facilitatorIP" on the \ref NATTraversal.NetworkManager "NetworkManager" component.*

1. Beg, borrow, or steal a Linux or Windows server. It needs to be externally facing (not behind a router).
	* I recommend [AWS](https://aws.amazon.com/free/) or [rackspace](https://www.rackspace.com/) but there are many affordable (or free) options.
2. Find the appropriate Facilitator at `Assets/Plugins/NAT Traversal for UNET/`
	* You will see two Facilitator files. Facilitator.exe is for Windows servers, the other one is for Linux.
3. Move the Facilitator onto your server (using something like Filezilla / ftp / carrier pigeons)
4. Run the Facilitator
	* Linux: `./Facilitator` 
	* Windows: `Facilitator.exe`
5. Make note of the IP and port that the Facilitator is running on. You will need them in a moment.

*Note: If you are connecting to a Linux server via putty and want to be able to logout and keep the Facilitator running, look into [screen](https://www.rackaid.com/blog/linux-screen-tutorial-and-how-to/).*

## Step 2 - Set up

Now that the Facilitator is up and running, you can start connecting some players.
1. Open up the `Example` scene that comes with the plugin
2. Select the \ref NATTraversal.NetworkManager "NetworkManager" in the scene
3. Fill in the \ref NATTraversal.NetworkManager.facilitatorIP "facilitatorIP" and \ref NATTraversal.NetworkManager.facilitatorPort "facilitatorPort" fields

That's it! You're done. Ok, well, not quite...let's make sure it's working before we pour the champagne.

## Step 3 - Test it

1. Copy the project to a computer that you would not be able to directly connect to.
2. Open the `Example` scene that is included with the plugin.
3. Select the \ref NATTraversal.NetworkManager "NetworkManager" and uncheck \ref NATTraversal.NetworkManager.connectRelay "connectRelay". We already know relay connections work, so let's turn them off for now so we can test NAT traversal.
4. Run the project. Use the GUI buttons to "Host"
	* If successful, a pink square will appear that you can control with the arrow keys.
5. Run the project on your computer.
6. Use the GUI buttons to "Join"
7. If all went well, you should now be connected and both players should be able to see each other's pink square moving around.

If your mind is not yet sufficiently blown, take a look at the usage stats for your game on Unity's online services portal. You should notice that the only CCU being taken up is by the hosts. You can connect as many players as you want all day without eating into your CCU or bandwidth at all, as long as they connect directly and not over the relays.

# What next?
You can use the provided \ref NATTraversal.NetworkManager "NetworkManager" on its own or as the base class for your own game's NetworkManager.
For the most part you will only ever need to call \ref NATTraversal.NetworkManager.StartHostAll "StartHostAll()" and \ref NATTraversal.NetworkManager.StartClientAll "StartClientAll()" but almost all of the internals are exposed so you can do lots of advanced things that I haven't even thought of.
You can use the NATHelper class to forward ports and punch holes yourself if you need lower level control but for most uses extending from the included \ref NATTraversal.NetworkManager "NetworkManager" and overriding a couple of methods is the most you will need.
You can override \ref NATTraversal.NetworkManager.OnHolePunchedServer "OnHolePunchedServer()" and \ref NATTraversal.NetworkManager.OnHolePunchedClient "OnHolePunchedClient()" to be informed when a hole is successfully punched.
For most methods that you override in \ref NATTraversal.NetworkManager "NetworkManager" you will want to make sure to call the base method to avoid causing unexpected behaviour. If you get stuck make sure to check out the [API documentation](annotated.html).

# How it Works
Punch-through is accomplished with the help of [RakNet](https://github.com/OculusVR/RakNet). Automatic port forwarding is accomplished using [Open.NAT](https://github.com/lontivero/Open.NAT). Both **upnp** and **nat pmp** are supported. Once the connection is established, all the HLAPI stuff works as normal. RakNet has some [good documentation](http://www.raknet.net/raknet/manual/natpunchthrough.html) on how punch-through works. The RakNet libraries are included for 32bit and 64bit Linux and Windows. Mac support is pending but the Linux libraries may actually work in some cases.
The included [NetworkManager](class_n_a_t_traversal_1_1_network_manager.html) requires Unity's matchmaking to work but if you have another way to pass around connection data (like steam lobbies) and 
you don't care about the relays, then you can still use this plugin without Unity's online services.

# But Why?
The primary motivation for creating this was so that small time developers like myself wouldn't be so dependent on Unity's relay servers. I also needed the fastest connections possible for my studio's own fast-paced game [Grabbles](http://grabblesgame.com) and I was not satisfied with the latency introduced by the relay servers when they were first introduced. I think this issue has been mostly resolved but a direct connection is always going to be faster.

# Known Issues
- Needs testing on more platforms. I suspect this works on mobile but I haven't had a chance to test it yet. It *should* work on any platform that RakNet works on as long as it has real sockets.
- RakNet doesn't work with WebSockets so no web builds, sorry.
- Requires Unity 5.2 or above.
- NAT punch-through works more often if you have at least two public IPs on the server your Facilitator is running on.
