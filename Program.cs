using MimeKit;
using MailKit.Net.Smtp;
using Supabase;
using Postgrest.Attributes;
using Postgrest.Models;

var builder = WebApplication.CreateBuilder(args);

// --- CONFIGURAÇÃO DO SUPABASE ---
var url = "https://vghtgwnlkdkcdsipfvew.supabase.co";
var key = "sb_publishable_8Gsb8EIxBukAq-xf6w5w3w_bFwATpUe";
var options = new SupabaseOptions { AutoConnectRealtime = true };
builder.Services.AddSingleton(new Supabase.Client(url, key, options));

builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// --- ROTA 1: INSCRIÇÃO (SALVA NO BANCO E ENVIA E-MAIL) ---
app.MapPost("/inscrever", async (Supabase.Client supabase, CandidatoRequest request) =>
{
    try
    {
        var novoCandidato = new Candidato 
        {
            Nome = request.Nome,
            Telefone = request.Telefone,
            Unidade = request.Unidade,
            Email = request.Email,
            FotoUrl = request.FotoUrl, // AJUSTE 1: Adicionado para receber a URL da foto do React
            Validado = false 
        };

        await supabase.From<Candidato>().Insert(novoCandidato);

        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress("MerendaChef", "lucasds151@gmail.com"));
        mensagem.To.Add(new MailboxAddress(novoCandidato.Nome, novoCandidato.Email));
        mensagem.Subject = "Inscrição Confirmada - MerendaChef";
        
        mensagem.Body = new TextPart("html") { 
            Text = $"<h1>Olá, {novoCandidato.Nome}!</h1><p>Sua inscrição para a unidade <b>{novoCandidato.Unidade}</b> foi recebida!</p>" 
        };

        using var client = new SmtpClient();
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        client.Connect("smtp.gmail.com", 587, false);
        client.Authenticate("lucasds151@gmail.com", "svaiojacveepmiis");
        client.Send(mensagem);
        client.Disconnect(true);

        return Results.Ok("Candidato salvo e e-mail enviado!");
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// --- ROTA 2: LISTAR CANDIDATOS ---
app.MapGet("/candidatos", async (Supabase.Client supabase) =>
{
    try
    {
        var resultado = await supabase.From<Candidato>().Get();
        
        var candidatosLimpos = resultado.Models.Select(c => new {
            id = c.Id,
            createdAt = c.CreatedAt,
            nome = c.Nome,
            telefone = c.Telefone,
            unidade = c.Unidade,
            email = c.Email,
            validado = c.Validado,
            fotoUrl = c.FotoUrl // AJUSTE 2: Adicionado para que a foto apareça na Dashboard
        });

        return Results.Ok(candidatosLimpos);
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// --- ROTA 3: VALIDAR CANDIDATO ---
app.MapPost("/validar", async (Supabase.Client supabase, ValidarRequest request) =>
{
    try
    {
        await supabase.From<Candidato>()
                      .Where(x => x.Email == request.Email)
                      .Set(x => x.Validado, request.Validado)
                      .Update();
                      
        return Results.Ok("Status atualizado com sucesso!");
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// --- ROTA 4: LOGIN DE USUÁRIOS ---
app.MapPost("/login", async (Supabase.Client supabase, LoginRequest request) =>
{
    try
    {
        // Busca no banco se existe alguém com esse e-mail E senha
        var resposta = await supabase.From<Usuario>()
                                     .Where(x => x.Email == request.Email && x.Senha == request.Senha)
                                     .Get();

        var usuario = resposta.Models.FirstOrDefault();

        if (usuario == null)
        {
            return Results.BadRequest("E-mail ou senha incorretos.");
        }

        // Se achou, devolve o perfil e o nome para o React
        return Results.Ok(new { 
            perfil = usuario.Perfil, 
            nome = usuario.Nome, 
            email = usuario.Email 
        });
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.Run();

// --- CLASSES DE APOIO ---

public class CandidatoRequest 
{
    public string Nome { get; set; }
    public string Telefone { get; set; }
    public string Unidade { get; set; }
    public string Email { get; set; }
    public string? FotoUrl { get; set; } // AJUSTE 3: Campo necessário para o JSON do React
}

public class ValidarRequest
{
    public string Email { get; set; }
    public bool Validado { get; set; }
}

[Table("candidatos")] 
public class Candidato : BaseModel
{
    [PrimaryKey("id", false)] 
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("Nome")]
    public string Nome { get; set; }

    [Column("Telefone")]
    public string Telefone { get; set; }

    [Column("Unidade")]
    public string Unidade { get; set; }

    [Column("Email")]
    public string Email { get; set; }

    [Column("Validado")]
    public bool Validado { get; set; }

    [Column("FotoUrl")]
    public string? FotoUrl { get; set; }
}

public class LoginRequest 
{
    public string Email { get; set; }
    public string Senha { get; set; }
}

[Table("usuarios")]
public class Usuario : BaseModel
{
    [PrimaryKey("id", false)] public int Id { get; set; }
    [Column("email")] public string Email { get; set; }
    [Column("senha")] public string Senha { get; set; }
    [Column("perfil")] public string Perfil { get; set; }
    [Column("nome")] public string Nome { get; set; }
}