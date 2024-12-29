using SocialNetwork;

var server = new Server("http://*:8080/");

var cts = new CancellationTokenSource();
var serverTask = server.StartAsync(cts.Token);

Console.WriteLine("Нажмите Enter для остановки сервера...");
Console.ReadLine();
cts.Cancel();
await serverTask;
