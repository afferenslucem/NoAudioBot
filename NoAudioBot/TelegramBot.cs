using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NoAudioBot;

public class TelegramBot
{
    private TelegramBotClient _client;

    private CancellationTokenSource _cancellationTokenSource = new();

    public TelegramBot(string token)
    {
        _client = new TelegramBotClient(token);
    }
    
    public void RunDriver()
    {
        Console.WriteLine("Started listening...");
        
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new [] { UpdateType.Message }
        };
        
        while (!_cancellationTokenSource.IsCancellationRequested)
        { 
            var task = _client.ReceiveAsync(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: _cancellationTokenSource.Token);

            Task.WaitAll(task);
        }
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        var message = update.Message;
        
        if (message?.Type == MessageType.Voice)
        {
            await botClient.SendTextMessageAsync(message.Chat.Id, "В данном чате запрещено отправлять голосовые сообщения!", replyToMessageId: message.MessageId, disableNotification: true, cancellationToken: cancellationToken);

            await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);

            Console.WriteLine($"Deleted message of the user: {message.From}");
        }
    }

    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        return Task.CompletedTask;
    }
}