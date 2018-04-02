IF YOU ARE MISSING AN INSTALLER/ DRIVER OR DO NOT KNOW WHERE TO FIND IT GO TO RELEASES AND GET THE LATEST 'FULL' RELEASE.


SETUP:


1) Make sure you have .NET 4.5.2 Installed.

2) Make sure you have vJoy 2.1.8 Installed (vJoy_218).

3) After the 'vJoySetp' run 'vJoyConf' (can be found somewhere in 'C:\Program Files\vJoy', searching for 'vJoyConf' in the start menu should work too) - Might also be called 'vJoyConfigure'


IF YOU DON'T KNOW WHAT YOU ARE DOING FOLLOW THE NEXT STEPS CAREFULLY AND DONT ENABLE/DISABLE ANYTHING OTHER THAN WHAT I SAY


4) In the vJoyConf setup you should see numbers at the top, if not locate them.

5) Click the very first one starting from the bottom left. (Right under it you should now see 'vJoy Device' and after clicking it should say 'vJoy Device: 1')

6) Make sure in the bottom left 'Enable vJoy' is check marked.

7) Under the catagory called POV Hat Switch click the 0. (After clicking 0 the apply button should enable, if not click again)

8) The Apply button should now be enabled. Click it.


YOU DID IT! Now lets double check our work!


9) In the start menu search "game controller"

10) You should see "Set up USB Controller" (Win. 7), if not you will have to search around on the internet, click it.

11) If you see vJoy Device in the new window and the Status is 'OK' then go to the next step.

12) Download and install the latest Xbox Controller Driver from Microsoft (Win 7 and lower only. 8 and up should be fine).

13) Download and install the correct version of 'ScpVBus', can be found on github. Just google it.

14) Build/ Compile/ and then run 'NetworkedVirtualController.exe'- Preferably allow both private and public network access if prompted.


YOU NEED 'vGenInterface.dll' AND 'vGenInterfaceWrap.dll' TO BE IN THE SAME FOLDER AS 'NetworkedVirtualController.exe'


What is 'Accept Raw Input' and 'Send Raw Input'?
- This will allow the the connected party to simulate actually pressing the host's keyboard.
- Useful for games that don't accept generic/ xbox controllers but allow for a second player to use the same keyboard as the first player.

What is 'Xbox Input' and 'Send as Xbox Input'?
- Some games, like the LEGO games, expect an xbox controller rather than a generic one.
- If that is the case both players must check the 'Xbox' checkbox.
- The program will attempt to enter a state that Simulates an Xbox controller.