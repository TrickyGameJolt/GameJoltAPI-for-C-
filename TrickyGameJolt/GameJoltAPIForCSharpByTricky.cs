// Lic:
// TrickyGameJolt/GameJoltAPIForCSharpByTricky.cs
// Simplistic Game Jolt API for C#
// version: 19.12.24
// Copyright (C)  Jeroen P. Broks
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
// claim that you wrote the original software. If you use this software
// in a product, an acknowledgment in the product documentation would be
// appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
// misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.
// EndLic

#define GAMEJOLT_DEBUG_MODE

#define GAMEJOLT_DEBUG_TROPHYFETCH

using System;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;



namespace TrickyGameJolt {

    public delegate void GJERROR(string message);


    /// <summary>
    /// Core features. 
    /// </summary>
    public static class GJAPI {
        /// <summary>
        /// Set to false if you don't want your program to crash when something happens!
        /// </summary>
        static bool crash = true;
        static public GJERROR ERRORFUNCTION = null;
        static public GJERROR MSG = delegate(string message) { Debug.WriteLine(message); };

        static internal string GET(string url) {
            /*
            string ret = string.Empty;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream)) {
                ret = reader.ReadToEnd();
            }
            */
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })) {
                client.BaseAddress = new Uri(url);
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                string result = response.Content.ReadAsStringAsync().Result;
                //Console.WriteLine("Result: " + result);
                return result;
            }
        }

        static internal string[] GET_Lines(string url) => GET(url).Split('\n');

        internal static string md5(string source) {
            string hash;
            {
                using (MD5 md5Hash = MD5.Create()) {
                    byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(source));
                    StringBuilder sBuilder = new StringBuilder();
                    for (int i = 0; i < data.Length; i++) {
                        sBuilder.Append(data[i].ToString("x2"));
                    }
                    hash = sBuilder.ToString();
                }
            }
            return hash;
        }

        internal static void chat(string msg) {
#if GAMEJOLT_DEBUG_MODE
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
#endif
        }

        internal static void myerr(string err) {
            var e = $"GAMEJOLT ERROR: {err}";
            ERRORFUNCTION?.Invoke(err);
            if (crash)
                throw new Exception(e);
            else {
                Console.WriteLine(e);
                Debug.WriteLine(e);
            }
        }


        internal static Dictionary<string, string> gjrequest(string action, string querystring, string privatekey) {
            try {
                var url = $"https://api.gamejolt.com/api/game/v1/{action}/?{querystring}";
                url += "&signature=" + md5(url + privatekey);
                chat($"Request sent: {url}");
                var ng = GET(url).Trim();

                chat("GJ returned:\n" + ng + "\nEND RETURN");

                var lines = ng.Split('\n');
                var ret = new Dictionary<string, string>();
                //ret["success"] = "NOT YET CHECKED";
                for (int li = 0; li < lines.Length; li++) {
                    var ln = lines[li].Trim();
                    Debug.WriteLine($"{li}\t{lines[li]}");
                    if (ln != "") {
                        var vr = ln.Split(':');
                        if (vr.Length != 2) {
                            myerr($"Game Jolt Parse error in line {li}");
                        } else {
                            var value = vr[1].Replace("\"", "").Trim();
                            var key = vr[0];
                            var kid = 0;
                            while (ret.ContainsKey(key)) {
                                kid++;
                                key = $"{vr[0]}{kid}";
                            }
                            ret[key] = value;
                        }
                    }
                }
#if GAMEJOLT_DEBUG_MODE
                foreach (string k in ret.Keys) { //k,v := range ret {
                    var v = ret[k];
                    chat($"\t{k} = '{v}'");
                }
#endif
                if ((!ret.ContainsKey("success")) || ret["success"] != "true") {
                    chat("Dit ging niet goed");
                    if (!ret.ContainsKey("message"))
                        myerr($"No message known\n{ng}\n{ret["success"]}\nLines:{lines.Length}");
                    else
                        myerr(ret["message"]);
                }
                return ret;
            } catch (Exception E) {
                Debug.WriteLine("ERROR!");
                Debug.WriteLine(E.Message);
                Debug.WriteLine(E.StackTrace);
                return null;
            }
        }
    }



    public class GJTrophy {
        GJUser Parent;
        int ID;
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Image_Url { get; private set; } // Please note, this API can NOT load the image itself, as there is no telling which engine you use for this class. So you'll have to write a loader for the image yourself.
        public string TrophyClass { get; private set; } // Bronze, Silver, Gold, Platinum
        public string AchievedDate { get; private set; }
        public bool Achieved => AchievedDate!="false";

        public void Award() => Parent.AwardTrophy($"{ID}");

        private GJTrophy(GJUser user) { Parent = user; }

        static void chat(string msg) => GJAPI.chat(msg);

        /// <summary>
        /// Fetches all trophies for the game this user has been attached to. Please note that the index numbers are in fact the trophy ID numbers as they are stored on the Game Jolt server!
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        static public Dictionary<int,GJTrophy> FetchAll(GJUser user) {
            GJAPI.chat($"Fetching Trophies for {user.gameid}");
            var ret = new Dictionary<int, GJTrophy>();
            var querystring = $"game_id={user.gameid}&username={user.userid}&user_token={user.token}"; //, user.gamekey);
            var url = $"https://api.gamejolt.com/api/game/v1/trophies/?{querystring}";
            url += "&signature=" + GJAPI.md5(url + user.gamekey);
            GJAPI.chat($"Request sent: {url}");
            var ng = GJAPI.GET(url).Trim().Split('\n');
            GJTrophy trophy=null;
            foreach(string ngline in ng) {
                var nglinec = ngline.Trim();
                var p = nglinec.IndexOf(':');
                var fld = nglinec.Substring(0, p).ToLower();
                var val = nglinec.Substring(p+1);
                if (val[0] == '"') val = val.Substring(1);
                if (val[val.Length - 1] == '"') val=val.Substring(0, val.Length - 1);
                chat($"{fld}:\t'{val}'");
                switch (fld) {
                    case "success":
                        if (val != "true") { chat("No Success!"); return null; }
                        break;
                    case "id":
                        try {
                            var i = Int32.Parse(val);
                            trophy = new GJTrophy(user);
                            ret[i] = trophy;
                            trophy.ID = i;
                        } catch {
                            Console.WriteLine($"Parsing index number ({val}) failed! Is something wrong with the server here?");
                        }
                        break;
                    case "title":
                        if (trophy == null) { Console.WriteLine($"Trying to attach a {fld} to a non-existent trophy!"); return null; }
                        trophy.Name = val;
                        break;
                    case "difficulty":
                        if (trophy == null) { Console.WriteLine($"Trying to attach a {fld} to a non-existent trophy!"); return null; }
                        trophy.TrophyClass = val;
                        break;
                    case "description":
                        if (trophy == null) { Console.WriteLine($"Trying to attach a {fld} to a non-existent trophy!"); return null; }
                        trophy.Description = val;
                        break;
                    case "image_url":
                        if (trophy == null) { Console.WriteLine($"Trying to attach a {fld} to a non-existent trophy!"); return null; }
                        trophy.Image_Url = val;
                        break;
                    case "achieved":
                        if (trophy == null) { Console.WriteLine($"Trying to attach a {fld} to a non-existent trophy!"); return null; }
                        trophy.AchievedDate = val;
                        break;

                    default: throw new Exception($"Unknown field {fld}({val})"); // debug! In normal use this line should be on comment
                }
            }
            return ret;
        }

    }



    /// <summary>
    /// Contains all the user definitions. var User=new GJUser(gameid,gameprivatekey,username,token) will login and store the data in variable "User".
    /// </summary>
    public class GJUser {

        readonly public string userid = "";
        readonly public string token = "";
        readonly public string gameid = "";
        readonly public string gamekey = "";
        /// <summary>Contains 'true' if succesfully logged in.</summary>
        readonly public bool LoggedIn = true;
        readonly public string idstring = "";
        readonly public string gamestuff = "";

        internal Dictionary<string, string> qreq(string action, string querystring) {
            var self = this;
            return GJAPI.gjrequest(action, querystring + self.idstring + self.gamestuff, self.gamekey);
        }

        /// <summary>
        /// Login to Game Jolt
        /// </summary>
        /// <param name="agameid">Game's ID</param>
        /// <param name="aprivatekey">Game's private key</param>
        /// <param name="ausername">User name</param>
        /// <param name="atoken">Token</param>
        public GJUser(string agameid, string aprivatekey, string ausername, string atoken) {
            try {
                userid = ausername;
                token = atoken;
                gameid = agameid;
                gamekey = aprivatekey; //getMD5Hash(privatekey)
                idstring = $"&username={ausername}&user_token={atoken}";
                gamestuff = $"&game_id={gameid}"; //+"&signature="+ret.gamesig
                var d = qreq("users/auth", "");
                if (!d.ContainsKey("success")) {
                    foreach(string k in d.Keys) {
                        Debug.WriteLine($"{k} = {d[k]}");
                        Console.WriteLine($"{k} = {d[k]}");
                    }
                }
                LoggedIn = d["success"] == "true";
            } catch (Exception error) {
                Debug.WriteLine($"Logging in failed: {error.Message}");
                LoggedIn = false;
            }
        }

        /// <summary>
        /// Submit a guest score. Returns true if succesful!
        /// </summary>
        /// <param name="guestname"></param>
        /// <param name="gameid"></param>
        /// <param name="privatekey"></param>
        /// <param name="score"></param>
        /// <param name="sort"></param>
        /// <param name="table_id"></param>
        /// <returns></returns>
        static public bool SubmitGuestScore(string guestname, string gameid, string privatekey, string score, string sort, string table_id) {
            var qs = $"&score={score.Replace(" ", "+")}&sort={sort}";
            if (table_id != "") { qs += "&table_id" + table_id; }
            qs += $"&guest={guestname.Replace(" ", "+")}&game_id={gameid}"; //+"&signature="+getMD5Hash(privatekey)
            var r = GJAPI.gjrequest("scores/add", qs, privatekey);
            return r["success"] == "true";
        }

        public bool SubmitScore(string score, string sort, string table_id) {
            var qs = $"&score={score.Replace(" ", "+")}&sort={sort}";
            if (table_id != "") { qs += "&table_id" + table_id; }
            var r = qreq("scores/add", qs);
            return r["success"] == "true";
        }

        /// <summary>
        /// Fetch scores from Game Jolt
        /// </summary>
        static public Dictionary<string, string> FetchScore(string username, string token, string gameid, string limit, string table_id, string privatekey) {
            var qs = "";
            if (username != "") {
                qs += "username=" + username + "&user_token=" + token;
            }
            if (limit != "") {
                if (qs != "") { qs += "&"; }
                qs += "limit=" + limit;
            }
            if (table_id != "") {
                if (qs != "") { qs += "&"; }
                qs += "table_id=" + table_id;
            }
            if (qs != "") { qs += "&"; }
            qs += "game_id=" + gameid;
            return GJAPI.gjrequest("scores", qs, privatekey);
        }

        /// <summary>
        /// Fetch scores from Game Jolt
        /// </summary>
        public Dictionary<string, string> FetchScore(string limit, string table_id) {
            return FetchScore(userid, token, gameid, limit, table_id, gamekey);
        }

        public void Ping() {
            qreq("sessions/ping", "");
        }

        // Opens a game session for a particular user. Allows you to tell Game Jolt that a user is playing your game. You must ping the session to keep it active and you must close it when you're done with it. Note that you can only have one open session at a time. If you try to open a new session while one is running, the system will close out your current one before opening a new one.
        public void OpenSession() {
            qreq("sessions/open", "");
        }

        // Same as OpenSession :P
        public void StartSession() {
            qreq("sessions/open", "");
        }


        // Closes the active session.
        public void CloseSession() {
            qreq("sessions/close", "");
        }

        public bool AwardTrophy(string id) {
            var r = qreq("trophies/add-achieved", "trophy_id=" + id);
            foreach (string k in r.Keys)
                GJAPI.MSG?.Invoke($"GJ:{k} = {r[k]}");
            return r["success"] == "true"; // temp line
        }

    }
}