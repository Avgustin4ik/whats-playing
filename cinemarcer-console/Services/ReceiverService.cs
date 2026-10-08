namespace cinemarcer_console.Services;

using Abstract;
using Microsoft.Extensions.Logging;using Movies;
using Telegram.Bot;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger<ReceiverServiceBase<UpdateHandler>> logger)
    : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
