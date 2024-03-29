﻿using System;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data;
using System.Collections.Specialized;
using ProjectStats.Model;

namespace ProjectStats.Client {
    public class GithubClient {
        public string UserName { get; set; }
        private string Password = null;
        public string Token { get; set; }
        public string LastResponse { get; protected set; }
        public JObject User { get; private set; }

        public string BaseUrl { get; set; }

        public GithubClient() : this(@"https://api.github.com") { }

        public GithubClient(string baseUrl) {
            this.BaseUrl = baseUrl;
        }

        public string GetRequestUrl(string relativePath) {
            return this.BaseUrl + relativePath;
        }

        public void Authenticate(string userName, string pwd) {
            this.UserName = userName;
            this.Password = pwd;
            //this.RequestToJSONArrayResponse(@"/issues");
            this.User = GetUserInfo(this.UserName);
        }

        private HttpWebRequest GetAuthenticatedRequest(string requestPath) {
            string url = GetRequestUrl(requestPath);
            HttpWebRequest req = HttpWebRequest.Create(url) as HttpWebRequest;

            string basicauth = Convert.ToBase64String(Encoding.UTF8.GetBytes(this.UserName + @":" + this.Password));
            req.Headers.Add(@"Authorization", @"Basic " + basicauth);
            return req;
        }

        public virtual string PostRequest(string requestPath, string requestBody) {
            HttpWebRequest req = GetAuthenticatedRequest(requestPath);
            req.Method = @"POST";
            using (StreamWriter sw = new StreamWriter(req.GetRequestStream())) {
                sw.WriteLine(requestBody);
                sw.WriteLine();
            }
            using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse) {
                if (resp == null) throw new NullReferenceException(@"The web request to " + requestPath + @" did not receive a response");
                if (resp.StatusCode >= HttpStatusCode.BadRequest) throw new Exception(@"The web request to " + requestPath + " returned HTTP Status " + resp.StatusCode.ToString());
                using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                    this.LastResponse = sr.ReadToEnd();
                    return this.LastResponse;
                }
            }
        }

        public virtual string GetRequest(string requestPath) {
            using (HttpWebResponse resp = GetAuthenticatedRequest(requestPath).GetResponse() as HttpWebResponse) {
                if (resp == null) throw new NullReferenceException(@"The web request to " + requestPath + @" did not receive a response");
                if (resp.StatusCode >= HttpStatusCode.BadRequest) throw new Exception(@"The web request to " + requestPath + " returned HTTP Status " + resp.StatusCode.ToString());
                using (StreamReader sr = new StreamReader(resp.GetResponseStream())) {
                    this.LastResponse = sr.ReadToEnd();
                    return this.LastResponse;
                }
            }
        }

        public JObject GetUserInfo(string userName) {
            return RequestToJSONResponse(@"/users/" + userName);
        }

        public JObject RequestToJSONResponse(string requestPath) {
            string response = GetRequest(requestPath);
            return JObject.Parse(response);
        }

        protected static bool AppendJsonData(string rawJson, DataTable dt) {
            string json = string.Format("{0}result: {1}{2}", @"{", rawJson, @"}");

            JObject root = JObject.Parse(json);
            JArray items = (JArray)root["result"];

            bool anyItems = false;

            foreach (JObject item in items) {
                anyItems = true;
                
                DataRow dr = dt.NewRow();

                foreach (DataColumn col in dt.Columns) {
                    JToken val = item.SelectToken(col.Caption);
                    if (val == null) continue;
                    dr[col] = val.ToString();
                }
                if (dr.IsNull(@"pull_request") == false &&
                    string.IsNullOrEmpty((string)dr[@"pull_request"]) == false) continue;
                dt.Rows.Add(dr);
            }
            dt.AcceptChanges();

            return anyItems;
        }

        private static string GetName(JToken token) {
            JProperty prop = token as JProperty;
            if (prop == null) return string.Empty;
            return prop.Name;
        }

        public DataTable RequestToDataTable(string requestPath) {
            DataTable ret = ProjectStatsModelFactory.CreateIssuesTable();
            AppendJsonData(GetRequest(requestPath), ret);
            return ret;
        }

        public DataTable ListIssues(string userOrOrg, string repo, DateTime? since) {
            // GET /repos/:user/:repo/issues
            int pageSize = 100;

            DataTable ret = ProjectStatsModelFactory.CreateIssuesTable();

            if (since.HasValue) {
                int page = 0;
                string template = @"/repos/{0}/{1}/issues?state=closed&page={2}&per_page={3}&since={4:s}&sort=updated&direction=desc";
                string response = string.Empty;
                do {
                    response = GetRequest(string.Format(template, userOrOrg, repo, ++page, pageSize, since));
                } while (AppendJsonData(response, ret));
            } else {
                // Apparently, can't fetch BOTH open and closed in the same API call.  Guh.
                int page = 0;
                string template = @"/repos/{0}/{1}/issues?state=open&page={2}&per_page={3}";
                string response = string.Empty;
                do {
                    response = GetRequest(string.Format(template, userOrOrg, repo, ++page, pageSize, since));
                } while (AppendJsonData(response, ret));

                page = 0;
                template = @"/repos/{0}/{1}/issues?state=closed&page={2}&per_page={3}";
                response = string.Empty;
                do {
                    response = GetRequest(string.Format(template, userOrOrg, repo, ++page, pageSize, since));
                } while (AppendJsonData(response, ret));
            }
            return ret;
        }

        public DataTable ListIssues(string userOrOrg, string repo) {
            return ListIssues(userOrOrg, repo, null);
        }

        public DataTable ListIssues(string filter, int pageSize, int pageNum, string sortField, bool sortAscending) {
            // GET /issues
            string requestPath = string.Format(@"/issues?filter={0}&sort={1}&direction={2}", filter, sortField, sortAscending ? @"asc" : @"desc");
            return RequestToDataTable(requestPath);
        }


        internal void CreateIssue(string userOrOrg, string repo, string title, string body, string assignee, int milestone) {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            var item = new { title = title, body = body, assignee = assignee, milestone = milestone };
            new JsonSerializer().Serialize(sw, item);
            string json = sb.ToString();

            // POST /repos/:user/:repo/issues
            string requestPath = string.Format(@"/repos/{0}/{1}/issues", userOrOrg, repo);
            PostRequest(requestPath, json);
        }
    }
}
