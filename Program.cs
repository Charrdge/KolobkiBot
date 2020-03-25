using System;
using System.IO;
using System.Net;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ColobkiMessage
{
	class Program
	{
        #region Key
        private const string KEYFILENAME = "TGKEY.txt";

        private static string _key;
        /// <summary>
        /// Ключ бота
        /// </summary>
        internal static string Key
        {
            get
            {
                if (_key is null || _key == "") _key = new StreamReader(KEYFILENAME).ReadLine();
                return _key;
            }
        }
        #endregion Key

        private static int _num = 0;
        public static int Num => ++_num;

        static void Main(string[] args)
		{
            Console.WriteLine("Connect to telegram...");

            var api = new TelegramBotClient(Key);

            var task = api.GetMeAsync();

            task.Wait();

            var me = task.Result;
            Console.WriteLine($"Telegram bot with ID {me.Id} and name {me.FirstName} has been connected");

            api.OnMessage += (object j, MessageEventArgs e) => new System.Threading.Tasks.Task(() => MessageActor(api, e)).Start();

            api.OnInlineQuery += (object j, InlineQueryEventArgs e) => new System.Threading.Tasks.Task(() => InlineActor(api, e)).Start();

            api.OnInlineResultChosen += (object j, ChosenInlineResultEventArgs e) => new System.Threading.Tasks.Task(() => InlineResultActor(api, e)).Start();

            api.StartReceiving();

            

            while (true) System.Threading.Thread.Sleep(10000);
        }

        public static void MessageActor(TelegramBotClient api, MessageEventArgs e)
        {
            if (e.Message.Type == Telegram.Bot.Types.Enums.MessageType.Text)
            {
                string path = DownloadGif(GetGifUrl(e.Message.Text));

                var chat = e.Message.Chat.Id;

                Stream stream = new FileStream(path, FileMode.Open);

                //var file = new Telegram.Bot.Types.InputFiles.InputOnlineFile(stream);

                var task = api.SendVideoAsync(chat, stream);
                
                task.Wait();

                File.Delete(path);
            }
        }

        public static void InlineActor(TelegramBotClient api, InlineQueryEventArgs e)
        {
            string text = e.InlineQuery.Query;

            string url = GetGifUrl(text);

            //Stream stream = new FileStream(path, FileMode.Open);

            Console.WriteLine("inline");

            api.AnswerInlineQueryAsync(e.InlineQuery.Id, new Telegram.Bot.Types.InlineQueryResults.InlineQueryResultBase[] {
                new Telegram.Bot.Types.InlineQueryResults.InlineQueryResultGif("1", url, url)
            }, 12000);
        }

        private static void InlineResultActor(TelegramBotClient api, ChosenInlineResultEventArgs e)
        {
            string id = e.ChosenInlineResult.Query;
            string path = e.ChosenInlineResult.ResultId;
        }

        private static string GetGifUrl(string str)
        {
            string url = "";

            var request = WebRequest.Create("http://www.laie-smileys.com/spray/index.php");
            request.Method = "POST";

            request.Headers[HttpRequestHeader.Referer] = "http://www.laie-smileys.com/spray/index.php";

            string postData = $"message={str}&smiley_back=1&entrance=2&text_color=1&end_b=-21&end_g=-21&loop=1&fadeto=&background=0&delay=1";
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);

            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.  
            request.ContentLength = byteArray.Length;

            // Get the request stream.  
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.  
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.  
            dataStream.Close();

            // Get the response.  
            WebResponse response = request.GetResponse();
            // Display the status.  
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);

            // Get the stream containing content returned by the server.  
            // The using block ensures the stream is automatically closed.
            using (dataStream = response.GetResponseStream())
            {
                // Open the stream using a StreamReader for easy access.  
                var reader = new StreamReader(dataStream);
                // Read the content.  
                string responseFromServer = reader.ReadToEnd();
                // Display the content.
                url = "http://www.laie-smileys.com/spray/" + responseFromServer.Substring(responseFromServer.IndexOf("download"));
                url = url.Remove(url.IndexOf("\""));
                Console.WriteLine(url);
            }

            // Close the response.  
            response.Close();

            return url;
        }

        private static string DownloadGif(string url)
        {
            var webClient = new WebClient();
            string path = $"{Num}.gif";
            webClient.DownloadFile(url, path);
            return path;
        }
    }
}