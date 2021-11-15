using Microsoft.Playwright;
using System.Text.Json;

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync();
var page = await browser.NewPageAsync();

//Escutando as respostas e logando-as
var responses = new Dictionary<string, string>();
page.Response += (_, response) => WriteResponse(response);
//Abrindo a página do fundo
await page.GotoAsync("https://meuportfol.io/analise-de-ativos/fundos/capitania-premium-mst-fi-rf-cred-priv-lp");
//Esperando o título do fundo até prosseguir
await page.WaitForSelectorAsync("text=CAPITANIA PREMIUM MASTER FI RF CP LP");
//Tirando um screenshot e salvando-o
await page.ScreenshotAsync(new PageScreenshotOptions { Path = "screenshot.png" });
//Extraindo o html e salvando-o
var pageContent = await page.ContentAsync();
var fileName = Path.Combine(Environment.CurrentDirectory, "pageContent.html");
await File.WriteAllTextAsync(fileName, pageContent);

WriteResponsesToFile();

async void WriteResponse(IResponse response)
{
    var request = response.Request;
    //Não queremos nada que não seja get e xhr
    if (!"GET".Equals(request.Method)
        || !"xhr".Equals(request.ResourceType)) return;

    var url = response.Url;
    //Logando em tela todos os status e urls
    Console.WriteLine("<< " + response.Status + " " + url);
    //Se for uma API e status 200 então guarda sua url e retorno em um dicionário
    if (response.Status == 200 
        && url.Contains("https://api"))
    {
        var body = await response.TextAsync();        
        if (!responses.ContainsKey(url)) responses.Add(url, body);   
    }
}

async void WriteResponsesToFile()
{
    if (responses.Count == 0)
    {
        Console.WriteLine("Sem responses para persistir no arquivo");
        return;
    }
    var serializeOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    //por algum motivo o primeiro item pode vir nulo
    var filteredResponses = responses.Where(x => x.Key is not null).ToDictionary(x => x.Key, x => x.Value);
    var json = JsonSerializer.Serialize(filteredResponses, serializeOptions);
    var fileName = Path.Combine(Environment.CurrentDirectory, "responses.json");
    await File.WriteAllTextAsync(fileName, json);
}