There are three bat files in this folder:
1) MapReduce.bat
	This will run a command line application which demonstrates mapreduce and the datastore. type help at any time in this application to see a list of relevant commands. from the main menu "Sort" will sort several thousand integers using a mapreduce sort algorithm on a simulated local network, "wordcount" will count the number of occurances in a sentance on a simulated local network using mapreduce, finally datastore will enter an interactive mode allowing you to put and get data into a datastore on a simulated network.

2) peerTube Broadcast.bat
	This will run a peerTube instance set up for broadcasting, once you are in the main menu click "Shout". Make sure you have a webcam plugged in and properly installed.

3) peerTube Listener.bat
	This will run a peerTube instance set up for listening, once you are in the main menu click "listen". The listener is set up for listening to a broadcast from another peer on the local machine. To listen to a broadcast on a remote machine go into peerTube folder, open BootstrapContacts.txt and enter the IP address/port of the remote machine, preserve the long string of GUIDs. This setup is a bit odd, the idea is that you can distributed the broadcast contacts file and that's all the information anyone needs to listen to a broadcast - it's to avoid requireing the user to type long strings into a connection box on the main menu.