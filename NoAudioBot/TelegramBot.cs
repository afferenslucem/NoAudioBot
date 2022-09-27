using Serilog;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace NoAudioBot;

public class TelegramBot
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly TelegramBotClient _client;

    private User _botUser = null!;
    private readonly ILogger _logger = LogPoint.GetLogger<TelegramBot>();

    private string _declineMessage = "";

    public TelegramBot(string token)
    {
        _client = new TelegramBotClient(token);
    }

    public async Task RunDriver()
    {
        try
        {
            _botUser = await _client.GetMeAsync();

            _logger.Information($"My id is {_botUser.Id}");
            _logger.Information($"My name is {_botUser.Username}");

            _declineMessage = await File.ReadAllTextAsync("decline-message.html");
            _logger.Information($"Decline message: {_declineMessage}");

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = new[] { UpdateType.Message }
            };

            _logger.Information("Start listening...");
            while (!_cancellationTokenSource.IsCancellationRequested)
                await _client.ReceiveAsync(
                    HandleUpdateAsync,
                    HandlePollingErrorAsync,
                    receiverOptions,
                    _cancellationTokenSource.Token);
        }
        catch (Exception e)
        {
            _logger.Fatal(e, "Receiving error");
            throw;
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = update.Message;

            if (message?.NewChatMembers?.Any(member => member.Id == _botUser.Id) ?? false)
            {
                _logger.Information($"Bot added to chat: {message.Chat.Title}");
                return;
            }

            if (message?.LeftChatMember?.Id == _botUser.Id)
            {
                _logger.Information($"Bot removed from chat: {message.Chat.Title}");
                return;
            }

            if (message?.Type == MessageType.Voice)
            {
                await botClient.SendTextMessageAsync(
                    message.Chat.Id,
                    _declineMessage, 
                    replyToMessageId: message.MessageId,
                    disableNotification: true, 
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken
                );

                await botClient.DeleteMessageAsync(message.Chat.Id, message.MessageId, cancellationToken);

                _logger.Information($"Deleted message of the user: {message.From}");
            }
        }
        catch (Exception e)
        {
            _logger.Error(e, "Could not handle message");
            throw;
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
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