namespace PrototipoBackend.DTOs;

public class LoginRequest 
{
    public string Cpf { get; set; }
    public string Senha { get; set; }
}

public class RegistroRequest 
{
    public string Nome { get; set; }
    public string Email { get; set; }
    public string Cpf { get; set; }
    public string Senha { get; set; }
    public string Perfil { get; set; }
}

public class CadastroCompletoRequest
{
    public string Nome { get; set; }
    public string Cpf { get; set; }
    public string Email { get; set; }
    public string Senha { get; set; }
    public string Telefone { get; set; }
    public string Unidade { get; set; }
    public string? FotoUrl { get; set; }
}

// AS DUAS CLASSES NOVAS QUE FALTAVAM:

public class EsqueciSenhaRequest 
{
    public string Cpf { get; set; }
}

public class TrocarSenhaRequest 
{
    public string Cpf { get; set; }
    public string NovaSenha { get; set; }
}