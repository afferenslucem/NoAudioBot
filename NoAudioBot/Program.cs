using NoAudioBot;
using Telegram.Bot;



var token = File.ReadAllText("token.txt");
var bot = new TelegramBot(token);
bot.RunDriver();