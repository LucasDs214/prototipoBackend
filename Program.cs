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

// Configuração de CORS para permitir que o React converse com a API
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();

// --- ROTA 1: INSCRIÇÃO (SALVA NO BANCO E ENVIA E-MAIL) ---
app.MapPost("/inscrever", async (Supabase.Client supabase, CandidatoRequest request) =>
{
    try
    {
        // 1. Converter os dados simples do React para o Modelo do Supabase
        var novoCandidato = new Candidato 
        {
            Nome = request.Nome,
            Telefone = request.Telefone,
            Unidade = request.Unidade,
            Email = request.Email,
            Validado = false // Por padrão, entra como não validado
        };

        // 2. SALVAR NO BANCO (SUPABASE)
        await supabase.From<Candidato>().Insert(novoCandidato);

        // 3. DISPARAR E-MAIL DE CONFIRMAÇÃO
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

// --- ROTA 2: LISTAR CANDIDATOS (PARA A TELA DE GESTÃO) ---
app.MapGet("/candidatos", async (Supabase.Client supabase) =>
{
    try
    {
        var resultado = await supabase.From<Candidato>().Get();
        
        // Mapeia os dados do Supabase para um objeto simples que o conversor JSON entenda
        var candidatosLimpos = resultado.Models.Select(c => new {
            id = c.Id,
            createdAt = c.CreatedAt,
            nome = c.Nome,
            telefone = c.Telefone,
            unidade = c.Unidade,
            email = c.Email,
            validado = c.Validado
        });

        return Results.Ok(candidatosLimpos);
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

// --- ROTA 3: VALIDAR CANDIDATO (CHECKBOX NA DASHBOARD) ---
app.MapPost("/validar", async (Supabase.Client supabase, ValidarRequest request) =>
{
    try
    {
        // Usa o método Set() para atualizar apenas a coluna "Validado", sem precisar do objeto completo
        await supabase.From<Candidato>()
                      .Where(x => x.Email == request.Email)
                      .Set(x => x.Validado, request.Validado)
                      .Update();
                      
        return Results.Ok("Status atualizado com sucesso!");
    }
    catch (Exception ex) { return Results.Problem(ex.Message); }
});

app.Run();

// ======================================================================
// CLASSES DE APOIO
// ======================================================================

// --- DTOs: Classes simples para receber os dados do Frontend via JSON ---
public class CandidatoRequest 
{
    public string Nome { get; set; }
    public string Telefone { get; set; }
    public string Unidade { get; set; }
    public string Email { get; set; }
}

public class ValidarRequest
{
    public string Email { get; set; }
    public bool Validado { get; set; }
}

// --- MODELO DE DADOS: Mapeia estritamente a tabela do Supabase ---
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
}