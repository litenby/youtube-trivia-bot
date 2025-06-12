# Youtube Trivia Bot

A trivia game for Youtube live stream chats that runs on a Windows computer. 

## Features
- Responds to commands in the youtube chat.
- Questions and answers are stored in two text files on admin's computer.
- Questions can be added in the chatroom, by moderators only if enabled.

## Quick Start
1. **Download and extract** the bot.
2. **Configure Google OAuth2** (see below).
3. **Edit configuration** (`YoutubeTriviaBot.exe.config`).
4. **Add your questions/answers** to `questions.txt` and `answers.txt`.
5. **Run** `YoutubeTriviaBot.exe`.

## Commands
!Trivia - start the game <br>
!stop - Stop the game   <br>
!myscore - Show your score   <br>
!add question#answer - Add a question (if allowed)   <br>
!highscores - List top scores   <br>

## Setup Instructions
### 1. Google OAuth2 Credentials
Log in to google with the user that will be the bot. It should be a user that has moderator or owner access to the channel since these can send messages more frequently. In google search for Youtube API Console Click the link called Youtube Data API Overview - Google Developers On the left click Get Auth Credentials. 
Where it says "Open the Credentials page" click that
Click Create to create a project
Accept the terms of service
Name the project anything and then click create.
Click Create credentials
Select OAuth client ID
Click configure consent screen
Enter a product name, can be anything
Leave other boxes blank
Click Save at the bottom
Under Create OAuth client ID select "Other"
Type a name in the name box such as "My Client"
On the next screen where it says OAuth client, click OK. 
Under OAuth 2.0 client IDs, click the download arrow on the far right side
Download the file
In Windows Explorer rename the long name of client_secret_345342432362-kjkklkewjfqlkf to simply client_secret.json
Under API & Services Click Dashboard
Click Enable APIS and Services
Search for YouTube Data API v3 and click on it
Click the blue Enable box

If you have multiple google ID's it's important to keep an eye on the upper right to
make sure it doesn't switch to another one while doing these steps
### 2. Find Your LiveChatID
find the livechatid for the live stream chat using the Youtube API Console (Live stream must be created first)
Each livestream chat has a unique id. You can get this id using the Youtube API Console. 
Make sure you are logged in to youtube with the channel owner's account
In API Explorer click
youtube.liveBroadcasts.list
In the "part" box put "snippet"
In the broadcastStatus box select Active (or Upcoming if it is still upcoming)
Click Execute at the bottom
Scroll down and find the value for liveChatId
Copy this value to notepad temporarily
3) install Microsoft .NET 4.6.1 if not installed
4) create a directory for the trivia bot, such as c:\triviabot
5) uncompress the trivia bot zip file into the triviabot directory
6) Add questions and answers to the questions.txt and answers.txt.
The question and answer should be on the same line in each file.
Don't put question mark at the end of the questions. The bot adds the question mark. 
The cursor should be on the next line at the end of the document. There shouldn't be any extra lines.
Using NotePad++ seems to show more if there are hidden lines or text that could interfere with reading 
the file. 
### 3. Edit configuration file (triviabot.config)
Open YoutubeTriviaBot.exe.config 
Edit the following values. Use double backslashes for directory names. Also might want to use a directory with a short simple path
with no spaces or special characters, etc. 

questionsLocation - location to the questions.txt
answersLocation - location to the answers.txt
scoresLocation - location to the scores.txt
clientSecretsLocation - location to the client_secrets.json
getMsgDelay - how often in seconds that the bot retrieves chat messages from youtube
questionDelay - time period to answer question
advertiseDelay - how long in seconds between advertising the trivia bot's commands in the chat
livechatid - the unique id of the chat found in step 2
botName - set this to match the name of the bot's user name, for example Trivia Bot. This is so the bot ignores messages from itself in the chat.
restrictedAdd - set to "yes" and only the name in the questionAuthor field will be able to add questions. Set to "no" and anyone can use the add question command
questionAuthor - the bot will only add questions from this user name if restrictedAdd is set to "yes"

### 4. Run the program
8) Double click the YoutubeTriviaBot.exe to run the program
After about 10 seconds it will finish startup
If you have the correct LiveChatID you should see messages in the console window
when someone types something in the chat
There aren't any commands that can be entered in the console, everything is done
in the chat. 
To exit the program hit any key

### 5. Demostration Video
www.youtube.com/video

### FAQ / Troubleshooting
