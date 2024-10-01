using Azure.Messaging.ServiceBus;
using System.Text.Json;

//Set the following variables to the appropriate values
const string outputFilePath = "";
const string queueName = "";
var client = new ServiceBusClient(connectionString: "");

var receiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
{
    SubQueue = SubQueue.DeadLetter
});

var deletedMessages = new List<object>();
try
{
    int messageCount = 0;
    while (true)
    {
        var messages = await receiver.ReceiveMessagesAsync(maxMessages: 100, maxWaitTime: TimeSpan.FromSeconds(5));
        if (messages.Count == 0)
        {
            Console.WriteLine("No more messages in the deadletter queue.");
            break;
        }

        foreach (var message in messages)
        {
            messageCount++;
            var bodyJson = JsonSerializer.Deserialize<JsonElement>(message.Body.ToString());

            var messageDetails = new
            {
                SequenceNumber = message.SequenceNumber,
                EnqueuedTime = message.EnqueuedTime,
                Body = bodyJson
            };

            deletedMessages.Add(messageDetails);

            await receiver.CompleteMessageAsync(message);
            Console.WriteLine($"Deleted message: SequenceNumber: {message.SequenceNumber}");
        }
    }

    Console.WriteLine($"Deleted {messageCount} messages from the deadletter queue.");
}
finally
{
    await receiver.CloseAsync();
    await client.DisposeAsync();
}

string jsonOutput = JsonSerializer.Serialize(deletedMessages, new JsonSerializerOptions { WriteIndented = true });
await File.WriteAllTextAsync(outputFilePath, jsonOutput);
Console.WriteLine("Finished cleaning up the deadletter queue.");
