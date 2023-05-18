/*
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: utils/userful.cs
 * @created: 2023-05-14
 * @updated: 2023-05-16
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
namespace BitABit.utils
{
    /// <summary>
    /// Userful Class - this class is pretty much use internally to generate tokens, console colors and etc...
    /// </summary>
    public class userful
    {
        /// <summary>
        /// Create a random token using A-Z, a-z, 0-9.
        /// </summary>
        /// <param name="size">Size of Token.</param>
        /// <returns>Returns the token in a string format</returns>
        public string RandomToken(int size)
        {
            //no special chars in dic. because it's going into requests..
            string dictionary = "abcdefghijklmnopqrstuvwxyz1234567890ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string token = "";
            for (int i = 0; i < size; i++)
            {
                Random rand = new Random();
                int choice = rand.Next(0, dictionary.Count());
                token += dictionary[choice].ToString();
            }
            return token;
        }
        /// <summary>
        /// Write a console message with color.
        /// </summary>
        /// <param name="msg">Message to Write with colored marks.</param>
        ///<returns>A console message colored.</returns>
        private void Write(string msg) {
            string[] ss = msg.Split('{', '}');
            ConsoleColor c;
            foreach(var s in ss) {
                if(s.StartsWith("/")) {
                    Console.ResetColor();
                } else if(s.StartsWith("=") && Enum.TryParse(s.Substring(1), out c)) {
                    Console.ForegroundColor = c;
                } else {
                    Console.Write(s);
                }
            }
        }
        /// <summary>
        /// Format debug Console Log messages.
        /// </summary>
        /// <param name="ref_class">Reference where the log comming from based in Class.</param>
        /// <param name="from">Where it came (ex. <c>OnTwitchIRCMessageReceived()</c>)</param>
        /// <param name="message">Message that you want to pass.</param>
        /// <param name="type_message">Type of message <see cref="DebugMessageType">ChatDebugMessageType</see></param>
        /// <returns>Console Message Formated and Colored.</returns>
        public void SendConsoleLog(string ref_class, string from, string message, int type_message = 0) {
            string msg = "_";
            switch(type_message) {
                case 0: { //INFO
                    msg = "{=Blue}[ BitABit - {=Yellow}IRC{/}" + (from != null ? " - {=Gray}" + from + "{/}" : "") + " ]{/} :: " + ref_class + (message != null ? " - " + message + " " : "") + "\n";
                    break;
                }
                case 1: { //SUCCESS [OK]
                    msg = "{=Blue}[ BitABit - {=Yellow}IRC{/}" + (from != null ? " - {=Gray}" + from + "{/}" : "") + " ]{/} :: " + ref_class + (message != null ? " - " + message + " " : "") + "- [{=Green}OK{/}]\n";
                    break;
                }
                case 2: { //ERROR
                    msg = "{=Red}[ BitABit{/} - {=Yellow}IRC{/}" + (from != null ? " - {=Gray}" + from + "{/}" : "") + " {=Red}] :: " + ref_class + (message != null ? " - " + message + " " : "") + "{/}\n";
                    break;
                }
                case 3: { //WARNING
                    msg = "{=Blue}[ BitABit - {=Yellow}IRC{/}" + (from != null ? " - {=Gray}" + from + "{/}" : "") + " ]{/} :: {=Yellow}" + ref_class + (message != null ? " - " + message + " " : "") + "{/}\n";
                    break;
                }
                case 4: { //FAILED
                    msg = "{=White}[ {=Red}BitABit{/} - {=Yellow}IRC{/}" + (from != null ? " - {=Gray}" + from + "{/}" : "") + " {=White}]{/} :: {=Red}" + ref_class + "{/}" + (message != null ? " - " + message + " " : "") + "-{/} {=White}[{/} {=Red}FAILED{/} {=White}]{/}\n";
                    break;
                }
            }
            Write(msg);
        }
    }
    /// <summary>Message Logger Structure</summary>
    public struct DebugMessageType {
        /// <summary>Structure member of DebugMessageType</summary>
        public static readonly int INFO = 0, SUCCESS = 1, ERROR = 2, WARNING = 3, FAILED = 4;
    }
}
