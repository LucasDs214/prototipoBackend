using PrototipoBackend.Models;
using PrototipoBackend.DTOs;
using System.Text.Json; // Adicionado para a API do Resend
using System.Text;      // Adicionado para a API do Resend

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

        // --- 2. NOVA ROTA: ESQUECI A MINHA SENHA (COM RESEND API) ---
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

                // Puxa a chave do Resend do Render
                var resendApiKey = Environment.GetEnvironmentVariable("Resend__ApiKey");
                
                if (string.IsNullOrEmpty(resendApiKey)) 
                    return Results.Problem("A chave da API do Resend não foi configurada no Render.");

                // --- NOVA LÓGICA DE ENVIO (HTTP POST NA PORTA 443) ---
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendApiKey}");

                var emailData = new
                {
                    from = "MerendaChef <onboarding@resend.dev>", // Obrigatório no plano grátis
                    to = new[] { usuario.Email }, // TEM de ser o teu email cadastrado no Resend para funcionar
                    subject = "Recuperação de Senha - MerendaChef",
                    html = $"<h2>Olá, {usuario.Nome}</h2><p>A sua senha provisória é: <b>{senhaProvisoria}</b></p><p>Ao iniciar sessão, deverá registar uma nova senha.</p>"
                };

                var content = new StringContent(JsonSerializer.Serialize(emailData), Encoding.UTF8, "application/json");

                // Faz a chamada à API do Resend (Nunca é bloqueada pelo Render)
                var resendResponse = await httpClient.PostAsync("https://api.resend.com/emails", content);

                if (!resendResponse.IsSuccessStatusCode)
                {
                    var erroResend = await resendResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"Erro Resend: {erroResend}");
                    return Results.Problem("Ocorreu um erro ao enviar o e-mail pela API do Resend.");
                }

                return Results.Ok("Uma senha provisória foi enviada para o seu e-mail.");
            }
            catch (Exception ex) 
            { 
                Console.WriteLine($"Erro crítico: {ex.Message}");
                return Results.Problem($"Erro interno: {ex.Message}"); 
            }
        });

        // --- 3. NOVA ROTA: ALTERAR SENHA PROVISÓRIA ---
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

        // --- 4. REGISTAR UTILIZADOR COMUM (ADMIN/PROFESSORES) ---
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