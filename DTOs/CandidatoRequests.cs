namespace PrototipoBackend.DTOs;

public class CandidatoRequest 
{
    public string Nome { get; set; }
    public string Telefone { get; set; }
    public string Unidade { get; set; }
    public string Email { get; set; }
    public string? FotoUrl { get; set; } 
}

public class ValidarRequest
{
    public string Email { get; set; }
    public bool Validado { get; set; }
}