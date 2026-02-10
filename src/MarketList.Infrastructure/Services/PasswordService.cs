using MarketList.Application.Interfaces;

namespace MarketList.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    public string HashSenha(string senha)
    {
        return BCrypt.Net.BCrypt.HashPassword(senha);
    }

    public bool VerificarSenha(string hash, string senha)
    {
        return BCrypt.Net.BCrypt.Verify(senha, hash);
    }
}
