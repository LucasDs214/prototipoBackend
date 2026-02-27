namespace PrototipoBackend.Config;

public static class SupabaseConfig
{
    public static void AddSupabaseConfig(this IServiceCollection services)
    {
        var url = "https://vghtgwnlkdkcdsipfvew.supabase.co";
        var key = "sb_publishable_8Gsb8EIxBukAq-xf6w5w3w_bFwATpUe";
        var options = new Supabase.SupabaseOptions { AutoConnectRealtime = true };
        
        services.AddSingleton(new Supabase.Client(url, key, options));
    }
}