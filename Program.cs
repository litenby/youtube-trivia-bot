using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TriviaBotApp
{
    internal class TriviaBot
    {
        private Timer _messageTimer;
        private Timer _questionTimer;
        private Timer _commandTimer;
        private Timer _startupMessageTimer;

        private int _firstConnect = 0;
        private string _nextPageToken;
        private int _stage = 0;
        private int _done = 1;
        private string _currentQuestion = "";
        private string _currentAnswer = "";
        private int _currentQuestionLine = 0;
        private string[] _recentMessages = new string[75];
        private int _questionAnswered = 0;
        private DateTime _askTime;
        private string _hint1 = "";
        private string _msgToSend = "";
        private int _startUpMsgHoldBack = 1;
        private readonly string _basePath = @"c:\triviabot\";
        private string QuestionsFile => Path.Combine(_basePath, "questions.txt");
        private string AnswersFile => Path.Combine(_basePath, "answers.txt");
        private string ScoresFile => Path.Combine(_basePath, "scores.txt");
        private string ClientSecretsFile => Path.Combine(_basePath, "client_secrets.json");

        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("Trivia Bot");
            Console.WriteLine("==========");

            var triviaBot = new TriviaBot();
            triviaBot.Start();

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private void StackMessages(string input)
        {
            for (int i = 0; i < 74; i++)
            {
                _recentMessages[i + 1] = _recentMessages[i];
            }
            _recentMessages[0] = input;
        }

        public string GetScores()
        {
            var entries = File.ReadAllLines(ScoresFile);
            var orderedEntries = entries.OrderByDescending(x => int.Parse(x.Split(',')[1]));

            var topScores = orderedEntries.Take(5);
            var highScores = "High scores: ";
            foreach (var score in topScores)
            {
                highScores += score + " | ";
            }

            return highScores;
        }

        private void AddQuestion(string newQuestion)
        {
            var counter = 0;
            string question = "";
            string answer = "";
            using (var file = new StreamReader(QuestionsFile))
            {
                while ((file.ReadLine()) != null)
                {
                    counter++;
                }
            }

            var values = newQuestion.Split('#');
            question = values[0].Substring(5);
            answer = values[1];

            File.AppendAllText(AnswersFile, answer + Environment.NewLine);
            File.AppendAllText(QuestionsFile, question + Environment.NewLine);
        }

        private static void UpdateLine(string newText, string fileName, int lineToEdit)
        {
            var lines = File.ReadAllLines(fileName);
            lines[lineToEdit] = newText;
            File.WriteAllLines(fileName, lines);
        }

        public int GetUserScore(string userName)
        {
            int score = 0;
            using (var file = new StreamReader(ScoresFile))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(userName))
                    {
                        var values = line.Split(',');
                        int.TryParse(values[1], out score);
                    }
                }
            }

            return score;
        }

        public void AddPoint(string userName)
        {
            int score = 0;
            int foundLine = -1;
            string replacement = "";
            string newUserLine = "";
            int destinationLine = 0;

            using (var file = new StreamReader(ScoresFile))
            {
                string line;
                int counter = 0;
                while ((line = file.ReadLine()) != null)
                {
                    if (line.Contains(userName))
                    {
                        foundLine = counter;
                        var values = line.Split(',');
                        int.TryParse(values[1], out score);
                        score++;
                        replacement = $"{values[0]},{score}";
                    }
                    counter++;
                }
            }

            if (foundLine == -1)
            {
                newUserLine = $"{userName},1";
                destinationLine = File.ReadAllLines(ScoresFile).Length + 1;
                File.AppendAllText(ScoresFile, newUserLine + Environment.NewLine);
            }
            else
            {
                UpdateLine(replacement, ScoresFile, foundLine);
            }
        }

        public void GenerateHints()
        {
            var sb = new StringBuilder(_currentAnswer);
            for (int i = 1; i < sb.Length - 1; i++)
            {
                sb[i] = '_';
            }
            _hint1 = sb.ToString();
        }

        public void GetQuestion()
        {
            var lines = File.ReadAllLines(QuestionsFile);
            var randomLineNumber = new Random().Next(0, lines.Length - 1);
            _currentQuestion = lines[randomLineNumber];
            _currentQuestionLine = randomLineNumber;
        }

        public void GetAnswer()
        {
            var answerLines = File.ReadAllLines(AnswersFile);
            _currentAnswer = answerLines[_currentQuestionLine];
        }

        private void MessageTimer_Tick(object state)
        {
            try
            {
                GetMsg(_currentAnswer);
                Console.WriteLine($"{DateTime.Now} - Retrieving new messages");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        private void CommandTimer_Tick(object state)
        {
            try
            {
                _startUpMsgHoldBack = 0;
                Console.WriteLine("Messages are now allowed to be sent.");
                _commandTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        private void StartupMessageTimer_Tick(object state)
        {
            try
            {
                SendMsg("Commands: !trivia !stop !myscore");
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions)
                {
                    Console.WriteLine($"Error: {e.Message}");
                }
            }
            GC.Collect();
            Thread.Sleep(500);
        }

        private void QuestionTimer_Tick(object state)
        {
            if (_done == 1)
            {
                GetQuestion();
                GetAnswer();
                GenerateHints();
                _stage = 0;
                _done = 0;
                _questionAnswered = 0;
            }
            else if (_stage == 0 && _done != 1)
            {
                _stage++;
                _askTime = DateTime.Now;
                try
                {
                    _msgToSend = $"{_currentQuestion}?";
                    SendMsg(_msgToSend);
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (_stage == 1 && _done != 1)
            {
                _stage++;
                try
                {
                    // SendMsg("hint 1");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (_stage == 2 && _done != 1)
            {
                _stage++;
                try
                {
                    SendMsg($"Hint: {_hint1}");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (_stage == 3 && _done != 1)
            {
                _stage++;
                try
                {
                    // SendMsg("hint 3");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
            else if (_stage == 4 && _done != 1)
            {
                _stage = 0;
                _done = 1;
                try
                {
                    SendMsg($"Time is up! The correct answer was: {_currentAnswer}");
                }
                catch (AggregateException ex)
                {
                    foreach (var e in ex.InnerExceptions)
                    {
                        Console.WriteLine($"Error: {e.Message}");
                    }
                }
                GC.Collect();
                Thread.Sleep(500);
            }
        }

        private void Start()
        {
            _messageTimer = new Timer(MessageTimer_Tick, null, 3000, 2000);
            _commandTimer = new Timer(CommandTimer_Tick, null, 15000, 15000);
            _startupMessageTimer = new Timer(StartupMessageTimer_Tick, null, 30000, 3600000);
        }

        public async Task SendMsg(string message)
        {
            if (_startUpMsgHoldBack != 0)
            {
                Console.WriteLine($"HELD BACK: {message}");
                return;
            }

            Console.WriteLine($"SENT: {message}");
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

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = this.GetType().ToString()
            });

            var liveChatMessage = new LiveChatMessage
            {
                Snippet = new LiveChatMessageSnippet
                {
                    Type = "textMessageEvent",
                    LiveChatId = "YOUR_LIVE_CHAT_ID",
                    TextMessageDetails = new LiveChatTextMessageDetails
                    {
                        MessageText = message
                    }
                }
            };

            await youtubeService.LiveChatMessages.Insert(liveChatMessage, "snippet").ExecuteAsync();
        }

        public async Task GetMsg(string curAnswer)
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

            string liveChatId = "YOUR_LIVE_CHAT_ID";
            var chatRequest = ytService.LiveChatMessages.List(liveChatId, "id,snippet,authorDetails");
            chatRequest.PageToken = _nextPageToken;

            var chatResponse = await chatRequest.ExecuteAsync();
            _nextPageToken = chatResponse.NextPageToken;

            foreach (var message in chatResponse.Items)
            {
                var messageId = message.Id;
                var displayName = message.AuthorDetails.DisplayName;
                var displayMessage = message.Snippet.DisplayMessage;
                var messageTime = message.Snippet.PublishedAt ?? DateTime.MinValue;

                if (displayName != "Trivia Bot" &&
                    !_recentMessages.Contains(messageId) &&
                    _startUpMsgHoldBack == 0)
                {
                    StackMessages(messageId);
                    Console.WriteLine($"{DateTime.Now} Received: {displayMessage}");

                    if (displayMessage.IndexOf(curAnswer, StringComparison.OrdinalIgnoreCase) >= 0 &&
                        _done == 0 &&
                        _questionAnswered == 0)
                    {
                        _questionAnswered = 1;
                        _done = 1;

                        var delay = (DateTime.Now - messageTime).Seconds;
                        await SendMsg($"You got it, {displayName}! [{delay}s] The correct answer was: {curAnswer}.");
                        AddPoint(displayName);
                    }
                    else if (displayMessage.Contains("!trivia"))
                    {
                        _done = 1;
                        _stage = 0;
                        await SendMsg("Trivia Bot started! First question coming up...");
                        _questionTimer = new Timer(QuestionTimer_Tick, null, 0, 10000);
                    }
                    else if (displayMessage.Contains("!stop"))
                    {
                        _done = 1;
                        await SendMsg($"Trivia stopped by {displayName}");
                        _questionTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                    }
                    else if (displayMessage.Contains("!myscore"))
                    {
                        var score = GetUserScore(displayName);
                        await SendMsg($"{displayName}'s score: {score}");
                    }
                    else if (displayMessage.Contains("!add"))
                    {
                        AddQuestion(displayMessage);
                        await SendMsg("Question added.");
                    }
                    else if (displayMessage.Contains("!highscores"))
                    {
                        var scores = GetScores();
                        await SendMsg(scores);
                    }
                }
            }
        }
    }
}
