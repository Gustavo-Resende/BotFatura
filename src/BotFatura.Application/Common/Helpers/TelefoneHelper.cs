namespace BotFatura.Application.Common.Helpers;

/// <summary>
/// Helper para manipulação de números de telefone e JIDs do WhatsApp.
/// </summary>
public static class TelefoneHelper
{
    /// <summary>
    /// Mascara número de telefone ou JID para exibição em logs.
    /// Exemplo: 5511999998888 -> 55119****8888
    /// </summary>
    /// <param name="numero">Número ou JID a ser mascarado</param>
    /// <returns>Número mascarado para proteção de dados sensíveis</returns>
    public static string MascararNumero(string numero)
    {
        if (string.IsNullOrWhiteSpace(numero) || numero.Length < 8)
            return "****";

        return numero[..4] + new string('*', numero.Length - 8) + numero[^4..];
    }

    /// <summary>
    /// Extrai a parte numérica de um JID do WhatsApp.
    /// Exemplo: "5511999998888@s.whatsapp.net" -> "5511999998888"
    /// </summary>
    /// <param name="jid">JID completo do WhatsApp</param>
    /// <returns>Apenas a parte numérica do JID</returns>
    public static string ExtrairNumeroDoJid(string jid)
    {
        if (string.IsNullOrWhiteSpace(jid))
            return string.Empty;

        return jid.Split('@')[0];
    }

    /// <summary>
    /// Verifica se o JID é de um grupo do WhatsApp.
    /// </summary>
    public static bool EhJidDeGrupo(string jid)
    {
        return !string.IsNullOrWhiteSpace(jid) && jid.Contains("@g.us");
    }

    /// <summary>
    /// Verifica se o JID é do tipo LID (linked device).
    /// </summary>
    public static bool EhJidLid(string jid)
    {
        return !string.IsNullOrWhiteSpace(jid) && jid.Contains("@lid");
    }
}
