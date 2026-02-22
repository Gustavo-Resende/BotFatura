using Ardalis.Specification;
using BotFatura.Domain.Entities;

namespace BotFatura.Application.Clientes.Queries.ObterClientePorWhatsApp;

public sealed class ClientePorWhatsAppSpec : Specification<Cliente>
{
    public ClientePorWhatsAppSpec(string whatsApp)
    {
        Query.Where(c => c.WhatsApp == whatsApp);
    }
}
