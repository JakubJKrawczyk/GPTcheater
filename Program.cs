using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Toolkit.Uwp.Notifications;
using Newtonsoft.Json;

namespace GPTcheater;

class Program
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(Keys vKey);

    static async Task Main()
    {
        Console.WriteLine("Nasłuchiwanie skrótu Ctrl + .");
        while (true)
        {
            if (IsShortcutPressed())
            {
                ShowNotification("Wykryto screen capture");
                Console.WriteLine("Wykonywanie zrzutu ekranu...");
                string imagePath = CaptureScreen();
                string response = await SendToGpt(imagePath);
                ShowNotification(response);
            }
            await Task.Delay(100);
            if(IsExitPressed()) break;
        }
        Console.WriteLine("Naciśnij Enter, aby zakończyć...");
        Console.ReadLine(); // Czekaj na Enter przed zamknięciem
    }

    static bool IsShortcutPressed()
    {
        return (GetAsyncKeyState(Keys.ControlKey) < 0 && GetAsyncKeyState(Keys.OemPeriod) < 0);
    }

    static bool IsExitPressed()
    {
        return (GetAsyncKeyState(Keys.ControlKey) < 0 && GetAsyncKeyState(Keys.Oemcomma) < 0);
    }

    static string CaptureScreen()
    {
        string filePath = Path.Combine(Path.GetTempPath(), "screenshot.png");
        if (Screen.PrimaryScreen != null)
            using (Bitmap bmp = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                bmp.Save(filePath, ImageFormat.Png);
            }

        return filePath;
    }

    static async Task<string> SendToGpt(string imagePath)
    {
        string apiKey = "Bearer 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";
        string apiUrl = "https://api.openai.com/v1/chat/completions";

        using (HttpClient client = new HttpClient())
        {
            
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Headers.Add("Authorization", apiKey);
            
            // Konwertujemy obraz na Base64
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            string base64Image = Convert.ToBase64String(imageBytes);
            string imageUrl = $"data:image/png;base64,{base64Image}";
            
            // Tworzymy JSON payload
            var requestBody = new
            {
                model = "gpt-4o-mini-2024-07-18",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Answer question on image with correct answer or answers. use short response" },
                            new { type = "image_url", image_url = new { url = imageUrl } }
                        }
                    }
                },
                max_tokens = 300
            };

            string json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            request.Content = content;
            
            HttpResponseMessage response = await client.SendAsync(request);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonResponse);
            
            // Deserializuj JSON
            var responseDeserializeObject = JsonConvert.DeserializeObject<Root>(jsonResponse);
            if (responseDeserializeObject is null)
            {
                
                return "error parsing object";
            }
            // Wydobądź odpowiedź modelu
            string answer = responseDeserializeObject.choices[0].message.content;

            Console.WriteLine(answer);
            return answer;
                
            
        }
    }


    static void ShowNotification(string message)
    {
        new ToastContentBuilder()
            .SetToastDuration(ToastDuration.Short)
            .AddText("GPT Odpowiedź")
            .AddText(message).Show();
        


    }
}