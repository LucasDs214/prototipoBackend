using PrototipoBackend.Models;
using PrototipoBackend.DTOs;
using System.Text.Json;
using System.Text;

namespace PrototipoBackend.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // --- 1. LOGIN ---
        app.MapPost("/login", async (Supabase.Client supabase, LoginRequest request) =>
        {
            try
            {
                var resposta = await supabase.From<Usuario>().Where(x => x.Cpf == request.Cpf).Get();
                var usuario = resposta.Models.FirstOrDefault();

                if (usuario == null || !BCrypt.Net.BCrypt.Verify(request.Senha, usuario.Senha))
                    return Results.BadRequest("CPF ou senha incorretos.");

                return Results.Ok(new { 
                    perfil = usuario.Perfil, 
                    nome = usuario.Nome, 
                    email = usuario.Email,
                    precisaTrocarSenha = usuario.SenhaTemporaria 
                });
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 2. NOVA ROTA: ESQUECI A MINHA SENHA (AGORA COM A API DO BREVO) ---
        app.MapPost("/esqueci-senha", async (Supabase.Client supabase, EsqueciSenhaRequest request) =>
        {
            try
            {
                var resposta = await supabase.From<Usuario>().Where(x => x.Cpf == request.Cpf).Get();
                var usuario = resposta.Models.FirstOrDefault();

                if (usuario == null) return Results.BadRequest("CPF não encontrado no sistema.");

                // Gera uma senha aleatória de 8 caracteres
                string senhaProvisoria = Guid.NewGuid().ToString().Substring(0, 8);

                // Atualiza a senha no banco e liga o booleano de senha temporária
                await supabase.From<Usuario>()
                              .Where(x => x.Cpf == request.Cpf)
                              .Set(x => x.Senha, BCrypt.Net.BCrypt.HashPassword(senhaProvisoria))
                              .Set(x => x.SenhaTemporaria, true)
                              .Update();

                // Puxa a chave do Brevo do Render
                var brevoApiKey = Environment.GetEnvironmentVariable("Brevo__ApiKey");
                
                if (string.IsNullOrEmpty(brevoApiKey)) 
                    return Results.Problem("A chave da API do Brevo não foi configurada no Render.");

                // --- NOVA LÓGICA DE ENVIO (BREVO API) ---
                using var httpClient = new HttpClient();
                
                // O Brevo usa um cabeçalho chamado 'api-key'
                httpClient.DefaultRequestHeaders.Add("api-key", brevoApiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Estrutura de dados exigida pelo Brevo
                var emailData = new
                {
                    sender = new { name = "MerendaChef", email = "lucasds151@gmail.com" }, // TEM DE SER O TEU EMAIL REGISTADO NO BREVO
                    to = new[] { new { email = usuario.Email, name = usuario.Nome } }, // AGORA PODE SER QUALQUER EMAIL DO MUNDO!
                    subject = "Recuperação de Senha - MerendaChef",
                    htmlContent = $"<h2>Olá, {usuario.Nome}</h2><p>A sua senha provisória é: <b>{senhaProvisoria}</b></p><p>Ao iniciar sessão, deverá registar uma nova senha.</p>"
                };

                var content = new StringContent(JsonSerializer.Serialize(emailData), Encoding.UTF8, "application/json");

                // Faz a chamada à API do Brevo
                var brevoResponse = await httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

                if (!brevoResponse.IsSuccessStatusCode)
                {
                    var erroBrevo = await brevoResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erro Brevo: {erroBrevo}");
                    return Results.Problem("Ocorreu um erro ao enviar o e-mail pela API do Brevo.");
                }

                return Results.Ok("Uma senha provisória foi enviada para o seu e-mail registado.");
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Erro crítico: {ex.Message}");
                return Results.Problem($"Erro interno: {ex.Message}"); 
            }
        });

        // --- 3. ROTA: ALTERAR SENHA PROVISÓRIA ---
        app.MapPost("/trocar-senha", async (Supabase.Client supabase, TrocarSenhaRequest request) =>
        {
            try
            {
                await supabase.From<Usuario>()
                              .Where(x => x.Cpf == request.Cpf)
                              .Set(x => x.Senha, BCrypt.Net.BCrypt.HashPassword(request.NovaSenha))
                              .Set(x => x.SenhaTemporaria, false)
                              .Update();

                return Results.Ok("Senha alterada com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 4. REGISTAR UTILIZADOR COMUM ---
        app.MapPost("/registrar-usuario", async (Supabase.Client supabase, RegistroRequest request) =>
        {
            try
            {
                var novoUsuario = new Usuario { 
                    Nome = request.Nome,
                    Email = request.Email,
                    Cpf = request.Cpf,
                    Perfil = request.Perfil, 
                    Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha) 
                };
                await supabase.From<Usuario>().Insert(novoUsuario);
                return Results.Ok("Utilizador registado com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });

        // --- 5. CADASTRAR NOVO CANDIDATO ---
        app.MapPost("/cadastrar-candidato", async (Supabase.Client supabase, CadastroCompletoRequest request) =>
        {
            try
            {
                var novoUsuario = new Usuario { 
                    Nome = request.Nome, 
                    Email = request.Email,
                    Cpf = request.Cpf, 
                    Perfil = "candidato", 
                    Senha = BCrypt.Net.BCrypt.HashPassword(request.Senha) 
                };
                await supabase.From<Usuario>().Insert(novoUsuario);

                var novoCandidato = new Candidato { 
                    Nome = request.Nome, 
                    Cpf = request.Cpf,
                    Telefone = request.Telefone, 
                    Unidade = request.Unidade, 
                    Email = request.Email, 
                    FotoUrl = request.FotoUrl, 
                    Validado = false 
                };
                await supabase.From<Candidato>().Insert(novoCandidato);

                return Results.Ok("Conta criada e inscrição realizada com sucesso!");
            }
            catch (Exception ex) { return Results.Problem(ex.Message); }
        });
    }
}