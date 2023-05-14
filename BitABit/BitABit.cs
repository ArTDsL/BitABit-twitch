/**
 * 
 * BitABit - Twitch C# Easy API
 * 
 * @file: BitABit.cs
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
namespace BitABit {
    public class Initialize {
        public static string? ClientId;
        public static string? ClientSecret;
        /// <summary>
        /// Create a new BitABit Object.
        /// </summary>
        /// <param name="id">The APP Client ID found in twitch dev console → Apps → YOUR_APP_NAME (<see href="https://dev.twitch.tv/console">HERE</see>).</param>
        /// <param name="secret">The APP Client Secret found in twitch dev console → Apps → YOUR_APP_NAME (<see href="https://dev.twitch.tv/console">HERE</see>).</param>
        public void Keys(string id, string secret) {
            ClientId = id;
            ClientSecret = secret;
        }
    }
}