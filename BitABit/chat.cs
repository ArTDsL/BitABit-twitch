﻿/*
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: chat.cs
 * @created: 2023-05-14
 * @updated: 2023-05-18
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
using System.Reflection.PortableExecutable;
using System.Threading.Channels;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Drawing;
#pragma warning disable CS8600, CS8602 // YOU WILL NOT SURVIVE LITTLE WARNING RATS !!!!!
namespace BitABit {
    /// <summary>
    /// Chat Class.
    /// </summary>
    public class chat {
        private userful _userful = new userful();
        /// <summary>Server</summary>
        private static readonly string server = "irc.chat.twitch.tv"; //Non-SSL standard
        /// <summary>Port</summary>
        private static readonly int port = 6667;
        /// <summary>User nick (same used to login in twitch.tv)</summary>
        private static string? _nick;
        /// <summary>Channel</summary>
        private static string? _channel;
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
        /// <summary>IRC OnTwitchIRCMessageReceived() Loop</summary>
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
        /// <returns></returns>
        public async Task StartChat(string nick, string access_token, string channel) {
            int retry = 0;
            bool conn = false;
            IRCLoop = true;
            _nick = nick;
            _channel = channel;
            _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Connecting to " + server + ":" + port, DebugMessageType.INFO);
            TwitchIRCCli = new TcpClient();
            while(retry < 5 && conn == false) {
                try {
                    TwitchIRCCli.Connect(server, port);
                    conn = true;
                    _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Connection established", DebugMessageType.SUCCESS);
                } catch(Exception e) {
                    retry++;
                    _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Unable to Start Chat Connection: " + e.Message + " - [ Retrying, attempt {=Yellow}" + retry + "{/} from {=Red}5{/} ]", DebugMessageType.WARNING);
                }
            }
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
            //parsing commands
            string _cmd = "";
            int _cmd_pos = 0;
            bool isParam = false;
            //Parse command reference
            for(int i = 0; i < data.Count(); i++) {
                for(int l = 0; l < IRCCMDS.Count(); l++) {
                    if((" " + data[i] + " ") == (" " + IRCCMDS[l] + " ")) {//adding spaces, this will avoid something like "emojis:landCAPster;" got between words or something like...
                        _cmd = IRCCMDS[l];
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
                            _cmd_pos = i;
                            isParam = false;
                            break;
                        } else {
                            continue;
                        }
                    }
                }
            }
            //parsing caps
            string[][] BADGES = new string[50][]; //max 50 badges to parse
            int biCount = 0;
            var EMOTES = new Dictionary<string, Dictionary<int, string>>();
            string[] EMOTE_SET = new string[100]; //max 100 emote-sets to parse
            string _emote_only, _vip, _color, _id, _mod, _subscriber, _room_id, _turbo, _tmi_sent_ts, _user_id, _user_type, _display_name, _host;
            bool isEmotesNull = true;
            bool isBadgesNull = true;
            bool isEmoteSetNull = true;
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
                    }else
                    //parsing emotes
                    if(splitted_tags[i].Contains("emotes=")) {
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
                    }else
                    //parsing emote sets
                    if(splitted_tags[i].Contains("emote-sets=")) { 
                        if(splitted_tags[i].Contains(",")) {
                            //more than 1
                            string[] _emote_sets = splitted_tags[i].Replace("emote-sets=", "").Split(",");
                            foreach(string es in _emote_sets) {
                                if(es == null || es == "" || es == " ") {
                                    isEmoteSetNull = true;
                                }
                            }
                            if(_emote_sets != null) {
                                for(int l = 0; l <_emote_sets.Count(); l++) {
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
                    break;
                }
                case "HOSTTARGET": {
                    break;
                }
                case "RECONNECT": {
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
                    break;
                }
                case "001": case "002": case "003": case "004": case "375": case "372": case "376": {
                    if(login_count >= 6) {
                        //login successful
                        _userful.SendConsoleLog("Twitch Chat", "StartChat()", "Login successful", DebugMessageType.SUCCESS);
                        login_count = 0;
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
        }
        /// <summary>
        /// Handle IRC received messages.
        /// </summary>
        /// <returns>Returns a list of with the parsed message.</returns>
        static async void OnTwitchIRCMessageReceived(object? obj) {
            if(obj != null) {
                TwitchIRCLoopCT = (CancellationToken)obj;
            }
            await Task.Run(async () => {
                userful usf = new userful();
                
                while(chat.IRCLoop == true) {
                    if(IRCLoop == false) {
                        break;
                    }
                    string? line = await chat.TwitchIRCStreamReader.ReadLineAsync();
                    if(line == null || line == " " || line == "") {
                        line = "NULL_PARSE";
                    } else {
                        //debug
                        usf.SendConsoleLog("Twitch Chat", "OnTwitchIRCMessageReceived()", line, DebugMessageType.INFO);
                        string[]? splited_line = line.Split(" ");
                        chat Chat = new chat();
                        await Chat.ParseInput(splited_line);
                    }
                }
            });
        }
    }
    /// <summary>
    /// Parsed Messages List
    /// </summary>
    public class CAPS_PARSED {
        public string[][] badges { get; set; }
        public string color { get; set; }
        public string display_name { get; set; }
        public int emote_only { get; set; }
        public Dictionary<string, Dictionary<int, string>> emotes { get; set; }
        public string id { get; set; }
        public int mod { get; set; }
        public int room_id { get; set; }
        public int subscriber { get; set; }
        public int turbo { get; set; }
        public Int64 tmi_sent_ts { get; set; }
        public Int64 user_id { get; set; }
        public string user_type { get; set; }
        public string[] source { get; set; }
        public string[] command { get; set; }
    }
}