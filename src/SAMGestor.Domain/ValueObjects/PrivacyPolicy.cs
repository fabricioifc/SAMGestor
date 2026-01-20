using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

/// <summary>
/// Value Object que encapsula a política de privacidade do retiro
/// </summary>
public sealed class PrivacyPolicy : ValueObject
{
    public string Title { get; private set; }
    
    /// <summary>
    /// Conteúdo completo da política (pode ser HTML ou Markdown)
    /// </summary>
    public string Body { get; private set; }
    
    /// <summary>
    /// Versão/identificador da política (ex: "v1.0", "2024-01-20")
    /// </summary>
    public string Version { get; private set; }
    
    public DateTime PublishedAt { get; private set; }

    private PrivacyPolicy() { }

    public PrivacyPolicy(string title, string body, string version, DateTime? publishedAt = null)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Versão da política é obrigatória.", nameof(version));
        
        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentException("Corpo da política é obrigatório.", nameof(body));

        Title = string.IsNullOrWhiteSpace(title) 
            ? "Política de Privacidade" 
            : title.Trim();
        Body = body.Trim();
        Version = version.Trim();
        PublishedAt = publishedAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Version;
        yield return Body;
    }
    
    public static PrivacyPolicy CreateDefault()
    {
        var version = $"v1.0-{DateTime.UtcNow:yyyy-MM-dd}";
        var title = "Política de Privacidade - LGPD";
        
        var body = @"
A Comunidade Servos Adoradores da Misericórdia - COMUNIDADE SAM respeita o direito à privacidade 
em conformidade com a LGPD — Lei Geral de Proteção de Dados (Lei n.º 13.709/2018).

1. Identificação e Definições Iniciais
Controladora: Comunidade Servos Adoradores da Misericórdia, CNPJ 08.220.941/0001-42
Email: contato@comunidadesam.org | Telefone: (49) 3565-0790
Responsável: Tiago Lopes Gonçalves

2. Coleta e Tratamento de Dados
Dados coletados conforme necessário para inscrições em eventos, formações e retiros.

3. Finalidade
- Formar e melhorar o banco de dados
- Identificar e contactar participantes
- Enviar comunicações e atualizações
- Processar inscrições e pagamentos

Para mais informações: contato@comunidadesam.org
        ";

        return new PrivacyPolicy(title, body, version);
    }
}
