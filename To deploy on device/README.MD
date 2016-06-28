It is mandatory to deploy the following files:
- iot.config
- lstprg.txt
- Prog.config
- file\spr.css
Those need to be deployed in the followinf path: \\yourdevice\c$\Data\Users\DefaultAccount\AppData\Local\Packages\f0a90161-2188-40ed-83aa-2eb67a0326da_ptervq1kyxgz4\LocalState
make sure you'll create the directory "file" and you'll place the spr.css file in it. 

If you want to use Azure IoT hub to send messages to your device and get infos from it, you'll need to edit the iot.config file and put your Azure IoT Hub connection string for this device.

The file Prog.config contains settings for the sprinkler, number of sprinklers, their names. So far, this file is using a very simple URL style parameter. It has to start with '?' and uws '&' to separate the sesstings and '=' between the setting and the value.
As an example: ?sec=Key1234&spr=5&du=25&spn1=zone peach tree&spn0=back zone&spn2=another zone&spn3=garden&spn4=Greenhouse
- *sec* is the security key you'll need to use in the URL
- *spr* is the number of sprinklers
- *spnX* where X is a number starting at 0 (for the first sprinkler) and does contains the name of sprinkler. Special caracters may not be supported yet
- *du* is the default duration for sprinkling
A migration to json is planned later.