/*
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: auth.cs
 * @created: 2023-05-14
 * @updated: 2023-05-15
 * @autor: Arthur 'ArTDsL'/'ArThDsL' Dias dos Santos Lasso
 * @copyright: Copyright (c) 2023. Arthur 'ArTDsL'/'ArThDsL' Dias dos Santos Lasso. All Rights Reserved. Distributed under MIT license.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * -------------
 * BE AWARE: ALL FUNCTIONS HERE ARE PART OF "OIDC AUTHORIZATION CODE FLOW", BE CAREFUL!
 * SHARE PERSONAL CODES (SECRET CODE) WITH USER WOULD BE CATASTROPHIC! SEE THE FULL 
 * WARNING IN:
 * https://github.com/ArTDsL/BitABit-twitch/blob/main/README.md
 * READ MORE ABOUT AUTHENTICATION METHODS IN:
 * https://dev.twitch.tv/docs/authentication/#authentication-flows
 * -------------
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Diagnostics;
using BitABit.utils;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
#pragma warning disable CS8602, CS8604 //Not null
namespace BitABit{
    /// <summary>
    /// Authentication Class (OIDC AUTHORIZATION CODE FLOW ONLY).
    /// </summary>
    public class auth{
        //RefreshToken
        private static string? access_token;
        private static string? refreshed_token;
        private static string[]? scopes;
        private static string? token_type;
        //AccessToken
        private static string? token;
        private static int? expires;
        private static string? refresh_token;
        private static string? id_token;
        //internal
        private readonly userful userfulF = new userful();
        private static HttpListener http_listener = new HttpListener();
        private static string http_url = "http://localhost:7779/";
        private static string? recv_auth_code;
        private static string? recv_state;
        /// <summary>
        /// Get the new Token Type generated from RefreshToken()
        /// </summary>
        public static string? RToken_Type {
            get {
                return token_type;
            }
        }
        /// <summary>
        /// Get the scopes passed in RefreshToken()
        /// </summary>
        public static string[]? RScopes {
            get {
                return scopes;
            }
        }
        /// <summary>
        /// Get the new Refresh token generated from RefreshToken() 
        /// </summary>
        public static string? RRefresh_Token {
            get {
                return refreshed_token;
            }
        }
        /// <summary>
        /// Get the new Access Token generated from RefreshToken()
        /// </summary>
        public static string? RToken {
            get {
                return access_token;
            }
        }
        /// <summary>
        /// Get ID token generated from RequestAuth()
        /// </summary>
        public static string? Id_Token {
            get {
                return id_token;
            }
        }
        /// <summary>
        ///  Get refresh token generated from RequestAuth()
        /// </summary>
        public static string? Refresh_Token {
            get {
                return refresh_token;
            }
        }
        /// <summary>
        /// Get token expiration timestamp generated from RequestAuth()
        /// </summary>
        public static int? Token_Expires {
            get {
                return expires;
            }
        }
        /// <summary>
        /// Get token generated from RequestAuth()
        /// </summary>
        public static string? Token {
            get {
                return token;
            }
        }
        /// <summary>
        /// Revoke user access token.
        /// </summary>
        /// <param name="access_token">Access Token to revoke.</param>
        /// <returns>Returns <see href="https://learn.microsoft.com/pt-br/dotnet/csharp/language-reference/operators/true-false-operators">true</see> if access token has been revoked, otherwise <see href="https://learn.microsoft.com/pt-br/dotnet/csharp/language-reference/operators/true-false-operators">false</see>.</returns>
        public async Task<bool> RevokeAccessToken(string access_token) {
            string token_url = "https://id.twitch.tv/oauth2/revoke";
            HttpClient token_request = new HttpClient();
            var token_data = new Dictionary<string, string>{
                { "client_id", Initialize.ClientId },
                { "token", Initialize.ClientSecret }
            };
            var token_request_content = new FormUrlEncodedContent(token_data);
            var token_response = await token_request.PostAsync(token_url, token_request_content);
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RevokeAccessToken()", "Requesting Twitch to revoke the Access Token...", DebugMessageType.INFO);
            if(token_response.StatusCode == HttpStatusCode.OK) {
                userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RevokeAccessToken()", "Twitch Revoke the Access Token", DebugMessageType.SUCCESS);
                return true;
            } else {
                userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RevokeAccessToken()", "Twitch Didn't Revoke the Access Token", DebugMessageType.FAILED);
                return false;
            }
        }
        /// <summary>
        /// Validate a token generated by RequestAuth() or RefreshToken()
        /// </summary>
        /// <param name="access_token">Access token to validate.</param>
        /// <returns><see href="https://learn.microsoft.com/pt-br/dotnet/csharp/language-reference/operators/true-false-operators">true</see> if access token is valid, <see href="https://learn.microsoft.com/pt-br/dotnet/csharp/language-reference/operators/true-false-operators">false</see> if token has expired.</returns>
        public async Task<bool> IsValidToken(string access_token) {
            string token_url = "https://id.twitch.tv/oauth2/validate";
            HttpClient token_request = new HttpClient();
            using(var req_msg = new HttpRequestMessage(HttpMethod.Get, token_url)) {
                req_msg.Headers.Authorization = new AuthenticationHeaderValue("OAuth", access_token);
                var token_response = await token_request.SendAsync(req_msg);
                userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "IsValidToken()", "requesting Twitch to try validate the token...", DebugMessageType.INFO);
                var token_twitch_response = await token_response.Content.ReadAsStringAsync();
                if(token_response.StatusCode == HttpStatusCode.OK) {
                    ValidateToken? validateToken = JsonSerializer.Deserialize<ValidateToken>(token_twitch_response);
                    if(validateToken.client_id != null) {
                        userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "IsValidToken()", "Twitch has Validate the Token [ {=Green}VALID{/} ]", DebugMessageType.SUCCESS);
                        return true;
                    } else {
                        userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "IsValidToken()", "Twitch has Validate the Token [ {=Red}NOT VALID{/} ]", DebugMessageType.SUCCESS);
                        return false;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Authenticate on Twitch with 'OIDC Implicit Grant Flow'.
        /// </summary>
        /// <param name="scopes">Scopes to request (check scopes <see href="https://dev.twitch.tv/docs/authentication/scopes/">HERE</see>)</param>
        /// <param name="force_verify">Force user to re-verify.</param>
        /// <returns>Return nothing <c>void</c>, user validation may come as a string format using <seealso cref="auth.Token"/>, <seealso name="auth.expires"/>, <seealso name="auth.refresh_token"/>, <seealso name="auth.id_token"/>.</returns>
        public void RequestAuth(string[] scopes, bool force_verify = false){
            //add last scope openID (required)
            string parsed_scopes = "";
            for (int i = 0; i < scopes.Count() - 1; i++){
                    parsed_scopes += scopes[i] + "+";
            }
            parsed_scopes += "openid";
            string verify_force = "";
            if(force_verify == false){
                verify_force = "&force_verify=false";
            } else {
                verify_force = "&force_verify=true";
            }
            string state = userfulF.RandomToken(56);
            recv_state = state;
            string auth_url = "https://id.twitch.tv/oauth2/authorize?response_type=code" + verify_force + "&client_id=" + Initialize.ClientId + "&redirect_uri=http://localhost:7779/receive_auth&scope=" + parsed_scopes + "&state=" + state + "&nonce=" + state;
            //load webbrowser asking user to confirm
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RequestAuth()", "{=Yellow}Opening Twitch Authorization page on client's Web Browser{/}", DebugMessageType.INFO);
            Process.Start(new ProcessStartInfo(auth_url) { UseShellExecute = true, CreateNoWindow = true });
            //start HTTP server to listen response from twitch
            http_listener.Prefixes.Add(http_url);
            http_listener.Start();
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RequestAuth()", "Listening to " + http_url, DebugMessageType.INFO);
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RequestAuth()", "{=Yellow}WAITING... USER MUST AUTHORIZE THE APPLICATION ON WEB BROWSER...{/}", DebugMessageType.INFO);
            Task httpTaskListener = HTTPReceiver();
            httpTaskListener.GetAwaiter().GetResult();
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RequestAuth()", "Authorization Steps Complete your application is now Authorized", DebugMessageType.SUCCESS);
            return;
        }
        /// <summary>
        /// Request new token when the other expires.
        /// </summary>
        /// <param name="refresh_Token">The old refresh token generated in the previous request).</param>
        public async Task RefreshToken(string refresh_Token) {
            string token_url = "https://id.twitch.tv/oauth2/token";
            HttpClient token_request = new HttpClient();
            var token_data = new Dictionary<string, string>{
                { "client_id", Initialize.ClientId },
                { "client_secret", Initialize.ClientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refresh_Token }
            };
            var token_request_content = new FormUrlEncodedContent(token_data);
            var token_response = await token_request.PostAsync(token_url, token_request_content);
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RefreshToken()", "{=Yellow}WAITING FOR TWITCH REPLY WITH THE NEW ACCESS TOKEN...{/}", DebugMessageType.INFO);
            var token_twitch_response = await token_response.Content.ReadAsStringAsync();
            RefreshToken? refToken = JsonSerializer.Deserialize<RefreshToken>(token_twitch_response);
            access_token = refToken.access_token;
            refreshed_token = refToken.refresh_token;
            scopes = refToken.scopes;
            token_type = refToken.token_type;
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RefreshToken()", "Received new token from Twitch", DebugMessageType.SUCCESS);
            return;
        }
        //post request with a "x-www-form-urlencoded" header parameter to get token and refresh token
        private async void RequestAuthToken(string code) {
            string token_url = "https://id.twitch.tv/oauth2/token";
            HttpClient token_request = new HttpClient();
            var token_data = new Dictionary<string, string>{
                { "grant_type", "authorization_code" },
                { "redirect_uri", "http://localhost:7779/auth_callback" },
                { "client_id", Initialize.ClientId },
                { "code", code },
                { "client_secret", Initialize.ClientSecret }
            };
            var token_request_content = new FormUrlEncodedContent(token_data);
            var token_response = await token_request.PostAsync(token_url, token_request_content);
            userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "RequestAuthToken()", "{=Yellow}WAITING FOR TWITCH REPLY WITH AUTHORIZATION TOKEN...", DebugMessageType.INFO);
            var token_twitch_response = await token_response.Content.ReadAsStringAsync();
            AuthToken? authToken = JsonSerializer.Deserialize<AuthToken>(token_twitch_response);
            token = authToken.access_token;
            refresh_token = authToken.refresh_token;
            expires = authToken.expires_in;
            id_token = authToken.id_token;
            //making a internal request to end the http internal loop
            if(token_twitch_response != null) {
                HttpClient confirmAuth = new HttpClient();
                await confirmAuth.GetAsync("http://localhost:7779/auth_callback");
                return;
            }
        }
        //small http server to handle ONLY AUTH REQUESTS - sec. reasons!
        private async Task HTTPReceiver() {
            bool http_running = true;
            while(http_running == true) {
                HttpListenerContext http_context = await http_listener.GetContextAsync();
                HttpListenerRequest request = http_context.Request;
                HttpListenerResponse response = http_context.Response;
                if(request.Url.ToString() == http_url + "favicon.ico") {
                    continue;
                }
                userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Connection Detected - " + request.Url.ToString() + " | " + "[{=Green}" + request.HttpMethod + "{/}]", DebugMessageType.INFO);
                if((request.HttpMethod == "GET") && (request.Url.AbsolutePath == "/close")) {
                    userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Shutdown Request Manually - " + request.Url.Query, DebugMessageType.INFO);
                    http_running = false;
                }
                if((request.HttpMethod == "GET") && (request.Url.AbsolutePath == "/receive_auth")) {
                    bool isDenied = false;
                    string[] split_parameters = request.Url.Query.Split("&");
                    //authorized
                    if(split_parameters[0].Contains("?code=") && split_parameters[split_parameters.Count() - 1].Replace("state=", "") == recv_state) {
                        userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Received from Twitch (Authorization Code) - " + request.Url.Query, DebugMessageType.SUCCESS);
                        recv_auth_code = split_parameters[0].Replace("?code=", "");
                    }
                    //unauthorized
                    if(split_parameters[0] == "?error=access_denied") {
                        userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "User Denied Application access...", DebugMessageType.ERROR);
                        isDenied = true;
                    }

                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.StatusDescription = "OK";
                    response.ProtocolVersion = new Version("1.1");
                    response.Close();
                    if(isDenied == true) {
                        //Do not go to next step
                        http_running = false;
                        userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Closing... Please restart the proccess if you want to try again...", DebugMessageType.WARNING);
                        return;
                    }
                    userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Start Requesting the Authentication Token", DebugMessageType.INFO);
                    RequestAuthToken(recv_auth_code);
                    continue;
                }
                if((request.Url.AbsolutePath == "/auth_callback")) {
                    userfulF.SendConsoleLog("Twitch Auth [OIDC CODE FLOW]", "HTTPReceiver()", "Received from Twitch (Authorization Token)", DebugMessageType.SUCCESS);
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.StatusDescription = "OK";
                    response.ProtocolVersion = new Version("1.1");
                    response.Close();
                    http_running = false;
                }
            }
            return;
        }
    }
    /// <summary>Authentication Token List</summary>
    public class AuthToken {
        /// <summary>Access Token</summary>
        public string? access_token { get; set; }
        /// <summary>Access Token expiration stamp</summary>
        public int? expires_in { get; set; }
        /// <summary>ID Token</summary>
        public string? id_token { get; set; }
        /// <summary>Refresh Token</summary>
        public string? refresh_token { get; set; }
        /// <summary>Required scopes string array</summary>
        public string[]? scopes { get; set; }
        /// <summary>Token Type</summary>
        public string? token_type { get; set; }
    }
    /// <summary>Refresh Token List</summary>
    public class RefreshToken {
        /// <summary>Access Token</summary>
        public string? access_token { get; set; }
        /// <summary>Refresh Token</summary>
        public string? refresh_token { get; set; }
        /// <summary>Required scopes string array</summary>
        public string[]? scopes { get; set; }
        /// <summary>Token Type</summary>
        public string? token_type { get; set; }
    }
    /// <summary>Validate Token List</summary>
    public class ValidateToken {
        /// <summary>Client ID </summary>
        public string? client_id { get; set; }
        /// <summary>Login</summary>
        public string? login { get; set; }
        /// <summary>Required scopes string array</summary>
        public string[]? scopes { get; set; }
        /// <summary>User ID</summary>
        public string? user_id { get; set; }
        /// <summary>Token exipiration stamp</summary>
        public int? expires_in { get; set; }
    }
}
