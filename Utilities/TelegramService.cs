namespace Melodies25.Utilities
{
    using System.Net.Http;
    using System.Threading.Tasks;

    public class TelegramService
    {
        private readonly string _token = "7742561607:AAF9GXVU7uuJTYOkewv0jnVc1Yyr0EoURsE"; // Ваш токен
        private readonly string _chatId = "1270335468"; // Ваш chat_id

        public async Task SendNotificationAsync(string message)
        {
            var client = new HttpClient();
            var url = $"https://api.telegram.org/bot{_token}/sendMessage?chat_id={_chatId}&text={message}";

            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Message sent successfully!");
            }
            else
            {
                Console.WriteLine("Failed to send message.");
            }
        }
    }
}
