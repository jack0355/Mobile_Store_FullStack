using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class ApiClient
{
    private static readonly HttpClient _http = new HttpClient();
    private static readonly string _baseUrl = "https://localhost:7281";



    public static string AccessToken { get; set; } = "";
    public static string RefreshToken { get; set; } = "";


    public static async Task<bool> LoginAsync(string email , string password)
    {
        var payload = new {email = email, password = password};
        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");


        var response = await _http.PostAsync($"{_baseUrl}/api/Auth/Login", content);
        if (!response.IsSuccessStatusCode) return false;


        var responsejson = await response.Content.ReadAsStringAsync();
        var tokens = JsonConvert.DeserializeObject<TokenResponse>(responsejson);

        if (tokens == null) return false;

        AccessToken = tokens.AccessToken;
        RefreshToken = tokens.RefreshToken;
        return true;

    }

    public class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = ""; 
    
    }

}
