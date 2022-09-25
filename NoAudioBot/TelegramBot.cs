using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace NoAudioBot;

public class TelegramBot
{
    private ILogger _logger = LogPoint.GetLogger<TelegramBot>();
    
    private readonly TelegramBotClient _client;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public TelegramBot(string token)
    {
        _client = new TelegramBotClient(token);
    }
    
    public void RunDriver()
    {
        try
        {
            _logger.Information("Started listening...");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var task = _client.ReceiveAsync(
                    HandleUpdateAsync,
                    HandlePollingErrorAsync,
                    receiverOptions,
                    _cancellationTokenSource.Token);

                Task.WaitAll(task);
            }
        }
        catch (Exception e)
        {
            _logger.Fatal(e, "Receiving error");
            throw;
        }
    }
    
    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            var message = update.Message;
            
            if (message?.Type == MessageType.Voice)
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "В данном чате запрещено отправлять голосовые сообщения!", replyToMessageId: message.MessageId, disableNotification: true, cancellationToken: cancellationToken);

                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken: cancellationToken);

                _logger.Information($"Deleted message of the user: {message.From}");
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Could not handle message");
            throw;
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

        _logger.Error(errorMessage);
        return Task.CompletedTask;
    }
}