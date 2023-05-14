# BitABit-twitch
BitABit is a Twitch library made in C# (**.NET 6.0**) for ***Twitch.TV***!

Distributed under MIT License.


***BE AWARE: THIS IS "OIDC AUTHORIZATION CODE GRANT FLOW"*** ([check here](https://dev.twitch.tv/docs/authentication/getting-tokens-oauth/#authorization-code-grant-flow)): It means that **BitABit** has a buit-in server, so be careful about distribuite the application with your TWITCH APPLICATION **SECRET TOKEN** (this is secret and cannot be shared, lol)! We **EXTREMLY RECOMMEND** that you make YOUR OWN SERVER for authentication, and use only the other functions if you are going to use **BitABit** to create a production **Client-side Application**. If you gonna use **BitABit** for **personal propouses** or **server-side** then you are safe!

_PS: In the nearest future i will make a **Implicit Code Flow** for C# and integrate in BitABit._

### How to use

- Import BitABit DLL in references inside your project;

```csharp
using BitABit;
namespace YourProject{
	public class main{
		private static BitABit.Initialize Initialize = new BitABit.Initialize();
		private static BitABit.auth Auth = new BitABit.auth();
		private static string? token;
		private static strinng? refresh_token;
		static void Main(){
			Initialize.Keys("YOUR_APP_CLIENT_ID", "YOUR_APP_SECRET");
			string[] scopes = {"moderator:read:followers", "moderator:read:chatters"};
			Auth.RequestAuth(scopes, false);
			//output token and refresh token will come in:
			token = auth.Token;
			refresh_token = auth.Refresh_Token;
			Console.WriteLine("Token: " + token);
			Console.WriteLine("Refresh Token: " + refresh_token);
			return;
		}
	}
}
```

(_i'm gonna use the above variables for all examples..._)

To verify if Token is Valid:
```csharp
bool token_valid = await Auth.IsValidToken(token);
if(token == false){
	Console.WriteLine("Token is not valid or is expired...");
	//generate another token using the example bellow ↓↓↓
}
```

To generate another access token (you need previous refresh token):
```csharp
await Auth.RefreshToken(refresh_token);
token = auth.RToken;
refresh_token = auth.RRefresh_Token;
Console.WriteLine("New Token: " + token);
Console.WriteLine("New Refresh Token: " + refresh_token);
```

PS: _If you try to revalidate using refresh token 2/3 times and operation fails, you probably need to ask user to repeat the `RequestAuth()` process, this happens because: **User remove your app**, **User lost the refresh token for some reason** or **Twitch run into some error and she are forcing you to revalidate**._

PSS: _**On a multi-thread app** is **highly recommended** that you use 1 token for multiple requests instead of re-gen another one... Se more info about this [here](https://dev.twitch.tv/docs/authentication/refresh-tokens/#handling-token-refreshes-in-a-multi-threaded-app)._

**more comming soon...**