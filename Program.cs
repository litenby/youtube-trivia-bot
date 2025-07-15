using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace youtube_trivia_bot
{
    internal class TriviaBot
    {
        
        int firstConnect = 0;
        int currentQuestionLine = 0;
        int stage = 0;
        int done = 1;
        int questionAnswered = 0;
        int warmupDelay = 1;

        String nextpagetoken;
        String currentQuestion = "0";
        String currentAnswer = "0";
        String[] recentMessages = new string[75];
        String hint1 = "";
        String msgToSend = "";

        DateTime askTime;

        Timer Timer1;
        Timer Timer2;
        Timer Timer3;
        Timer Timer4;

        private AppConfig config;

        private string QuestionsFile => Path.Combine(config.BasePath, "questions.txt");
        private string AnswersFile => Path.Combine(config.BasePath, "answers.txt");
        private string ScoresFile => Path.Combine(config.BasePath, "scores.txt");
        private string ClientSecretsFile => Path.Combine(config.BasePath, "client_secrets.json");


        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Youtube Trivia Bot");
            Console.WriteLine("==================");

            TriviaBot tb = new TriviaBot();
            tb.start();
            Console.WriteLine("Trivia Bot is running. Press any key to exit the program.");
            Console.ReadKey();
        }

        private void LoadConfig()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found: " + configPath);
                return;
            }

            string json = File.ReadAllText(configPath);
            config = JsonConvert.DeserializeObject<AppConfig>(json);
            Console.WriteLine("Loaded configuration file. LiveChatId status: " + (string.IsNullOrEmpty(config.LiveChatId) ? "[none]" : "[loaded]"));
            Console.WriteLine("LiveChatId: " + config.LiveChatId);
        }

        /*
        void stackMessages(String input1)
        {
            for (int i = 0; i < 74; i++)
            {
                recentMessages[i + 1] = recentMessages[i];
            }
            recentMessages[0] = input1;
        }
        */
        public string getScores()
        {
            string[] Entry = File.ReadAllLines(ScoresFile);
            var orderedEntries = Entry.OrderByDescending(x => int.Parse(x.Split(',')[1]));
            var myList = orderedEntries.Take(5);
            String highScores = "High scores: ";
            foreach (var score in myList)
            {
                highScores += score + " | ";
            }
            return highScores;
        }

        void addQuestion(String newQuestion)
        {
            int counter = 0;
            string myLine = "";
            string question = "";
            string answer = "";
            System.IO.StreamReader file = new System.IO.StreamReader(QuestionsFile);

            while ((myLine = file.ReadLine()) != null)
            {
                counter++;
            }

            string[] values = newQuestion.Split('#');
            question = values[0];
            answer = values[1];
            question = question.Remove(0, 5);
            file.Close();
            File.AppendAllText(AnswersFile, answer + Environment.NewLine);
            File.AppendAllText(QuestionsFile, question + Environment.NewLine);
        }

        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit] = newText;
            File.WriteAllLines(fileName, arrLine);
        }

        public int getUserScore(String userName)
        {
            int userScore = 0;
            int counter = 0;
            string myLine = "";
            System.IO.StreamReader file = new System.IO.StreamReader(ScoresFile);

            while ((myLine = file.ReadLine()) != null)
            {
                if (myLine.Contains(userName))
                {
                    string[] values = myLine.Split(',');
                    Int32.TryParse(values[1], out userScore);
                }
                counter++;
            }

            file.Close();
            return userScore;
        }

        public void addPoint(String userName)
        {
            int val1 = 0;
            int counter = 0;
            int foundLine = -1;
            string myLine = "";
            String replacement = "";
            String newUserLine = "";
            int destinationLine = 0;
            System.IO.StreamReader file = new System.IO.StreamReader(ScoresFile);

            while ((myLine = file.ReadLine()) != null)
            {
                if (myLine.Contains(userName))
                {
                    foundLine = counter;
                    string[] values = myLine.Split(',');
                    Int32.TryParse(values[1], out val1);
                    val1++;
                    replacement = values[0] + "," + val1;
                }
                counter++;
            }

            file.Close();

            if (foundLine == -1)
            {
                newUserLine = userName + "," + "1";
                destinationLine = counter + 1;
                File.AppendAllText(ScoresFile, newUserLine + Environment.NewLine);
            }
            else
            {
                lineChanger(replacement, ScoresFile, foundLine);
                foundLine = -1;
            }
        }

        public void generateHints()
        {
            StringBuilder sb = new StringBuilder(currentAnswer);
            for (int i = 1; i < sb.Length - 1; i++)
            {
                sb[i] = '_';
            }
            hint1 = sb.ToString();
        }

        public void getQuestion()
        {
            var lines = File.ReadAllLines(QuestionsFile);
            var r = new Random();
            var randomLineNumber = r.Next(0, lines.Length - 1);
            var line = lines[randomLineNumber];
            currentQuestion = line;
            currentQuestionLine = randomLineNumber;
        }

        public void getAnswer()
        {
            var answerLines = File.ReadAllLines(AnswersFile);
            currentAnswer = answerLines[currentQuestionLine];
        }

        private void start()
        {
            Console.WriteLine("Starting game timers.");
            Timer1 = new Timer(Timer1_Tick, null, 3000, 2000);        // Retrieves channel messages every 2 seconds.
            Timer3 = new Timer(Timer3_Tick, null, 15000, 15000);      // Asks trivia question, waits 15 seconds, gives hint, then gives answer.
            Timer4 = new Timer(Timer4_Tick, null, 30000, 3600000);    // Prints available commands to the channel. 
            Console.WriteLine("Loading configuration file.");
            LoadConfig();
        }

    public async Task sendMsg(string myMessage)
        {
            if (warmupDelay == 0)
            {
                Console.WriteLine("Sent Message: " + myMessage);
                UserCredential credential;
                using (var stream = new FileStream(ClientSecretsFile, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        new[] { YouTubeService.Scope.Youtube },
                        "user",
                        CancellationToken.None,
                        new FileDataStore(this.GetType().ToString())
                    );
                }

                firstConnect = 1;

                var youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = this.GetType().ToString()
                });

                LiveChatMessage comments = new LiveChatMessage();
                LiveChatMessageSnippet mySnippet = new LiveChatMessageSnippet();
                LiveChatTextMessageDetails txtDetails = new LiveChatTextMessageDetails();
                txtDetails.MessageText = myMessage;
                mySnippet.TextMessageDetails = txtDetails;
                mySnippet.LiveChatId = config.LiveChatId;
                mySnippet.Type = "textMessageEvent";
                comments.Snippet = mySnippet;
                comments = await youtubeService.LiveChatMessages.Insert(comments, "snippet").ExecuteAsync();
            }
            else
            {
                Console.WriteLine("Message not sent until program startup complete: " + myMessage);
            }
        }

        public async Task getMsg(String curAnswer)
        {
            UserCredential credential;
            using (var stream = new FileStream(ClientSecretsFile, FileMode.Open, FileAccess.Read))
            {
                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(this.GetType().ToString())
                );
            }

            var ytService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            String liveChatId = config.LiveChatId;
            var chatMessages = ytService.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
            chatMessages.PageToken = nextpagetoken;
            var chatResponse = await chatMessages.ExecuteAsync();
            nextpagetoken = chatResponse.NextPageToken;

            long? pollinginterval = chatResponse.PollingIntervalMillis;
            PageInfo pageInfo = chatResponse.PageInfo;
            List<LiveChatMessageListResponse> messages = new List<LiveChatMessageListResponse>();
            Console.WriteLine(chatResponse.PageInfo.TotalResults + " total messages " + chatResponse.PageInfo.ResultsPerPage + " results per page" + nextpagetoken);

            foreach (var chatMessage in chatResponse.Items)
            {
                string messageId = chatMessage.Id;
                string displayName = chatMessage.AuthorDetails.DisplayName;
                string displayMessage = chatMessage.Snippet.DisplayMessage;
                System.DateTime messageTime = chatMessage.Snippet.PublishedAt.Value;

                var now = DateTime.Now;
                var timeSince = now - messageTime;
                int toSeconds = timeSince.Seconds;

                if (displayName != config.DisplayName && recentMessages.Contains(messageId).Equals(false) && warmupDelay == 0)
                {
                    Console.WriteLine("recent message: " + messageTime + " Delay: " + toSeconds + "  " + displayMessage);

                    if ((displayMessage.IndexOf(curAnswer, StringComparison.OrdinalIgnoreCase) >= 0) && done == 0 && questionAnswered == 0)
                    {
                        questionAnswered = 1;
                        done = 1;
                        String msg = "You got it, " + displayName + "! [" + toSeconds + "secs] The correct answer was: " + curAnswer + ".";
                        await sendMsg(msg);
                        addPoint(displayName);
                    }
                    else if (displayMessage.Contains("!trivia"))
                    {
                        done = 1;
                        stage = 0;
                        String msg = "Trivia Bot started! First question coming up...";
                        await sendMsg(msg);
                        Timer2 = new Timer(new TimerCallback(Timer2_Tick), null, 0, 10000);
                    }
                    else if (displayMessage.Contains("!stop"))
                    {
                        done = 1;
                        String msg = "Trivia Stopped by " + displayName;
                        await sendMsg(msg);
                        Timer2.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else if (displayMessage.Contains("!myscore"))
                    {
                        int score = getUserScore(displayName);
                        String msg = displayName + "'s score: " + score;
                        await sendMsg(msg);
                    }
                    else if (displayMessage.Contains("!add"))
                    {
                        addQuestion(displayMessage);
                        String msg = "Question added.";
                        await sendMsg(msg);
                    }
                    else if (displayMessage.Contains("!highscores"))
                    {
                        string msg = getScores();
                        await sendMsg(msg);
                    }
                }
            }
        }

        void Timer1_Tick(object state)
        {
            try
            {
                getMsg(currentAnswer);
                Console.WriteLine(DateTime.Now + " getting new Youtube livechat messages");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        void Timer2_Tick(object state)
        {
            if (done == 1)
            {
                getQuestion();
                getAnswer();
                generateHints();
                stage = 0;
                done = 0;
                questionAnswered = 0;
            }
            else if (stage == 0 && done != 1)
            {
                stage++;
                askTime = DateTime.Now;
                try
                {
                    string msg = currentQuestion + "?";
                    sendMsg(msg);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 1 && done != 1)
            {
                stage++;
                try
                {
                    // sendMsg("hint");
                    // Additional hints can be added here in the future if desired.
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 2 && done != 1)
            {
                stage++;
                try
                {
                    sendMsg("Hint: " + hint1);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 3 && done != 1)
            {
                stage++;
                try
                {
                    // sendMsg("hint");
                    // Additional hints can be added here in the future if desired.
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (stage == 4 && done != 1)
            {
                stage = 0;
                done = 1;
                try
                {
                    sendMsg("Time is up! The correct answer was:  " + currentAnswer);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
        }
        void Timer3_Tick(object state)
        {
            try
            {
                warmupDelay = 0;
                Console.WriteLine("Messages now allowed to be sent to Youtube chat.");
                Timer3.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        void Timer4_Tick(object state)
        {
            try
            {
                sendMsg("Commands:  !trivia  !stop  !myscore ");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }
    }
}
