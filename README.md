# BitABit - Twitch Lib for C#

---

[![Main Branch Workflow](https://github.com/ArTDsL/BitABit-twitch/actions/workflows/dotnet.yml/badge.svg?branch=main)](https://github.com/ArTDsL/BitABit-twitch) [![.NET CORE 6.0](https://img.shields.io/badge/.NETCore-6.0-blue.svg)](https://dotnet.microsoft.com/pt-br/download/dotnet/6.0) [![build - development](https://img.shields.io/badge/status-development-orange.svg)](!#) [![Licensed under MIT](https://img.shields.io/badge/License-MIT-lime.svg)](LICENSE) [![Open Source](https://img.shields.io/badge/Community-Open%20Source-white.svg)](!#) [![Open for Contributions](https://img.shields.io/badge/open%20for-contributions-skyblue.svg)](!#) [![C#](https://img.shields.io/badge/C%23-lime.svg)](https://learn.microsoft.com/pt-br/dotnet/csharp/)
---

BitABit is a Twitch library made in C# (**.NET 6.0**) for ***Twitch.TV***!

Distributed under MIT License.


***BE AWARE: THIS IS "OIDC AUTHORIZATION CODE GRANT FLOW"*** ([check here](https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#authorization-code-grant-flow)): It means that **BitABit** has a buit-in server, so be careful about distribuite the application with your TWITCH APPLICATION **SECRET TOKEN** (this is secret and cannot be shared, lol)! We **EXTREMLY RECOMMEND** that you make YOUR OWN SERVER for authentication, and use only the other functions if you are going to use **BitABit** to create a production **Client-side Application**. If you gonna use **BitABit** for **personal propouses** or **server-side** then you are safe!

_PS: In the nearest future i will make a **Implicit Code Flow** for C# and integrate in BitABit._

### How to use

## Initialize

- Import BitABit DLL in references inside your project;

```csharp
using BitABit;
//
Initialize.Keys("YOUR_APP_CLIENT_ID", "YOUR_APP_SECRET");
```

---

(_i'm gonna use the above variables for all examples..._)

---

## Authentication (OIDC AUTHORIZATION CODE FLOW)

***Code Auth***

```csharp
string[] scopes = {"moderator:read:followers", "moderator:read:chatters"};
Auth.RequestAuth(scopes:scopes, force_verify:false);
//output
string? token = auth.Token;
string? refresh_token = auth.Refresh_Token;
Console.WriteLine("Token: " + token);
Console.WriteLine("Refresh Token: " + refresh_token);
```

***Token Validation***

To verify if Token is Valid:
```csharp
bool token_valid = await Auth.IsValidToken(access_token:token);
//output
if(token == false){
	Console.WriteLine("Token is not valid or is expired...");
}
```

***Request New Token***

To generate another access token (you need previous refresh token):
```csharp
await Auth.RefreshToken(refresh_Token:refresh_token);
//output
string? token = auth.RToken;
string? refresh_token = auth.RRefresh_Token;
Console.WriteLine("New Token: " + token);
Console.WriteLine("New Refresh Token: " + refresh_token);
```

PS: _If you try to revalidate using refresh token 2/3 times and operation fails, you probably need to ask user to repeat the `RequestAuth()` process, this happens because: **User remove your app**, **User lost the refresh token for some reason** or **Twitch run into some error and she are forcing you to revalidate**._

PSS: _**On a multi-thread app** is **highly recommended** that you use 1 token for multiple requests instead of re-gen another one... Se more info about this [here](https://dev.twitch.tv/docs/authentication/refresh-tokens/#handling-token-refreshes-in-a-multi-threaded-app)._

---

## Chat

***Connecting to a Twitch.tv Chat***

```csharp
//access_token is the same you generated in RequestAuth() or RefreshToken() function. 
await Chat.StartChat(nick:"YOUR_USER_NAME", access_token:token, channel:"CHANNEL_TO_CONNECT", debug:true);
```

***Disconnecting from a Twitch.tv Chat***
```csharp
//true if disconnect has been successful, otherwise will give an error an return False.
if(await Chat.CloseChat()) {
	Console.WriteLine("GOOD JOB!");
}
```

_PS: `StartChat()` already make login, and get the Twitch CAPS. Also set's the loop._

***Get Last Message From Chat***

```csharp
var last_message = Chat.GetLastMessage();
//will return as a List<MESSAGE_PARSED>
```

***Send a Normal Message in Chat***
_User must be connected_
```csharp
await Chat.SendChatMessage("Hello World from BitABit!");
```

**more comming soon...**