# Youtube Trivia Bot  
  
A trivia game for Youtube live stream chats that runs on a Windows computer.   
  
--------------------------------------------------------------------------------  
Features:  
--------------------------------------------------------------------------------   
- Responds to commands in the youtube chat.  
- Questions and answers are stored in two text files on admin's computer.  
- Questions can be added in the chatroom, by moderators only if enabled.  
  
--------------------------------------------------------------------------------  
Quick Start:  
--------------------------------------------------------------------------------  
1. Download and extract the program files.  
2. Set up Google OAuth2 credentials (see detailed instructions below).  
3. Copy appsettings.template.json → appsettings.json  
4. Edit appsettings.json and add your YouTube LiveChat ID.  
5. Run YoutubeSpamFilter.exe  
  
--------------------------------------------------------------------------------  
Program Setup:  
--------------------------------------------------------------------------------  
1. Ensure Microsoft .NET Framework 4.6.1 is installed.  
2. Create a folder for the program (e.g., C:\spamfilter).  
3. Extract the program files into that folder.  
4. Rename appsettings.template.json to appsettings.json  
5. Edit appsettings.json and paste in your LiveChatID value.  
6. Save the file.  
7. Place client_secret.json into the same directory as appsettings.json  


--------------------------------------------------------------------------------  
Google OAuth2 Setup:  
--------------------------------------------------------------------------------  
1. Sign in to Google using the account that will run the bot (must be a moderator or owner).  
2. Search for "YouTube API Console" in Google.  
3. Open the "YouTube Data API Overview - Google Developers" link.  
4. In the left sidebar, click "Get Auth Credentials".  
5. Click "Open the Credentials page".  
6. Click "Create" to make a new project.  
7. Accept the terms of service.  
8. Name your project (any name is fine), then click Create.  
9. Click "Create Credentials" > "OAuth client ID".  
10. Click "Configure consent screen".  
11. Enter a product name (can be anything).  
12. Leave other fields blank and click Save.  
13. Under "Create OAuth client ID", select "Other" (or "Desktop App" if "Other" is unavailable).  
14. Name it (e.g., "My Client") and click Create.  
15. On the success screen, click OK.  
16. Click the download icon next to your new client to download the credentials.  
17. Rename the downloaded file to: client_secret.json  
18. Go to API & Services > Dashboard  
19. Click "Enable APIs and Services"  
20. Search for "YouTube Data API v3", click it, then click Enable.  
21. Ensure you're still signed into the correct Google account (top-right corner).  
	
--------------------------------------------------------------------------------  
Get Your YouTube LiveChat ID:  
--------------------------------------------------------------------------------  
1. Go to the YouTube API Explorer.  
2. Make sure you are signed in as the channel owner.  
3. Click youtube.liveBroadcasts.list  
4. In the "part" field, enter: snippet  
5. In "broadcastStatus", select: active (or upcoming)  
6. Click Execute.  
7. In the response, find and copy the value of: liveChatId  

--------------------------------------------------------------------------------  
Commands:
-------------------------------------------------------------------------------- 
!Trivia - start the game    
!stop - Stop the game  
!myscore - Show your score  
!add question#answer - Add a question (if allowed)   
!highscores - List top scores  




--------------------------------------------------------------------------------  
Customize the Configuration File:
-------------------------------------------------------------------------------- 
Open YoutubeTriviaBot.exe.config   
Edit the following values. Use double backslashes for directory names. Also might want to use a directory with a short simple path  
with no spaces or special characters, etc.   
  
Add questions and answers to the questions.txt and answers.txt.  
The question and answer should be on the same line in each file.  
Don't put question mark at the end of the questions. The bot adds the question mark.   
The cursor should be on the next line at the end of the document. There shouldn't be any extra lines.  
Using NotePad++ seems to show more if there are hidden lines or text that could interfere with reading   
the file.   

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
  
--------------------------------------------------------------------------------  
Run the Program:  
-------------------------------------------------------------------------------- 
8) Double click the YoutubeTriviaBot.exe to run the program  
After about 10 seconds it will finish startup  
If you have the correct LiveChatID you should see messages in the console window  
when someone types something in the chat  
There aren't any commands that can be entered in the console, everything is done  
in the chat.   

![Screenshot of the trivia bot running in the chat](images/screenshot1.png)