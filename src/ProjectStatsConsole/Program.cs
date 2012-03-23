using System;
using CommandLine;
using Newtonsoft.Json.Linq;
using ConsoleApplication;
using System.IO;
using System.Data;
using ProjectStats.Client;

namespace ProjectStatsConsole {
    public class Program : CommandLineProgram<Program, Program.Arguments> {
        public static void Main(string[] args) {
            Program me = new Program();
            me.RunProgram(args);
        }

        protected override void Validate(Arguments arguments) {
            if (arguments == null) throw new ArgumentNullException(@"Arguments", @"Program arguments cannot be null");

            if (string.IsNullOrEmpty(arguments.LoadFromFile)) {
                RequireString(@"Repository Name", arguments.RepositoryName);

                if (string.IsNullOrEmpty(arguments.Token)) {
                    if (string.IsNullOrEmpty(arguments.UserName) ||
                        string.IsNullOrEmpty(arguments.Password)) {
                        throw new ArgumentNullException(@"UserName/Password", @"User Name and Password is required if Token is not specified");
                    }
                }
            }
        }

        protected override void Run(Arguments args) {
            GithubClient client = null;
            if (args.MockClient) {
                Out(@"Using Mock Client");
                client = new MockGithubClient();
            } else {
                client = new GithubClient();
            }

            DataTable dt = new DataTable();
            if (string.IsNullOrEmpty(args.LoadFromFile) == false) {
                Out(@"Reading issues data from file {0}", args.LoadFromFile);
                dt.ReadXml(args.LoadFromFile);
            } else {
                Out(@"Authenticating as user {0}", args.UserName);
                client.Authenticate(args.UserName, args.Password);

                Out(@"Pulling issues data for {0}/{1}", args.OrgName ?? args.UserName, args.RepositoryName);
                dt = client.ListIssues(args.OrgName ?? args.UserName, args.RepositoryName);
                if (string.IsNullOrEmpty(args.SaveToFile) == false) {
                    Out(@"Saving issues data to file {0}", args.SaveToFile);
                    dt.WriteXml(args.SaveToFile, XmlWriteMode.WriteSchema);
                }
            }
        }

        protected override void Exit(Arguments arguments) {
            if (arguments.WaitForExit) WaitForExit();
        }

        public class Arguments {

            [Option(@"q", @"mock", HelpText = @"Use a mock github client that does not require Internet access.  For testing only.")]
            public bool MockClient = false;

            [Option(@"o", @"org", HelpText = @"The organization under which the repository lives.  If empty, the user name is used instead.")]
            public string OrgName = null;

            [Option(@"r", @"repository", HelpText = @"The name of the Github repository")]
            public string RepositoryName = null;

            [Option(@"u", @"user", HelpText = @"The user name for authentication")]
            public string UserName = null;

            [Option(@"p", @"password", HelpText = @"The specified user's password")]
            public string Password = null;

            [Option(@"t", @"token", HelpText = @"The specified user's API token (instead of password)")]
            public string Token = null;

            [Option(@"l", @"load", HelpText = @"Path and file name to read Github data.  If this option is specified, the program will ignore any user, org or repository parameters.")]
            public string LoadFromFile = null;

            [Option(@"s", @"save", HelpText = @"Path and file name to create from Github output. The specified file will be overwritten if it exists.")]
            public string SaveToFile = null;

            [Option(@"t", @"test", HelpText = @"If TRUE, then test connection to Github and exit.")]
            public bool TestConnection = false;

            [Option(@"w", @"wait", HelpText = @"If TRUE, then wait for <Enter> before ending the program.")]
            public bool WaitForExit = false;

            public override string ToString() {
                return string.Format(@"Repository = {0}/{5}, Test = {4}, Wait for Exit = {1}, {2} {3}",
                    this.RepositoryName, this.WaitForExit,
                    string.IsNullOrEmpty(SaveToFile) ? @"Load from" : @"Save to",
                    string.IsNullOrEmpty(SaveToFile) ? this.LoadFromFile: this.SaveToFile,
                    this.TestConnection, this.OrgName ?? this.UserName
                    );
            }
        }
    }
}
