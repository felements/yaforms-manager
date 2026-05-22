using Google.Apis.Auth.OAuth2;
using Google.Apis.Forms.v1;
using Google.Apis.Forms.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace YaForms.Api;

public sealed class GoogleFormsClient : IDisposable
{
    private readonly FormsService _service;

    private GoogleFormsClient(FormsService service) => _service = service;

    public static async Task<GoogleFormsClient> CreateAsync(string credentialsPath, CancellationToken ct = default)
    {
        var tokenDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "yaforms");

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            GoogleClientSecrets.FromFile(credentialsPath).Secrets,
            [FormsService.Scope.FormsBody],
            "user",
            ct,
            new FileDataStore(tokenDir, fullPath: true));

        var service = new FormsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "YaForms CLI",
        });

        return new GoogleFormsClient(service);
    }

    public async Task<string> CreateFormAsync(string title, CancellationToken ct = default)
    {
        var form = await _service.Forms
            .Create(new Form { Info = new Info { Title = title } })
            .ExecuteAsync(ct);
        return form.FormId;
    }

    public async Task BatchUpdateAsync(string formId, IList<Request> requests, CancellationToken ct = default)
    {
        await _service.Forms
            .BatchUpdate(new BatchUpdateFormRequest { Requests = requests }, formId)
            .ExecuteAsync(ct);
    }

    public void Dispose() => _service.Dispose();
}
