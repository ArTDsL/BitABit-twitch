# BitABit-twitch
BitABit is a Twitch library made in C# **.NET 6.0** for Twitch.TV

Licensed under MIT License.

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

**more comming soon...**