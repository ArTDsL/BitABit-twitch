/*
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: chat.cs
 * @created: 2023-05-14
 * @updated: 2023-05-24
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
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using BitABit.utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Threading.Channels;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
#pragma warning disable CS8600, CS8601, CS8602, CS8603, CS0219, CS8604 // YOU WILL NOT SURVIVE LITTLE WARNING RATS !!!!!
namespace BitABit {
    /// <summary>
    /// Delegate to deal with received chat messages (user).
    /// </summary>
    public delegate void OnChatMessageReceived();
    /// <summary>
    /// Delegate to deal with user login (user).
    /// </summary>
    public delegate void OnChatLogin();
    /// <summary>
    /// Delegate to deal when user disconnect from chat (normally or banned) (user).
    /// </summary>
    public delegate void OnChatChannelLeave(string channel, bool user_banned);
    /// <summary>
    /// Chat Class.
    /// </summary>
    public class chat {
        private userful _userful = new userful();
        /// <summary>Connection attempts of reconnect</summary>
        private static int retry = 1;
        private static bool _debug;
        private static string? _access_token;
        private static bool IsRetrying;
        /// <summary>Server</summary>
        private static readonly string server = "irc.chat.twitch.tv"; //Non-SSL standard
        /// <summary>Port</summary>
        private static readonly int port = 6667;
        /// <summary>User nick (same used to login in twitch.tv)</summary>
        private static string? _nick;
        /// <summary>Channel</summary>
        private static string? _channel;
        private static bool _user_banned = false;
        /// <summary>Twitch IRC Client TCP Connection</summary>
        private static TcpClient? TwitchIRCCli;
        /// <summary>Twitch IRC TCP Connection Stream</summary>
        private static NetworkStream? TwitchIRCStream;
        /// <summary>Twitch IRC TCP Stream Writer</summary>
        private static StreamWriter? TwitchIRCStreamWriter;
        /// <summary>Twitch IRC TCP Stream Reader</summary>
        private static StreamReader? TwitchIRCStreamReader;
        /// <summary> Login Line Count (7 == logged in)</summary>
        private static int? login_count = 0;
        /// <summary> Twitch IRC OnTwitchIRCMessageReceived() Loop Cancelation Token</summary>
        private static CancellationToken TwitchIRCLoopCT;
        /// <summary> Twitch IRC OnTwitchIRCMessageReceived() Loop Cancelation Token Source</summary>
        private static CancellationTokenSource? TwitchIRCLoopCTS;
        /// <summary>Parsed Message List</summary>
        public static List<MESSAGE_PARSED>? message;
        /// <summary> Last Message Cache</summary>
        private static List<MESSAGE_PARSED>? _last_msgCache { get; set; }
        /// <summary>Event Receive Chat Message</summary>
        public event OnChatMessageReceived? OnChatMessageReceived = new OnChatMessageReceived(fnull);
        /// <summary>Event on login into chat</summary>
        public event OnChatLogin? OnChatLogin = new OnChatLogin(fnull);
        /// <summary>Event on leave chat normally or banned</summary>
        public event OnChatChannelLeave? OnChatChannelLeave;
        //Event Variables (loop)
        private static bool NOTIFY_MsgRecv = false;
        private static bool NOTIFY_CTLogin = false;
        private static bool NOTIFY_CTChnlLeave = false;
        /// <returns>Set to <c>false</c> to stop the loop (not recommended if you don't wan't to close the connection).</returns>
        private static bool IRCLoop;
        private static readonly string[] IRCCMDS = new string[] { "JOIN", "NICK", "NOTICE", "PART", "PASS", "PING", "PONG", "PRIVMSG", "CLEARCHAT", "CLEARMSG", "GLOBALUSERSTATE", "HOSTTARGET", "NOTICE", "RECONNECT", "ROOMSTATE", "USERNOTICE", "USERSTATE", "WHISPER", "CAP" };
        private static readonly string[] IRCODES = new string[] { "001", "002", "003", "004", "375", "372", "376", "421", "353" };
        /// <summary>
        /// Make a Connection to Twitch IRC No-SSL
        /// </summary>
        /// <param name="nick">User nickname</param>
        /// <param name="access_token">Valid Access Token</param>
        /// <param name="channel">Channel name (current is user twitch channel's name (ex. <c>https://twitch.tv/arthdsl</c> would be <c>#arthdsl</c>)), DON'T USE # in the beggin.</param>
        /// <param name="debug">Enable debug messages comming from IRC Socket (may you receive every single debug message available, even a single PING)!</param>
        /// <returns></returns>
        public async Task StartChat(string nick, string access_token, string channel, bool debug = false) {
            bool conn = false;
            _debug = debug;
            IRCLoop = true;
            _nick = nick;
            _channel = channel;
            _access_token = access_token;
            if(IsRetrying == false) {
                _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Connecting to " + server + ":" + port, DebugMessageType.INFO);
            }
            TwitchIRCCli = new TcpClient();
            while(retry <= 5 && conn == false) {
                try {
                    TwitchIRCCli.Connect(server, port);
                    conn = true;
                    _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Connection established", DebugMessageType.SUCCESS);
                } catch(Exception e) {
                    IsRetrying = true;
                    retry++;
                    _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Unable to Start Chat Connection: " + e.Message + " - [ Retrying, attempt {=Yellow}" + retry + "{/} from {=Red}5{/} ]", DebugMessageType.WARNING);
                    if(retry == 5 && conn == false) {
                        Environment.Exit(1);
                    }
                }
            }
            Callback_Exec();
            TwitchIRCStream = TwitchIRCCli.GetStream();
            TwitchIRCStreamWriter = new StreamWriter(TwitchIRCStream) { NewLine = "\r\n", AutoFlush = true };
            TwitchIRCStreamReader = new StreamReader(TwitchIRCStream);
            //Run Receiver
            TwitchIRCLoopCTS = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(new WaitCallback(OnTwitchIRCMessageReceived), TwitchIRCLoopCTS.Token);
            //login
            await SendAuth(access_token, nick);
            await Task.Delay(-1, TwitchIRCLoopCT);
        }
        /// <summary>
        /// Close connection to Twitch Chat properly.
        /// </summary>
        /// <returns>Will return <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/true-false-operators">true</see> if connection has been closed successfully, otherwise will return <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/true-false-operators">False</see>.</returns>
        public async Task<bool> CloseChat() {
            try {
                await TwitchIRCStreamWriter.WriteLineAsync("PART");
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "Left the channel #" + _channel, DebugMessageType.SUCCESS);
                TwitchIRCStreamReader.Close();
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "Closing Sockets", DebugMessageType.INFO);
                TwitchIRCStreamWriter.Close();
                TwitchIRCStream.Close();
                TwitchIRCCli.Close();
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "All Sockets are closed" + _channel, DebugMessageType.SUCCESS);
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "Disconnected with success", DebugMessageType.INFO);
                TwitchIRCLoopCTS.Cancel();
                IRCLoop = false;
                return true;
            } catch(Exception e) {
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "Unable to close connection: " + e.Message, DebugMessageType.ERROR);
                return false;
            }
        }
        /// <summary>
        /// Close connection (used when system are retrying connection)
        /// </summary>
        /// <returns></returns>
        private void CloseConnection() {
            try {
                TwitchIRCStreamReader.Close();
                TwitchIRCStreamWriter.Close();
                TwitchIRCStream.Close();
                TwitchIRCCli.Close();
            } catch(Exception e) {
                _userful.SendConsoleLog("Twitch Chat", "CloseChat()", "Unable to close connection: " + e.Message, DebugMessageType.ERROR);
            }
            return;
        }
        /// <summary>
        /// Login into Twitch IRC
        /// </summary>
        /// <param name="pass">OAuth Access Token</param>
        /// <param name="nick">Twitch Username</param>
        private async Task SendAuth(string pass, string nick) {
            _userful.SendConsoleLog("Twitch Chat", "SendAuth()", "Trying to login-in into chat...", DebugMessageType.INFO);
            await TwitchIRCStreamWriter.WriteLineAsync("CAP REQ :twitch.tv/membership twitch.tv/tags twitch.tv/commands");
            await TwitchIRCStreamWriter.WriteLineAsync("PASS oauth:" + pass);
            await TwitchIRCStreamWriter.WriteLineAsync("NICK " + nick);
        }
        /// <summary>
        /// Join Twitch IRC Channel
        /// </summary>
        /// <param name="channel">Channel name (current is user twitch channel's name (ex. <c>twitch.tv/arthdsl</c> would be <c>#arthdsl</c>))</param>
        private async Task JoinChannel(string channel) {
            _userful.SendConsoleLog("Twitch Chat", "JoinChannel()", "Trying to join channel #" + channel, DebugMessageType.INFO);
            await TwitchIRCStreamWriter.WriteLineAsync("JOIN #" + channel);
            return;
        }
        /// <summary>
        /// Send a message on connected chat.
        /// </summary>
        /// <param name="message">Message that you want to send.</param>
        /// <returns></returns>
        public async Task SendChatMessage(string message) {
            await TwitchIRCStreamWriter.WriteLineAsync("PRIVMSG #" + _channel + " :" + message);
            return;
        }
        /// <summary>
        /// Keep connection alive sending PONG to IRC.
        /// </summary>
        private async Task KeepAlive() {
            _userful.SendConsoleLog("Twitch Chat", "KeepAlive()", "Sending \"Pong\" to {=Green}keep alive{/}", DebugMessageType.SUCCESS);
            await TwitchIRCStreamWriter.WriteLineAsync("PONG :tmi.twitch.tv");
            return;
        }
        /// <summary>
        /// Parse the data received by IRC message.
        /// </summary>
        /// <param name="data"></param>
        private async Task ParseInput(string[] data) {
            /*
             * ok so this is going to be a massive big function, cause i want to parse everything here
             * without create other functions!
             * 
             * Mom.. I'm sorry, i swear someday i will fix this mess :pray:
             * I was running to finish this, i was forced..
             * 
             */
            if(data[0] == null || data[0] == " " || data[0] == "") {
                return;
            }
            //caps variables
            string[][] BADGES = new string[50][]; //max 50 badges to parse
            int biCount = 0;
            Dictionary<string, Dictionary<int, string>>? EMOTES = new Dictionary<string, Dictionary<int, string>>();
            string[]? EMOTE_SET = new string[100]; //max 100 emote-sets to parse
            string[]? SOURCES = new string[2];
            string[]? COMMAND = new string[2];
            string? _emote_only = null;
            string? _vip = null;
            string? _color = null;
            string? _id = null;
            string? _mod = null;
            string? _subscriber = null;
            string? _room_id = null;
            string? _turbo = null;
            string? _tmi_sent_ts = null;
            string? _user_id = null;
            string? _user_type = null;
            string? _display_name = null;
            string? _host = null;
            string? _param = null;
            string? _msg_id = null;
            string? _target_user_id = null;
            bool? isEmotesNull = true;
            bool? isBadgesNull = true;
            bool? isEmoteSetNull = true;
            //commands variables
            string _cmd = "";
            int _cmd_pos = 0;
            bool isParam = false;
            //Parse command reference
            for(int i = 0; i < data.Count(); i++) {
                for(int l = 0; l < IRCCMDS.Count(); l++) {
                    if((" " + data[i] + " ") == (" " + IRCCMDS[l] + " ")) {//adding spaces, this will avoid something like "emojis:landCAPster;" got between words or something like...
                        _cmd = IRCCMDS[l];
                        COMMAND[0] = _cmd;
                        COMMAND[1] = "#" + _channel;
                        _cmd_pos = i;
                        isParam = true;
                        break;
                    } else {
                        continue;
                    }
                }
            }
            if(isParam == false) {
                for(int i = 0; i < data.Count(); i++) {
                    for(int l = 0; l < IRCODES.Count(); l++) {
                        if((" " + data[i] + " ") == (" " + IRCODES[l] + " ")) {//adding spaces (same as the other)
                            _cmd = IRCODES[l];
                            COMMAND[0] = _cmd;
                            COMMAND[1] = "#" + _channel;
                            _cmd_pos = i;
                            isParam = false;
                            break;
                        } else {
                            continue;
                        }
                    }
                }
            }
            //getting host
            for(int i = 0; i < data.Count(); i++) {
                if(data[i].Contains("tmi.twitch.tv")) {
                    _host = data[i].Substring(1, data[i].Length - 1);
                }
            }
            //parsing caps
            if(data[0].Contains("@") == true) {
                //contain caps,
                // count badge-info (add to all arrays, so we don't replace any existent)
                string[] splitted_tags = data[0].Replace("@", String.Empty).Split(";");
                //parsing badges
                for(int i = 0; i < splitted_tags.Count(); i++) {
                    if(splitted_tags[i].Contains("badge-info=") || splitted_tags[i].Contains("badges=")) {
                        if(splitted_tags[i].Contains(",")) {
                            //multi-badge parsing
                            string[] multibadge = splitted_tags[i].Replace("badge-info=", String.Empty).Replace("badges=", String.Empty).Split(",");
                            if(multibadge != null) {
                                for(int l = 0; l < multibadge.Count(); l++) {
                                    if(biCount > 1) {
                                        string[] multibadge_badge = multibadge[l].Split("/");
                                        BADGES[biCount - 1] = new string[2];
                                        BADGES[biCount - 1][0] = multibadge_badge[0];
                                        BADGES[biCount - 1][1] = multibadge_badge[1];
                                        biCount += 1;
                                        isBadgesNull = false;
                                    } else if(biCount == 0) {
                                        string[] multibadge_badge = multibadge[l].Split("/");
                                        BADGES[l] = new string[2];
                                        BADGES[l][0] = multibadge_badge[0];
                                        BADGES[l][1] = multibadge_badge[1];
                                        biCount += 1;
                                        isBadgesNull = false;
                                    }
                                }
                            }
                        } else {
                            //unique badge parsing
                            if(splitted_tags[i].Contains("badge-info=")) {
                                biCount += 1;
                            }
                            string badge_check = splitted_tags[i].Replace("badge-info=", String.Empty).Replace("badges=", String.Empty);
                            if(badge_check != null || badge_check != "" || badge_check != " ") {
                                string[] _badges = badge_check.Split("/");
                                bool can_go = true;
                                foreach(string b in _badges) {
                                    if(b == null || b == "" || b == " " || b == String.Empty) {
                                        isBadgesNull = true;
                                        can_go = false;
                                    } else {
                                        can_go = true;
                                    }
                                }
                                if(can_go == true) {
                                    BADGES[0] = new string[2];
                                    BADGES[0][0] = _badges[0];
                                    BADGES[0][1] = _badges[1];
                                    biCount += 1;
                                    isBadgesNull = false;
                                }
                            }
                        }
                    //parsing emotes
                    } else if(splitted_tags[i].Contains("emotes=")) {
                        if(splitted_tags[i].Contains("/")) {
                            //multi emote parsing
                            string[] emotes_spplited = splitted_tags[i].Replace("emotes=", "").Split("/");
                            for(int l = 0; l < emotes_spplited.Count(); l++) {
                                //split emotes and positions
                                string[] _emote_spllited = emotes_spplited[l].Split(":");
                                //save emote name/id for further processing
                                string _emote_splitted_name = _emote_spllited[0];
                                if(_emote_spllited[1].Contains(",")) {
                                    //split positions (multi positions)
                                    string[] _emote_splitted_pos = _emote_spllited[1].Split(",");
                                    bool can_go = true;
                                    foreach(string e_p in _emote_splitted_pos) {
                                        if(e_p == null || e_p == "" || e_p == " " || e_p == String.Empty) {
                                            isEmotesNull = true;
                                            can_go = false;
                                        } else {
                                            can_go = true;
                                        }
                                    }
                                    if(can_go == true) {
                                        var dict_pos = new Dictionary<int, string>();
                                        int epos = -1;
                                        for(int j = 0; j < _emote_splitted_pos.Count(); j++) {
                                            string[] emote_pos = _emote_splitted_pos[j].Split("-");
                                            //split emote pos start / end
                                            epos += 1;
                                            dict_pos.Add(epos, emote_pos[0]);
                                            epos += 1;
                                            dict_pos.Add(epos, emote_pos[1]);
                                            isEmotesNull = false;
                                        }
                                        EMOTES.Add(_emote_splitted_name, dict_pos);
                                    }
                                } else {
                                    //split positions (one position)
                                    //using the _emote_spllited to fetch the unique pos
                                    string[] emote_pos = _emote_spllited[1].Split("-");
                                    var dict_pos = new Dictionary<int, string>();
                                    dict_pos.Add(0, emote_pos[0]);
                                    dict_pos.Add(1, emote_pos[1]);
                                    EMOTES.Add(_emote_splitted_name, dict_pos);
                                    isEmotesNull = false;
                                }
                            }
                        } else {
                            //unique emote parsing
                            if(splitted_tags[i].Replace("emotes=", "").Contains(":")) {
                                string[] _emote = splitted_tags[i].Replace("emotes=", "").Split(":");
                                //check if emote has multipos (repeat)
                                string emote_splitted_name = _emote[0];
                                if(_emote[1].Contains(",")) {
                                    //split positions (multi positions)
                                    string[] _emote_splitted_pos = _emote[1].Split(",");
                                    bool can_go = true;
                                    foreach(string e_p in _emote_splitted_pos) {

                                        if(e_p == null || e_p == "" || e_p == " " || e_p == String.Empty) {
                                            isEmotesNull = true;
                                            can_go = false;
                                        } else {
                                            can_go = true;
                                        }
                                    }
                                    if(can_go == true) {
                                        var dict_pos = new Dictionary<int, string>();
                                        int epos = -1;
                                        for(int j = 0; j < _emote_splitted_pos.Count(); j++) {
                                            string[] emote_pos = _emote_splitted_pos[j].Split("-");
                                            //split emote pos start / end
                                            epos += 1;
                                            dict_pos.Add(epos, emote_pos[0]);
                                            epos += 1;
                                            dict_pos.Add(epos, emote_pos[1]);
                                            isEmotesNull = false;
                                        }
                                        EMOTES.Add(emote_splitted_name, dict_pos);
                                    }
                                } else {
                                    //split positions (one position)
                                    //using the _emote_spllited to fetch the unique pos
                                    string[] emote_pos = _emote[1].Split("-");
                                    var dict_pos = new Dictionary<int, string>();
                                    dict_pos.Add(0, emote_pos[0]);
                                    dict_pos.Add(1, emote_pos[1]);
                                    EMOTES.Add(emote_splitted_name, dict_pos);
                                    isEmotesNull = false;
                                }
                            }
                        }
                    //parsing emote sets
                    } else if(splitted_tags[i].Contains("emote-sets=")) {
                        if(splitted_tags[i].Contains(",")) {
                            //more than 1
                            string[] _emote_sets = splitted_tags[i].Replace("emote-sets=", "").Split(",");
                            foreach(string es in _emote_sets) {
                                if(es == null || es == "" || es == " ") {
                                    isEmoteSetNull = true;
                                }
                            }
                            if(_emote_sets != null) {
                                for(int l = 0; l < _emote_sets.Count(); l++) {
                                    EMOTE_SET[l] = _emote_sets[l];
                                }
                                isEmoteSetNull = false;
                            }
                        } else {
                            //just 1 or 0
                            string _emote_sets = splitted_tags[i].Replace("emote-sets=", String.Empty);
                            if(_emote_sets != null && _emote_sets != "" && _emote_sets != " ") {
                                EMOTE_SET[0] = _emote_sets;
                                isEmoteSetNull = false;
                            }
                        }
                    }                    
                    string[] property = splitted_tags[i].Split("=");
                    switch(property[0]) {
                        case "color": {
                            _color = property[1];
                            break;
                        }
                        case "display-name": {
                            _display_name = property[1];
                            break;
                        }
                        case "turbo": {
                            _turbo = property[1];
                            break;
                        }
                        case "user-id": {
                            _user_id = property[1];
                            break;
                        }
                        case "id": {
                            _id = property[1];
                            break;
                        }
                        case "vip": {
                            _vip = property[1];
                            break;
                        }
                        case "tmi-sent-ts": {
                            _tmi_sent_ts = property[1];
                            break;
                        }
                        case "room-id": {
                            _room_id = property[1];
                            break;
                        }
                        case "mod": {
                            _mod = property[1];
                            break;
                        }
                        case "emote-only": {
                            _emote_only = property[1];
                            break;
                        }
                        case "subscriber": {
                            _subscriber = property[1];
                            break;
                        }
                        case "user-type": {
                            string[] ut = property[1].Split(" ");
                            _user_type = ut[0];
                            break;
                        }
                        case "msg-id": {
                            string[] msid = property[1].Split(" ");
                            _msg_id = msid[0];
                            break;
                        }
                        case "target-user-id": {
                            string[] tui = property[1].Split(" ");
                            _target_user_id = tui[0];
                            break;
                        }
                    }
                    /*
                     * 
                     * There is more to parse such as private message components and subscriptions...
                     * I will refactor this code when i have some time, parsing everything in only a
                     * function was a crazy shit ideia... I regret every single second of this decision,
                     * even when i tought this was a good ideia, i was DUMB... I WAS DUMB, I'M A FUCKING
                     * DUMB, dobby is a bad elf...
                     * 
                     */
                }
            }
            //parsing parameters
            if((_cmd_pos + 2) < data.Count()) {//parameter position
                int param_pos = _cmd_pos + 2;
                for(int i = param_pos; i < data.Count(); i++) {
                    if(param_pos == i) {
                        _param += data[i].Substring(1, data[i].Length - 1);
                    } else {
                        _param += " " + data[i];
                    }
                }
            } else {
                _param = null;
            }
            //
            //--------------- ↓↓ Test Only [ will be removed ] ↓↓ ---------------
            if(_debug == true) {
                if(isBadgesNull == false) {
                    for(int i = 0; i < BADGES.Count(); i++) {
                        if(BADGES[i] != null) {
                            Console.WriteLine(": ID [" + BADGES[i][0] + "] | L [" + BADGES[i][1] + "]");
                        }
                    }
                }
                if(isEmotesNull == false) {
                    foreach(var item in EMOTES) {
                        Console.Write("\nEMOTE: ID [" + item.Key + "] | ");
                        foreach(var itemm in item.Value) {
                            Console.Write("[" + itemm.Value + "]");
                        }
                        Console.Write("\n");
                    }
                }
                if(isEmoteSetNull == false) {
                    foreach(var item in EMOTE_SET) {
                        if(item != null) {
                            Console.WriteLine("EMOTE_SET: " + item);
                        }
                    }
                }
                Console.WriteLine("HOST: " + _host);
            }
            //--------------- ↑↑ Test Only [ will be removed ] ↑↑ ---------------
            switch(_cmd) {
                //normal IRC messages
                case "NOTICE": {
                    //login failed - Don't need to be parsed cause don't come with CAPS (not that i know ;-;)
                    if(data[2] == "*") {
                        if((data[3] + data[4] + data[5]) == ":Login authentication failed") {
                            _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "User failed to authenticate (Login authentication failed)", DebugMessageType.FAILED);
                        }
                        if((data[3] + data[4] + data[5]) == ":Improperly formatted auth") {
                            _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "User failed to authenticate(Improperly formatted auth)", DebugMessageType.FAILED);
                        }
                        login_count = 0;
                        Environment.Exit(0);// If user can't login something is dead wrong, application has to be shut down to be fixed.
                    }
                    if(_msg_id == "msg_banned" || _msg_id == "already_banned" || _msg_id == "untimeout_banned") {
                        _user_banned = true;
                    }
                    break;
                }
                case "PART": {
                    OnChatChannelLeave = new OnChatChannelLeave(I_OnChatChannelLeave);
                    I_OnChatChannelLeave(_channel, _user_banned);
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Disconnect from channel " + _channel, DebugMessageType.INFO);
                    break;
                }
                case "PING": {
                    await KeepAlive();
                    break;
                }
                case "PRIVMSG": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Received a PRIVMSG", DebugMessageType.SUCCESS);
                    break;
                }
                case "421": { //unknow command
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Unknow command...", DebugMessageType.ERROR);
                    break;
                }
                case "CAP": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Received CAP's", DebugMessageType.SUCCESS);
                    break;
                }
                case "CLEARMSG": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Messages from user {=Yellow}" + data[3].Replace(":", "") + "{/} has been cleared.", DebugMessageType.INFO);
                    break;
                }
                case "CLEARCHAT": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "All messages in channel are cleared.", DebugMessageType.INFO);
                    break;
                }
                case "GLOBALUSERSTATE": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Global state message received, ensure connection and authentication.", DebugMessageType.INFO);
                    break;
                }
                case "HOSTTARGET": {
                    string[] hostcase = _param.Split(" ");
                    if(hostcase[0] == "-") {
                        //stop hosting
                        _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", _channel + " stop hosting with " + _param[1] + " viewers", DebugMessageType.INFO);
                        break;
                    } else {
                        //start hosting
                        _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", _channel + " start hosting " + _param[0] + " with " + _param[1] + " viewers!", DebugMessageType.INFO);
                        break;
                    }
                }
                case "RECONNECT": {
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "BitABit Received a Twitch Mainteance WARNING! Please, reconnect to avoid termination.", DebugMessageType.WARNING);
                    await Task.Delay(10000);
                    break;
                }
                case "ROOMSTATE": {
                    break;
                }
                case "USERNOTICE": {
                    break;
                }
                case "USERSTATE": {
                    break;
                }
                case "WHISPER": {
                    //<to-user> is one parameter before command but he is also the HOST
                    break;
                }
                //in a row
                case "001":
                case "002":
                case "003":
                case "004":
                case "375":
                case "372":
                case "376": {
                    if(login_count >= 6) {
                        //login successful
                        _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Login successful", DebugMessageType.SUCCESS);
                        login_count = 0;
                        retry = 0;
                        IsRetrying = false;
                        I_OnChatLogin();
                        //Connect user to channel
                        if(_channel != null) {
                            await JoinChannel(_channel);
                        }
                        return;
                    } else {
                        login_count++;
                    }
                    break;
                }
                //returns
                case "353": { //joined channel has been successful
                    _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "User joined in channel #" + _channel, DebugMessageType.SUCCESS);
                    break;
                }
            }
            //test message param
            if(_debug == true && _param != null) {
                _userful.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "PARAMETER: " + _param, DebugMessageType.INFO);
            }
            //list the LAST MESSAGE
            message = new List<MESSAGE_PARSED>();
            message.Add(new MESSAGE_PARSED() {
                badges = BADGES,
                color = _color,
                display_name = _display_name,
                emote_only = _emote_only,
                emotes = EMOTES,
                id = _id,
                mod = _mod,
                room_id = _room_id,
                subscriber = _subscriber,
                turbo = _turbo,
                tmi_sent_ts = _tmi_sent_ts,
                user_id = _user_id,
                user_type = _user_type,
                source = SOURCES,
                command = COMMAND,
                parameters = _param
            });
            if(message == null) {
                message = null;
            }
            I_OnChatMessageReceived();
        }
        /// <summary>
        /// Null returning method to avoid null exception on event creation.
        /// </summary>
        private static void fnull(){
            return;
        }
        /// <summary>
        /// Get the last messsage received in chat
        /// </summary>
        /// <returns>Last message received in chat in List format <see cref="List{MESSAGE_PARSED}"/></returns>
        public List<MESSAGE_PARSED> GetLastMessage() {
            if(message.Count() >= 1) {
                return chat.message;
            }
            List<MESSAGE_PARSED> n = new List<MESSAGE_PARSED>();
            n = null;
            return n;
        }
        /// <summary>
        /// Handle IRC received message (ALL);
        /// </summary>
        /// <returns>Returns a list of with the parsed message.</returns>
        private static void OnTwitchIRCMessageReceived(object? obj) {
            if(obj != null) {
                TwitchIRCLoopCT = (CancellationToken)obj;
            }
            Thread T = new Thread(async () => {
                //await Task.Run(async () => {
                userful usf = new userful();
                chat Chat = new chat();
                while(chat.IRCLoop == true) {
                    if(IRCLoop == false) {
                        break;
                    }
                    try {
                        string? line = await chat.TwitchIRCStreamReader.ReadLineAsync();
                        if(line == null || line == " " || line == "") {
                            line = "NULL_PARSE";
                        } else {
                            //debug
                            if(_debug == true) {
                                usf.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", line, DebugMessageType.INFO);
                            }
                            string[]? splited_line = line.Split(" ");
                            await Chat.ParseInput(splited_line);
                        }
                    } catch(Exception e) {
                        Chat.CloseConnection();
                        if(_debug == true) {
                            usf.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "Fail to connect " + e.Message + " [ Retrying to connect, attempt {=Yellow}" + retry + "{/} from {=Green}5{/} ]", DebugMessageType.INFO);
                        }
                        if(_nick != null && _access_token != null && _channel != null) {
                            retry++;
                            IsRetrying = true;
                            await Chat.StartChat(_nick, _access_token, _channel, _debug);
                        } else {
                            usf.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", "There is something wrong with your connection and authentication, please try again...", DebugMessageType.ERROR);
                        }
                        return;
                    }
                }
            });
            T.Start();
        }
        //----------------- EVENT HANDLER FUNCTIONS -----------------------
        /// <summary> Handles Received messages (Chat user messages).</summary>
        private void I_OnChatMessageReceived() {
            Task task = Task.Run(() => {
                chat Chat = new chat();
                List<MESSAGE_PARSED> last_message = Chat.GetLastMessage();
                if(last_message != null && chat._last_msgCache != last_message) {
                    if(last_message.Count() >= 1) {
                        Console.WriteLine("COLOR: " + last_message[0].color);
                        chat.NOTIFY_MsgRecv = true;
                    }
                    _last_msgCache = last_message;
                }
            });
        }
        private void I_OnChatChannelLeave(string channel, bool user_banned) {
            NOTIFY_CTChnlLeave = true;
        }
        private void I_OnChatLogin() {
            NOTIFY_CTLogin = true;
        }
        //----------------- END OF EVENT HANDLER FUNCTIONS ----------------
        //Handle Callbacks
        unsafe private void Callback_Exec() {
            Thread T = new Thread(() => {
                chat Chat = new chat();
                while(true) {
                    if(chat.NOTIFY_MsgRecv == true) {
                        chat.NOTIFY_MsgRecv = false;
                        try {
                            if(message is null) {
                                return;
                            } else { 
                                OnChatMessageReceived.Invoke();
                            }
                        } catch(Exception) { /* DON'T NEED A PARSE, JUST TO AVOID NULL ERRORS... */ }
                    }
                    if(chat.NOTIFY_CTChnlLeave == true) {
                        chat.NOTIFY_CTChnlLeave = false;
                        if(_channel != null) {
                            OnChatChannelLeave.Invoke(_channel, _user_banned);
                        }
                    }
                    if(chat.NOTIFY_CTLogin == true) {
                        chat.NOTIFY_CTLogin = false;
                        OnChatLogin.Invoke();
                    }
                    Thread.Yield(); //avoid deadlocks
                }
            });
            T.Start();
        }
    }
    /// <summary>
    /// Parsed Messages List
    /// </summary>
    public class MESSAGE_PARSED {
        /// <summary>Badges</summary>
        public string[][]? badges { get; set; }
        /// <summary>Color</summary>
        public string? color { get; set; }
        /// <summary>Display Name</summary>
        public string? display_name { get; set; }
        /// <summary>Emotes Only</summary>
        public string? emote_only { get; set; }
        /// <summary>Emotes</summary>
        public Dictionary<string, Dictionary<int, string>>? emotes { get; set; }
        /// <summary>ID</summary>
        public string? id { get; set; }
        /// <summary>Mod</summary>
        public string? mod { get; set; }
        /// <summary>Room ID</summary>
        public string? room_id { get; set; }
        /// <summary>Subscriber</summary>
        public string? subscriber { get; set; }
        /// <summary>Turbo</summary>
        public string? turbo { get; set; }
        /// <summary>Time Stamp</summary>
        public string? tmi_sent_ts { get; set; }
        /// <summary>User ID</summary>
        public string? user_id { get; set; }
        /// <summary>User Type</summary>
        public string? user_type { get; set; }
        /// <summary>Source</summary>
        public string[]? source { get; set; }
        /// <summary>Command</summary>
        public string[]? command { get; set; }
        /// <summary>Parameters</summary>
        public string? parameters;
    }
}