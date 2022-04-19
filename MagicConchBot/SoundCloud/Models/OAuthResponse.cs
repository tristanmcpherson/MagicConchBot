namespace MagicConchBot.Services
{
    public record OAuthResponse(
        string access_token,
        int expires_in,
        string refresh_token,
        string scope,
        string token_type
    );
}