Antidote
=====

**Antidote is an All-in-One Internet Cafe solution to manage public computers**

Antidote is a means to deter malicious actions and limit usage time for each user in a shared computer environment.
It achieves this by forcing users to sign-in to our software (separate from windows login) before using the computer. After sign-in, various aspects of the session is monitored and controlled.
This repository contains the desktop client side of the software. There's also API Server, WebSocket Server, Admin Client repositories. Please check them out!


## Features
* **Limit computer usage time** Admins can set how much time a person can according to group policy
* **Monitor sessions** Each session is monitored in real-time and admins can see what each user is doing at any given time
* **Manage users** Every user is registered in the system and admins can control access for individual users and also view their past sessions
* **Process protection** The desktop client is protected by SSDT Hooking method (32bit only) and hearbeat monitoring to revive the process in case of failure


## Built With

* [Newtonsoft.Json](https://www.newtonsoft.com/json) - Used to serialize/deserialize JSON
* [RestSharp](http://restsharp.org/) - Http client
* [websocket-sharp](https://github.com/sta/websocket-sharp) - WebSocket
* [Fody, Costura](https://github.com/Fody/Costura) - Used to bundle project into a single executable file


## Authors

* **YuHong(Daniel) Kim** - [LinkedIn](https://www.linkedin.com/in/yuhong-kim)

See also the list of [contributors](https://github.com/yhware/antidote-client/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details

