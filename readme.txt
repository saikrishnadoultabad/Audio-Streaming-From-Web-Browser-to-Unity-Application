This project directory consists of two folders
whisper.unity.nlp -> this folder consists of the Unity application
WebApp -> this folder consists of the webserver from which we are streaming audio.

To run the Unity application, open the folder in Unity Editor and run as you normally would.
Note that to use render streaming, we are not using the default render streaming provided by the Unity application.

Now to run the webserver, in the WebApp folder, run the 'Run' Windows executable file for Windows.
For Linux/MacOs execute the following commands from the terminal.
npm run build
npm run start127

After this open the web server from your web browser at 'http:127.0.0.1' or any of the links printed in the terminal.
To stream the project, open Receiver sample. To select your microphone device, choose from the list that is displayed below the video streaming component. Also note that the microphone won't work in the Unity application, this needs to be setup from the web server side.

Please use headphones while testing the application as without headphones it would just echo. As the web server is a client system and the Unity application is a server system, they should be deployed on two different machines. For testing, it is not required.
