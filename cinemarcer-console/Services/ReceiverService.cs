using Console.Advanced.Abstract;
using Telegram.Bot;

namespace Console.Advanced.Services;

using Microsoft.Extensions.Logging;

// Compose Receiver and UpdateHandler implementation
public class ReceiverService(ITelegramBotClient botClient, UpdateHandler updateHandler, ILogger<ReceiverServiceBase<UpdateHandler>> logger)
    : ReceiverServiceBase<UpdateHandler>(botClient, updateHandler, logger);
