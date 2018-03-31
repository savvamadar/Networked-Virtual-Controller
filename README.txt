SETUP:

1) Make sure you have .Net 4.5.2 Installed

2) Make sure you have vJoy installed (tested on v218)

3) After the vJoySetp run vJoyConf (can be found somewhere in C:\Program Files\vJoy, searching for vJoyConf in the start menu should work too)


IF YOU DON'T KNOW WHAT YOU ARE DOING FOLLOW THE NEXT STEPS CAREFULLY AND DONT ENABLE/DISABLE ANYTHING OTHER THAN WHAT I SAY


4) In the vJoyConf setup you should see numbers at the top, if not locate them.

5) Click the very first one starting from the bottom left. (Right under it you should now see 'vJoy Device' and after clicking it should say 'vJoy Device: 1')

6) Make sure in the bottom left 'Enable vJoy' is check marked.

7) Under the catagory called POV Hat Switch click the 0. (After clicking 0 the apply button should enable, if not click again)

8) The Apply button should now be enabled. Click it.


YOU DID IT! Now lets double check our work!


9) In the start menu search "game controller"

10) You should see "Set up USB Controller" (Win. 7), if not you will have to search around on the internet, click it.

11) If you see vJoy Device in the new window and the Status is 'OK' you're done!


YOU NEED 'vJoyInterface.dll' AND 'vJoyInterfaceWrap.dll', can be found in /bin/Debug/, to compile and run it.
