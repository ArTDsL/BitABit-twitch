/**
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: utils/userful.cs
 * @created: 2023-05-14
 * @updated: 2023-05-14
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
    public class userful
    {
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
        public void Write(string msg) {
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
    }
}
