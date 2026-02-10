namespace MarketList.Application.Interfaces;

public interface IPasswordService
{
    string HashSenha(string senha);
    bool VerificarSenha(string hash, string senha);
}
