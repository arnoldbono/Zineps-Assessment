namespace CarrierIntegrationWebApp.Services;

public class BearerTokenService
{
    private string? _token;
    
    public string? Token
    {
        get => _token;
        set => _token = value;
    }
    
    public bool HasToken() => !string.IsNullOrEmpty(_token);
    
    public void RemoveToken()
    {
        _token = null;
    }
}
